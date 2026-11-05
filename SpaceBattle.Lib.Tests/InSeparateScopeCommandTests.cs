using System;
using App;
using App.Scopes;
using Moq;
using SpaceBattle.Lib;
using Xunit;

namespace SpaceBattle.Lib.Tests
{
    public class InSeparateScopeCommandTests : IDisposable
    {
        public InSeparateScopeCommandTests()
        {
            // Инициализация библиотечных скоупов App.Scopes перед запуском тестов
            new InitCommand().Execute();
        }

        public void Dispose()
        {
            // Очистка контекста текущего потока после завершения каждого теста
            var clearCmd = Ioc.Resolve<object>("IoC.Scope.Current.Clear");
            var executeMethod = clearCmd.GetType().GetMethod("Execute");
            executeMethod?.Invoke(clearCmd, null);
        }

        [Fact]
        public void Constructor_NullTargetScope_ThrowsArgumentNullException()
        {
            // Arrange
            var mockCommand = new Mock<ICommand>();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new InSeparateScopeCommand(null!, mockCommand.Object));
            Assert.Equal("targetScope", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullTargetCommand_ThrowsArgumentNullException()
        {
            // Arrange
            var fakeScope = new object();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new InSeparateScopeCommand(fakeScope, null!));
            Assert.Equal("targetCommand", ex.ParamName);
        }

        [Fact]
        public void Execute_SwitchesToTargetScope_ExecutesCommand_AndRestoresPreviousScope()
        {
    // Arrange: получаем исходный скоуп и создаем дочерний скоуп для конкретной игры
            var initialScope = Ioc.Resolve<object>("IoC.Scope.Current");
            var gameScope = Ioc.Resolve<object>("IoC.Scope.Create");

    // Регистрируем зависимость, которая будет существовать только внутри gameScope
            var setScopeCmd = Ioc.Resolve<object>("IoC.Scope.Current.Set", gameScope);
            setScopeCmd.GetType().GetMethod("Execute")?.Invoke(setScopeCmd, null);

            var registerCmd = Ioc.Resolve<object>(
                "IoC.Register",
                "Game.SpecificValue",
                (Func<object[], object>)(_ => 999)
            );
            registerCmd.GetType().GetMethod("Execute")?.Invoke(registerCmd, null);

    // Возвращаем поток в initialScope
            setScopeCmd = Ioc.Resolve<object>("IoC.Scope.Current.Set", initialScope);
            setScopeCmd.GetType().GetMethod("Execute")?.Invoke(setScopeCmd, null);

            int? valueReadInsideCommand = null;
            var targetCommandMock = new Mock<ICommand>();
            targetCommandMock.Setup(c => c.Execute()).Callback(() =>
            {
        // В момент выполнения команды активен gameScope, поэтому значение успешно разрешается
                valueReadInsideCommand = Ioc.Resolve<int>("Game.SpecificValue");
            });

            var inSeparateScopeCmd = new InSeparateScopeCommand(gameScope, targetCommandMock.Object);

    // Act
            inSeparateScopeCmd.Execute();

    // Assert
            Assert.Equal(999, valueReadInsideCommand);
            targetCommandMock.Verify(c => c.Execute(), Times.Once);

    // Проверяем, что после завершения скоуп вернулся к initialScope
            Assert.Same(initialScope, Ioc.Resolve<object>("IoC.Scope.Current"));

    // В initialScope зависимость отсутствует: при попытке подняться выше rootScope выбрасывается Exception
            var ex = Assert.Throws<Exception>(() => Ioc.Resolve<int>("Game.SpecificValue"));
            Assert.Equal("The root scope has no a parent scope.", ex.Message);
        }

        [Fact]
        public void Execute_WhenTargetCommandThrowsException_StillRestoresScopeInFinally()
        {
            // Arrange
            var initialScope = Ioc.Resolve<object>("IoC.Scope.Current");
            var gameScope = Ioc.Resolve<object>("IoC.Scope.Create");

            var failingCommandMock = new Mock<ICommand>();
            failingCommandMock.Setup(c => c.Execute()).Throws(new InvalidOperationException("Игровой сбой"));

            var inSeparateScopeCmd = new InSeparateScopeCommand(gameScope, failingCommandMock.Object);

            // Act & Assert: команда пробрасывает исходное исключение наружу
            var thrownEx = Assert.Throws<InvalidOperationException>(() => inSeparateScopeCmd.Execute());
            Assert.Equal("Игровой сбой", thrownEx.Message);

            // Главная проверка инварианта: скоуп потока гарантированно восстановлен
            Assert.Same(initialScope, Ioc.Resolve<object>("IoC.Scope.Current"));
        }

        [Fact]
        public void Execute_WhenScopeSetCommandImplementsLibICommand_ExecutesDirectly()
        {
            // Arrange: создаем временный изолированный скоуп для подмены системной стратегии
            var tempScope = Ioc.Resolve<object>("IoC.Scope.Create");
            var setTempCmd = Ioc.Resolve<object>("IoC.Scope.Current.Set", tempScope);
            setTempCmd.GetType().GetMethod("Execute")?.Invoke(setTempCmd, null);

            var executedLibCommand = false;
            var mockLibCommand = new Mock<ICommand>();
            mockLibCommand.Setup(m => m.Execute()).Callback(() => executedLibCommand = true);

            // Переопределяем "IoC.Scope.Current.Set" так, чтобы возвращался экземпляр SpaceBattle.Lib.ICommand
            var regOverrideCmd = Ioc.Resolve<object>(
                "IoC.Register",
                "IoC.Scope.Current.Set",
                (Func<object[], object>)(_ => mockLibCommand.Object)
            );
            regOverrideCmd.GetType().GetMethod("Execute")?.Invoke(regOverrideCmd, null);

            var dummyTargetCmd = new Mock<ICommand>();
            var inSeparateScopeCmd = new InSeparateScopeCommand(tempScope, dummyTargetCmd.Object);

            // Act
            inSeparateScopeCmd.Execute();

            // Assert: проверяем ветку прямого вызова libCmd.Execute()
            Assert.True(executedLibCommand);
        }

        [Fact]
        public void Execute_WhenScopeSetReturnsObjectWithoutExecuteMethod_ThrowsInvalidOperationException()
        {
            // Arrange
            var tempScope = Ioc.Resolve<object>("IoC.Scope.Create");
            var setTempCmd = Ioc.Resolve<object>("IoC.Scope.Current.Set", tempScope);
            setTempCmd.GetType().GetMethod("Execute")?.Invoke(setTempCmd, null);

            // Регистрируем объект без метода Execute (например, обычную строку)
            var regInvalidCmd = Ioc.Resolve<object>(
                "IoC.Register",
                "IoC.Scope.Current.Set",
                (Func<object[], object>)(_ => "NotACommand")
            );
            regInvalidCmd.GetType().GetMethod("Execute")?.Invoke(regInvalidCmd, null);

            var dummyTargetCmd = new Mock<ICommand>();
            var inSeparateScopeCmd = new InSeparateScopeCommand(tempScope, dummyTargetCmd.Object);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => inSeparateScopeCmd.Execute());
            Assert.Contains("неподдерживаемый тип", ex.Message);
        }
    }
}