using System;

namespace SpaceBattle.Lib;

[AttributeUsage(AttributeTargets.Property)]
public class AdapterAttribute : Attribute
{
    public Type TargetInterface { get; }
    public string PropertyName { get; }
    public Type StrategyType { get; }
    public string MethodName { get; }

    public AdapterAttribute(Type targetInterface, string propertyName, Type strategyType, string methodName)
    {
        TargetInterface = targetInterface;
        PropertyName = propertyName;
        StrategyType = strategyType;
        MethodName = methodName;
    }
}

public static class MovementCalculations
{
    public static NVector GetVelocityByAngle(object[] args)
    {
        return new NVector(); // заглушка для компиляции
    }
}
