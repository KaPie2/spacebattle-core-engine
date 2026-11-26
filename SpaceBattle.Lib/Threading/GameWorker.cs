using System.Collections.Concurrent;

namespace SpaceBattle.Lib;

/// <summary>Moves incoming commands to local games and runs at most one game quantum per iteration.</summary>
/// <remarks>Use the host thread's inbox. Only that thread runs the iteration and accesses the games.</remarks>
public sealed class GameWorker : IWorkerInbox
{
    private const int IdleWaitMilliseconds = 50;

    private readonly BlockingCollection<ICommand> _inbox;
    private readonly Dictionary<string, ScheduledGame> _games;
    private readonly Func<ICommand, Exception, ICommand> _exceptionHandler;
    private readonly int _incomingBatchSize;
    private readonly GameScheduler _scheduler = new();

    public GameWorker(
        BlockingCollection<ICommand> inbox,
        IReadOnlyDictionary<string, ScheduledGame> games,
        Func<ICommand, Exception, ICommand> exceptionHandler,
        int incomingBatchSize = 32)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(incomingBatchSize);

        _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        _games = new Dictionary<string, ScheduledGame>(games, StringComparer.Ordinal);
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _incomingBatchSize = incomingBatchSize;

        foreach (var game in _games.Values)
        {
            _scheduler.NotifyReady(game);
        }
    }

    public bool TryEnqueue(GameCommandMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.GameId);
        ArgumentNullException.ThrowIfNull(message.Command);

        if (!_games.ContainsKey(message.GameId))
        {
            throw new InvalidOperationException($"Game '{message.GameId}' is not registered on this worker.");
        }

        return _inbox.TryAdd(new DeliverCommand(this, message));
    }

    public void RunIteration()
    {
        for (int received = 0; received < _incomingBatchSize && _inbox.TryTake(out var command); received++)
        {
            CommandExecution.Execute(command, _exceptionHandler);
        }

        if (!_scheduler.TryRunNext() && _inbox.TryTake(out var nextCommand, IdleWaitMilliseconds))
        {
            CommandExecution.Execute(nextCommand, _exceptionHandler);
        }
    }

    private sealed class DeliverCommand(GameWorker worker, GameCommandMessage message) : ICommand
    {
        public void Execute()
        {
            var game = worker._games[message.GameId];
            game.Receive(message.Command);
            worker._scheduler.NotifyReady(game);
        }
    }
}
