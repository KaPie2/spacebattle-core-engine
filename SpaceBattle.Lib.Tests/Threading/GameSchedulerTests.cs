namespace SpaceBattle.Lib.Tests;

public class GameSchedulerTests
{
    [Fact]
    public void NotifyReady_RejectsNullGame()
    {
        var scheduler = new GameScheduler();

        Assert.Throws<ArgumentNullException>(() => scheduler.NotifyReady(null!));
    }

    [Fact]
    public void TryRunNext_RunsAnIdleGameOnlyAfterItHasWork()
    {
        var scheduler = new GameScheduler();
        var executed = new List<string>();
        var game = new TestGame(0, () => executed.Add("A"));

        Assert.False(scheduler.TryRunNext());
        scheduler.NotifyReady(game);
        Assert.False(scheduler.TryRunNext());
        Assert.Empty(executed);

        game.RemainingQuanta = 1;
        scheduler.NotifyReady(game);
        Assert.True(scheduler.TryRunNext());
        Assert.False(scheduler.TryRunNext());
        Assert.Equal(new[] { "A" }, executed);
    }

    [Fact]
    public void TryRunNext_GivesEachReadyGameOneTurnBeforeRequeuingIt()
    {
        var scheduler = new GameScheduler();
        var executed = new List<string>();
        scheduler.NotifyReady(new TestGame(3, () => executed.Add("A")));
        scheduler.NotifyReady(new TestGame(2, () => executed.Add("B")));

        Assert.True(scheduler.TryRunNext());
        Assert.Equal(new[] { "A" }, executed);
        Assert.True(scheduler.TryRunNext());
        Assert.Equal(new[] { "A", "B" }, executed);
        Assert.True(scheduler.TryRunNext());
        Assert.True(scheduler.TryRunNext());
        Assert.True(scheduler.TryRunNext());
        Assert.False(scheduler.TryRunNext());
        Assert.Equal(new[] { "A", "B", "A", "B", "A" }, executed);
    }

    [Fact]
    public void NotifyReady_KeepsOnlyOneQueueEntryForEachGame()
    {
        var scheduler = new GameScheduler();
        var executed = new List<string>();
        var first = new TestGame(2, () => executed.Add("A"));
        scheduler.NotifyReady(first);
        scheduler.NotifyReady(first);
        scheduler.NotifyReady(new TestGame(1, () => executed.Add("B")));

        Assert.True(scheduler.TryRunNext());
        Assert.True(scheduler.TryRunNext());
        Assert.True(scheduler.TryRunNext());
        Assert.False(scheduler.TryRunNext());
        Assert.Equal(new[] { "A", "B", "A" }, executed);
    }

    [Fact]
    public void TryRunNext_SkipsAGameThatBecameIdleWhileQueued()
    {
        var scheduler = new GameScheduler();
        var executed = new List<string>();
        var first = new TestGame(1, () => executed.Add("A"));
        scheduler.NotifyReady(first);
        scheduler.NotifyReady(new TestGame(1, () => executed.Add("B")));
        first.RemainingQuanta = 0;

        Assert.True(scheduler.TryRunNext());
        Assert.Equal(new[] { "B" }, executed);
        Assert.False(scheduler.TryRunNext());

        first.RemainingQuanta = 1;
        scheduler.NotifyReady(first);
        Assert.True(scheduler.TryRunNext());
        Assert.Equal(new[] { "B", "A" }, executed);
    }

    [Fact]
    public void TryRunNext_PropagatesFailureAndPreservesOtherReadyGames()
    {
        var scheduler = new GameScheduler();
        var executed = new List<string>();
        var failure = new InvalidOperationException();
        scheduler.NotifyReady(new TestGame(2, () => throw failure));
        scheduler.NotifyReady(new TestGame(1, () => executed.Add("B")));

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => scheduler.TryRunNext()));
        Assert.True(scheduler.TryRunNext());
        Assert.Equal(new[] { "B" }, executed);
        Assert.False(scheduler.TryRunNext());
    }

    private sealed class TestGame(int remainingQuanta, Action execute) : IScheduledGame
    {
        public int RemainingQuanta { get; set; } = remainingQuanta;
        public bool HasWork => RemainingQuanta > 0;

        public void Execute()
        {
            RemainingQuanta--;
            execute();
        }
    }
}
