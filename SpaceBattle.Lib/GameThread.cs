using System.Collections.Concurrent;
using App;

namespace SpaceBattle.Lib;

public class GameThread
{
    private readonly Thread _thread;
    private readonly BlockingCollection<ICommand> _queue;
    private readonly object? _initialScope;
    private volatile bool _stopRequested = false;
    private Action _strategy;

    public BlockingCollection<ICommand> Queue => _queue;

    public GameThread(BlockingCollection<ICommand> queue, object? initialScope = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _initialScope = initialScope;
        _strategy = DefaultExecutionLoop;
        _thread = new Thread(Run);
    }

    public void Start() => _thread.Start();

    public void StopImmediately()
    {
        _stopRequested = true;
    }

    public void SetStrategy(Action newStrategy)
    {
        _strategy = newStrategy ?? throw new ArgumentNullException(nameof(newStrategy));
    }

    private void Run()
    {
        if (_initialScope != null)
        {
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", _initialScope).Execute();
        }

        while (!_stopRequested)
        {
            _strategy();
        }
    }

    private void DefaultExecutionLoop()
    {
        if (_queue.TryTake(out var cmd, 50))
        {
            try
            {
                cmd.Execute();
            }
            catch (Exception ex)
            {
                Ioc.Resolve<ICommand>("Exception.Handler", cmd, ex).Execute();
            }
        }
    }

    public void Join() => _thread.Join();
}
