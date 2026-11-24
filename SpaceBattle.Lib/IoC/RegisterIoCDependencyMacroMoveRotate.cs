using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyMacroMoveRotate : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Macro.Move",
            (object[] args) => new CreateMacroCommandStrategy("Move").Resolve(args)
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Macro.Rotate",
            (object[] args) => new CreateMacroCommandStrategy("Rotate").Resolve(args)
        ).Execute();
    }
}
