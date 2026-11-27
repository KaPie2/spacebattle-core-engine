using App;
using App.Scopes;
using Moq;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyActionsStopTests
{
    public RegisterIoCDependencyActionsStopTests()
    {
        new InitCommand().Execute();

        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();

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
    public void RegisterIoCDependencyActionsStop_DependencyResolves()
    {
        var registerCommand = new RegisterIoCDependencyActionsStop();
        registerCommand.Execute();

        var mockInjectable = new Mock<ICommandInjectable>();

        var order = new Dictionary<string, object>
        {
            { "operation", "Move" },
            { "obj", mockInjectable.Object }
        };

        var command = Ioc.Resolve<ICommand>("Actions.Stop", order);

        Assert.NotNull(command);
        Assert.IsType<StopCommand>(command);
    }

    [Fact]
    public void StopCommand_ExecutesInConstantTime_ByInjectingEmptyCommand()
    {
        var mockInjectable = new Mock<ICommandInjectable>();
        var stopCommand = new StopCommand(mockInjectable.Object);

        // Act
        stopCommand.Execute();

        // Assert
        mockInjectable.Verify(i => i.Inject(It.IsAny<EmptyCommand>()), Times.Once);
    }
}
