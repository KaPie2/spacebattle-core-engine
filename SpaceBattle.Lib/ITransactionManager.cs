namespace SpaceBattle.Lib;

public interface ITransactionManager
{
    void BeginTransaction(IDictionary<string, object> obj);
    void SetProperty(string key, object value);
    T GetProperty<T>(string key);
    void Commit();
    void Rollback();
}
