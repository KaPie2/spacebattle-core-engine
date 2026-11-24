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
        var resolvers = parameterTypes.Select(CreateParameterResolver).ToArray();

        return args =>
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException("Отсутствуют входные аргументы для создания команды.", nameof(args));

            var state = (IDictionary<string, object>)args[0];
            var resolvedArgs = new object[resolvers.Length];
            for (int i = 0; i < resolvers.Length; i++)
            {
                resolvedArgs[i] = resolvers[i](state);
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

    private static Func<IDictionary<string, object>, object> CreateParameterResolver(Type parameterType)
    {
        var parameterName = parameterType.Name;
        if (!parameterType.IsInterface)
            return state => Ioc.Resolve<object>(parameterName, state);

        var singularKey = $"Adapter.{parameterName}";
        var pluralKey = $"Adapters.{parameterName}";
        string? resolvedKey = null;

        return state =>
        {
            if (resolvedKey != null)
                return Ioc.Resolve<object>(resolvedKey, state);

            try
            {
                var adapter = Ioc.Resolve<object>(singularKey, state);
                resolvedKey = singularKey;
                return adapter;
            }
            catch
            {
                resolvedKey = pluralKey;
                return Ioc.Resolve<object>(pluralKey, state);
            }
        };
    }
}
