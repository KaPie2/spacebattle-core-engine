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

        // запуск выполнения потока
        public void Start() => _thread.Start();

        // немедленная остановка цикла
        public void StopImmediately()
        {
            _stopRequested = true;
        }

        // подмена текущей стратегии выполнения
        public void SetStrategy(Action newStrategy)
        {
            _strategy = newStrategy ?? throw new ArgumentNullException(nameof(newStrategy));
        }

        // основной цикл обработки команд
        private void Run()
        {
            if (_initialScope != null)
            {
                // установка начальной области видимости для потока
                Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", _initialScope).Execute();
            }

            while (!_stopRequested)
            {
                _strategy();
            }
        }

        // стандартная стратегия извлечения и выполнения команды из очереди
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
                    // вызов маршрутизатора исключений
                    Ioc.Resolve<ICommand>("Exception.Handler", cmd, ex).Execute();
                }
            }
        }

        // ожидание завершения потока
        public void Join() => _thread.Join();
    }
}
