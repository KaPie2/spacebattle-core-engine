using App;
using Moq;
using Xunit;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class CreateMacroCommandStrategyTests
{
    public CreateMacroCommandStrategyTests()
    {
        new InitCommand().Execute();
    }

    [Fact]
    public void Strategy_ResolvesMacroCommand_And_ExecuteAllCommands()
    {
        // Arrange
        var specName = "Test_" + Guid.NewGuid().ToString("N");
        var cmd1Name = "Cmd1_" + Guid.NewGuid().ToString("N");
        var cmd2Name = "Cmd2_" + Guid.NewGuid().ToString("N");

        var cmd1 = new Mock<ICommand>();
        var cmd2 = new Mock<ICommand>();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Specs." + specName,
            (object[] args) => new string[] { cmd1Name, cmd2Name }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            cmd1Name,
            (object[] args) => cmd1.Object
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            cmd2Name,
            (object[] args) => cmd2.Object
        ).Execute();

        // Act
        var strategy = new CreateMacroCommandStrategy(specName);
        var macro = strategy.Resolve(new object[] { });

        // Assert
        Assert.NotNull(macro);
        macro.Execute();

        cmd1.Verify(c => c.Execute(), Times.Once);
        cmd2.Verify(c => c.Execute(), Times.Once);
    }

    [Fact]
    public void StrategyThrows_IfSpecsDependencyNotFound()
    {
        // Arrange
        var specName = "NotFound__" + Guid.NewGuid().ToString("N");
        var strategy = new CreateMacroCommandStrategy(specName);

        // Act + Assert
        Assert.Throws<Exception>(() => strategy.Resolve(new object[] { }));
    }

    [Fact]
    public void StrategyThrows_IfCommandDependencyNotFound()
    {
        // Arrange
        var specName = "NotFound_" + Guid.NewGuid().ToString("N");
        var badCmdName = "BadCmd_" + Guid.NewGuid().ToString("N");

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Specs." + specName,
            (object[] args) => new string[] { badCmdName }
        ).Execute();

        var strategy = new CreateMacroCommandStrategy(specName);

        // Act + Assert
        Assert.Throws<Exception>(() => strategy.Resolve(new object[] { }));
    }
}
