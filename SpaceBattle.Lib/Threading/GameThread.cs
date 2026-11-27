using System.Collections.Concurrent;
using App;

namespace SpaceBattle.Lib;

public class GameThread : ICommandReceiver
{
    private readonly Thread _thread;
    private readonly BlockingCollection<ICommand> _queue;
    private readonly Func<ICommand, Exception, ICommand> _exceptionHandler;
    private readonly object? _initialScope;
    private volatile bool _stopRequested = false;
    private Action _strategy;

    public BlockingCollection<ICommand> Queue => _queue;

    public GameThread(
        BlockingCollection<ICommand> queue,
        Func<ICommand, Exception, ICommand> exceptionHandler,
        object? initialScope = null)
    {
        _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _initialScope = initialScope;
        _strategy = DefaultExecutionLoop;
        _thread = new Thread(Run);
    }

    public GameThread(BlockingCollection<ICommand> queue, object? initialScope = null)
        : this(queue, (cmd, ex) => Ioc.Resolve<ICommand>("Exception.Handler", cmd, ex), initialScope)
    {
    }

    public void Receive(ICommand command)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        _queue.Add(command);
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
                _exceptionHandler(cmd, ex).Execute();
            }
        }
    }

    public void Join() => _thread.Join();
}
