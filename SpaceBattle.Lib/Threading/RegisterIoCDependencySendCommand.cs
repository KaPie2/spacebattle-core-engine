using App;
using App.Scopes;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencySendCommand : ICommand
{
    public void Execute()
    {
        var registerDependencyCommand = Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.Send",
            (object[] args) =>
            {
                var command = (ICommand)args[0];
                var receiver = (ICommandReceiver)args[1];
                return new SendCommand(command, receiver);
            }
        );

        registerDependencyCommand.Execute();
    }
}
