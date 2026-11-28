using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyCollisionTests
{
    [Fact]
    public void RegisterDependencies_ResolvesSuccessfully()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        new RegisterIoCDependencyCollision().Execute();

        var checker = Ioc.Resolve<ICollisionChecker>("Collision.Checker", 2.5);
        Assert.NotNull(checker);

        var defaultChecker = Ioc.Resolve<ICollisionChecker>("Collision.Checker");
        Assert.NotNull(defaultChecker);

        var grid = Ioc.Resolve<ISpatialGrid>("Collision.Grid", 100);
        Assert.NotNull(grid);
        Assert.Equal(100, grid.CellSize);

        var defaultGrid = Ioc.Resolve<ISpatialGrid>("Collision.Grid");
        Assert.NotNull(defaultGrid);
        Assert.Equal(SpatialGrid.DefaultCellSize, defaultGrid.CellSize);

        var objMock = new Mock<IMovingObject>();

        var cmd = Ioc.Resolve<SpaceBattle.Lib.ICommand>(
            "Commands.CheckCollision",
            objMock.Object,
            grid,
            checker
        );

        Assert.NotNull(cmd);
        Assert.IsType<CheckCollisionCommand>(cmd);
    }
}
