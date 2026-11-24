using App;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class RegisterDependencyCommandInjectableCommandTests
{
    public RegisterDependencyCommandInjectableCommandTests()
    {
        new InitCommand().Execute();
    }

    [Fact]
    public void RegisterDependencyCommandInjectableCommand_DependencyResolves()
    {
        var registerCommand = new RegisterDependencyCommandInjectableCommand();

        registerCommand.Execute();

        var exception1 = Record.Exception(() => Ioc.Resolve<ICommand>("Commands.CommandInjectable"));
        var exception2 = Record.Exception(() => Ioc.Resolve<ICommandInjectable>("Commands.CommandInjectable"));
        var exception3 = Record.Exception(() => Ioc.Resolve<CommandInjectableCommand>("Commands.CommandInjectable"));

        Assert.Null(exception1);
        Assert.Null(exception2);
        Assert.Null(exception3);
    }
}
