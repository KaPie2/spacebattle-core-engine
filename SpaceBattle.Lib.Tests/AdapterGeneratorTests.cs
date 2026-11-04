using System.Reflection;

namespace SpaceBattle.Lib.Tests;

public class AdapterGeneratorTests
{
    [Fact]
    public void Reflection_Should_Read_Interface_Properties_And_Attributes()
    {
        Type targetType = typeof(IMovingObject);
        PropertyInfo[] properties = targetType.GetProperties();

        var positionProp = properties.FirstOrDefault(p => p.Name == "Position");
        var velocityProp = properties.FirstOrDefault(p => p.Name == "Velocity");

        Assert.NotNull(positionProp);
        Assert.NotNull(velocityProp);

        Assert.True(positionProp.CanRead);
        Assert.True(positionProp.CanWrite);

        Assert.True(velocityProp.CanRead);
        Assert.False(velocityProp.CanWrite);

        var adapterAttr = velocityProp.GetCustomAttribute<AdapterAttribute>();
        Assert.NotNull(adapterAttr);
        Assert.Equal(typeof(IMovingObject), adapterAttr.TargetInterface);
        Assert.Equal("Velocity", adapterAttr.PropertyName);
        Assert.Equal("GetVelocityByAngle", adapterAttr.MethodName);
    }

    [Fact]
    public void Generator_Should_Build_Correct_CSharp_Code()
    {
        // Arrange
        var generator = new AdapterCodeGenerator();
        Type targetType = typeof(IMovingObject);

        // Act
        string generatedCode = generator.Generate(targetType);

        // Assert
        Assert.Contains("public class MovingObjectAdapter : IMovingObject", generatedCode);
        Assert.Contains("private readonly IDictionary<string, object> _obj;", generatedCode);
        Assert.Contains("public MovingObjectAdapter(IDictionary<string, object> obj) => _obj = obj;", generatedCode);

        // проверка геттера и сеттера для Position
        Assert.Contains("get => IoC.Resolve<NVector>(\"IMovingObject.Position.Get\", _obj);", generatedCode);
        Assert.Contains("set => IoC.Resolve<ICommand>(\"IMovingObject.Position.Set\", _obj, value).Execute();", generatedCode);

        // проверка геттера для Velocity (он не должен иметь сеттера)
        Assert.Contains("get => IoC.Resolve<NVector>(\"IMovingObject.Velocity.Get\", _obj);", generatedCode);
        Assert.DoesNotContain("IMovingObject.Velocity.Set", generatedCode);
    }
}
