using System;
using Moq;
using SpaceBattle.Lib;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class ExceptionHandlerRouterTests
{
    private sealed class DummyCommand : ICommand
    {
        public void Execute() { }
    }

    private sealed class SecondaryDummyCommand : ICommand
    {
        public void Execute() { }
    }

    [Fact]
    public void Register_NullArguments_ThrowsArgumentNullException()
    {
        var router = new ExceptionHandlerRouter();
        Func<ICommand, Exception, ICommand> validFactory = (_, _) => new Mock<ICommand>().Object;

        var exCmd = Assert.Throws<ArgumentNullException>(() =>
            router.Register(null!, typeof(Exception), validFactory));
        Assert.Equal("commandType", exCmd.ParamName);

        var exType = Assert.Throws<ArgumentNullException>(() =>
            router.Register(typeof(DummyCommand), null!, validFactory));
        Assert.Equal("exceptionType", exType.ParamName);

        var exFactory = Assert.Throws<ArgumentNullException>(() =>
            router.Register(typeof(DummyCommand), typeof(Exception), null!));
        Assert.Equal("handlerFactory", exFactory.ParamName);
    }

    [Fact]
    public void Handle_NullArguments_ThrowsArgumentNullException()
    {
        var router = new ExceptionHandlerRouter();
        var cmd = new DummyCommand();
        var ex = new Exception("Test");

        var exCmd = Assert.Throws<ArgumentNullException>(() => router.Handle(null!, ex));
        Assert.Equal("command", exCmd.ParamName);

        var exArg = Assert.Throws<ArgumentNullException>(() => router.Handle(cmd, null!));
        Assert.Equal("exception", exArg.ParamName);
    }

    [Fact]
    public void Handle_ExactMatch_ReturnsRegisteredHandler()
    {
        var router = new ExceptionHandlerRouter();
        var expectedHandler = new Mock<ICommand>().Object;
        var failedCommand = new DummyCommand();
        var thrownException = new InvalidOperationException("Exact error");

        router.Register(
            typeof(DummyCommand),
            typeof(InvalidOperationException),
            (cmd, ex) =>
            {
                Assert.Same(failedCommand, cmd);
                Assert.Same(thrownException, ex);
                return expectedHandler;
            });

        var actualHandler = router.Handle(failedCommand, thrownException);

        Assert.Same(expectedHandler, actualHandler);
    }

    [Fact]
    public void Handle_BaseExceptionMatch_ResolvesThroughExceptionHierarchy()
    {
        var router = new ExceptionHandlerRouter();
        var baseExceptionHandler = new Mock<ICommand>().Object;

        router.Register(
            typeof(DummyCommand),
            typeof(ArgumentException),
            (_, _) => baseExceptionHandler);

        var failedCommand = new DummyCommand();
        var derivedException = new ArgumentNullException("param");

        var actualHandler = router.Handle(failedCommand, derivedException);

        Assert.Same(baseExceptionHandler, actualHandler);
    }

    [Fact]
    public void Handle_InterfaceCommandMatch_ResolvesThroughCommandInterfaces()
    {
        var router = new ExceptionHandlerRouter();
        var interfaceHandler = new Mock<ICommand>().Object;

        router.Register(
            typeof(ICommand),
            typeof(Exception),
            (_, _) => interfaceHandler);

        var failedCommand = new SecondaryDummyCommand();
        var exception = new InvalidOperationException("Interface fallback");

        var actualHandler = router.Handle(failedCommand, exception);

        Assert.Same(interfaceHandler, actualHandler);
    }

    [Fact]
    public void Handle_SpecificRouteOverridesGeneralRoute_RespectsPriority()
    {
        var router = new ExceptionHandlerRouter();
        var generalHandler = new Mock<ICommand>().Object;
        var specificHandler = new Mock<ICommand>().Object;

        router.Register(typeof(ICommand), typeof(Exception), (_, _) => generalHandler);
        router.Register(typeof(DummyCommand), typeof(InvalidOperationException), (_, _) => specificHandler);

        var handlerForDummy = router.Handle(new DummyCommand(), new InvalidOperationException());
        var handlerForSecondary = router.Handle(new SecondaryDummyCommand(), new InvalidOperationException());

        Assert.Same(specificHandler, handlerForDummy);
        Assert.Same(generalHandler, handlerForSecondary);
    }

    [Fact]
    public void Handle_ObjectFallbackRoute_ResolvesWhenRegisteredOnObject()
    {
        var router = new ExceptionHandlerRouter();
        var rootObjectHandler = new Mock<ICommand>().Object;

        router.Register(typeof(object), typeof(object), (_, _) => rootObjectHandler);

        var actualHandler = router.Handle(new DummyCommand(), new Exception());

        Assert.Same(rootObjectHandler, actualHandler);
    }

    [Fact]
    public void Handle_NoMatchingRoute_ReturnsDefaultLogExceptionCommand()
    {
        var router = new ExceptionHandlerRouter();
        var failedCommand = new DummyCommand();
        var exception = new Exception("Unhandled failure");

        var handler = router.Handle(failedCommand, exception);

        var logCommand = Assert.IsType<LogExceptionCommand>(handler);
        Assert.Same(failedCommand, logCommand.FailedCommand);
        Assert.Same(exception, logCommand.Exception);
    }

    [Fact]
    public void Handle_RetryOnceThenLogChain_WorksEndToEnd()
    {
        var router = new ExceptionHandlerRouter();
        var receiverMock = new Mock<ICommandReceiver>();

        router.Register(
            typeof(DummyCommand),
            typeof(Exception),
            (cmd, _) => new RepeatOnceCommand(cmd, receiverMock.Object));

        router.Register(
            typeof(RepeatedCommand),
            typeof(Exception),
            (cmd, ex) => new LogExceptionCommand(cmd, ex));

        var originalCommand = new DummyCommand();
        var firstException = new InvalidOperationException("Transient error");
        var secondException = new InvalidOperationException("Persistent error");

        var firstHandler = router.Handle(originalCommand, firstException);
        var repeatHandler = Assert.IsType<RepeatOnceCommand>(firstHandler);
        Assert.Same(originalCommand, repeatHandler.TargetCommand);

        var repeatedWrapper = new RepeatedCommand(originalCommand);
        var secondHandler = router.Handle(repeatedWrapper, secondException);
        var logHandler = Assert.IsType<LogExceptionCommand>(secondHandler);
        Assert.Same(repeatedWrapper, logHandler.FailedCommand);
    }
}