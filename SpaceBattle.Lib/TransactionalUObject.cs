using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib;

public class TransactionalUObject : IUObject
{
    private readonly IUObject _target;
    private readonly ITransactionManager _manager;

    public TransactionalUObject(IUObject target, ITransactionManager manager)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    public object GetProperty(string key) => _manager.GetValue(_target, key);

    public void SetProperty(string key, object value) => _manager.SetValue(_target, key, value);

    public object this[string key]
    {
        get => GetProperty(key);
        set => SetProperty(key, value);
    }

    public ICollection<string> Keys => _manager.GetKeys(_target);

    public ICollection<object> Values => Keys.Select(GetProperty).ToList();

    public int Count => Keys.Count;

    public bool IsReadOnly => _target.IsReadOnly;

    public void Add(string key, object value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (ContainsKey(key))
        {
            throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
        }

        SetProperty(key, value);
    }

    public void Add(KeyValuePair<string, object> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        foreach (var key in Keys.ToList())
        {
            Remove(key);
        }
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
        return TryGetValue(item.Key, out var value) && Equals(value, item.Value);
    }

    public bool ContainsKey(string key) => _manager.ContainsKey(_target, key);

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        int index = arrayIndex;
        foreach (var key in Keys)
        {
            array[index++] = new KeyValuePair<string, object>(key, GetProperty(key));
        }
    }

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
        foreach (var key in Keys)
        {
            yield return new KeyValuePair<string, object>(key, GetProperty(key));
        }
    }

    public bool Remove(string key) => _manager.RemoveKey(_target, key);

    public bool Remove(KeyValuePair<string, object> item)
    {
        return Contains(item) && Remove(item.Key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
    {
        return _manager.TryGetValue(_target, key, out value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
