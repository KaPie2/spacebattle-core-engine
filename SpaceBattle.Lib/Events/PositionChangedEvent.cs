namespace SpaceBattle.Lib;

public class PositionChangedEvent : IDomainEvent
{
    public object ShipId { get; }
    public NVector OldPosition { get; }
    public NVector NewPosition { get; }
    public DateTime Timestamp { get; }

    public PositionChangedEvent(object shipId, NVector oldPosition, NVector newPosition)
    {
        ShipId = shipId ?? throw new ArgumentNullException(nameof(shipId));
        OldPosition = oldPosition ?? throw new ArgumentNullException(nameof(oldPosition));
        NewPosition = newPosition ?? throw new ArgumentNullException(nameof(newPosition));
        Timestamp = DateTime.UtcNow;
    }
}
