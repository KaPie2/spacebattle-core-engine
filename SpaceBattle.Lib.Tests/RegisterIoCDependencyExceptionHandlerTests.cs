using System;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;
using Xunit;

namespace SpaceBattle.Lib.Tests;

public class RegisterIoCDependencyExceptionHandlerTests : IDisposable
{
    private sealed class TestGameCommand : ICommand
    {
        public void Execute() { }
    }

    public RegisterIoCDependencyExceptionHandlerTests()
    {
        new InitCommand().Execute();

        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    public void Dispose()
    {
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Clear").Execute();
    }

    [Fact]
    public void RegisterExceptionRouteCommand_NullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var router = new ExceptionHandlerRouter();
        var cmdType = typeof(TestGameCommand);
        var exType = typeof(Exception);
        Func<ICommand, Exception, ICommand> factory = (_, _) => new Mock<ICommand>().Object;

        // Act & Assert
        var exRouter = Assert.Throws<ArgumentNullException>(() =>
            new RegisterExceptionRouteCommand(null!, cmdType, exType, factory));
        Assert.Equal("router", exRouter.ParamName);

        var exCmd = Assert.Throws<ArgumentNullException>(() =>
            new RegisterExceptionRouteCommand(router, null!, exType, factory));
        Assert.Equal("commandType", exCmd.ParamName);

        var exTypeArg = Assert.Throws<ArgumentNullException>(() =>
            new RegisterExceptionRouteCommand(router, cmdType, null!, factory));
        Assert.Equal("exceptionType", exTypeArg.ParamName);

        var exFactory = Assert.Throws<ArgumentNullException>(() =>
            new RegisterExceptionRouteCommand(router, cmdType, exType, null!));
        Assert.Equal("handlerFactory", exFactory.ParamName);
    }

    [Fact]
    public void RegisterExceptionRouteCommand_Execute_AddsRouteToRouter()
    {
        // Arrange
        var router = new ExceptionHandlerRouter();
        var expectedHandler = new Mock<ICommand>().Object;

        var registerRouteCmd = new RegisterExceptionRouteCommand(
            router,
            typeof(TestGameCommand),
            typeof(InvalidOperationException),
            (_, _) => expectedHandler);

        // Act
        registerRouteCmd.Execute();
        var actualHandler = router.Handle(new TestGameCommand(), new InvalidOperationException());

        // Assert
        Assert.Same(expectedHandler, actualHandler);
    }

    [Fact]
    public void Execute_RegistersExceptionHandlerAndRouteRegistrationInIoC()
    {
        // Arrange
        var registerDependencyCmd = new RegisterIoCDependencyExceptionHandler();
        registerDependencyCmd.Execute();

        var failedCommand = new TestGameCommand();
        var exception = new InvalidOperationException("Boom");

        // Act 1: до регистрации правил должен возвращаться резервный LogExceptionCommand
        var defaultHandler = Ioc.Resolve<ICommand>("Exception.Handler", failedCommand, exception);
        Assert.IsType<LogExceptionCommand>(defaultHandler);

        // Act 2: регистрируем правило через ключ "Exception.Handler.Register"
        var customHandler = new Mock<ICommand>().Object;
        Func<ICommand, Exception, ICommand> customFactory = (cmd, ex) =>
        {
            Assert.Same(failedCommand, cmd);
            Assert.Same(exception, ex);
            return customHandler;
        };

        Ioc.Resolve<ICommand>(
            "Exception.Handler.Register",
            typeof(TestGameCommand),
            typeof(InvalidOperationException),
            customFactory
        ).Execute();

        // Act 3: повторно разрешаем обработчик через "Exception.Handler"
        var resolvedHandler = Ioc.Resolve<ICommand>("Exception.Handler", failedCommand, exception);

        // Assert
        Assert.Same(customHandler, resolvedHandler);
    }

    [Fact]
    public void Execute_IsolatesRoutersBetweenDifferentGameScopes()
    {
        // Arrange: создаём два независимых скоупа для разных игровых матчей
        var baseScope = Ioc.Resolve<object>("IoC.Scope.Current");
        var gameScope1 = Ioc.Resolve<object>("IoC.Scope.Create", baseScope);
        var gameScope2 = Ioc.Resolve<object>("IoC.Scope.Create", baseScope);

        var scope1SpecificHandler = new Mock<ICommand>().Object;

        // Настраиваем скоуп Игры 1: регистрируем свой роутер и кастомное правило
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", gameScope1).Execute();
        new RegisterIoCDependencyExceptionHandler().Execute();
        Ioc.Resolve<App.ICommand>(
            "Exception.Handler.Register",
            typeof(TestGameCommand),
            typeof(InvalidOperationException),
            (Func<ICommand, Exception, ICommand>)((_, _) => scope1SpecificHandler)
        ).Execute();

        // Настраиваем скоуп Игры 2: регистрируем свой роутер без кастомного правила
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", gameScope2).Execute();
        new RegisterIoCDependencyExceptionHandler().Execute();

        var failedCmd = new TestGameCommand();
        var ex = new InvalidOperationException("Game error");

        // Act & Assert для Игры 1
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", gameScope1).Execute();
        var handlerInGame1 = Ioc.Resolve<ICommand>("Exception.Handler", failedCmd, ex);
        Assert.Same(scope1SpecificHandler, handlerInGame1);

        // Act & Assert для Игры 2 (правило из Игры 1 сюда не протекло)
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", gameScope2).Execute();
        var handlerInGame2 = Ioc.Resolve<ICommand>("Exception.Handler", failedCmd, ex);
        Assert.IsType<LogExceptionCommand>(handlerInGame2);

        // Act & Assert для родительского скоупа (в нём зависимости вообще нет)
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", baseScope).Execute();
        var parentEx = Assert.Throws<Exception>(() =>
            Ioc.Resolve<ICommand>("Exception.Handler", failedCmd, ex));
        Assert.Equal("The root scope has no a parent scope.", parentEx.Message);
    }
}