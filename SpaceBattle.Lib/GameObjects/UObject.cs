using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib;

public class UObject : IUObject
{
    private readonly IDictionary<string, object> _properties;

    public UObject()
    {
        _properties = new Dictionary<string, object>();
    }

    public UObject(IDictionary<string, object> initialProperties)
    {
        if (initialProperties is null)
        {
            throw new ArgumentNullException(nameof(initialProperties));
        }

        _properties = new Dictionary<string, object>(initialProperties);
    }

    public object GetProperty(string key)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return _properties[key];
    }

    public void SetProperty(string key, object value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _properties[key] = value ?? throw new ArgumentNullException(nameof(value));
    }

    public object this[string key]
    {
        get => GetProperty(key);
        set => SetProperty(key, value);
    }

    public ICollection<string> Keys => _properties.Keys;
    public ICollection<object> Values => _properties.Values;
    public int Count => _properties.Count;
    public bool IsReadOnly => _properties.IsReadOnly;

    public void Add(string key, object value)
    {
        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        _properties.Add(key, value ?? throw new ArgumentNullException(nameof(value)));
    }

    public void Add(KeyValuePair<string, object> item)
    {
        Add(item.Key, item.Value);
    }

    public void Clear() => _properties.Clear();

    public bool Contains(KeyValuePair<string, object> item) => _properties.Contains(item);

    public bool ContainsKey(string key) => _properties.ContainsKey(key);

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _properties.CopyTo(array, arrayIndex);

    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _properties.GetEnumerator();

    public bool Remove(string key) => _properties.Remove(key);

    public bool Remove(KeyValuePair<string, object> item) => _properties.Remove(item);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value) => _properties.TryGetValue(key, out value);

    IEnumerator IEnumerable.GetEnumerator() => _properties.GetEnumerator();
}
