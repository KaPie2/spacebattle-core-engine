using System.Collections;
using App;
using App.Scopes;
using Moq;

namespace SpaceBattle.Lib.Tests;

public class TransactionTests
{
    public TransactionTests()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();
    }

    [Fact]
    public void TransactionManager_StateTransitions_InvalidCallsThrow()
    {
        var manager = new TransactionManager();

        Assert.False(manager.IsActive);
        Assert.Throws<InvalidOperationException>(() => manager.Commit());
        Assert.Throws<InvalidOperationException>(() => manager.Rollback());

        manager.BeginTransaction();
        Assert.True(manager.IsActive);
        Assert.Equal(1, manager.CurrentTransactionId);

        Assert.Throws<InvalidOperationException>(() => manager.BeginTransaction());

        manager.Rollback();
        Assert.False(manager.IsActive);
    }

    [Fact]
    public void TransactionManager_NullArguments_ThrowArgumentNullException()
    {
        var manager = new TransactionManager();
        var uobj = new UObject();

        Assert.Throws<ArgumentNullException>(() => manager.CreateTransactionalObject(null!));
        Assert.Throws<ArgumentNullException>(() => manager.SetValue(null!, "K", 1));
        Assert.Throws<ArgumentNullException>(() => manager.SetValue(uobj, null!, 1));
        Assert.Throws<ArgumentNullException>(() => manager.SetValue(uobj, "K", null!));
        Assert.Throws<ArgumentNullException>(() => manager.TryGetValue(null!, "K", out _));
        Assert.Throws<ArgumentNullException>(() => manager.TryGetValue(uobj, null!, out _));
        Assert.Throws<ArgumentNullException>(() => manager.RemoveKey(null!, "K"));
        Assert.Throws<ArgumentNullException>(() => manager.RemoveKey(uobj, null!));
        Assert.Throws<ArgumentNullException>(() => manager.GetKeys(null!));

        Assert.Throws<ArgumentNullException>(() => new TransactionalUObject(null!, manager));
        Assert.Throws<ArgumentNullException>(() => new TransactionalUObject(uobj, null!));
        Assert.Throws<ArgumentNullException>(() => new TransactionalCommand(null!, manager));
        Assert.Throws<ArgumentNullException>(() => new TransactionalCommand(new Mock<ICommand>().Object, null!));
        Assert.Throws<ArgumentNullException>(() => new TransactionLogEntry(1, null!, "K", null, 1));
        Assert.Throws<ArgumentNullException>(() => new TransactionLogEntry(1, uobj, null!, null, 1));
    }

    [Fact]
    public void TransactionManager_WritesGoToTemporaryCacheUntilCommit()
    {
        var realObj = new UObject
        {
            ["FuelLevel"] = 10,
            ["Location"] = new NVector(0, 0)
        };

        var manager = new TransactionManager();
        var txObj = manager.CreateTransactionalObject(realObj);

        manager.BeginTransaction();

        txObj["FuelLevel"] = 7;
        txObj["Location"] = new NVector(5, 3);

        // Temporary cache returns updated values (Read-Your-Own-Writes)
        Assert.Equal(7, txObj["FuelLevel"]);
        Assert.Equal(new NVector(5, 3), txObj["Location"]);

        // Underlying real object has NOT changed yet
        Assert.Equal(10, realObj["FuelLevel"]);
        Assert.Equal(new NVector(0, 0), realObj["Location"]);

        // Log entries are Pending
        Assert.Equal(2, manager.Log.Count);
        Assert.All(manager.Log, e => Assert.Equal(TransactionStatus.Pending, e.Status));

        manager.Commit();

        // After Commit, real object is updated and log status is Success
        Assert.Equal(7, realObj["FuelLevel"]);
        Assert.Equal(new NVector(5, 3), realObj["Location"]);
        Assert.All(manager.Log, e => Assert.Equal(TransactionStatus.Success, e.Status));
    }

    [Fact]
    public void TransactionManager_Rollback_DiscardsCachedChangesAndKeepsOriginalValues()
    {
        var realObj = new UObject
        {
            ["FuelLevel"] = 10,
            ["Location"] = new NVector(11, 5)
        };

        var manager = new TransactionManager();
        var txObj = manager.CreateTransactionalObject(realObj);

        manager.BeginTransaction();
        txObj["FuelLevel"] = 6;
        txObj["Location"] = new NVector(15, 8);
        txObj["TempFlag"] = true;

        Assert.True(txObj.ContainsKey("TempFlag"));
        Assert.Equal(3, txObj.Count);

        manager.Rollback();

        // Original object completely untouched
        Assert.Equal(10, realObj["FuelLevel"]);
        Assert.Equal(new NVector(11, 5), realObj["Location"]);
        Assert.False(realObj.ContainsKey("TempFlag"));
        Assert.Equal(2, realObj.Count);

        // Transactional view reflects clean rolled-back state
        Assert.Equal(10, txObj["FuelLevel"]);
        Assert.False(txObj.ContainsKey("TempFlag"));

        // Log records Fail status
        Assert.Equal(3, manager.Log.Count);
        Assert.All(manager.Log, e => Assert.Equal(TransactionStatus.Fail, e.Status));
    }

    [Fact]
    public void TransactionManager_RemoveKey_CommitAndRollbackBehavior()
    {
        var realObj = new UObject
        {
            ["A"] = 1,
            ["B"] = 2
        };

        var manager = new TransactionManager();
        var txObj = manager.CreateTransactionalObject(realObj);

        // Rollback removal
        manager.BeginTransaction();
        Assert.True(txObj.Remove("A"));
        Assert.False(txObj.Remove("NonExistent"));
        Assert.False(txObj.ContainsKey("A"));
        Assert.Throws<KeyNotFoundException>(() => txObj["A"]);
        Assert.Single(txObj.Keys);
        Assert.True(realObj.ContainsKey("A"));

        manager.Rollback();
        Assert.True(realObj.ContainsKey("A"));

        // Commit removal
        manager.BeginTransaction();
        Assert.True(txObj.Remove("B"));
        manager.Commit();

        Assert.False(realObj.ContainsKey("B"));
        Assert.True(realObj.ContainsKey("A"));
    }

    [Fact]
    public void TransactionalUObject_WithoutActiveTransaction_MutatesTargetDirectly()
    {
        var realObj = new UObject { ["X"] = 1 };
        var manager = new TransactionManager();
        var txObj = manager.CreateTransactionalObject(realObj);

        txObj["Y"] = 2;
        Assert.Equal(2, realObj["Y"]);

        Assert.True(txObj.Remove("X"));
        Assert.False(realObj.ContainsKey("X"));
    }

    [Fact]
    public void TransactionalUObject_DictionaryInterfaceOperations_WorkInsideTransaction()
    {
        var realObj = new UObject { ["Init"] = 0 };
        var manager = new TransactionManager();
        var txObj = manager.CreateTransactionalObject(realObj);

        manager.BeginTransaction();

        txObj.Add("Added", 10);
        txObj.Add(new KeyValuePair<string, object>("Pair", 20));
        Assert.Throws<ArgumentNullException>(() => txObj.Add(null!, 1));
        Assert.Throws<ArgumentException>(() => txObj.Add("Added", 99));

        Assert.False(txObj.IsReadOnly);
        Assert.Equal(3, txObj.Count);
        Assert.Contains("Added", txObj.Keys);
        Assert.Contains(20, txObj.Values);
        Assert.True(txObj.Contains(new KeyValuePair<string, object>("Pair", 20)));
        Assert.False(txObj.Contains(new KeyValuePair<string, object>("Pair", 999)));

        var arr = new KeyValuePair<string, object>[3];
        txObj.CopyTo(arr, 0);
        Assert.Throws<ArgumentNullException>(() => txObj.CopyTo(null!, 0));

        var enumCount = 0;
        foreach (var _ in (IEnumerable)txObj)
        {
            enumCount++;
        }
        Assert.Equal(3, enumCount);

        Assert.True(txObj.Remove(new KeyValuePair<string, object>("Pair", 20)));
        Assert.False(txObj.Remove(new KeyValuePair<string, object>("Pair", 20)));

        txObj.Clear();
        Assert.Empty(txObj);
        Assert.Single(realObj); // realObj still untouched!

        manager.Rollback();
        Assert.Single(realObj);
        Assert.Equal(0, realObj["Init"]);
    }

    [Fact]
    public void TransactionalCommand_MacroCommandWithFailure_RollsBackFuelAndPosition()
    {
        var ship = new UObject
        {
            ["Position"] = new NVector(0, 0),
            ["Velocity"] = new NVector(5, 0),
            ["FuelLevel"] = 10,
            ["FuelConsumption"] = 3
        };

        var manager = new TransactionManager();
        var txShip = manager.CreateTransactionalObject(ship);

        var fuelAdapterMock = new Mock<IFuelable>();
        fuelAdapterMock.SetupGet(f => f.FuelLevel).Returns(() => (int)txShip["FuelLevel"]);
        fuelAdapterMock.SetupSet(f => f.FuelLevel = It.IsAny<int>())
            .Callback<int>(val => txShip["FuelLevel"] = val);
        fuelAdapterMock.SetupGet(f => f.FuelConsumption).Returns(() => (int)txShip["FuelConsumption"]);

        var movingAdapterMock = new Mock<IMovingObject>();
        movingAdapterMock.SetupGet(m => m.Position).Returns(() => (NVector)txShip["Position"]);
        movingAdapterMock.SetupGet(m => m.Velocity).Returns(() => (NVector)txShip["Velocity"]);
        movingAdapterMock.SetupSet(m => m.Position = It.IsAny<NVector>())
            .Throws(new InvalidOperationException("Collision blocked movement"));

        var macro = new MacroCommand(new ICommand[]
        {
            new BurnFuelCommand(fuelAdapterMock.Object),
            new MoveCommand(movingAdapterMock.Object)
        });

        var txCommand = new TransactionalCommand(macro, manager);

        Assert.Throws<InvalidOperationException>(() => txCommand.Execute());

        // Fuel was burned in temporary cache, then MoveCommand failed -> Rollback kept original FuelLevel = 10!
        Assert.Equal(10, ship["FuelLevel"]);
        Assert.Equal(new NVector(0, 0), ship["Position"]);
        Assert.False(manager.IsActive);
        Assert.Single(manager.Log);
        Assert.Equal(TransactionStatus.Fail, manager.Log[0].Status);
    }

    [Fact]
    public void TransactionalCommand_MacroCommandSucceeds_CommitsFuelAndPosition()
    {
        var ship = new UObject
        {
            ["Position"] = new NVector(0, 0),
            ["Velocity"] = new NVector(5, 2),
            ["FuelLevel"] = 10,
            ["FuelConsumption"] = 4
        };

        var manager = new TransactionManager();
        var txShip = manager.CreateTransactionalObject(ship);

        var fuelAdapterMock = new Mock<IFuelable>();
        fuelAdapterMock.SetupGet(f => f.FuelLevel).Returns(() => (int)txShip["FuelLevel"]);
        fuelAdapterMock.SetupSet(f => f.FuelLevel = It.IsAny<int>())
            .Callback<int>(val => txShip["FuelLevel"] = val);
        fuelAdapterMock.SetupGet(f => f.FuelConsumption).Returns(() => (int)txShip["FuelConsumption"]);

        var movingAdapterMock = new Mock<IMovingObject>();
        movingAdapterMock.SetupGet(m => m.Position).Returns(() => (NVector)txShip["Position"]);
        movingAdapterMock.SetupGet(m => m.Velocity).Returns(() => (NVector)txShip["Velocity"]);
        movingAdapterMock.SetupSet(m => m.Position = It.IsAny<NVector>())
            .Callback<NVector>(val => txShip["Position"] = val);

        var macro = new MacroCommand(new ICommand[]
        {
            new CheckFuelCommand(fuelAdapterMock.Object),
            new MoveCommand(movingAdapterMock.Object),
            new BurnFuelCommand(fuelAdapterMock.Object)
        });

        var txCommand = new TransactionalCommand(macro, manager);
        txCommand.Execute();

        Assert.Equal(6, ship["FuelLevel"]);
        Assert.Equal(new NVector(5, 2), ship["Position"]);
        Assert.False(manager.IsActive);
        Assert.Equal(2, manager.Log.Count);
        Assert.All(manager.Log, e => Assert.Equal(TransactionStatus.Success, e.Status));
    }

    [Fact]
    public void RegisterIoCDependencyTransaction_ResolvesManagerAndCommand()
    {
        new RegisterIoCDependencyTransaction().Execute();

        var manager = Ioc.Resolve<ITransactionManager>("Transactions.Manager");
        Assert.NotNull(manager);
        Assert.IsType<TransactionManager>(manager);

        var innerCmdMock = new Mock<ICommand>();

        var txCmdWithExplicitManager = Ioc.Resolve<ICommand>("Commands.Transactional", innerCmdMock.Object, manager);
        Assert.IsType<TransactionalCommand>(txCmdWithExplicitManager);
        txCmdWithExplicitManager.Execute();

        var txCmdWithResolvedManager = Ioc.Resolve<ICommand>("Commands.Transactional", innerCmdMock.Object);
        Assert.IsType<TransactionalCommand>(txCmdWithResolvedManager);
        txCmdWithResolvedManager.Execute();

        innerCmdMock.Verify(c => c.Execute(), Times.Exactly(2));
    }
}
