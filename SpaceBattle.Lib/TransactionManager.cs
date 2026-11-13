namespace SpaceBattle.Lib;

public class TransactionManager : ITransactionManager
{
    private IDictionary<string, object>? _target;
    private readonly Dictionary<string, object> _pendingUpdates = new();
    private bool _inTransaction;

    public bool InTransaction => _inTransaction;
    public IDictionary<string, object>? Target => _target;
    public IReadOnlyDictionary<string, object> PendingUpdates => _pendingUpdates;

    public void BeginTransaction(IDictionary<string, object> obj)
    {
        _target = obj ?? throw new ArgumentNullException(nameof(obj));
        _pendingUpdates.Clear();
        _inTransaction = true;
    }

    public void SetProperty(string key, object value)
    {
        if (!_inTransaction)
        {
            throw new InvalidOperationException("Транзакция не открыта.");
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _pendingUpdates[key] = value;
    }

    public T GetProperty<T>(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (_inTransaction && _pendingUpdates.TryGetValue(key, out var pendingVal))
        {
            return (T)pendingVal;
        }

        if (_target != null && _target.TryGetValue(key, out var actualVal))
        {
            return (T)actualVal;
        }

        throw new KeyNotFoundException($"Свойство {key} не найдено.");
    }

    public void Commit()
    {
        if (!_inTransaction || _target == null)
        {
            return;
        }

        foreach (var kvp in _pendingUpdates)
        {
            _target[kvp.Key] = kvp.Value;
        }

        _pendingUpdates.Clear();
        _inTransaction = false;
    }

    public void Rollback()
    {
        _pendingUpdates.Clear();
        _inTransaction = false;
    }
}
