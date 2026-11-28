using System.Collections.Concurrent;

namespace SpaceBattle.Lib.Tests;

public class GameWorkerTests
{
    private static readonly TimeSpan Quantum = TimeSpan.FromMilliseconds(10);

    [Fact]
    public void ScheduledGame_StopsAtTheCommandBoundaryWhenTimeRunsOut()
    {
        var clock = new ManualClock();
        var game = NewGame(clock);
        var executed = new List<int>();
        game.Receive(new TestCommand(() =>
        {
            executed.Add(1);
            clock.Advance(TimeSpan.FromMilliseconds(6));
        }));
        game.Receive(new TestCommand(() =>
        {
            executed.Add(2);
            clock.Advance(TimeSpan.FromMilliseconds(4));
        }));
        game.Receive(new TestCommand(() => executed.Add(3)));

        game.Execute();

        Assert.Equal(new[] { 1, 2 }, executed);
        Assert.True(game.HasWork);

        game.Execute();

        Assert.Equal(new[] { 1, 2, 3 }, executed);
        Assert.False(game.HasWork);
    }

    [Fact]
    public void RunIteration_LimitsIntakeAndRunsOneQuantumOfAlreadyQueuedWork()
    {
        using var inbox = new BlockingCollection<ICommand>(3);
        var clock = new ManualClock();
        var game = NewGame(clock);
        var executed = new List<string>();
        game.Receive(new TestCommand(() =>
        {
            executed.Add("first game command");
            clock.Advance(Quantum);
        }));
        game.Receive(new TestCommand(() => executed.Add("second game command")));
        var worker = new GameWorker(inbox, new Dictionary<string, ScheduledGame> { ["game"] = game },
            UnexpectedError, incomingBatchSize: 2);
        inbox.Add(new TestCommand(() => executed.Add("inbox 1")));
        inbox.Add(new TestCommand(() => executed.Add("inbox 2")));
        inbox.Add(new TestCommand(() => executed.Add("inbox 3")));

        worker.RunIteration();

        Assert.Equal(new[] { "inbox 1", "inbox 2", "first game command" }, executed);
        Assert.Single(inbox);
        Assert.True(game.HasWork);

        worker.RunIteration();

        Assert.Equal(new[] { "inbox 1", "inbox 2", "first game command", "inbox 3", "second game command" }, executed);
        Assert.False(game.HasWork);
    }

    [Fact]
    public void Route_ReportsAFullWorkerInboxAndAcceptsAgainAfterItDrains()
    {
        using var inbox = new BlockingCollection<ICommand>(1);
        var game = NewGame(new ManualClock());
        var worker = new GameWorker(inbox, new Dictionary<string, ScheduledGame> { ["game"] = game }, UnexpectedError);
        var router = new GameCommandRouter();
        router.SetRoute("game", worker);
        var executed = new List<int>();
        var first = new TestCommand(() => executed.Add(1));
        var second = new TestCommand(() => executed.Add(2));

        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", first));
        Assert.Equal(CommandRoutingResult.QueueFull, router.Route("game", second));
        worker.RunIteration();

        Assert.Equal(new[] { 1 }, executed);
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", second));
        worker.RunIteration();
        Assert.Equal(new[] { 1, 2 }, executed);
    }

    [Fact]
    public async Task RunIteration_RunsRoutedGamesAndTheirHandlersInScopeOnTheWorkerThread()
    {
        using var inbox = new BlockingCollection<ICommand>(8);
        using var stop = new CancellationTokenSource();
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clock = new ManualClock();
        var executed = new List<(string Name, string Scope, int ThreadId)>();
        var activeScope = "worker";
        var submittingThreadId = Environment.CurrentManagedThreadId;
        var failure = new InvalidOperationException("A1");
        ICommand? handledCommand = null;
        Exception? handledError = null;

        void Record(string name) => executed.Add((name, activeScope, Environment.CurrentManagedThreadId));

        ScheduledGame GameInScope(string scope) => new(Quantum, clock, quantum => new TestCommand(() =>
        {
            var previous = activeScope;
            try
            {
                activeScope = scope;
                quantum.Execute();
            }
            finally
            {
                activeScope = previous;
            }
        }), (command, error) => new TestCommand(() =>
        {
            handledCommand = command;
            handledError = error;
            Record("handler");
        }));

        var worker = new GameWorker(inbox, new Dictionary<string, ScheduledGame>
        {
            ["A"] = GameInScope("A"),
            ["B"] = GameInScope("B")
        }, UnexpectedError);
        var router = new GameCommandRouter();
        router.SetRoute("A", worker);
        router.SetRoute("B", worker);
        var failingCommand = new TestCommand(() =>
        {
            Record("A1");
            clock.Advance(Quantum);
            throw failure;
        });

        inbox.Add(new TestCommand(() => Record("control")));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("A", failingCommand));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("A", new TestCommand(() =>
        {
            Record("A2");
            finished.SetResult();
        })));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("B", new TestCommand(() => Record("B1"))));
        Assert.Empty(executed);

        var workerTask = Task.Factory.StartNew(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                worker.RunIteration();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        try
        {
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            stop.Cancel();
            await workerTask.WaitAsync(TimeSpan.FromSeconds(5));
        }

        Assert.Equal(new[]
        {
            ("control", "worker"),
            ("A1", "A"),
            ("handler", "A"),
            ("B1", "B"),
            ("A2", "A")
        }, executed.Select(item => (item.Name, item.Scope)));
        Assert.Same(failingCommand, handledCommand);
        Assert.Same(failure, handledError);
        Assert.Equal("worker", activeScope);
        var workerThreadId = Assert.Single(executed.Select(item => item.ThreadId).Distinct());
        Assert.NotEqual(submittingThreadId, workerThreadId);
    }

    private static ScheduledGame NewGame(ManualClock clock) => new(Quantum, clock, quantum => quantum, UnexpectedError);

    private static ICommand UnexpectedError(ICommand command, Exception error) =>
        throw new InvalidOperationException($"Unexpected failure in {command.GetType().Name}.", error);

    private sealed class TestCommand(Action run) : ICommand
    {
        public void Execute() => run();
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}
