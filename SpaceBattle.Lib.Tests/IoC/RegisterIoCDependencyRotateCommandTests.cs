using System.Windows.Input;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyRotateCommandTests
{
    public RegisterIoCDependencyRotateCommandTests()
    {
        new InitCommand().Execute();
    }

    [Fact]
    public void RegisterIoCDependencyRotateCommand_IsResolvingDependency()
    {
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Adapters.IRotatingObject",
            (object[] args) =>
            {
                var dict = (IDictionary<string, object>)args[0];
                var mock = new Mock<IRotatingObject>();

                if (dict.ContainsKey("Angle"))
                {
                    mock.Setup(x => x.Angle).Returns((Angle)dict["Angle"]);
                }
                if (dict.ContainsKey("AngleVelocity"))
                {
                    mock.Setup(x => x.AngleVelocity).Returns((Angle)dict["AngleVelocity"]);
                }

                return mock.Object;
            }
        ).Execute();

        new RegisterIoCDependencyRotateCommand().Execute();

        var rotatingObjectDict = new Dictionary<string, object>
        {
            {"Angle", new Angle(new int[]{1, 8})},
            {"AngleVelocity", new Angle(new int[]{1, 8})}
        };

        var cmd = Ioc.Resolve<ICommand>("Commands.RotateCommand", rotatingObjectDict);
        Assert.NotNull(cmd);
    }
}