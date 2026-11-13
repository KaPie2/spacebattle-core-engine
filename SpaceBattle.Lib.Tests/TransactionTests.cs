using App;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class TransactionTests
{
    [Fact]
    public void TransactionManager_Rollback_RestoresOriginalState_OnMacroFailure()
    {
        // T3.5: TransactionManager_Rollback_RestoresOriginalState_OnMacroFailure (проверка отката координат)
        var obj = new UObject(new Dictionary<string, object>
        {
            ["Position"] = new NVector(10, 20),
            ["FuelLevel"] = 100
        });

        var manager = new TransactionManager();
        manager.BeginTransaction(obj);

        // В рамках макрокоманды шаг 1 меняет координаты
        manager.SetProperty("Position", new NVector(15, 25));
        Assert.Single(manager.PendingUpdates);
        Assert.Same(obj, manager.Target);
        Assert.Equal(new NVector(15, 25), manager.GetProperty<NVector>("Position"));

        // Имитируем сбой следующего шага макрокоманды (например, отказ по топливу)
        // Выполняем откат
        manager.Rollback();

        // Исходное состояние объекта не изменено
        Assert.Equal(new NVector(10, 20), obj["Position"]);
        Assert.Equal(100, obj["FuelLevel"]);
        Assert.False(manager.InTransaction);
    }

    [Fact]
    public void TransactionManager_Commit_PersistsAllChanges()
    {
        // T3.6: TransactionManager_Commit_PersistsAllChanges (проверка фиксации изменений)
        var obj = new UObject(new Dictionary<string, object>
        {
            ["Position"] = new NVector(10, 20),
            ["FuelLevel"] = 100
        });

        var manager = new TransactionManager();
        manager.BeginTransaction(obj);

        manager.SetProperty("Position", new NVector(18, 30));
        manager.SetProperty("FuelLevel", 75);

        // До фиксации в целевом словаре остаются старые значения
        Assert.Equal(new NVector(10, 20), obj["Position"]);
        Assert.Equal(100, obj["FuelLevel"]);

        // Фиксируем изменения
        manager.Commit();

        // Изменения применились к целевому объекту
        Assert.Equal(new NVector(18, 30), obj["Position"]);
        Assert.Equal(75, obj["FuelLevel"]);
        Assert.False(manager.InTransaction);
    }

    [Fact]
    public void BeginTransaction_ThrowsArgumentNullException_WhenTargetIsNull()
    {
        var manager = new TransactionManager();
        Assert.Throws<ArgumentNullException>(() => manager.BeginTransaction(null!));
    }

    [Fact]
    public void SetProperty_ThrowsInvalidOperationException_WhenNotInTransaction()
    {
        var manager = new TransactionManager();
        var ex = Assert.Throws<InvalidOperationException>(() => manager.SetProperty("Key", "Value"));
        Assert.Equal("Транзакция не открыта.", ex.Message);
    }

    [Fact]
    public void SetProperty_ThrowsArgumentNullException_WhenKeyIsNull()
    {
        var manager = new TransactionManager();
        manager.BeginTransaction(new Dictionary<string, object>());
        Assert.Throws<ArgumentNullException>(() => manager.SetProperty(null!, "Value"));
    }

    [Fact]
    public void GetProperty_ThrowsArgumentNullException_WhenKeyIsNull()
    {
        var manager = new TransactionManager();
        Assert.Throws<ArgumentNullException>(() => manager.GetProperty<string>(null!));
    }

    [Fact]
    public void GetProperty_ThrowsKeyNotFoundException_WhenKeyNotFound()
    {
        var manager = new TransactionManager();
        var obj = new Dictionary<string, object>();
        manager.BeginTransaction(obj);

        var ex = Assert.Throws<KeyNotFoundException>(() => manager.GetProperty<int>("NonExisting"));
        Assert.Contains("NonExisting", ex.Message);
    }

    [Fact]
    public void GetProperty_ReturnsTargetValue_WhenNotInPending()
    {
        var obj = new Dictionary<string, object>
        {
            ["Speed"] = 42
        };

        var manager = new TransactionManager();
        manager.BeginTransaction(obj);

        var speed = manager.GetProperty<int>("Speed");
        Assert.Equal(42, speed);
    }

    [Fact]
    public void GetProperty_ReturnsTargetValue_WhenTransactionNotActive()
    {
        var obj = new Dictionary<string, object>
        {
            ["Speed"] = 99
        };

        var manager = new TransactionManager();
        manager.BeginTransaction(obj);
        manager.Commit();

        var speed = manager.GetProperty<int>("Speed");
        Assert.Equal(99, speed);
    }

    [Fact]
    public void Commit_DoesNothing_WhenNotInTransaction()
    {
        var manager = new TransactionManager();
        manager.Commit();
        Assert.False(manager.InTransaction);
    }

    [Fact]
    public void Commit_DoesNothing_WhenTargetIsNull()
    {
        var manager = new TransactionManager();
        // Target is null
        manager.Commit();
        Assert.Null(manager.Target);
    }

    [Fact]
    public void RegisterIoCDependencyTransaction_RegistersSuccessfully()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        new RegisterIoCDependencyTransaction().Execute();

        var tm1 = Ioc.Resolve<ITransactionManager>("Transaction.Manager");
        Assert.NotNull(tm1);

        var tm2 = Ioc.Resolve<ITransactionManager>("Transactions.Manager");
        Assert.NotNull(tm2);
    }
}
