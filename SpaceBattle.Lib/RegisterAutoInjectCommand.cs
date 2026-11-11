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
        // выбор конструктора с максимальным количеством параметров
        var constructor = _commandType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault() ?? throw new InvalidOperationException($"Тип {_commandType.Name} не содержит публичных конструкторов.");

        // кэширование информации о типах параметров конструктора
        var parameters = constructor.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

        // регистрация стратегии автоматического внедрения зависимостей в ioc
        Ioc.Resolve<App.ICommand>("IoC.Register", _commandName, (object[] args) =>
        {
            // проверка наличия целевого объекта
            if (args == null || args.Length == 0)
                throw new ArgumentException("Отсутствуют входные аргументы для создания команды.", nameof(args));

            var targetObject = (IDictionary<string, object>)args[0];
            var resolvedArgs = new object[parameterTypes.Length];

            // подстановка аргументов конструктора
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                var paramType = parameterTypes[i];

                if (paramType.IsInterface)
                {
                    // разрешение адаптера по зарегистрированным ключам
                    try
                    {
                        resolvedArgs[i] = Ioc.Resolve<object>($"Adapter.{paramType.Name}", targetObject);
                    }
                    catch
                    {
                        resolvedArgs[i] = Ioc.Resolve<object>($"Adapters.{paramType.Name}", targetObject);
                    }
                }
                else
                {
                    // разрешение значения для неинтерфейсного параметра
                    resolvedArgs[i] = Ioc.Resolve<object>(paramType.Name, targetObject);
                }
            }

            // создание экземпляра команды
            return Activator.CreateInstance(_commandType, resolvedArgs)!;
        }).Execute();
    }
}
