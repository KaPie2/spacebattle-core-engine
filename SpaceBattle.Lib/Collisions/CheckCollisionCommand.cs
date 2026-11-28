using App;

namespace SpaceBattle.Lib;

public class CheckCollisionCommand : ICommand
{
    private readonly IMovingObject _obj;
    private readonly ISpatialGrid _grid;
    private readonly ICollisionChecker _checker;

    public CheckCollisionCommand(
        IMovingObject obj,
        ISpatialGrid grid,
        ICollisionChecker checker)
    {
        _obj = obj ?? throw new ArgumentNullException(nameof(obj));
        _grid = grid ?? throw new ArgumentNullException(nameof(grid));
        _checker = checker ?? throw new ArgumentNullException(nameof(checker));
    }

    public void Execute()
    {
        var candidates = _grid.GetCandidates(_obj);
        foreach (var candidate in candidates)
        {
            if (_checker.HaveCollided(_obj, candidate))
            {
                Ioc.Resolve<ICommand>("Commands.CollisionHandler", _obj, candidate).Execute();
            }
        }
    }
}
