using System.Reflection;

namespace SpaceBattle.Lib;

internal static class AdapterFactoryBuilder
{
    internal static (IAdapterFactory<T> Factory, string SourceCode) Build<T>()
    {
        var interfaceType = typeof(T);
        var sourceCode = new AdapterCodeGenerator().Generate(interfaceType);
        var adapterClassName = AdapterCodeGenerator.GetAdapterClassName(interfaceType);
        var factoryType = new AdapterCompiler().Compile(sourceCode, $"SpaceBattle.Lib.Generated.{adapterClassName}Factory");
        var factory = (IAdapterFactory<T>)Activator.CreateInstance(factoryType)!;
        return (factory, sourceCode);
    }

    internal static IEnumerable<(string Key, Func<object[], object> Strategy)> GetAttributeStrategies(Type interfaceType)
    {
        foreach (var property in interfaceType.GetProperties())
        {
            var attribute = property.GetCustomAttribute<AdapterAttribute>();
            if (attribute?.StrategyType == null || string.IsNullOrEmpty(attribute.MethodName))
                continue;

            var method = attribute.StrategyType.GetMethod(attribute.MethodName, BindingFlags.Public | BindingFlags.Static);
            if (method != null)
                yield return (attribute.DependencyKey, args => method.Invoke(null, new object[] { args })!);
        }
    }
}
