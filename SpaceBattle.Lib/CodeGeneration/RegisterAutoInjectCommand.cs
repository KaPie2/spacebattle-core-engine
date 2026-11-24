using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

        // компиляция фабрики конструктора через выражения для исключения рефлексии в рантайме
        var argsParam = Expression.Parameter(typeof(object[]), "args");
        var ctorArgs = new Expression[parameterTypes.Length];
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            var arrayElement = Expression.ArrayIndex(argsParam, Expression.Constant(i));
            ctorArgs[i] = Expression.Convert(arrayElement, parameterTypes[i]);
        }
        var newExpr = Expression.New(constructor, ctorArgs);
        var compiledFactory = Expression.Lambda<Func<object[], ICommand>>(newExpr, argsParam).Compile();

        // подготовка и кэширование резолверов параметров
        var resolvers = new Func<IDictionary<string, object>, object>[parameterTypes.Length];
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            var pType = parameterTypes[i];
            if (pType.IsInterface)
            {
                var keySingular = $"Adapter.{pType.Name}";
                var keyPlural = $"Adapters.{pType.Name}";

                // ленивое определение и кэширование ключа без накладных расходов исключений в цикле игры
                string? determinedKey = null;
                resolvers[i] = (targetObject) =>
                {
                    if (determinedKey != null)
                    {
                        return Ioc.Resolve<object>(determinedKey, targetObject);
                    }

                    try
                    {
                        var res = Ioc.Resolve<object>(keySingular, targetObject);
                        determinedKey = keySingular;
                        return res;
                    }
                    catch
                    {
                        determinedKey = keyPlural;
                        return Ioc.Resolve<object>(keyPlural, targetObject);
                    }
                };
            }
            else
            {
                var nonInterfaceKey = pType.Name;
                resolvers[i] = (targetObject) => Ioc.Resolve<object>(nonInterfaceKey, targetObject);
            }
        }

        // регистрация стратегии автоматического внедрения зависимостей в ioc
        Ioc.Resolve<App.ICommand>("IoC.Register", _commandName, (object[] args) =>
        {
            // проверка наличия целевого объекта
            if (args == null || args.Length == 0)
                throw new ArgumentException("Отсутствуют входные аргументы для создания команды.", nameof(args));

            var targetObject = (IDictionary<string, object>)args[0];
            var resolvedArgs = new object[resolvers.Length];

            // подстановка аргументов конструктора через закэшированные резолверы
            for (int i = 0; i < resolvers.Length; i++)
            {
                resolvedArgs[i] = resolvers[i](targetObject);
            }

            // создание экземпляра команды через скомпилированную фабрику
            return compiledFactory(resolvedArgs);
        }).Execute();
    }
}
