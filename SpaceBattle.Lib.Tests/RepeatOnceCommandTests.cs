using System;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class RepeatOnceCommandTests : IDisposable
{
    public RepeatOnceCommandTests()
    {
        new InitCommand().Execute();

        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    public void Dispose()
    {
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Clear").Execute();
    }

    [Fact]
    public void RepeatedCommand_NullInnerCommand_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new RepeatedCommand(null!));
        Assert.Equal("innerCommand", ex.ParamName);
    }

    [Fact]
    public void RepeatedCommand_Execute_DelegatesToInnerCommand()
    {
        // Arrange
        var innerCommandMock = new Mock<ICommand>();
        var repeatedCommand = new RepeatedCommand(innerCommandMock.Object);

        // Act
        repeatedCommand.Execute();

        // Assert
        Assert.Same(innerCommandMock.Object, repeatedCommand.InnerCommand);
        innerCommandMock.Verify(c => c.Execute(), Times.Once);
    }

    [Fact]
    public void RepeatOnceCommand_NullTargetCommand_ThrowsArgumentNullException()
    {
        var receiverMock = new Mock<ICommandReceiver>();

        var ex = Assert.Throws<ArgumentNullException>(() => new RepeatOnceCommand(null!, receiverMock.Object));
        Assert.Equal("targetCommand", ex.ParamName);
    }

    [Fact]
    public void RepeatOnceCommand_NullReceiver_ThrowsArgumentNullException()
    {
        var commandMock = new Mock<ICommand>();

        var ex = Assert.Throws<ArgumentNullException>(() => new RepeatOnceCommand(commandMock.Object, null!));
        Assert.Equal("receiver", ex.ParamName);
    }

    [Fact]
    public void RepeatOnceCommand_Execute_WrapsCommandAndSendsToReceiverViaIoC()
    {
        // Arrange
        var targetCommandMock = new Mock<ICommand>();
        var receiverMock = new Mock<ICommandReceiver>();
        var sendCommandMock = new Mock<ICommand>();

        ICommand? capturedSentCommand = null;
        ICommandReceiver? capturedReceiver = null;

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.Send",
            (Func<object[], object>)(args =>
            {
                capturedSentCommand = (ICommand)args[0];
                capturedReceiver = (ICommandReceiver)args[1];
                return sendCommandMock.Object;
            })
        ).Execute();

        var repeatOnceCommand = new RepeatOnceCommand(targetCommandMock.Object, receiverMock.Object);

        // Act
        repeatOnceCommand.Execute();

        // Assert
        Assert.Same(targetCommandMock.Object, repeatOnceCommand.TargetCommand);
        Assert.Same(receiverMock.Object, repeatOnceCommand.Receiver);

        Assert.NotNull(capturedSentCommand);
        var repeatedWrapper = Assert.IsType<RepeatedCommand>(capturedSentCommand);
        Assert.Same(targetCommandMock.Object, repeatedWrapper.InnerCommand);
        Assert.Same(receiverMock.Object, capturedReceiver);

        sendCommandMock.Verify(c => c.Execute(), Times.Once);
    }
}