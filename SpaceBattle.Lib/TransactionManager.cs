using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib;

public class TransactionManager : ITransactionManager
{
    public const int DefaultMaxLogEntries = 10_000;
    private static readonly object RemovedSentinel = new();

    private sealed class TransactionFrame
    {
        public long TransactionId { get; }
        public Dictionary<IUObject, Dictionary<string, object?>> Changes { get; } =
            new(ReferenceEqualityComparer.Instance);
        public List<TransactionLogEntry> Entries { get; } = new();

        public TransactionFrame(long transactionId)
        {
            TransactionId = transactionId;
        }
    }

    private readonly Stack<TransactionFrame> _frames = new();
    private readonly List<TransactionLogEntry> _log = new();
    private readonly int _maxLogEntries;
    private long _nextTransactionId;

    public TransactionManager(int maxLogEntries = DefaultMaxLogEntries)
    {
        if (maxLogEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLogEntries), "Max log entries must be positive.");
        }

        _maxLogEntries = maxLogEntries;
    }

    public bool IsActive => _frames.Count > 0;
    public long CurrentTransactionId => _frames.Count > 0 ? _frames.Peek().TransactionId : 0;
    public IReadOnlyList<TransactionLogEntry> Log => _log;

    public void BeginTransaction()
    {
        _nextTransactionId++;
        _frames.Push(new TransactionFrame(_nextTransactionId));
    }

    public void Commit()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        var frame = _frames.Pop();

        if (_frames.Count > 0)
        {
            var parent = _frames.Peek();
            foreach (var (target, changes) in frame.Changes)
            {
                if (!parent.Changes.TryGetValue(target, out var parentChanges))
                {
                    parentChanges = new Dictionary<string, object?>();
                    parent.Changes[target] = parentChanges;
                }

                foreach (var (key, value) in changes)
                {
                    parentChanges[key] = value;
                }
            }

            parent.Entries.AddRange(frame.Entries);
            return;
        }

        ApplyAtomically(frame);
    }

    public void Rollback()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("No active transaction to roll back.");
        }

        var frame = _frames.Pop();
        foreach (var entry in frame.Entries)
        {
            entry.Status = TransactionStatus.Fail;
        }
    }

    public void ClearLog() => _log.Clear();

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
        RecordPendingChange(target, key, oldValue, value, value);
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

        foreach (var frame in _frames)
        {
            if (frame.Changes.TryGetValue(target, out var changes) &&
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

        RecordPendingChange(target, key, existing, null, RemovedSentinel);
        return true;
    }

    public ICollection<string> GetKeys(IUObject target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        var keys = new HashSet<string>(target.Keys);

        foreach (var frame in _frames.Reverse())
        {
            if (frame.Changes.TryGetValue(target, out var changes))
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
        }

        return keys.ToList();
    }

    private void RecordPendingChange(
        IUObject target,
        string key,
        object? oldValue,
        object? loggedNewValue,
        object? cachedValue)
    {
        var frame = _frames.Peek();

        if (!frame.Changes.TryGetValue(target, out var changes))
        {
            changes = new Dictionary<string, object?>();
            frame.Changes[target] = changes;
        }

        changes[key] = cachedValue;

        var entry = new TransactionLogEntry(
            frame.TransactionId,
            target,
            key,
            oldValue,
            loggedNewValue,
            TransactionStatus.Pending);

        frame.Entries.Add(entry);
        AppendLog(entry);
    }

    private void AppendLog(TransactionLogEntry entry)
    {
        if (_log.Count >= _maxLogEntries)
        {
            _log.RemoveAt(0);
        }

        _log.Add(entry);
    }

    private static void ApplyAtomically(TransactionFrame frame)
    {
        var appliedBackups = new List<(IUObject Target, string Key, bool Existed, object? OriginalValue)>();

        try
        {
            foreach (var (target, changes) in frame.Changes)
            {
                foreach (var (key, value) in changes)
                {
                    bool existed = target.TryGetValue(key, out var originalValue);
                    appliedBackups.Add((target, key, existed, originalValue));

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

            foreach (var entry in frame.Entries)
            {
                entry.Status = TransactionStatus.Success;
            }
        }
        catch
        {
            for (int i = appliedBackups.Count - 1; i >= 0; i--)
            {
                var (target, key, existed, originalValue) = appliedBackups[i];
                if (existed)
                {
                    target.SetProperty(key, originalValue!);
                }
                else
                {
                    target.Remove(key);
                }
            }

            foreach (var entry in frame.Entries)
            {
                entry.Status = TransactionStatus.Fail;
            }

            throw;
        }
    }
}
