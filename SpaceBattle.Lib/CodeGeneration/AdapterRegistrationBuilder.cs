using System.Reflection;

namespace SpaceBattle.Lib;

internal static class AdapterRegistrationBuilder
{
    internal static ICommand Build(Type interfaceType)
    {
        var sourceCode = new AdapterCodeGenerator().Generate(interfaceType);
        var registrationSource = AdapterCodeGenerator.GenerateRegistration(interfaceType);
        var registrationClass = AdapterCodeGenerator.GetRegistrationClassName(interfaceType);
        var registrationType = new AdapterCompiler().Compile(
            new[] { sourceCode, registrationSource },
            $"SpaceBattle.Lib.Generated.{registrationClass}"
        );
        return (ICommand)Activator.CreateInstance(registrationType, sourceCode, GetAttributeStrategies(interfaceType))!;
    }

    private static IEnumerable<(string Key, Func<object[], object> Strategy)> GetAttributeStrategies(Type interfaceType)
    {
        foreach (var property in AdapterCodeGenerator.GetAdapterProperties(interfaceType))
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
