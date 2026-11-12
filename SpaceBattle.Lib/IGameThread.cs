using System.Collections.Concurrent;

namespace SpaceBattle.Lib;

public interface IGameThread
{
    BlockingCollection<ICommand> Queue { get; }
    void StopImmediately();
    void SetStrategy(Action newStrategy);
}
