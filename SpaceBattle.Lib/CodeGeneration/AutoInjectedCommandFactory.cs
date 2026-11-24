using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        var keysByScope = new ConditionalWeakTable<object, string>();

        return state =>
        {
            var scope = Ioc.Resolve<object>("IoC.Scope.Current");
            if (keysByScope.TryGetValue(scope, out var resolvedKey))
                return Ioc.Resolve<object>(resolvedKey, state);

            var key = singularKey;
            object adapter;
            try
            {
                adapter = Ioc.Resolve<object>(key, state);
            }
            catch
            {
                key = pluralKey;
                adapter = Ioc.Resolve<object>(key, state);
            }

            keysByScope.TryAdd(scope, key);
            return adapter;
        };
    }
}
