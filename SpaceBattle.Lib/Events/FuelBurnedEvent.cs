namespace SpaceBattle.Lib;

public class FuelBurnedEvent : IDomainEvent
{
    public object ShipId { get; }
    public int RemainingFuel { get; }
    public int BurnedAmount { get; }
    public DateTime Timestamp { get; }

    public FuelBurnedEvent(object shipId, int remainingFuel, int burnedAmount)
    {
        ShipId = shipId ?? throw new ArgumentNullException(nameof(shipId));
        RemainingFuel = remainingFuel;
        BurnedAmount = burnedAmount;
        Timestamp = DateTime.UtcNow;
    }
}
