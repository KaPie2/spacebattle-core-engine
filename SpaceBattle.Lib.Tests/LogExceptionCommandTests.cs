using System;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;
using Xunit;

namespace SpaceBattle.Lib.Tests
{
    public class LogExceptionCommandTests : IDisposable
    {
        public LogExceptionCommandTests()
        {
            new InitCommand().Execute();

            var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
            var setScopeCmd = Ioc.Resolve<object>("IoC.Scope.Current.Set", testScope);
            setScopeCmd.GetType().GetMethod("Execute")?.Invoke(setScopeCmd, null);
        }

        public void Dispose()
        {
            var clearCmd = Ioc.Resolve<object>("IoC.Scope.Current.Clear");
            clearCmd.GetType().GetMethod("Execute")?.Invoke(clearCmd, null);
        }

        [Fact]
        public void Constructor_NullFailedCommand_ThrowsArgumentNullException()
        {
            var exception = new InvalidOperationException("Test error");

            var ex = Assert.Throws<ArgumentNullException>(() => new LogExceptionCommand(null!, exception));
            Assert.Equal("failedCommand", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullException_ThrowsArgumentNullException()
        {
            var mockCommand = new Mock<ICommand>();

            var ex = Assert.Throws<ArgumentNullException>(() => new LogExceptionCommand(mockCommand.Object, null!));
            Assert.Equal("exception", ex.ParamName);
        }

        [Fact]
        public void Constructor_ValidArguments_SetsPropertiesCorrectly()
        {
            var mockCommand = new Mock<ICommand>();
            var exception = new InvalidOperationException("Test error");

            var logCommand = new LogExceptionCommand(mockCommand.Object, exception);

            Assert.Same(mockCommand.Object, logCommand.FailedCommand);
            Assert.Same(exception, logCommand.Exception);
        }

        [Fact]
        public void Execute_LogsFormattedMessageThroughIoC()
        {
            string? capturedLogMessage = null;
            Action<string> loggerStrategy = msg => capturedLogMessage = msg;

            var registerCmd = Ioc.Resolve<object>(
                "IoC.Register",
                "Logger.Log",
                (Func<object[], object>)(_ => loggerStrategy)
            );
            registerCmd.GetType().GetMethod("Execute")?.Invoke(registerCmd, null);

            var mockCommand = new Mock<ICommand>();
            var exception = new InvalidOperationException("Недостаточно топлива");
            var logCommand = new LogExceptionCommand(mockCommand.Object, exception);

            logCommand.Execute();

            Assert.NotNull(capturedLogMessage);
            Assert.Contains(mockCommand.Object.GetType().Name, capturedLogMessage);
            Assert.Contains("Недостаточно топлива", capturedLogMessage);
        }

        [Fact]
        public void Execute_WhenLoggerNotRegistered_ThrowsException()
        {
            var mockCommand = new Mock<ICommand>();
            var exception = new Exception("Ошибка");
            var logCommand = new LogExceptionCommand(mockCommand.Object, exception);

            var ex = Assert.Throws<Exception>(() => logCommand.Execute());
            Assert.Equal("The root scope has no a parent scope.", ex.Message);
        }
    }
}