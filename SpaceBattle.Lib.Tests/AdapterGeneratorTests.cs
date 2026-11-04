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
}
