using App;

namespace SpaceBattle.Lib;

public class RegisterIoCDependencyThreadStopCommands : ICommand
{
    public void Execute()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.HardStop",
            (object[] args) => new HardStopCommand((IGameThread)args[0])
        ).Execute();

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.SoftStop",
            (object[] args) => new SoftStopCommand((IGameThread)args[0])
        ).Execute();
    }
}
