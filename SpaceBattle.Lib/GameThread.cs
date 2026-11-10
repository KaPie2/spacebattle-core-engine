using System;
using System.Threading;
using System.Collections.Concurrent;
using App;

namespace SpaceBattle.Lib
{
    public class GameThread
    {
        private readonly Thread _thread;
        private readonly BlockingCollection<ICommand> _queue;
        private volatile bool _stopRequested = false;
        private Action _strategy;

        public BlockingCollection<ICommand> Queue => _queue;

        public GameThread(BlockingCollection<ICommand> queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
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
                    // вызов двухуровневого маршрутизатора исключений
                    Ioc.Resolve<ICommand>("Exception.Handler", cmd, ex).Execute();
                }
            }
        }

        public void Join() => _thread.Join();
    }
}
