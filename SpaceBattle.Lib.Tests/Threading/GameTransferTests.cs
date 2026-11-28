using System.Collections.Concurrent;

namespace SpaceBattle.Lib.Tests;

public class GameTransferTests
{
    private const string GameId = "ship";
    private static readonly TimeSpan Quantum = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void Proxy_RetriesTheFirstCommandWhenTheDestinationQueueIsFull()
    {
        using var destination = new BlockingCollection<GameCommandMessage>(1);
        var occupyingMessage = new GameCommandMessage("other", new EmptyCommand());
        destination.Add(occupyingMessage);
        var proxy = new GameProxy(GameId, message => destination.TryAdd(message));
        var first = new EmptyCommand();
        var second = new EmptyCommand();
        proxy.Receive(first);
        proxy.Receive(second);

        proxy.Execute();
        Assert.True(proxy.HasWork);
        Assert.Same(occupyingMessage, Assert.Single(destination));
        Assert.True(destination.TryTake(out _));

        proxy.Execute();
        proxy.Execute();
        Assert.True(proxy.HasWork);
        Assert.True(destination.TryTake(out var forwarded));
        Assert.Equal(new GameCommandMessage(GameId, first), forwarded);

        proxy.Execute();
        Assert.False(proxy.HasWork);
        Assert.Equal(new GameCommandMessage(GameId, second), Assert.Single(destination));
    }

