using App;
using App.Scopes;
using Moq;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyActionsStartTests
{
    public RegisterIoCDependencyActionsStartTests()
    {
        new InitCommand().Execute();

        var mockReceiver = new Mock<ICommandReceiver>();
        Ioc.Resolve<App.ICommand>("IoC.Register", "Game.CommandReceiver",
            (object[] _) => mockReceiver.Object).Execute();

        Ioc.Resolve<App.ICommand>("IoC.Register", "Commands.Move",
            (object[] args) => new Mock<ICommand>().Object).Execute();

        Ioc.Resolve<App.ICommand>("IoC.Register", "Commands.Send",
            (object[] args) =>
            {
                var cmd = (ICommand)args[0];
                var rec = (ICommandReceiver)args[1];
                return new SendCommand(cmd, rec);
            }).Execute();
    }

    [Fact]
    public void RegisterIoCDependencyActionsStart_DependencyResolves()
    {
        var registerCommand = new RegisterIoCDependencyActionsStart();
        registerCommand.Execute();

        var order = new Dictionary<string, object>
        {
            { "operation", "Move" },
            { "obj", new object() }
        };

        var command = Ioc.Resolve<ICommand>("Actions.Start", order);

        Assert.NotNull(command);
        Assert.IsType<SendCommand>(command);
    }
}
