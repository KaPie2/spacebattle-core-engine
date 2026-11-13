using System.Collections.Concurrent;
using App;
using Moq;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class ThreadStopCommandsTests
{
    [Fact]
    public void HardStopCommand_StopsThreadImmediately()
    {
        // T3.3: HardStopCommand_StopsThreadImmediately (проверка игнорирования оставшейся очереди)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);
        var executedCount = 0;

        var slowCommand = new Mock<ICommand>();
        slowCommand.Setup(c => c.Execute()).Callback(() =>
        {
            Interlocked.Increment(ref executedCount);
            Thread.Sleep(60);
        });

        for (int i = 0; i < 5; i++)
        {
            queue.Add(slowCommand.Object);
        }

        thread.Start();
        Thread.Sleep(20);

        var hardStop = new HardStopCommand(thread);
        hardStop.Execute();
        thread.Join();

        // Поток остановлен немедленно, не дожидаясь всех элементов очереди
        Assert.True(executedCount < 5);
    }

    [Fact]
    public void SoftStopCommand_DrainsAllQueueItems_BeforeThreadExits()
    {
        // T3.4: SoftStopCommand_DrainsAllQueueItems_BeforeThreadExits (проверка 100% доработки задач)
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);
        var executedCount = 0;

        for (int i = 0; i < 10; i++)
        {
            var cmd = new Mock<ICommand>();
            cmd.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));
            queue.Add(cmd.Object);
        }

        thread.Start();

        var softStop = new SoftStopCommand(thread);
        softStop.Execute();
        thread.Join();

        // 100% задач очереди доработано до завершения потока
        Assert.Equal(10, executedCount);
        Assert.Empty(queue);
    }

    [Fact]
    public void SoftStopCommand_WhenQueueIsEmpty_StopsThreadImmediately()
    {
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        thread.Start();

        var softStop = new SoftStopCommand(thread);
        softStop.Execute();
        thread.Join();

        Assert.Empty(queue);
    }

    [Fact]
    public void HardStopCommand_ThrowsArgumentNullException_WhenThreadIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new HardStopCommand(null!));
    }

    [Fact]
    public void SoftStopCommand_ThrowsArgumentNullException_WhenThreadIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new SoftStopCommand(null!));
    }

    [Fact]
    public void GameThread_ThrowsArgumentNullException_WhenQueueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new GameThread(null!));
    }

    [Fact]
    public void GameThread_SetStrategy_ThrowsArgumentNullException_WhenStrategyIsNull()
    {
        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);
        Assert.Throws<ArgumentNullException>(() => thread.SetStrategy(null!));
    }

    [Fact]
    public void GameThread_WithInitialScope_ExecutesScopeSetCommand()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");

        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, scope);

        var executed = false;
        var cmd = new Mock<ICommand>();
        cmd.Setup(c => c.Execute()).Callback(() =>
        {
            var currentScope = Ioc.Resolve<object>("IoC.Scope.Current");
            Assert.Same(scope, currentScope);
            executed = true;
        });

        queue.Add(cmd.Object);
        thread.Start();

        var softStop = new SoftStopCommand(thread);
        softStop.Execute();
        thread.Join();

        Assert.True(executed);
    }

    [Fact]
    public void GameThread_CatchBlock_DispatchesToExceptionHandler()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        var handlerExecuted = false;
        var handlerCommand = new Mock<ICommand>();
        handlerCommand.Setup(h => h.Execute()).Callback(() => handlerExecuted = true);

        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Exception.Handler",
            (object[] args) => handlerCommand.Object
        ).Execute();

        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue, scope);

        var failingCommand = new Mock<ICommand>();
        failingCommand.Setup(c => c.Execute()).Throws(new InvalidOperationException("Test exception"));

        queue.Add(failingCommand.Object);
        thread.Start();

        // Даем время на выполнение сбойной команды и диспетчеризацию исключения
        Thread.Sleep(80);

        thread.StopImmediately();
        thread.Join();

        Assert.True(handlerExecuted);
    }

    [Fact]
    public void RegisterIoCDependencyThreadStopCommands_RegistersSuccessfully()
    {
        new App.Scopes.InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();

        new RegisterIoCDependencyThreadStopCommands().Execute();

        var queue = new BlockingCollection<ICommand>();
        var thread = new GameThread(queue);

        var hardStop = Ioc.Resolve<ICommand>("Commands.HardStop", thread);
        Assert.IsType<HardStopCommand>(hardStop);

        var softStop = Ioc.Resolve<ICommand>("Commands.SoftStop", thread);
        Assert.IsType<SoftStopCommand>(softStop);
    }
}
