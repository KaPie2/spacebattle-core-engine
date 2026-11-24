using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using App;
using App.Scopes;
using Moq;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class GameThreadTests
{
    public GameThreadTests()
    {
        // инициализация скоупов и установка тестового контекста
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void GameThread_ExecutesCommandsFromQueue()
    {
        // arrange (подготовка очереди и создание потока)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, (_, _) => new EmptyCommand());

        int commandCount = 10;
        var countdown = new CountdownEvent(commandCount);

        for (int i = 0; i < commandCount; i++)
        {
            queue.Add(new ActionCommand(() => countdown.Signal()));
        }

        // act (запуск потока)
        thread.Start();

        // assert (ожидание обработки команд и остановка потока)
        bool allExecuted = countdown.Wait(TimeSpan.FromSeconds(2));
        thread.StopImmediately();
        thread.Join();

        Assert.True(allExecuted, "The thread did not process all commands in time.");
    }

    [Fact]
    public void GameThread_ExecutesNextCommandAfterHandlingException()
    {
        using var queue = new BlockingCollection<ICommand>();
        using var lastCommandExecuted = new ManualResetEventSlim();
        var executionOrder = new List<string>();
        var error = new InvalidOperationException("Test exception");
        var failingCommand = new ActionCommand(() =>
        {
            executionOrder.Add("second");
            throw error;
        });
        ICommand? handledCommand = null;
        Exception? handledError = null;
        var thread = new GameThread(queue, (command, exception) => new ActionCommand(() =>
        {
            handledCommand = command;
            handledError = exception;
            executionOrder.Add("handler");
        }));
        thread.Receive(new ActionCommand(() => executionOrder.Add("first")));
        thread.Receive(failingCommand);
        thread.Receive(new ActionCommand(() =>
        {
            executionOrder.Add("third");
            lastCommandExecuted.Set();
        }));

        thread.Start();
        try
        {
            Assert.True(lastCommandExecuted.Wait(TimeSpan.FromSeconds(2)), "The third command did not execute.");
        }
        finally
        {
            thread.StopImmediately();
            thread.Join();
        }

        Assert.Same(failingCommand, handledCommand);
        Assert.Same(error, handledError);
        Assert.Equal(new[] { "first", "second", "handler", "third" }, executionOrder);
    }

    [Fact]
    public void GameThread_InitializesScope_FromConstructorParameter()
    {
        // arrange (подготовка области видимости, очереди и потока)
        var targetScope = Ioc.Resolve<object>("IoC.Scope.Create");
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, (_, _) => new EmptyCommand(), targetScope);

        object? resolvedScopeInThread = null;
        var scopeResolvedEvent = new ManualResetEvent(false);

        queue.Add(new ActionCommand(() =>
        {
            resolvedScopeInThread = Ioc.Resolve<object>("IoC.Scope.Current");
            scopeResolvedEvent.Set();
        }));

        // act (запуск потока)
        thread.Start();

        // assert (ожидание проверки скоупа)
        bool wasSet = scopeResolvedEvent.WaitOne(TimeSpan.FromSeconds(2));
        thread.StopImmediately();
        thread.Join();

        Assert.True(wasSet);
        Assert.Same(targetScope, resolvedScopeInThread);
    }

    [Fact]
    public void GameThread_SetStrategy_ExecutesCustomStrategy()
    {
        // arrange (подготовка очереди и создание потока)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, (_, _) => new EmptyCommand());

        var customStrategyExecuted = new ManualResetEvent(false);

        // act (смена стратегии выполнения на пользовательскую и запуск потока)
        thread.SetStrategy(() =>
        {
            customStrategyExecuted.Set();
            thread.StopImmediately();
        });

        thread.Start();

        // assert (ожидание выполнения пользовательской стратегии)
        bool wasExecuted = customStrategyExecuted.WaitOne(TimeSpan.FromSeconds(2));
        thread.Join();

        Assert.True(wasExecuted);
    }

    [Fact]
    public void GameThread_ThrowsArgumentNullException_OnNullQueueOrStrategy()
    {
        // arrange, act & assert (проверка валидации входных аргументов)
        Assert.Throws<ArgumentNullException>(() => new GameThread(null!, (_, _) => new EmptyCommand()));

        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, (_, _) => new EmptyCommand());
        Assert.Throws<ArgumentNullException>(() => thread.SetStrategy(null!));
    }

    [Fact]
    public void GameThread_RequiresExceptionHandlerBeforeStart()
    {
        using var queue = new BlockingCollection<ICommand>();

        Assert.Throws<ArgumentNullException>("exceptionHandler", () => new GameThread(queue, null!));
    }

    [Fact]
    public void GameThread_ImplementsICommandReceiver_AndEnqueuesCommand()
    {
        // arrange (создание очереди и потока как получателя команд)
        var queue = new BlockingCollection<ICommand>();
        ICommandReceiver receiver = new GameThread(queue, (_, _) => new EmptyCommand());
        var mockCommand = new Mock<ICommand>();

        // act (прием команды через интерфейс icommandreceiver)
        receiver.Receive(mockCommand.Object);

        // assert (проверка помещения команды в очередь)
        Assert.Single(queue);
        Assert.Same(mockCommand.Object, queue.Take());

        // assert (проверка валидации на null)
        Assert.Throws<ArgumentNullException>(() => receiver.Receive(null!));
    }
}

// вспомогательная команда для выполнения переданного делегата
public class ActionCommand : ICommand, App.ICommand
{
    private readonly Action _action;
    public ActionCommand(Action action) => _action = action;
    public void Execute() => _action();
}
