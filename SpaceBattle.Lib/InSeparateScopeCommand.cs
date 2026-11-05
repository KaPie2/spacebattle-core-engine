using System;
using App;

namespace SpaceBattle.Lib
{
    public class InSeparateScopeCommand : ICommand
    {
        private readonly object _targetScope;
        private readonly ICommand _targetCommand;

        public InSeparateScopeCommand(object targetScope, ICommand targetCommand)
        {
            _targetScope = targetScope ?? throw new ArgumentNullException(nameof(targetScope));
            _targetCommand = targetCommand ?? throw new ArgumentNullException(nameof(targetCommand));
        }

        public void Execute()
        {
            // 1. Сохраняем скоуп, который сейчас активен в вызывающем потоке
            var previousScope = Ioc.Resolve<object>("IoC.Scope.Current");

            try
            {
                // 2. Устанавливаем целевой скоуп игры для текущего потока
                ExecuteSetScopeCommand(_targetScope);

                // 3. Выполняем целевую команду
                _targetCommand.Execute();
            }
            finally
            {
                // 4. Гарантированно восстанавливаем исходный скоуп даже при сбое
                ExecuteSetScopeCommand(previousScope);
            }
        }

        private static void ExecuteSetScopeCommand(object scope)
        {
            var commandObj = Ioc.Resolve<object>("IoC.Scope.Current.Set", scope);

            if (commandObj is ICommand libCmd)
            {
                libCmd.Execute();
            }
            else
            {
                // На случай вызова библиотечной команды из App.Scopes
                var executeMethod = commandObj?.GetType().GetMethod("Execute");
                if (executeMethod != null)
                {
                    executeMethod.Invoke(commandObj, null);
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Команда смены скоупа имеет неподдерживаемый тип: {commandObj?.GetType().FullName ?? "null"}");
                }
            }
        }
    }
}