using System.Linq.Expressions;
using System.Reflection;
using App;

namespace SpaceBattle.Lib;

internal static class AutoInjectedCommandFactory
{
    internal static Func<object[], object> Build(Type commandType)
    {
        var constructor = commandType.GetConstructors()
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .FirstOrDefault() ?? throw new InvalidOperationException($"Тип {commandType.Name} не содержит публичных конструкторов.");
        var parameterTypes = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var createCommand = CompileConstructor(constructor, parameterTypes);
        var dependencyKeys = parameterTypes
            .Select(type => type.IsInterface ? $"Adapters.{type.Name}" : type.Name)
            .ToArray();

        return args =>
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException("Отсутствуют входные аргументы для создания команды.", nameof(args));

            var state = (IDictionary<string, object>)args[0];
            var resolvedArgs = new object[dependencyKeys.Length];
            for (int i = 0; i < dependencyKeys.Length; i++)
            {
                resolvedArgs[i] = Ioc.Resolve<object>(dependencyKeys[i], state);
            }

            return createCommand(resolvedArgs);
        };
    }

    private static Func<object[], ICommand> CompileConstructor(ConstructorInfo constructor, Type[] parameterTypes)
    {
        var args = Expression.Parameter(typeof(object[]), "args");
        var constructorArgs = new Expression[parameterTypes.Length];
        for (int i = 0; i < parameterTypes.Length; i++)
        {
            var argument = Expression.ArrayIndex(args, Expression.Constant(i));
            constructorArgs[i] = Expression.Convert(argument, parameterTypes[i]);
        }

        return Expression.Lambda<Func<object[], ICommand>>(Expression.New(constructor, constructorArgs), args).Compile();
    }
}
