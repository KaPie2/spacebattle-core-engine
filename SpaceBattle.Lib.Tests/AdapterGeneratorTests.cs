using System;
using System.Linq;
using System.Reflection;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AdapterGeneratorTests
{
    [Fact]
    public void AdapterBuilder_ThrowsArgumentException_WhenNotInterface()
    {
        // arrange (подготовка генератора кода)
        var generator = new AdapterCodeGenerator();

        // act & assert (проверка выброса исключения для неинтерфейсного типа)
        Assert.Throws<ArgumentException>(() => generator.Generate(typeof(NVector)));
    }

    [Fact]
    public void AdapterBuilder_GeneratesValidProperties_ForMovingObject()
    {
        // arrange (подготовка генератора кода)
        var generator = new AdapterCodeGenerator();

        // act (генерация кода адаптера для imovingobject)
        var generatedCode = generator.Generate(typeof(IMovingObject));

        // assert (проверка наличия объявлений классов адаптера и фабрики)
        Assert.Contains("public class MovingObjectAdapter : SpaceBattle.Lib.IMovingObject", generatedCode);
        Assert.Contains("public class MovingObjectAdapterFactory : IAdapterFactory<SpaceBattle.Lib.IMovingObject>", generatedCode);

        // assert (проверка формирования вызовов ioc.resolve для свойств)
        Assert.Contains("get => Ioc.Resolve<SpaceBattle.Lib.NVector>(\"IMovingObject.Position.Get\", _obj);", generatedCode);
        Assert.Contains("set => Ioc.Resolve<App.ICommand>(\"IMovingObject.Position.Set\", _obj, value).Execute();", generatedCode);

        // assert (проверка формирования вызова стратегии по атрибуту adapter)
        Assert.Contains("get => Ioc.Resolve<SpaceBattle.Lib.NVector>(\"IMovingObject.Velocity.GetVelocityByAngle\", _obj);", generatedCode);
    }

    [Fact]
    public void Reflection_Reads_AdapterAttribute_Metadata()
    {
        // arrange (получение типа интерфейса через рефлексию)
        Type targetType = typeof(IMovingObject);

        // act (получение свойства velocity и атрибута adapter)
        var velocityProp = targetType.GetProperty("Velocity");
        Assert.NotNull(velocityProp);
        var attr = velocityProp.GetCustomAttribute<AdapterAttribute>();

        // assert (проверка метаданных атрибута adapter)
        Assert.NotNull(attr);
        Assert.Equal(typeof(IMovingObject), attr.TargetInterface);
        Assert.Equal("Velocity", attr.PropertyName);
        Assert.Equal(typeof(MovementCalculations), attr.StrategyType);
        Assert.Equal("GetVelocityByAngle", attr.MethodName);
    }
}