    [Fact]
    public async Task Transfer_MovesQueuedCommandsAndKeepsTheOldAddressWhileAQuantumIsRunning()
    {
        using var sourceInbox = new BlockingCollection<ICommand>(8);
        using var targetInbox = new BlockingCollection<ICommand>(8);
        using var releaseCurrentCommand = new ManualResetEventSlim();
        using var stop = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var changes = new ConcurrentQueue<(int State, int Thread)>();
        var clock = new TestClock();
        var game = new ScheduledGame(Quantum, clock, command => command, UnexpectedError);
        var source = Worker(sourceInbox, game);
        var target = Worker(targetInbox);
        var router = new GameCommandRouter();
        router.SetRoute(GameId, source);
        int state = 0;

        void Change(int digit)
        {
            state = state * 10 + digit;
            changes.Enqueue((state, Environment.CurrentManagedThreadId));
            clock.AdvanceQuantum();
        }

        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() =>
        {
            entered.SetResult();
            if (!releaseCurrentCommand.Wait(Timeout))
            {
                throw new TimeoutException("The first command was not released.");
            }

            Change(1);
        })));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => Change(2))));

        var sourceLoop = StartWorker(source, stop.Token);
        var targetLoop = StartWorker(target, stop.Token);
        try
        {
            await entered.Task.WaitAsync(Timeout);
            var moved = router.TryTransferAsync(GameId, target);
            Assert.False(moved.IsCompleted);
            Assert.True(source.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() =>
            {
                Change(3);
                finished.SetResult();
            }))));

            releaseCurrentCommand.Set();
            Assert.True(await moved.WaitAsync(Timeout));
            await finished.Task.WaitAsync(Timeout);
        }
        finally
        {
            releaseCurrentCommand.Set();
            stop.Cancel();
            await Task.WhenAll(sourceLoop, targetLoop).WaitAsync(Timeout);
        }

        var observed = changes.ToArray();
        Assert.Equal(new[] { 1, 12, 123 }, observed.Select(item => item.State));
        Assert.NotEqual(observed[0].Thread, observed[1].Thread);
        Assert.Equal(observed[1].Thread, observed[2].Thread);
    }

    [Fact]
    public async Task Transfer_UpdatesTheRouteAndKeepsAProxyThatCanWaitForSpace()
    {
        using var sourceInbox = new BlockingCollection<ICommand>(2);
        using var targetInbox = new BlockingCollection<ICommand>(1);
        var source = Worker(sourceInbox, NewGame());
        var target = Worker(targetInbox);
        var router = new GameCommandRouter();
        var executed = new List<string>();
        router.SetRoute(GameId, source);

        var moved = router.TryTransferAsync(GameId, target);
        source.RunIteration();
        target.RunIteration();
        Assert.True(await moved.WaitAsync(Timeout));

        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => executed.Add("new route"))));
        Assert.True(source.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() => executed.Add("old route")))));
        source.RunIteration();
        Assert.Empty(executed);
        Assert.Single(targetInbox);

        target.RunIteration();
        Assert.Equal(new[] { "new route" }, executed);
        source.RunIteration();
        target.RunIteration();
        Assert.Equal(new[] { "new route", "old route" }, executed);
        Assert.Empty(sourceInbox);
        Assert.Empty(targetInbox);
    }

    [Fact]
    public async Task Transfer_LeavesTheSourceActiveIfTheTargetQueueIsFullAndCanBeRetried()
    {
        using var sourceInbox = new BlockingCollection<ICommand>(4);
        using var targetInbox = new BlockingCollection<ICommand>(1);
        var source = Worker(sourceInbox, NewGame());
        var target = Worker(targetInbox);
        var router = new GameCommandRouter();
        router.SetRoute(GameId, source);
        var executed = new List<int>();
        targetInbox.Add(new EmptyCommand());

        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => executed.Add(1))));
        var firstAttempt = router.TryTransferAsync(GameId, target);
        source.RunIteration();

        Assert.False(await firstAttempt.WaitAsync(Timeout));
        Assert.Equal(new[] { 1 }, executed);
        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => executed.Add(2))));
        source.RunIteration();
        Assert.Equal(new[] { 1, 2 }, executed);

        target.RunIteration();
        var retried = router.TryTransferAsync(GameId, target);
        source.RunIteration();
        target.RunIteration();
        Assert.True(await retried.WaitAsync(Timeout));

        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => executed.Add(3))));
        target.RunIteration();
        Assert.Equal(new[] { 1, 2, 3 }, executed);
    }

    [Fact]
    public async Task Transfer_CanReturnTheGameToAWorkerWithPendingProxyCommands()
    {
        using var firstInbox = new BlockingCollection<ICommand>(4);
        using var secondInbox = new BlockingCollection<ICommand>(1);
        var first = Worker(firstInbox, NewGame());
        var second = Worker(secondInbox);
        var router = new GameCommandRouter();
        router.SetRoute(GameId, first);
        var executed = new List<int>();

        var moved = router.TryTransferAsync(GameId, second);
        first.RunIteration();
        second.RunIteration();
        Assert.True(await moved.WaitAsync(Timeout));

        secondInbox.Add(new EmptyCommand());
        Assert.True(first.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() => executed.Add(1)))));
        first.RunIteration();
        Assert.Empty(executed);

        second.RunIteration();
        var returned = router.TryTransferAsync(GameId, first);
        second.RunIteration();
        first.RunIteration();
        Assert.True(await returned.WaitAsync(Timeout));
        Assert.Equal(new[] { 1 }, executed);

        Assert.True(second.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() => executed.Add(2)))));
        second.RunIteration();
        first.RunIteration();
        Assert.Equal(new[] { 1, 2 }, executed);
    }

    [Fact]
    public async Task Proxies_ForwardInBothDirectionsFromFullWorkerInboxes()
    {
        const string otherGameId = "other";
        using var firstInbox = new BlockingCollection<ICommand>(1);
        using var secondInbox = new BlockingCollection<ICommand>(1);
        var first = Worker(firstInbox, NewGame());
        var second = new GameWorker(secondInbox,
            new Dictionary<string, ScheduledGame> { [otherGameId] = NewGame() }, UnexpectedError);
        var router = new GameCommandRouter();
        router.SetRoute(GameId, first);
        router.SetRoute(otherGameId, second);
        var executed = new List<string>();

        var firstMove = router.TryTransferAsync(GameId, second);
        first.RunIteration();
        second.RunIteration();
        Assert.True(await firstMove.WaitAsync(Timeout));
        var secondMove = router.TryTransferAsync(otherGameId, first);
        second.RunIteration();
        first.RunIteration();
        Assert.True(await secondMove.WaitAsync(Timeout));

        Assert.True(first.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() => executed.Add(GameId)))));
        Assert.True(second.TryEnqueue(new GameCommandMessage(otherGameId, new TestCommand(() => executed.Add(otherGameId)))));
        first.RunIteration();
        Assert.Empty(executed);

        second.RunIteration();
        first.RunIteration();
        second.RunIteration();
        Assert.Equal(new[] { otherGameId, GameId }, executed);
        Assert.Empty(firstInbox);
        Assert.Empty(secondInbox);
    }

    [Fact]
    public async Task Transfer_RejectsAnIdentifierThatAlreadyBelongsToAnotherGame()
    {
        using var sourceInbox = new BlockingCollection<ICommand>(4);
        using var targetInbox = new BlockingCollection<ICommand>(4);
        var source = Worker(sourceInbox, NewGame());
        var target = Worker(targetInbox, NewGame());
        var router = new GameCommandRouter();
        router.SetRoute(GameId, source);
        var executed = new List<string>();

        var moved = router.TryTransferAsync(GameId, target);
        source.RunIteration();
        Assert.False(await moved.WaitAsync(Timeout));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route(GameId, new TestCommand(() => executed.Add("source"))));
        Assert.True(target.TryEnqueue(new GameCommandMessage(GameId, new TestCommand(() => executed.Add("target")))));
        source.RunIteration();
        target.RunIteration();
        Assert.Equal(new[] { "source", "target" }, executed);
    }

    private static GameWorker Worker(BlockingCollection<ICommand> inbox, ScheduledGame? game = null)
    {
        var games = new Dictionary<string, ScheduledGame>();
        if (game is not null)
        {
            games.Add(GameId, game);
        }

        return new GameWorker(inbox, games, UnexpectedError);
    }

    private static Task StartWorker(GameWorker worker, CancellationToken stop) =>
        Task.Factory.StartNew(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                worker.RunIteration();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private static ScheduledGame NewGame() => new(Quantum, new TestClock(), command => command, UnexpectedError);

    private static ICommand UnexpectedError(ICommand command, Exception error) =>
        throw new InvalidOperationException($"Unexpected failure in {command.GetType().Name}.", error);

    private sealed class TestCommand(Action execute) : ICommand
    {
        public void Execute() => execute();
    }

    private sealed class TestClock : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;
        public void AdvanceQuantum() => _timestamp += Quantum.Ticks;
    }
}
