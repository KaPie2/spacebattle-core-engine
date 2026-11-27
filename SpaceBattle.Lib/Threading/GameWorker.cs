using System.Collections.Concurrent;

namespace SpaceBattle.Lib;

/// <summary>Moves incoming commands to local games and runs at most one game quantum per iteration.</summary>
/// <remarks>Use the host thread's inbox. Assign each game to one worker. Only its owner executes it.</remarks>
public sealed class GameWorker : IWorkerInbox
{
    private const int IdleWaitMilliseconds = 50;

    private readonly BlockingCollection<ICommand> _inbox;
    private readonly ConcurrentDictionary<string, GameSlot> _games = new(StringComparer.Ordinal);
    private readonly Func<ICommand, Exception, ICommand> _exceptionHandler;
    private readonly int _incomingBatchSize;
    private readonly GameScheduler _scheduler = new();
    private readonly Queue<GameSlot> _forwarding = new();

    public GameWorker(
        BlockingCollection<ICommand> inbox,
        IReadOnlyDictionary<string, ScheduledGame> games,
        Func<ICommand, Exception, ICommand> exceptionHandler,
        int incomingBatchSize = 32)
    {
        ArgumentNullException.ThrowIfNull(games);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(incomingBatchSize);

        _inbox = inbox ?? throw new ArgumentNullException(nameof(inbox));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _incomingBatchSize = incomingBatchSize;

        foreach (var (gameId, game) in games)
        {
            ArgumentNullException.ThrowIfNull(game);
            var slot = new GameSlot(this, new GameLocation(gameId, this), game);
            _games[gameId] = slot;
            _scheduler.NotifyReady(slot);
        }
    }

    public bool TryEnqueue(GameCommandMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.GameId);
        ArgumentNullException.ThrowIfNull(message.Command);

        if (!_games.TryGetValue(message.GameId, out var slot))
        {
            throw new InvalidOperationException($"Game '{message.GameId}' is not registered on this worker.");
        }

        return slot.AcceptsMessages && TryPost(new WorkerCommand(() => Deliver(slot, message.Command)));
    }

    public void RunIteration()
    {
        for (int received = 0; received < _incomingBatchSize && _inbox.TryTake(out var command); received++)
        {
            CommandExecution.Execute(command, _exceptionHandler);
        }

        bool forwarded = TryForwardNext();
        if (!_scheduler.TryRunNext() && !forwarded && _inbox.TryTake(out var nextCommand, IdleWaitMilliseconds))
        {
            CommandExecution.Execute(nextCommand, _exceptionHandler);
        }
    }

    internal Task<bool> TryTransferAsync(string gameId, GameWorker destination, Action updateRoute)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transfer = new WorkerCommand(() => Transfer(gameId, destination, updateRoute, completion));
        if (!TryPost(transfer))
        {
            completion.SetResult(false);
        }

        return completion.Task;
    }

    private bool TryPost(ICommand command)
    {
        try
        {
            return _inbox.TryAdd(command);
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
        catch (InvalidOperationException) when (_inbox.IsAddingCompleted)
        {
            // A completed inbox refuses messages, including when it closes during TryAdd.
            return false;
        }
    }

    private void Deliver(GameSlot slot, ICommand command)
    {
        if (slot.Game is { } game)
        {
            game.Receive(command);
            _scheduler.NotifyReady(slot);
            return;
        }

        slot.Proxy.Receive(command);
        ScheduleForwarding(slot);
    }

    private void ScheduleForwarding(GameSlot slot)
    {
        if (slot.Proxy.HasWork && !slot.ForwardingScheduled)
        {
            slot.ForwardingScheduled = true;
            _forwarding.Enqueue(slot);
        }
    }

    private bool TryForwardNext()
    {
        if (!_forwarding.TryDequeue(out var slot))
        {
            return false;
        }

        slot.ForwardingScheduled = false;
        int pending = slot.Proxy.PendingCount;
        CommandExecution.Execute(slot.Proxy, _exceptionHandler);
        ScheduleForwarding(slot);
        return slot.Proxy.PendingCount < pending;
    }

    private void Transfer(string gameId, GameWorker destination, Action updateRoute, TaskCompletionSource<bool> completion)
    {
        if (!_games.TryGetValue(gameId, out var source) || source.Game is not { } game)
        {
            completion.SetResult(false);
            return;
        }

        // This command runs between quanta. Unbind the old scheduler slot before publishing the game.
        source.Game = null;
        bool published = false;
        try
        {
            published = destination.TryAcceptTransfer(source.Location, game, updateRoute, completion);
        }
        finally
        {
            if (!published)
            {
                source.Game = game;
                completion.SetResult(false);
            }
        }
    }

    private bool TryAcceptTransfer(
        GameLocation location, ScheduledGame game, Action updateRoute, TaskCompletionSource<bool> completion)
    {
        var reservation = new GameSlot(this, location);
        var slot = _games.GetOrAdd(location.GameId, reservation);
        if (!ReferenceEquals(slot.Location, location))
        {
            return false;
        }

        var attach = new WorkerCommand(() =>
        {
            slot.Proxy.DeliverPendingTo(game);
            slot.Game = game;
            _scheduler.NotifyReady(slot);
            slot.AcceptsMessages = true;
            location.Owner = this;
            updateRoute();
            completion.SetResult(true);
        });

        bool posted = false;
        try
        {
            posted = TryPost(attach);
            return posted;
        }
        finally
        {
            if (!posted && ReferenceEquals(slot, reservation))
            {
                _games.TryRemove(location.GameId, out _);
            }
        }
    }

    private sealed class GameLocation(string gameId, GameWorker owner)
    {
        public string GameId { get; } = gameId;
        public volatile GameWorker Owner = owner;
    }

    private sealed class GameSlot(GameWorker worker, GameLocation location, ScheduledGame? game = null) : IScheduledGame
    {
        public GameLocation Location { get; } = location;
        public ScheduledGame? Game { get; set; } = game;
        public GameProxy Proxy { get; } = new(location.GameId, message =>
        {
            var owner = location.Owner;
            return !ReferenceEquals(worker, owner) && owner.TryEnqueue(message);
        });

        public volatile bool AcceptsMessages = game is not null;
        public bool ForwardingScheduled { get; set; }
        public bool HasWork => Game?.HasWork == true;

        public void Execute() => Game?.Execute();
    }

    private sealed class WorkerCommand(Action execute) : ICommand
    {
        public void Execute() => execute();
    }
}
