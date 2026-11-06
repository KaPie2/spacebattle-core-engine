using System;
using System.Collections.Generic;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RegisterAdapterCommandTests
{
    public RegisterAdapterCommandTests()
    {
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void RegisterAdapterCommand_RegistersInIoC_AndCreatesViaResolve()
    {
        // Arrange
        var command = new RegisterAdapterCommand<IMovingObject>();
        var state = new Dictionary<string, object>();

        // Act
        command.Execute();

        // Assert
        var adapter = Ioc.Resolve<IMovingObject>("Adapter.IMovingObject", state);

        Assert.NotNull(adapter);
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }
}
