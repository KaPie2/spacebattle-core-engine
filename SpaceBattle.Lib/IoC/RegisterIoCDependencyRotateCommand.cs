using System.Windows.Input;
using App;
using App.Scopes;



namespace SpaceBattle.Lib;


public class RegisterIoCDependencyRotateCommand : ICommand
{
    public void Execute()
    {
        var registerRotateCommandDependency = Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Commands.RotateCommand",
            (object[] args) =>
            {
                var rotatingObject = (IDictionary<string, object>)args[0];
                var adapter = Ioc.Resolve<IRotatingObject>("Adapters.IRotatingObject", rotatingObject);
                return new RotateCommand(adapter);

            }
        );

        registerRotateCommandDependency.Execute();
    }
}