namespace SpaceBattle.Lib;

public class TransactionLogEntry
{
    public long TransactionId { get; }
    public IUObject Target { get; }
    public string PropertyKey { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
    public TransactionStatus Status { get; internal set; }

    public TransactionLogEntry(
        long transactionId,
        IUObject target,
        string propertyKey,
        object? oldValue,
        object? newValue,
        TransactionStatus status = TransactionStatus.Pending)
    {
        TransactionId = transactionId;
        Target = target ?? throw new ArgumentNullException(nameof(target));
        PropertyKey = propertyKey ?? throw new ArgumentNullException(nameof(propertyKey));
        OldValue = oldValue;
        NewValue = newValue;
        Status = status;
    }
}
