using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class CheckCollisionCommandTests
{
    [Fact]
    public void Constructor_NullArguments_ThrowsArgumentNullException()
    {
        var obj = new Mock<IMovingObject>().Object;
        var grid = new Mock<ISpatialGrid>().Object;
        var checker = new Mock<ICollisionChecker>().Object;

        Assert.Throws<ArgumentNullException>(() => new CheckCollisionCommand(null!, grid, checker));
        Assert.Throws<ArgumentNullException>(() => new CheckCollisionCommand(obj, null!, checker));
        Assert.Throws<ArgumentNullException>(() => new CheckCollisionCommand(obj, grid, null!));
    }

    [Fact]
    public void Execute_NoCollision_DoesNotResolveOrExecuteHandler()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        var obj = new Mock<IMovingObject>().Object;
        var candidate = new Mock<IMovingObject>().Object;

        var gridMock = new Mock<ISpatialGrid>();
        gridMock.Setup(g => g.GetCandidates(obj)).Returns(new List<IMovingObject> { candidate });

        var checkerMock = new Mock<ICollisionChecker>();
        checkerMock.Setup(c => c.HaveCollided(obj, candidate)).Returns(false);

        var handlerCommandMock = new Mock<SpaceBattle.Lib.ICommand>();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.CollisionHandler",
            (object[] args) => handlerCommandMock.Object
        ).Execute();

        var cmd = new CheckCollisionCommand(obj, gridMock.Object, checkerMock.Object);

        cmd.Execute();

        handlerCommandMock.Verify(h => h.Execute(), Times.Never);
    }

    [Fact]
    public void Execute_CollisionDetected_ResolvesAndExecutesIoCCollisionHandler()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        var obj = new Mock<IMovingObject>().Object;
        var candidate = new Mock<IMovingObject>().Object;

        var gridMock = new Mock<ISpatialGrid>();
        gridMock.Setup(g => g.GetCandidates(obj)).Returns(new List<IMovingObject> { candidate });

        var checkerMock = new Mock<ICollisionChecker>();
        checkerMock.Setup(c => c.HaveCollided(obj, candidate)).Returns(true);

        var handlerCommandMock = new Mock<SpaceBattle.Lib.ICommand>();
        handlerCommandMock.Setup(h => h.Execute()).Verifiable();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.CollisionHandler",
            (object[] args) => handlerCommandMock.Object
        ).Execute();

        var cmd = new CheckCollisionCommand(obj, gridMock.Object, checkerMock.Object);

        cmd.Execute();

        handlerCommandMock.Verify(h => h.Execute(), Times.Once);
    }
}
