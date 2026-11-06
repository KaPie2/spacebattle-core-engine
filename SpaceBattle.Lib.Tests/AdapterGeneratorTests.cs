using System;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AdapterGeneratorTests
{
    [Fact]
    public void AdapterBuilder_ThrowsArgumentException_WhenNotInterface()
    {
        var generator = new AdapterCodeGenerator();

        Assert.Throws<ArgumentException>(() => generator.Generate(typeof(NVector)));
    }

    [Fact]
    public void AdapterBuilder_GeneratesValidProperties_ForMovingObject()
    {
        var generator = new AdapterCodeGenerator();
        var generatedCode = generator.Generate(typeof(IMovingObject));

        // наличие класса адаптера и класса фабрики
        Assert.Contains("public class MovingObjectAdapter : SpaceBattle.Lib.IMovingObject", generatedCode);
        Assert.Contains("public class MovingObjectAdapterFactory : IAdapterFactory<SpaceBattle.Lib.IMovingObject>", generatedCode);

        // свойства и правильный Ioc.Resolve
        Assert.Contains("get => Ioc.Resolve<SpaceBattle.Lib.NVector>(\"IMovingObject.Position.Get\", _obj);", generatedCode);
        Assert.Contains("set => Ioc.Resolve<App.ICommand>(\"IMovingObject.Position.Set\", _obj, value).Execute();", generatedCode);
    }
}
