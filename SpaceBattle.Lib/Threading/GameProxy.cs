namespace SpaceBattle.Lib;

/// <summary>Forwards commands received at a game's previous address.</summary>
/// <remarks>The owning worker adds commands and executes forwarding attempts.</remarks>
public sealed class GameProxy : IScheduledGame, ICommandReceiver
{
    private readonly string _gameId;
    private readonly Func<GameCommandMessage, bool> _tryForward;
    private readonly Queue<ICommand> _pendingCommands = new();

    public GameProxy(string gameId, Func<GameCommandMessage, bool> tryForward)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);

        _gameId = gameId;
        _tryForward = tryForward ?? throw new ArgumentNullException(nameof(tryForward));
    }

    public bool HasWork => _pendingCommands.Count > 0;

    internal int PendingCount => _pendingCommands.Count;

    public void Receive(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _pendingCommands.Enqueue(command);
    }

    public void Execute()
    {
        if (_pendingCommands.TryPeek(out var command) && _tryForward(new GameCommandMessage(_gameId, command)))
        {
            _pendingCommands.Dequeue();
        }
    }

    internal void DeliverPendingTo(ICommandReceiver receiver)
    {
        while (_pendingCommands.TryDequeue(out var command))
        {
            receiver.Receive(command);
        }
    }
}
