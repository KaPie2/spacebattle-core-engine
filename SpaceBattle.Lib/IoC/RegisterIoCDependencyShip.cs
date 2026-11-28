using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyShip : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.Create",
            (object[] args) => new Ship(
                args[0],
                (NVector)args[1],
                (NVector)args[2],
                (Angle)args[3],
                (int)args[4],
                (int)args[5],
                args.Length > 6 ? (Angle?)args[6] : null
            )
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.Move",
            (object[] args) => new MoveShipCommand((Ship)args[0])
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.CreateSnapshot",
            (object[] args) => ((Ship)args[0]).CreateSnapshot()
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.FromSnapshot",
            (object[] args) => Ship.FromSnapshot((ShipSnapshot)args[0])
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.ToJson",
            (object[] args) => ((Ship)args[0]).ToJson()
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Ship.FromJson",
            (object[] args) => Ship.FromJson((string)args[0])
        ).Execute();
    }
}
