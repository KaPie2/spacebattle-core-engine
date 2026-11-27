using Moq;
using App;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyMacroCommandTests
{
    public RegisterIoCDependencyMacroCommandTests()
    {
        new InitCommand().Execute();
    }

    [Fact]
    public void RegisterMacroCommand_ResolvesDependency()
    {
        // Arrange
        var cmd1 = new Mock<ICommand>();
        var cmd2 = new Mock<ICommand>();
        var commands = new ICommand[] { cmd1.Object, cmd2.Object };

        // Act
        var registerCommand = new RegisterIoCDependencyMacroCommand();
        registerCommand.Execute();
        var macro = Ioc.Resolve<ICommand>("Commands.Macro", (object)commands);

        // Assert
        Assert.NotNull(macro);
        macro.Execute();

        cmd1.Verify(c => c.Execute(), Times.Once);
        cmd2.Verify(c => c.Execute(), Times.Once);
    }
}
