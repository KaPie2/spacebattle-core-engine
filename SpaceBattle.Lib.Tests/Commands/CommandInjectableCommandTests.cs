using Moq;

namespace SpaceBattle.Lib.Tests;

public class CommandInjectableCommandTests
{
    [Fact]
    public void Execute_CallsInjectedCommand_WhenCommandIsInjected()
    {
        var mockCommand = new Mock<ICommand>();
        var injectableCommand = new CommandInjectableCommand();

        injectableCommand.Inject(mockCommand.Object);
        injectableCommand.Execute();

        mockCommand.Verify(c => c.Execute(), Times.Once);
    }

    [Fact]
    public void Execute_ThrowsException_WhenNoCommandInjected()
    {
        var injectableCommand = new CommandInjectableCommand();

        Assert.Throws<InvalidOperationException>(() => injectableCommand.Execute());
    }
}
