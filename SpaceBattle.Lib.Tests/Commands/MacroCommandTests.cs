using System.Reflection;
using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class MacroCommandTests
{
    [Fact]
    public void MacroCommand_ExecuteAllCommands()
    {
        // Arrange
        var cmd1 = new Mock<ICommand>();
        var cmd2 = new Mock<ICommand>();
        var macro = new MacroCommand(new ICommand[] { cmd1.Object, cmd2.Object });

        // Act
        macro.Execute();

        // Assert
        cmd1.Verify(c => c.Execute(), Times.Once());
        cmd2.Verify(c => c.Execute(), Times.Once());
    }

    [Fact]
    public void MacroCommand_ThrowsIfOneFails_RemainingNotExecuted()
    {
        // Arrange
        var cmd1 = new Mock<ICommand>();
        var cmd2 = new Mock<ICommand>();
        cmd2.Setup(c => c.Execute()).Throws<InvalidOperationException>();
        var cmd3 = new Mock<ICommand>();

        var macro = new MacroCommand(new ICommand[] { cmd1.Object, cmd2.Object, cmd3.Object });

        // Act
        Assert.Throws<InvalidOperationException>(() => macro.Execute());

        // Assert
        cmd1.Verify(c => c.Execute(), Times.Once());
        cmd2.Verify(c => c.Execute(), Times.Once());
        cmd3.Verify(c => c.Execute(), Times.Never());
    }
}
