using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class EventTests
{
    [Fact]
    public void PositionChangedEvent_InitializesPropertiesCorrectly()
    {
        var shipId = "ship-1";
        var oldPos = new NVector(1, 2);
        var newPos = new NVector(3, 4);

        var evt = new PositionChangedEvent(shipId, oldPos, newPos);

        Assert.Equal(shipId, evt.ShipId);
        Assert.Equal(oldPos, evt.OldPosition);
        Assert.Equal(newPos, evt.NewPosition);
        Assert.True(evt.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void PositionChangedEvent_ThrowsArgumentNullException_OnNullArguments()
    {
        var pos = new NVector(1, 1);
        Assert.Throws<ArgumentNullException>(() => new PositionChangedEvent(null!, pos, pos));
        Assert.Throws<ArgumentNullException>(() => new PositionChangedEvent("ship-1", null!, pos));
        Assert.Throws<ArgumentNullException>(() => new PositionChangedEvent("ship-1", pos, null!));
    }

    [Fact]
    public void FuelBurnedEvent_InitializesPropertiesCorrectly()
    {
        var shipId = "ship-1";
        var evt = new FuelBurnedEvent(shipId, 85, 15);

        Assert.Equal(shipId, evt.ShipId);
        Assert.Equal(85, evt.RemainingFuel);
        Assert.Equal(15, evt.BurnedAmount);
        Assert.True(evt.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void FuelBurnedEvent_ThrowsArgumentNullException_WhenShipIdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FuelBurnedEvent(null!, 50, 10));
    }
}
