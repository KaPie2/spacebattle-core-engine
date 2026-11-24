using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyActionsStart : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Actions.Start",
            (object[] args) =>
            {
                var order = (IDictionary<string, object>)args[0];
                var operation = (string)order["operation"];
                var obj = order["obj"];

                var receiver = Ioc.Resolve<ICommandReceiver>("Game.CommandReceiver");

                var command = Ioc.Resolve<ICommand>($"Commands.{operation}", obj);

                return Ioc.Resolve<ICommand>("Commands.Send", command, receiver);
            }
        ).Execute();
    }
}
