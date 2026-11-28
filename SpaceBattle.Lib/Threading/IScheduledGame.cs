namespace SpaceBattle.Lib;

/// <summary>A game that can execute one quantum on its owning worker.</summary>
public interface IScheduledGame : ICommand
{
    /// <summary>Whether there is a queued command or a simulation step due.</summary>
    bool HasWork { get; }
}
