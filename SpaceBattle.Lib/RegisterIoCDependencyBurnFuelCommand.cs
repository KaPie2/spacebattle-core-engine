using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyBurnFuelCommand : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.BurnFuel",
            (object[] args) =>
            {
                var fuelableObject = (IDictionary<string, object>)args[0];
                var adapter = Ioc.Resolve<IFuelable>("Adapters.IFuelable", fuelableObject);
                return new BurnFuelCommand(adapter);
            }
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.CheckFuel",
            (object[] args) =>
            {
                var fuelableObject = (IDictionary<string, object>)args[0];
                var adapter = Ioc.Resolve<IFuelable>("Adapters.IFuelable", fuelableObject);
                return new CheckFuelCommand(adapter);
            }
        ).Execute();
    }
}
