using System.Diagnostics.CodeAnalysis;

namespace SpaceBattle.Lib;

public interface ITransactionManager
{
    bool IsActive { get; }
    long CurrentTransactionId { get; }
    IReadOnlyList<TransactionLogEntry> Log { get; }

    void BeginTransaction();
    void Commit();
    void Rollback();

    IUObject CreateTransactionalObject(IUObject target);
    object GetValue(IUObject target, string key);
    void SetValue(IUObject target, string key, object value);
    bool TryGetValue(IUObject target, string key, [MaybeNullWhen(false)] out object value);
    bool ContainsKey(IUObject target, string key);
    bool RemoveKey(IUObject target, string key);
    ICollection<string> GetKeys(IUObject target);
}
