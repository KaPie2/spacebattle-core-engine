using System;
using System.Collections.Generic;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AutoInjectCommandTests
{
    public AutoInjectCommandTests()
    {
        // инициализация скоупов и установка тестового контекста
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void RegisterAutoInjectCommand_InjectsAdapter_Automatically()
    {
        // arrange (подготовка словаря состояния и регистрация адаптера)
        var dict = new Dictionary<string, object>();

        Ioc.Resolve<App.ICommand>("IoC.Register", "Adapter.IMovingObject", (object[] args) =>
        {
            return new DummyMovingAdapter();
        }).Execute();

        var registerCommand = new RegisterAutoInjectCommand("Game.Command.Move", typeof(DummyMoveCommand));
        registerCommand.Execute();

        // act (разрешение команды из ioc)
        var moveCommand = Ioc.Resolve<ICommand>("Game.Command.Move", dict);

        // assert (проверка типа созданной команды и внедренного адаптера)
        Assert.NotNull(moveCommand);
        Assert.IsType<DummyMoveCommand>(moveCommand);

        var dummyMove = (DummyMoveCommand)moveCommand;
        Assert.NotNull(dummyMove.MovingObject);
        Assert.IsType<DummyMovingAdapter>(dummyMove.MovingObject);
    }

    [Fact]
    public void RegisterAutoInjectCommand_InjectsAdapter_UsingFallbackPluralKey()
    {
        // arrange (подготовка словаря состояния и регистрация адаптера по альтернативному ключу)
        var dict = new Dictionary<string, object>();

        Ioc.Resolve<App.ICommand>("IoC.Register", "Adapters.ITestFallback", (object[] args) =>
        {
            return new DummyTestFallbackAdapter();
        }).Execute();

        var registerCommand = new RegisterAutoInjectCommand("Game.Command.TestFallback", typeof(DummyFallbackCommand));
        registerCommand.Execute();

        // act (разрешение команды из ioc)
        var cmd = Ioc.Resolve<ICommand>("Game.Command.TestFallback", dict);

        // assert (проверка успешного разрешения через альтернативный ключ)
        Assert.NotNull(cmd);
        var dummy = Assert.IsType<DummyFallbackCommand>(cmd);
        Assert.IsType<DummyTestFallbackAdapter>(dummy.Adapter);
    }

    [Fact]
    public void RegisterAutoInjectCommand_InjectsNonInterfaceParameter_WhenFallbackUsed()
    {
        // arrange (подготовка словаря состояния и регистрация неинтерфейсной зависимости)
        var dict = new Dictionary<string, object>();

        Ioc.Resolve<App.ICommand>("IoC.Register", "Int32", (object[] args) =>
        {
            return (object)42;
        }).Execute();

        var registerCommand = new RegisterAutoInjectCommand("Game.Command.NonInterface", typeof(DummyNonInterfaceCommand));
        registerCommand.Execute();

        // act (разрешение команды из ioc)
        var cmd = Ioc.Resolve<ICommand>("Game.Command.NonInterface", dict);

        // assert (проверка успешного внедрения параметра)
        Assert.NotNull(cmd);
        var dummyCmd = Assert.IsType<DummyNonInterfaceCommand>(cmd);
        Assert.Equal(42, dummyCmd.Value);
    }

    [Fact]
    public void RegisterAutoInjectCommand_ThrowsArgumentException_OnEmptyArgs()
    {
        // arrange (подготовка команды с автоматическим внедрением)
        var registerCommand = new RegisterAutoInjectCommand("Game.Command.NoArgs", typeof(DummyMoveCommand));
        registerCommand.Execute();

        // act & assert (проверка выброса исключения при пустом массиве аргументов)
        Assert.Throws<ArgumentException>(() => Ioc.Resolve<ICommand>("Game.Command.NoArgs"));
    }

    [Fact]
    public void RegisterAutoInjectCommand_ThrowsInvalidOperationException_WhenNoPublicConstructors()
    {
        // arrange (подготовка команды без публичных конструкторов)
        var registerCommand = new RegisterAutoInjectCommand("Game.Command.PrivateCtor", typeof(DummyPrivateCtorCommand));

        // act & assert (проверка выброса исключения при отсутствии публичных конструкторов)
        Assert.Throws<InvalidOperationException>(() => registerCommand.Execute());
    }
}

// вспомогательные классы для тестирования
public class DummyMovingAdapter : IMovingObject
{
    public NVector Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    public NVector Velocity => throw new NotImplementedException();
}

public class DummyMoveCommand : ICommand
{
    public IMovingObject MovingObject { get; }

    public DummyMoveCommand(IMovingObject movingObject)
    {
        MovingObject = movingObject;
    }

    public void Execute() { }
}

public class DummyNonInterfaceCommand : ICommand
{
    public int Value { get; }

    public DummyNonInterfaceCommand(int value)
    {
        Value = value;
    }

    public void Execute() { }
}

public class DummyPrivateCtorCommand : ICommand
{
    private DummyPrivateCtorCommand() { }
    public void Execute() { }
}

public interface ITestFallback { }

public class DummyTestFallbackAdapter : ITestFallback { }

public class DummyFallbackCommand : ICommand
{
    public ITestFallback Adapter { get; }

    public DummyFallbackCommand(ITestFallback adapter)
    {
        Adapter = adapter;
    }

    public void Execute() { }
}
