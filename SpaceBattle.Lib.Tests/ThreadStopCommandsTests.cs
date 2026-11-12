using System.Collections.Concurrent;
using App;
using App.Scopes;
using Moq;

namespace SpaceBattle.Lib.Tests;

public class ThreadStopCommandsTests
{
    public ThreadStopCommandsTests()
    {
        new InitCommand().Execute();
        var scope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", scope).Execute();
    }

    [Fact]
    public void Constructors_NullThread_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HardStopCommand(null!));
        Assert.Throws<ArgumentNullException>(() => new SoftStopCommand(null!));
    }

    [Fact]
    public void HardStopCommand_CallsStopImmediately()
    {
        var threadMock = new Mock<IGameThread>();
        var cmd = new HardStopCommand(threadMock.Object);

        cmd.Execute();

        threadMock.Verify(t => t.StopImmediately(), Times.Once);
    }

    [Fact]
    public void SoftStopCommand_CompletesAddingAndSetsDrainingStrategy()
    {
        var queue = new BlockingCollection<ICommand>();
        var threadMock = new Mock<IGameThread>();
        threadMock.SetupGet(t => t.Queue).Returns(queue);

        Action? capturedStrategy = null;
        threadMock.Setup(t => t.SetStrategy(It.IsAny<Action>()))
            .Callback<Action>(s => capturedStrategy = s);

        var remainingCmdMock = new Mock<ICommand>();
        queue.Add(remainingCmdMock.Object);

        var softStop = new SoftStopCommand(threadMock.Object);
        softStop.Execute();

        Assert.True(queue.IsAddingCompleted);
        Assert.NotNull(capturedStrategy);

        // First call drains remaining command
        capturedStrategy!();
        remainingCmdMock.Verify(c => c.Execute(), Times.Once);
        threadMock.Verify(t => t.StopImmediately(), Times.Never);

        // Second call sees empty queue and stops thread
        capturedStrategy();
        threadMock.Verify(t => t.StopImmediately(), Times.Once);
    }

    [Fact]
    public void SoftStopCommand_WhenAddingAlreadyCompleted_DoesNotThrowAndSetsStrategy()
    {
        var queue = new BlockingCollection<ICommand>();
        queue.CompleteAdding();

        var threadMock = new Mock<IGameThread>();
        threadMock.SetupGet(t => t.Queue).Returns(queue);

        var softStop = new SoftStopCommand(threadMock.Object);
        softStop.Execute();

        threadMock.Verify(t => t.SetStrategy(It.IsAny<Action>()), Times.Once);
    }

    [Fact]
    public void SoftStopCommand_RemainingCommandThrows_DispatchesToExceptionHandler()
    {
        var queue = new BlockingCollection<ICommand>();
        var threadMock = new Mock<IGameThread>();
        threadMock.SetupGet(t => t.Queue).Returns(queue);

        Action? capturedStrategy = null;
        threadMock.Setup(t => t.SetStrategy(It.IsAny<Action>()))
            .Callback<Action>(s => capturedStrategy = s);

        var failingCmdMock = new Mock<ICommand>();
        failingCmdMock.Setup(c => c.Execute()).Throws(new InvalidOperationException("boom"));
        queue.Add(failingCmdMock.Object);

        var handlerMock = new Mock<ICommand>();
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Exception.Handler",
            (object[] args) => handlerMock.Object
        ).Execute();

        var softStop = new SoftStopCommand(threadMock.Object);
        softStop.Execute();

        capturedStrategy!();

        handlerMock.Verify(h => h.Execute(), Times.Once);

        // Next tick sees empty queue and stops
        capturedStrategy();
        threadMock.Verify(t => t.StopImmediately(), Times.Once);
    }

    [Fact]
    public void HardStopCommand_RealThread_StopsImmediatelyAndDropsRemainingCommands()
    {
        var thread = new TestGameThread();
        int executedCount = 0;
        var hardStopExecuted = new ManualResetEvent(false);

        var cmd1 = new Mock<ICommand>();
        cmd1.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));

        var cmdAfterHardStop = new Mock<ICommand>();
        cmdAfterHardStop.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));

        thread.Queue.Add(cmd1.Object);
        thread.Queue.Add(new HardStopCommand(thread));
        thread.Queue.Add(new DelegateCommand(() => hardStopExecuted.Set()));
        thread.Queue.Add(cmdAfterHardStop.Object);

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(3));

        Assert.Equal(1, executedCount);
        Assert.False(hardStopExecuted.WaitOne(0));
        cmdAfterHardStop.Verify(c => c.Execute(), Times.Never);
    }

    [Fact]
    public void SoftStopCommand_RealThread_DrainsQueueAndStopsAndRejectsNewCommands()
    {
        var thread = new TestGameThread();
        int executedCount = 0;

        var cmd1 = new Mock<ICommand>();
        cmd1.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));

        var cmd2 = new Mock<ICommand>();
        cmd2.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));

        var cmd3 = new Mock<ICommand>();
        cmd3.Setup(c => c.Execute()).Callback(() => Interlocked.Increment(ref executedCount));

        thread.Queue.Add(cmd1.Object);
        thread.Queue.Add(new SoftStopCommand(thread));
        thread.Queue.Add(cmd2.Object);
        thread.Queue.Add(cmd3.Object);

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(3));

        Assert.Equal(3, executedCount);
        Assert.True(thread.Queue.IsAddingCompleted);
        Assert.Throws<InvalidOperationException>(() => thread.Queue.Add(new EmptyCommand()));
    }

    [Fact]
    public void SoftStopCommand_RealThread_DrainsThrowingCommandWithScopedExceptionHandler()
    {
        var gameScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", gameScope).Execute();

        var handled = new ManualResetEvent(false);
        Ioc.Resolve<App.ICommand>(
            "IoC.Register",
            "Exception.Handler",
            (object[] args) => new DelegateCommand(() => handled.Set())
        ).Execute();

        var afterThrowExecuted = new ManualResetEvent(false);
        var thread = new TestGameThread(gameScope);

        thread.Queue.Add(new SoftStopCommand(thread));
        thread.Queue.Add(new DelegateCommand(() => throw new InvalidOperationException("draining error")));
        thread.Queue.Add(new DelegateCommand(() => afterThrowExecuted.Set()));

        thread.Start();
        thread.Join(TimeSpan.FromSeconds(3));

        Assert.True(handled.WaitOne(0));
        Assert.True(afterThrowExecuted.WaitOne(0));
    }

    [Fact]
    public void RegisterIoCDependencyThreadStopCommands_ResolvesHardAndSoftStop()
    {
        var threadMock = new Mock<IGameThread>();

        new RegisterIoCDependencyThreadStopCommands().Execute();

        var hardStop = Ioc.Resolve<ICommand>("Commands.HardStop", threadMock.Object);
        var softStop = Ioc.Resolve<ICommand>("Commands.SoftStop", threadMock.Object);

        Assert.IsType<HardStopCommand>(hardStop);
        Assert.IsType<SoftStopCommand>(softStop);
    }

    private sealed class DelegateCommand : ICommand
    {
        private readonly Action _action;
        public DelegateCommand(Action action) => _action = action;
        public void Execute() => _action();
    }

    private sealed class TestGameThread : IGameThread
    {
        private readonly Thread _thread;
        private readonly object? _initialScope;
        private volatile bool _stopRequested;
        private Action _strategy;

        public BlockingCollection<ICommand> Queue { get; } = new();

        public TestGameThread(object? initialScope = null)
        {
            _initialScope = initialScope;
            _strategy = DefaultLoop;
            _thread = new Thread(Run);
        }

        public void Start() => _thread.Start();

        public void Join(TimeSpan timeout) => Assert.True(_thread.Join(timeout), "Thread did not stop in time.");

        public void StopImmediately() => _stopRequested = true;

        public void SetStrategy(Action newStrategy) => _strategy = newStrategy;

        private void Run()
        {
            if (_initialScope is not null)
            {
                Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", _initialScope).Execute();
            }

            while (!_stopRequested)
            {
                _strategy();
            }
        }

        private void DefaultLoop()
        {
            if (Queue.TryTake(out var cmd, 20))
            {
                cmd.Execute();
            }
        }
    }
}
