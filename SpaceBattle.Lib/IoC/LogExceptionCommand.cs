using System;
using App;

namespace SpaceBattle.Lib
{
    public class LogExceptionCommand : ICommand
    {
        private readonly ICommand _failedCommand;
        private readonly Exception _exception;

        public ICommand FailedCommand => _failedCommand;
        public Exception Exception => _exception;

        public LogExceptionCommand(ICommand failedCommand, Exception exception)
        {
            _failedCommand = failedCommand ?? throw new ArgumentNullException(nameof(failedCommand));
            _exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public void Execute()
        {
            // Получаем через IoC стратегию записи в лог и передаем сформированное сообщение
            var logAction = Ioc.Resolve<Action<string>>("Logger.Log");
            logAction($"Command '{_failedCommand.GetType().Name}' threw exception: {_exception.Message}");
        }
    }
}