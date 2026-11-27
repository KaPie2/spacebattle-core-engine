using App;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyShipTests
{
    [Fact]
    public void RegisterIoCDependencyShip_RegistersAllStrategiesSuccessfully()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        new RegisterIoCDependencyShip().Execute();

        // Ship.Create
        var ship = Ioc.Resolve<Ship>(
            "Ship.Create",
            "ship-ioc",
            new NVector(1, 2),
            new NVector(3, 4),
            new Angle(new int[] { 0, 360 }),
            100,
            10
        );
        Assert.NotNull(ship);
        Assert.Equal("ship-ioc", ship.Id);

        // Ship.Move
        var moveCmd = Ioc.Resolve<ICommand>("Ship.Move", ship);
        Assert.NotNull(moveCmd);
        moveCmd.Execute();
        Assert.Equal(new NVector(4, 6), ship.Position);

        // Ship.CreateSnapshot
        var snapshot = Ioc.Resolve<ShipSnapshot>("Ship.CreateSnapshot", ship);
        Assert.NotNull(snapshot);
        Assert.Equal("ship-ioc", snapshot.ShipId);

        // Ship.FromSnapshot
        var restoredFromSnapshot = Ioc.Resolve<Ship>("Ship.FromSnapshot", snapshot);
        Assert.NotNull(restoredFromSnapshot);
        Assert.Equal(ship.Position, restoredFromSnapshot.Position);

        // Ship.ToJson
        var json = Ioc.Resolve<string>("Ship.ToJson", ship);
        Assert.Contains("ship-ioc", json);

        // Ship.FromJson
        var restoredFromJson = Ioc.Resolve<Ship>("Ship.FromJson", json);
        Assert.NotNull(restoredFromJson);
        Assert.Equal(ship.Position, restoredFromJson.Position);
    }
}
