using App;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class AdapterBuilderTests
{
    public AdapterBuilderTests()
    {
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void Builder_Should_Compile_And_Cache_Adapter_Factory_In_IoC()
    {
        // Arrange
        var builder = new AdapterBuilder();
        var state = new Dictionary<string, object>();

        // Act
        builder.BuildAndRegister(typeof(IMovingObject));

        // Assert
        var adapter = Ioc.Resolve<IMovingObject>("Adapter.IMovingObject", state);

        Assert.NotNull(adapter);
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }
}
