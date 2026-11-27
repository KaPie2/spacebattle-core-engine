using Xunit;

namespace SpaceBattle.Lib.Tests;

public class ShipTests
{
    [Fact]
    public void Ship_Constructor_InitializesPropertiesCorrectly()
    {
        var pos = new NVector(10, 20);
        var vel = new NVector(1, 2);
        var dir = new Angle(new int[] { 45, 360 });
        var ship = new Ship("ship-42", pos, vel, dir, 100, 10);

        Assert.Equal("ship-42", ship.Id);
        Assert.Equal(pos, ship.Position);
        Assert.Equal(vel, ship.Velocity);
        Assert.Equal(dir, ship.Angle);
        Assert.Equal(100, ship.FuelLevel);
        Assert.Equal(10, ship.FuelConsumption);
        Assert.Empty(ship.DomainEvents);
    }

    [Fact]
    public void Ship_Constructor_ThrowsOnNullOrNegativeArguments()
    {
        var pos = new NVector(10, 20);
        var vel = new NVector(1, 2);
        var dir = new Angle(new int[] { 45, 360 });

        Assert.Throws<ArgumentNullException>(() => new Ship(null!, pos, vel, dir, 100, 10));
        Assert.Throws<ArgumentNullException>(() => new Ship("ship-1", null!, vel, dir, 100, 10));
        Assert.Throws<ArgumentNullException>(() => new Ship("ship-1", pos, null!, dir, 100, 10));
        Assert.Throws<ArgumentNullException>(() => new Ship("ship-1", pos, vel, null!, 100, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ship("ship-1", pos, vel, dir, -1, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Ship("ship-1", pos, vel, dir, 100, -1));
    }

    [Fact]
    public void Ship_Setters_ValidateInvariants()
    {
        var ship = new Ship("ship-1", new NVector(0, 0), new NVector(1, 1), new Angle(new int[] { 0, 360 }), 50, 5);

        ship.Position = new NVector(5, 5);
        Assert.Equal(new NVector(5, 5), ship.Position);

        ship.Velocity = new NVector(2, 2);
        Assert.Equal(new NVector(2, 2), ship.Velocity);

        ship.Angle = new Angle(new int[] { 90, 360 });
        Assert.Equal(new Angle(new int[] { 90, 360 }), ship.Angle);

        ship.AngleVelocity = new Angle(new int[] { 10, 360 });
        Assert.Equal(new Angle(new int[] { 10, 360 }), ship.AngleVelocity);

        ship.FuelLevel = 30;
        Assert.Equal(30, ship.FuelLevel);

        ship.FuelConsumption = 8;
        Assert.Equal(8, ship.FuelConsumption);

        Assert.Throws<ArgumentNullException>(() => ship.Position = null!);
        Assert.Throws<ArgumentNullException>(() => ship.Velocity = null!);
        Assert.Throws<ArgumentNullException>(() => ship.Angle = null!);
        Assert.Throws<ArgumentNullException>(() => ship.AngleVelocity = null!);
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.FuelLevel = -5);
        Assert.Throws<ArgumentOutOfRangeException>(() => ship.FuelConsumption = -2);
    }

    [Fact]
    public void Ship_Move_SufficientFuel_UpdatesStateAndEmitsEvents()
    {
        var initialPos = new NVector(10, 20);
        var vel = new NVector(3, 4);
        var ship = new Ship("ship-alpha", initialPos, vel, new Angle(new int[] { 0, 360 }), 50, 15);

        ship.Move();

        var expectedPos = new NVector(13, 24);
        Assert.Equal(expectedPos, ship.Position);
        Assert.Equal(35, ship.FuelLevel);

        Assert.Equal(2, ship.DomainEvents.Count);

        var posEvent = Assert.IsType<PositionChangedEvent>(ship.DomainEvents[0]);
        Assert.Equal("ship-alpha", posEvent.ShipId);
        Assert.Equal(initialPos, posEvent.OldPosition);
        Assert.Equal(expectedPos, posEvent.NewPosition);

        var fuelEvent = Assert.IsType<FuelBurnedEvent>(ship.DomainEvents[1]);
        Assert.Equal("ship-alpha", fuelEvent.ShipId);
        Assert.Equal(35, fuelEvent.RemainingFuel);
        Assert.Equal(15, fuelEvent.BurnedAmount);

        ship.ClearDomainEvents();
        Assert.Empty(ship.DomainEvents);
    }

    [Fact]
    public void Ship_Move_InsufficientFuel_ThrowsExceptionAndStateUnchanged()
    {
        var initialPos = new NVector(10, 20);
        var vel = new NVector(3, 4);
        var ship = new Ship("ship-beta", initialPos, vel, new Angle(new int[] { 0, 360 }), 5, 10);

        var ex = Assert.Throws<CommandException>(() => ship.Move());
        Assert.Contains("Недостаточно топлива", ex.Message);

        Assert.Equal(initialPos, ship.Position);
        Assert.Equal(5, ship.FuelLevel);
        Assert.Empty(ship.DomainEvents);
    }

    [Fact]
    public void Ship_SnapshotAndJson_SerializesAndRestoresCorrectly()
    {
        var ship = new Ship(
            "ship-gamma",
            new NVector(100, 200),
            new NVector(5, -5),
            new Angle(new int[] { 90, 360 }),
            75,
            12
        );

        var snapshot = ship.CreateSnapshot();
        Assert.Equal("ship-gamma", snapshot.ShipId);
        Assert.Equal(new int[] { 100, 200 }, snapshot.Position);
        Assert.Equal(new int[] { 5, -5 }, snapshot.Velocity);
        Assert.Equal(new int[] { 90, 360 }, snapshot.Direction);
        Assert.Equal(75, snapshot.FuelLevel);
        Assert.Equal(12, snapshot.FuelConsumption);

        var json = ship.ToJson();
        Assert.False(string.IsNullOrWhiteSpace(json));

        var restored = Ship.FromJson(json);
        Assert.Equal(ship.Id, restored.Id);
        Assert.Equal(ship.Position, restored.Position);
        Assert.Equal(ship.Velocity, restored.Velocity);
        Assert.Equal(ship.Angle, restored.Angle);
        Assert.Equal(ship.FuelLevel, restored.FuelLevel);
        Assert.Equal(ship.FuelConsumption, restored.FuelConsumption);

        // Next command on restored ship
        restored.Move();
        Assert.Equal(new NVector(105, 195), restored.Position);
        Assert.Equal(63, restored.FuelLevel);
        Assert.NotEmpty(restored.DomainEvents);
    }

    [Fact]
    public void Ship_FromJson_ThrowsOnInvalidJson()
    {
        Assert.Throws<ArgumentException>(() => Ship.FromJson(""));
        Assert.Throws<ArgumentException>(() => Ship.FromJson("   "));
        Assert.Throws<ArgumentNullException>(() => Ship.FromSnapshot(null!));
    }

    [Fact]
    public void Ship_IUObject_MethodsDelegateCorrectly()
    {
        var ship = new Ship(
            "ship-delta",
            new NVector(1, 2),
            new NVector(0, 1),
            new Angle(new int[] { 0, 360 }),
            40,
            5
        );

        Assert.Equal("ship-delta", ship.GetProperty("Id"));
        ship.SetProperty("CustomProp", "CustomValue");
        Assert.Equal("CustomValue", ship["CustomProp"]);

        Assert.True(ship.ContainsKey("Position"));
        Assert.True(ship.TryGetValue("FuelLevel", out var fuelVal));
        Assert.Equal(40, (int)fuelVal!);

        Assert.NotEmpty(ship.Keys);
        Assert.NotEmpty(ship.Values);
        Assert.True(ship.Count > 0);
        Assert.False(ship.IsReadOnly);

        ship.Add("Temp", 123);
        Assert.True(ship.ContainsKey("Temp"));
        Assert.True(ship.Remove("Temp"));

        ship.Add(new KeyValuePair<string, object>("PairKey", "PairVal"));
        Assert.Contains(new KeyValuePair<string, object>("PairKey", "PairVal"), ship);
        Assert.True(ship.Remove(new KeyValuePair<string, object>("PairKey", "PairVal")));

        var array = new KeyValuePair<string, object>[ship.Count];
        ship.CopyTo(array, 0);
        Assert.Equal(ship.Count, array.Length);

        var count = 0;
        foreach (var kvp in ship)
        {
            count++;
        }
        Assert.Equal(ship.Count, count);

        var count2 = 0;
        var enumerator = ((System.Collections.IEnumerable)ship).GetEnumerator();
        while (enumerator.MoveNext())
        {
            count2++;
        }
        Assert.Equal(ship.Count, count2);
    }

    [Fact]
    public void Ship_TransactionManager_AtomicCommitAndRollback()
    {
        var ship = new Ship(
            "ship-tx",
            new NVector(10, 10),
            new NVector(2, 2),
            new Angle(new int[] { 0, 360 }),
            30,
            10
        );

        var tx = new TransactionManager();
        tx.BeginTransaction(ship);

        // Transactional property modifications
        tx.SetProperty("Position", new NVector(12, 12));
        tx.SetProperty("FuelLevel", 20);

        // Before commit, actual ship is unchanged
        Assert.Equal(new NVector(10, 10), ship.Position);
        Assert.Equal(30, ship.FuelLevel);

        // Commit transaction
        tx.Commit();
        Assert.Equal(new NVector(12, 12), ship.Position);
        Assert.Equal(20, ship.FuelLevel);

        // Rollback test
        tx.BeginTransaction(ship);
        tx.SetProperty("Position", new NVector(99, 99));
        tx.Rollback();
        Assert.Equal(new NVector(12, 12), ship.Position);
    }
}
