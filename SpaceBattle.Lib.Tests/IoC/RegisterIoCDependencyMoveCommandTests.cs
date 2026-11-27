using System.Windows.Input;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyMoveCommandTests
{
    public RegisterIoCDependencyMoveCommandTests()
    {
        new InitCommand().Execute();
    }

    [Fact]
    public void RegisterIoCDependencyMoveCommand_IsResolvingDependency()
    {
        Ioc.Resolve<App.ICommand>(
        "IoC.Register",
        "Adapters.IMovingObject",
        (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];

            var mock = new Mock<IMovingObject>();
            if (dict.ContainsKey("Position"))
                mock.Setup(m => m.Position).Returns((NVector)dict["Position"]);
            if (dict.ContainsKey("Velocity"))
                mock.Setup(m => m.Velocity).Returns((NVector)dict["Velocity"]);

            return mock.Object;
        }
        ).Execute();

        new RegisterIoCDependencyMoveCommand().Execute();

        var movingObjectDict = new Dictionary<string, object>
        {
            { "Position", new NVector(new int[]{0, 0}) },
            { "Velocity", new NVector(new int[]{1, 1}) }
        };

        var cmd = Ioc.Resolve<ICommand>("Commands.MoveCommand", movingObjectDict);

        Assert.NotNull(cmd);
        cmd.Execute();
    }
}