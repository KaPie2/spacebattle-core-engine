using System.Collections.Concurrent;

namespace SpaceBattle.Lib;

/// <summary>Routes incoming commands to the worker inbox registered for each game.</summary>
public sealed class GameCommandRouter
{
    private readonly ConcurrentDictionary<string, IWorkerInbox> _routes = new(StringComparer.Ordinal);

    /// <summary>Sets the worker inbox used by subsequent lookups for the game.</summary>
    public void SetRoute(string gameId, IWorkerInbox inbox)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        ArgumentNullException.ThrowIfNull(inbox);

        _routes[gameId] = inbox;
    }

    public CommandRoutingResult Route(string gameId, ICommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        ArgumentNullException.ThrowIfNull(command);

        if (!_routes.TryGetValue(gameId, out var inbox))
        {
            return CommandRoutingResult.GameNotFound;
        }

        return inbox.TryEnqueue(new GameCommandMessage(gameId, command))
            ? CommandRoutingResult.Accepted
            : CommandRoutingResult.QueueFull;
    }
}
