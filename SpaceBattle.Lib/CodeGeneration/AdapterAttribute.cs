using System;

namespace SpaceBattle.Lib;

[AttributeUsage(AttributeTargets.Property)]
public class AdapterAttribute : Attribute
{
    public Type TargetInterface { get; }
    public string PropertyName { get; }
    public Type StrategyType { get; }
    public string MethodName { get; }

    internal string DependencyKey => $"{TargetInterface.Name}.{PropertyName}.{MethodName}";

    public AdapterAttribute(Type targetInterface, string propertyName, Type strategyType, string methodName)
    {
        TargetInterface = targetInterface;
        PropertyName = propertyName;
        StrategyType = strategyType;
        MethodName = methodName;
    }
}
