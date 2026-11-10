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
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void RegisterAutoInjectCommand_InjectsAdapter_Automatically()
    {
        // Arrange
        var dict = new Dictionary<string, object>();

        // регистрация заглушки для адаптера, имитирующая работу сгенерированной фабрики
        Ioc.Resolve<App.ICommand>("IoC.Register", "Adapter.IMovingObject", (object[] args) =>
        {
            return new DummyMovingAdapter();
        }).Execute();

        // регистрация нашей стратегии для тестовой команды движения
        var registerCommand = new RegisterAutoInjectCommand("Game.Command.Move", typeof(DummyMoveCommand));
        registerCommand.Execute();

        // Act
        var moveCommand = Ioc.Resolve<ICommand>("Game.Command.Move", dict);

        // Assert
        Assert.NotNull(moveCommand);
        Assert.IsType<DummyMoveCommand>(moveCommand);

        var dummyMove = (DummyMoveCommand)moveCommand;
        Assert.NotNull(dummyMove.MovingObject);
        Assert.IsType<DummyMovingAdapter>(dummyMove.MovingObject);
    }
}

// вспомогательные классы для теста
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
