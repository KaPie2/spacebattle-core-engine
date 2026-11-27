using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyCollision : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Collision.Checker",
            (object[] args) =>
            {
                var distance = args.Length > 0 ? Convert.ToDouble(args[0]) : CollisionChecker.DefaultCollisionDistance;
                return new CollisionChecker(distance);
            }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Collision.Grid",
            (object[] args) =>
            {
                var cellSize = args.Length > 0 ? Convert.ToInt32(args[0]) : SpatialGrid.DefaultCellSize;
                return new SpatialGrid(cellSize);
            }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.CheckCollision",
            (object[] args) =>
            {
                var obj = (IMovingObject)args[0];
                var grid = (ISpatialGrid)args[1];
                var checker = (ICollisionChecker)args[2];

                return new CheckCollisionCommand(obj, grid, checker);
            }
        ).Execute();
    }
}
