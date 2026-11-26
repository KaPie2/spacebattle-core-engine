namespace SpaceBattle.Lib;

/// <summary>Schedules ready games in round-robin order on a single worker.</summary>
/// <remarks>The owning worker sends readiness notifications and runs game quanta.</remarks>
public sealed class GameScheduler
{
    private readonly Queue<IScheduledGame> _readyGames = new();
    private readonly HashSet<IScheduledGame> _queuedGames = new(ReferenceEqualityComparer.Instance);

    public void NotifyReady(IScheduledGame game)
    {
        ArgumentNullException.ThrowIfNull(game);

        if (game.HasWork && _queuedGames.Add(game))
        {
            _readyGames.Enqueue(game);
        }
    }

    public bool TryRunNext()
    {
        while (_readyGames.TryDequeue(out var game))
        {
            _queuedGames.Remove(game);
            if (!game.HasWork)
            {
                continue;
            }

            game.Execute();
            NotifyReady(game);
            return true;
        }

        return false;
    }
}
