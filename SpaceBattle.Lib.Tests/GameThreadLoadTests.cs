using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class GameThreadLoadTests
{
    public GameThreadLoadTests()
    {
        // инициализация скоупов и установка тестового контекста
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public async Task GameThread_HandlesHighConcurrentLoad_WithoutLosingCommands()
    {
        // arrange (подготовка очереди, потока и счетчиков для параллельной нагрузки)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        int producerCount = 8;
        int commandsPerProducer = 150;
        int totalCommands = producerCount * commandsPerProducer;

        int executedCount = 0;
        var countdown = new CountdownEvent(totalCommands);

        // act (запуск потока и параллельная отправка команд из нескольких источников)
        thread.Start();

        var producers = new Task[producerCount];
        for (int p = 0; p < producerCount; p++)
        {
            producers[p] = Task.Run(() =>
            {
                for (int i = 0; i < commandsPerProducer; i++)
                {
                    queue.Add(new ActionCommand(() =>
                    {
                        Interlocked.Increment(ref executedCount);
                        countdown.Signal();
                    }));
                }
            });
        }

        await Task.WhenAll(producers);

        // assert (ожидание завершения обработки команд и остановка потока)
        bool allCompleted = countdown.Wait(TimeSpan.FromSeconds(5));
        thread.StopImmediately();
        thread.Join();

        Assert.True(allCompleted, "Не все команды были обработаны за отведенное время.");
        Assert.Equal(totalCommands, executedCount);
    }

    [Fact]
    public async Task GameThread_DoesNotDeadlock_WhenStrategyChangedUnderLoad()
    {
        // arrange (подготовка очереди и создание потока)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        var cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        // запуск фонового генератора команд
        var producerTask = Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                queue.Add(new ActionCommand(() => { }));
                Thread.Sleep(1);
            }
        });

        // act (динамическая смена стратегий выполнения под нагрузкой)
        thread.Start();

        for (int i = 0; i < 20; i++)
        {
            thread.SetStrategy(() =>
            {
                if (queue.TryTake(out var cmd, 10))
                {
                    cmd.Execute();
                }
            });
            Thread.Sleep(5);
        }

        // assert (остановка генератора и проверка завершения потока без дедлока)
        cancellationTokenSource.Cancel();
        await producerTask;

        thread.StopImmediately();
        thread.Join();
    }

    [Fact]
    public void GameThread_GracefullyStops_WhenStopImmediatelyCalledDuringActiveQueue()
    {
        // arrange (заполнение очереди большим объемом команд)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        for (int i = 0; i < 1000; i++)
        {
            queue.Add(new ActionCommand(() => Thread.Sleep(1)));
        }

        // act (запуск потока и немедленный запрос остановки)
        thread.Start();
        Thread.Sleep(50);
        thread.StopImmediately();

        // assert (проверка корректного выхода из цикла без зависания)
        thread.Join();
    }

    [Fact]
    public void GameThread_EndToEnd_ExecutesGeneratedAdapterAndAutoInjectedMoveCommand()
    {
        // arrange (регистрация стратегии скорости, адаптера и команды движения)
        var currentScope = Ioc.Resolve<object>("IoC.Scope.Current");
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, currentScope);

        // регистрация стратегии скорости до адаптера
        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Velocity.GetVelocityByAngle", (object[] args) =>
        {
            return new NVector(2, -5);
        }).Execute();

        // регистрация фабрики адаптера для imovingobject
        new RegisterAdapterCommand<IMovingObject>().Execute();

        // регистрация команды автоматического внедрения зависимостей для movecommand
        new RegisterAutoInjectCommand("Commands.Move", typeof(MoveCommand)).Execute();

        // регистрация зависимостей чтения и записи позиции объекта в скоупе
        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Get", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            return dict["Position"];
        }).Execute();

        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Set", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            var newPos = (NVector)args[1];
            return new ActionCommand(() => dict["Position"] = newPos);
        }).Execute();

        var gameObject = new Dictionary<string, object>
        {
            ["Position"] = new NVector(10, 20)
        };

        var moveCompletedEvent = new ManualResetEvent(false);
        Exception? caughtEx = null;

        Ioc.Resolve<App.ICommand>("IoC.Register", "Exception.Handler", (object[] args) =>
        {
            caughtEx = (Exception)args[1];
            return new ActionCommand(() => { });
        }).Execute();

        // act (создание команды через ioc, постановка в очередь потока и запуск)
        var moveCmd = Ioc.Resolve<ICommand>("Commands.Move", gameObject);

        queue.Add(moveCmd);
        queue.Add(new ActionCommand(() => moveCompletedEvent.Set()));

        thread.Start();

        // assert (ожидание выполнения команды движения и проверка новых координат)
        bool completed = moveCompletedEvent.WaitOne(TimeSpan.FromSeconds(3));
        thread.StopImmediately();
        thread.Join();

        if (caughtEx != null)
        {
            Assert.Fail($"MoveCommand threw exception: {caughtEx.GetType().Name} - {caughtEx.Message}\n{caughtEx.StackTrace}");
        }

        Assert.True(completed, "Команда движения не была выполнена игровым потоком.");
        var pos = (NVector)gameObject["Position"];
        Assert.Equal(new NVector(12, 15), pos);
    }
}
