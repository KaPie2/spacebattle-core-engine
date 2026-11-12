using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib;

public class TransactionManager : ITransactionManager
{
    private static readonly object RemovedSentinel = new();

    private readonly Dictionary<IUObject, Dictionary<string, object?>> _pendingChanges =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<TransactionLogEntry> _currentEntries = new();
    private readonly List<TransactionLogEntry> _log = new();

    public bool IsActive { get; private set; }
    public long CurrentTransactionId { get; private set; }
    public IReadOnlyList<TransactionLogEntry> Log => _log;

    public void BeginTransaction()
    {
        if (IsActive)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        CurrentTransactionId++;
        IsActive = true;
    }

    public void Commit()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        foreach (var (target, changes) in _pendingChanges)
        {
            foreach (var (key, value) in changes)
            {
                if (ReferenceEquals(value, RemovedSentinel))
                {
                    target.Remove(key);
                }
                else
                {
                    target.SetProperty(key, value!);
                }
            }
        }

        foreach (var entry in _currentEntries)
        {
            entry.Status = TransactionStatus.Success;
        }

        ResetCurrentTransaction();
    }

    public void Rollback()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }

        foreach (var entry in _currentEntries)
        {
            entry.Status = TransactionStatus.Fail;
        }

        ResetCurrentTransaction();
    }

    public IUObject CreateTransactionalObject(IUObject target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        return new TransactionalUObject(target, this);
    }

    public object GetValue(IUObject target, string key)
    {
        if (!TryGetValue(target, key, out var value))
        {
            throw new KeyNotFoundException($"The given key '{key}' was not present.");
        }

        return value;
    }

    public void SetValue(IUObject target, string key, object value)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (!IsActive)
        {
            target.SetProperty(key, value);
            return;
        }

        object? oldValue = TryGetValue(target, key, out var existing) ? existing : null;

        if (!_pendingChanges.TryGetValue(target, out var changes))
        {
            changes = new Dictionary<string, object?>();
            _pendingChanges[target] = changes;
        }

        changes[key] = value;

        var entry = new TransactionLogEntry(CurrentTransactionId, target, key, oldValue, value, TransactionStatus.Pending);
        _currentEntries.Add(entry);
        _log.Add(entry);
    }

    public bool TryGetValue(IUObject target, string key, [MaybeNullWhen(false)] out object value)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (IsActive && _pendingChanges.TryGetValue(target, out var changes) &&
            changes.TryGetValue(key, out var cachedValue))
        {
            if (ReferenceEquals(cachedValue, RemovedSentinel))
            {
                value = default;
                return false;
            }

            value = cachedValue!;
            return true;
        }

        return target.TryGetValue(key, out value);
    }

    public bool ContainsKey(IUObject target, string key)
    {
        return TryGetValue(target, key, out _);
    }

    public bool RemoveKey(IUObject target, string key)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!IsActive)
        {
            return target.Remove(key);
        }

        if (!TryGetValue(target, key, out var existing))
        {
            return false;
        }

        if (!_pendingChanges.TryGetValue(target, out var changes))
        {
            changes = new Dictionary<string, object?>();
            _pendingChanges[target] = changes;
        }

        changes[key] = RemovedSentinel;

        var entry = new TransactionLogEntry(CurrentTransactionId, target, key, existing, null, TransactionStatus.Pending);
        _currentEntries.Add(entry);
        _log.Add(entry);
        return true;
    }

    public ICollection<string> GetKeys(IUObject target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var keys = new HashSet<string>(target.Keys);

        if (IsActive && _pendingChanges.TryGetValue(target, out var changes))
        {
            foreach (var (key, value) in changes)
            {
                if (ReferenceEquals(value, RemovedSentinel))
                {
                    keys.Remove(key);
                }
                else
                {
                    keys.Add(key);
                }
            }
        }

        return keys.ToList();
    }

    private void ResetCurrentTransaction()
    {
        _pendingChanges.Clear();
        _currentEntries.Clear();
        IsActive = false;
    }
}
