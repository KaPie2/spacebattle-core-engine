using App;
using Moq;
using App.Scopes;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyMacroMoveRotateTests
{
    public RegisterIoCDependencyMacroMoveRotateTests()
    {
        new InitCommand().Execute();

        var newScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", newScope).Execute();
    }

    [Fact]
    public void RegisterMacroMoveRotate_ResolvesDependency()
    {
        // Arrange
        // регистрируем Specs.Move (список команд для движения)        
        var moveCmd1 = new Mock<ICommand>();
        var moveCmd2 = new Mock<ICommand>();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Specs.Move",
            (object[] args) => new string[] { "MoveCmd1", "MoveCmd2" }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "MoveCmd1",
            (object[] args) => moveCmd1.Object
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "MoveCmd2",
            (object[] args) => moveCmd2.Object
        ).Execute();

        // регистрируем Specs.Rotate (список команд для вращения)
        var rotateCmd1 = new Mock<ICommand>();
        var rotateCmd2 = new Mock<ICommand>();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Specs.Rotate",
            (object[] args) => new string[] { "RotateCmd1", "RotateCmd2" }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "RotateCmd1",
            (object[] args) => rotateCmd1.Object
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "RotateCmd2",
            (object[] args) => rotateCmd2.Object
        ).Execute();

        // Act
        new RegisterIoCDependencyMacroMoveRotate().Execute();

        var macroMove = Ioc.Resolve<ICommand>("Macro.Move");
        var macroRotate = Ioc.Resolve<ICommand>("Macro.Rotate");

        // Assert
        Assert.NotNull(macroMove);
        Assert.NotNull(macroRotate);

        macroMove.Execute();
        macroRotate.Execute();

        moveCmd1.Verify(c => c.Execute(), Times.Once);
        moveCmd2.Verify(c => c.Execute(), Times.Once);
        rotateCmd1.Verify(c => c.Execute(), Times.Once);
        rotateCmd2.Verify(c => c.Execute(), Times.Once);
    }
}
