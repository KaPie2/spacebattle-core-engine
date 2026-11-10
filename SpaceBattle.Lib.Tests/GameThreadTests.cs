using System;
using System.Collections.Concurrent;
using System.Threading;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class GameThreadTests
{
    public GameThreadTests()
    {
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void GameThread_ExecutesCommandsFromQueue()
    {
        // Arrange
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        int commandCount = 10;

        // установка счетчика для ожидания выполнения заданного количества команд
        var countdown = new CountdownEvent(commandCount);

        for (int i = 0; i < commandCount; i++)
        {
            queue.Add(new ActionCommand(() => countdown.Signal()));
        }

        // Act
        thread.Start();

        // Assert
        // ожидание максимум 2 секунды, чтобы поток успел обработать всю очередь
        bool allExecuted = countdown.Wait(TimeSpan.FromSeconds(2));

        thread.StopImmediately();

        // ожидание полной остановки потока
        thread.Join();

        Assert.True(allExecuted, "The thread did not process all commands in time.");
    }

    [Fact]
    public void GameThread_CatchBlock_DispatchesToExceptionHandler()
    {
        // Arrange
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        var exceptionHandledEvent = new ManualResetEvent(false);

        // регистрация заглушки маршрутизатора исключений в текущей области видимости
        Ioc.Resolve<App.ICommand>("IoC.Register", "Exception.Handler", (object[] args) =>
        {
            return new ActionCommand(() => exceptionHandledEvent.Set());
        }).Execute();

        // передача текущей области видимости в фоновый поток через очередь команд
        // выполнение этой команды фоновым потоком в первую очередь и настройка его ThreadLocal контекста
        var currentScope = Ioc.Resolve<object>("IoC.Scope.Current");
        queue.Add(new ActionCommand(() =>
        {
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", currentScope).Execute();
        }));

        // добавление в очередь команды, вызывающей исключение, строго после настройки контекста
        queue.Add(new ActionCommand(() => throw new InvalidOperationException("Test exception")));

        // Act
        thread.Start();

        // Assert
        // ожидание сигнала от зарегистрированного обработчика Exception.Handler
        bool wasHandled = exceptionHandledEvent.WaitOne(TimeSpan.FromSeconds(2));

        thread.StopImmediately();
        thread.Join();

        Assert.True(wasHandled, "Exception.Handler was not called after a command failure.");
    }
}

// вспомогательный класс для тестов, выполняющий переданное действие
public class ActionCommand : ICommand
{
    private readonly Action _action;
    public ActionCommand(Action action) => _action = action;
    public void Execute() => _action();
}
