using Xunit;

namespace SpaceBattle.Lib.Tests;

public class MoveShipCommandTests
{
    [Fact]
    public void MoveShipCommand_ThrowsArgumentNullException_WhenShipIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new MoveShipCommand(null!));
    }

    [Fact]
    public void MoveShipCommand_ExecutesShipMove()
    {
        var ship = new Ship(
            "ship-cmd",
            new NVector(0, 0),
            new NVector(5, 5),
            new Angle(new int[] { 0, 360 }),
            100,
            20
        );

        var cmd = new MoveShipCommand(ship);
        cmd.Execute();

        Assert.Equal(new NVector(5, 5), ship.Position);
        Assert.Equal(80, ship.FuelLevel);
        Assert.Equal(2, ship.DomainEvents.Count);
    }
}
