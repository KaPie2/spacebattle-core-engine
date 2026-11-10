using System;
using System.Collections.Generic;
using System.Linq;
using App;

namespace SpaceBattle.Lib;

public class RegisterAutoInjectCommand : ICommand
{
    private readonly string _commandName;
    private readonly Type _commandType;

    public RegisterAutoInjectCommand(string commandName, Type commandType)
    {
        _commandName = commandName ?? throw new ArgumentNullException(nameof(commandName));
        _commandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
    }

    public void Execute()
    {
        // регистрация умной стратегии создания команды в IoC
        Ioc.Resolve<App.ICommand>("IoC.Register", _commandName, (object[] args) =>
        {
            var targetObject = (IDictionary<string, object>)args[0];
            var constructor = _commandType.GetConstructors().First();
            var parameters = constructor.GetParameters();
            var resolvedArgs = new object[parameters.Length];

            // перебор параметров конструктора и автоматическая инъекция
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                if (paramType.IsInterface)
                {
                    // автоматическое разрешение интерфейса через ранее созданную фабрику адаптеров
                    resolvedArgs[i] = Ioc.Resolve<object>($"Adapter.{paramType.Name}", targetObject);
                }
                else
                {
                    // фолбэк для обычных типов
                    resolvedArgs[i] = Ioc.Resolve<object>(paramType.Name, targetObject);
                }
            }

            return Activator.CreateInstance(_commandType, resolvedArgs)!;
        }).Execute();
    }
}
