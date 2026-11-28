using App;
using App.Scopes;

namespace SpaceBattle.Lib;

public class RegisterDependencyCommandInjectableCommand : ICommand
{
    public void Execute()
    {
        var registerCommand = Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.CommandInjectable",
            (object[] args) =>
            {
                return new CommandInjectableCommand();
            }
        );

        registerCommand.Execute();
    }
}
