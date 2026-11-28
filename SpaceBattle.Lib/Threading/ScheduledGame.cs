namespace SpaceBattle.Lib;

/// <summary>Runs a queue of game commands in time slices on its owning worker.</summary>
/// <remarks>The quantum ends between commands. Once attached, only the owning worker accesses the game.</remarks>
public sealed class ScheduledGame : IScheduledGame, ICommandReceiver
{
    private readonly Queue<ICommand> _commands = new();
    private readonly TimeSpan _quantum;
    private readonly TimeProvider _clock;
    private readonly Func<ICommand, Exception, ICommand> _exceptionHandler;
    private readonly ICommand _scopedQuantum;

    public ScheduledGame(
        TimeSpan quantum,
        TimeProvider clock,
        Func<ICommand, ICommand> inGameScope,
        Func<ICommand, Exception, ICommand> exceptionHandler)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(quantum, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(inGameScope);

        _quantum = quantum;
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _scopedQuantum = inGameScope(new QuantumCommand(this))
            ?? throw new ArgumentException("The game scope must supply an executable command.", nameof(inGameScope));
    }

    public bool HasWork => _commands.Count > 0;

    public void Receive(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands.Enqueue(command);
    }

    public void Execute()
    {
        if (HasWork)
        {
            _scopedQuantum.Execute();
        }
    }

    private void ExecuteQuantum()
    {
        long startedAt = _clock.GetTimestamp();
        while (_commands.TryDequeue(out var command))
        {
            CommandExecution.Execute(command, _exceptionHandler);
            if (_clock.GetElapsedTime(startedAt) >= _quantum)
            {
                break;
            }
        }
    }

    private sealed class QuantumCommand(ScheduledGame game) : ICommand
    {
        public void Execute() => game.ExecuteQuantum();
    }
}
