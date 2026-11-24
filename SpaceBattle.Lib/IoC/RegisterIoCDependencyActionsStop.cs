using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyActionsStop : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Actions.Stop",
            (object[] args) =>
            {
                var order = (IDictionary<string, object>)args[0];

                var injectable = (ICommandInjectable)order["obj"];

                return new StopCommand(injectable);
            }
        ).Execute();
    }
}
