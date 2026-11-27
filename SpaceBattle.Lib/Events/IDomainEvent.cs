namespace SpaceBattle.Lib;

public interface IDomainEvent
{
    DateTime Timestamp { get; }
}
