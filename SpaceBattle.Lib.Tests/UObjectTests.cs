using System.Collections;

namespace SpaceBattle.Lib.Tests;

public class UObjectTests
{
    [Fact]
    public void Constructor_NullInitialProperties_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UObject(null!));
    }

    [Fact]
    public void Constructor_WithInitialProperties_CopiesValuesWithoutMutatingSource()
    {
        var source = new Dictionary<string, object> { { "Key", 42 } };
        var uobj = new UObject(source);

        uobj["Key"] = 100;

        Assert.Equal(42, source["Key"]);
        Assert.Equal(100, uobj["Key"]);
    }

    [Fact]
    public void GetAndSetProperty_ValidKey_ReadsAndWritesValue()
    {
        var uobj = new UObject();
        uobj.SetProperty("Position", new NVector(1, 2));

        Assert.Equal(new NVector(1, 2), uobj.GetProperty("Position"));
        Assert.Equal(new NVector(1, 2), uobj["Position"]);
    }

    [Fact]
    public void GetAndSetProperty_NullArguments_ThrowsArgumentNullException()
    {
        var uobj = new UObject();

        Assert.Throws<ArgumentNullException>(() => uobj.GetProperty(null!));
        Assert.Throws<ArgumentNullException>(() => uobj.SetProperty(null!, 1));
        Assert.Throws<ArgumentNullException>(() => uobj.SetProperty("Key", null!));
        Assert.Throws<ArgumentNullException>(() => uobj.Add(null!, 1));
        Assert.Throws<ArgumentNullException>(() => uobj.Add("Key", null!));
    }

    [Fact]
    public void GetProperty_MissingKey_ThrowsKeyNotFoundException()
    {
        var uobj = new UObject();

        Assert.Throws<KeyNotFoundException>(() => uobj.GetProperty("Missing"));
    }

    [Fact]
    public void DictionaryOperations_WorkAsExpected()
    {
        var uobj = new UObject();
        uobj.Add("A", 1);
        uobj.Add(new KeyValuePair<string, object>("B", 2));

        Assert.Equal(2, uobj.Count);
        Assert.False(uobj.IsReadOnly);
        Assert.True(uobj.ContainsKey("A"));
        Assert.True(((ICollection<KeyValuePair<string, object>>)uobj).Contains(new KeyValuePair<string, object>("B", 2)));
        Assert.Contains(new KeyValuePair<string, object>("B", 2), uobj);
        Assert.Contains("A", uobj.Keys);
        Assert.Contains(1, uobj.Values);

        Assert.True(uobj.TryGetValue("A", out var val));
        Assert.Equal(1, val);
        Assert.False(uobj.TryGetValue("C", out _));

        var array = new KeyValuePair<string, object>[2];
        uobj.CopyTo(array, 0);
        Assert.Equal(2, array.Length);

        var items = new List<KeyValuePair<string, object>>();
        foreach (var kv in uobj)
        {
            items.Add(kv);
        }
        Assert.Equal(2, items.Count);

        var enumerator = ((IEnumerable)uobj).GetEnumerator();
        Assert.True(enumerator.MoveNext());

        Assert.True(uobj.Remove("A"));
        Assert.True(uobj.Remove(new KeyValuePair<string, object>("B", 2)));
        Assert.Empty(uobj);

        uobj["X"] = 10;
        uobj.Clear();
        Assert.Empty(uobj);
    }
}
