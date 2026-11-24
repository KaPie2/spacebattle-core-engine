using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyMacroCommand : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.Macro",
            (object[] args) => new MacroCommand((ICommand[])args[0])
        ).Execute();
    }
}
