namespace SpaceBattle.Lib;

public interface IUObject : IDictionary<string, object>
{
    object GetProperty(string key);
    void SetProperty(string key, object value);
}
