using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib.Tests;

public class GameCommandRouterTests
{
    [Fact]
    public void Route_SendsGameIdAndCommandToTheSelectedWorker()
    {
        var router = new GameCommandRouter();
        using var firstWorker = new TestInbox();
        using var secondWorker = new TestInbox();
        router.SetRoute("first-game", firstWorker);
        router.SetRoute("second-game", secondWorker);
        var command = new EmptyCommand();

        Assert.Equal(CommandRoutingResult.Accepted, router.Route("second-game", command));

        Assert.False(firstWorker.TryTake(out _));
        Assert.True(secondWorker.TryTake(out var message));
        Assert.Equal(new GameCommandMessage("second-game", command), message);
    }

    [Fact]
    public void Route_ReturnsGameNotFoundForAnUnknownGame()
    {
        var router = new GameCommandRouter();
        using var worker = new TestInbox();
        router.SetRoute("known-game", worker);

        Assert.Equal(CommandRoutingResult.GameNotFound, router.Route("unknown-game", new EmptyCommand()));
        Assert.False(worker.TryTake(out _));
    }

    [Fact]
    public void Route_ReturnsQueueFullAndPreservesTheAcceptedCommand()
    {
        var router = new GameCommandRouter();
        using var worker = new TestInbox(capacity: 1);
        router.SetRoute("game", worker);
        var first = new EmptyCommand();
        var second = new EmptyCommand();

        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", first));
        Assert.Equal(CommandRoutingResult.QueueFull, router.Route("game", second));

        Assert.True(worker.TryTake(out var message));
        Assert.Equal(new GameCommandMessage("game", first), message);
        Assert.False(worker.TryTake(out _));
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", second));
        Assert.True(worker.TryTake(out var retried));
        Assert.Equal(new GameCommandMessage("game", second), retried);
    }

    [Fact]
    public void SetRoute_ReplacesTheWorkerForSubsequentCommands()
    {
        var router = new GameCommandRouter();
        using var oldWorker = new TestInbox();
        using var newWorker = new TestInbox();
        var first = new EmptyCommand();
        var second = new EmptyCommand();

        router.SetRoute("game", oldWorker);
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", first));
        router.SetRoute("game", newWorker);
        Assert.Equal(CommandRoutingResult.Accepted, router.Route("game", second));

        Assert.True(oldWorker.TryTake(out var oldMessage));
        Assert.Equal(new GameCommandMessage("game", first), oldMessage);
        Assert.False(oldWorker.TryTake(out _));
        Assert.True(newWorker.TryTake(out var newMessage));
        Assert.Equal(new GameCommandMessage("game", second), newMessage);
        Assert.False(newWorker.TryTake(out _));
    }

    [Fact]
    public void RoutedCommands_MakeTwoGamesOnTheSameWorkerRunInRoundRobinOrder()
    {
        var router = new GameCommandRouter();
        var scheduler = new GameScheduler();
        using var worker = new TestInbox();
        var firstGame = new TestGame();
        var secondGame = new TestGame();
        var games = new Dictionary<string, TestGame>
        {
            ["first-game"] = firstGame,
            ["second-game"] = secondGame
        };
        router.SetRoute("first-game", worker);
        router.SetRoute("second-game", worker);
        var executed = new List<string>();

        Assert.Equal(CommandRoutingResult.Accepted,
            router.Route("first-game", new TestCommand(() => executed.Add("first-1"))));
        Assert.Equal(CommandRoutingResult.Accepted,
            router.Route("first-game", new TestCommand(() => executed.Add("first-2"))));
        Assert.Equal(CommandRoutingResult.Accepted,
            router.Route("second-game", new TestCommand(() => executed.Add("second-1"))));
        Assert.Empty(executed);

        while (worker.TryTake(out var message))
        {
            var game = games[message.GameId];
            game.Enqueue(message.Command);
            scheduler.NotifyReady(game);
        }

        Assert.True(scheduler.TryRunNext());
        Assert.True(firstGame.HasWork);
        Assert.True(secondGame.HasWork);
        Assert.True(scheduler.TryRunNext());
        Assert.False(secondGame.HasWork);
        Assert.True(scheduler.TryRunNext());
        Assert.False(scheduler.TryRunNext());
        Assert.Equal(new[] { "first-1", "second-1", "first-2" }, executed);
    }

    private sealed class TestInbox(int capacity = 8) : IWorkerInbox, IDisposable
    {
        private readonly BlockingCollection<GameCommandMessage> _queue = new(capacity);

        public bool TryEnqueue(GameCommandMessage message) => _queue.TryAdd(message);

        public bool TryTake([NotNullWhen(true)] out GameCommandMessage? message) => _queue.TryTake(out message);

        public void Dispose() => _queue.Dispose();
    }

    private sealed class TestGame : IScheduledGame
    {
        private readonly Queue<ICommand> _commands = new();

        public bool HasWork => _commands.Count > 0;

        public void Enqueue(ICommand command) => _commands.Enqueue(command);

        public void Execute() => _commands.Dequeue().Execute();
    }

    private sealed class TestCommand(Action execute) : ICommand
    {
        public void Execute() => execute();
    }
}
