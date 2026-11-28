using System;
using System.Collections.Generic;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class RegisterAdapterCommandTests
{
    public RegisterAdapterCommandTests()
    {
        // инициализация скоупов и установка тестового контекста
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void RegisterAdapterCommand_RegistersInIoC_AndCreatesViaResolve()
    {
        // arrange (подготовка команды регистрации адаптера и зависимости скорости)
        var command = new RegisterAdapterCommand<IMovingObject>();
        var state = new Dictionary<string, object>();

        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Velocity.Get", (object[] args) => new NVector(1, 2)).Execute();

        // act (выполнение регистрации)
        command.Execute();

        // assert (проверка разрешения по основному контракту)
        var adapter = Ioc.Resolve<IMovingObject>("Adapters.IMovingObject", state);
        Assert.NotNull(adapter);
        Assert.IsAssignableFrom<IMovingObject>(adapter);
        var registrationType = adapter.GetType().Assembly.GetType(
            "SpaceBattle.Lib.Generated.MovingObjectAdapterRegistrationCommand",
            throwOnError: true
        );
        Assert.True(typeof(ICommand).IsAssignableFrom(registrationType));

        // assert (проверка разрешения свойства скорости)
        var velocity = adapter.Velocity;
        Assert.Equal(new NVector(1, 2), velocity);

        // assert (проверка разрешения исходного кода сгенерированного адаптера)
        var source = Ioc.Resolve<string>("Adapters.Source.IMovingObject");
        Assert.Contains("MovingObjectAdapter", source);
    }

    [Fact]
    public void RegisterAdapterCommand_AdapterReadsAndWritesPositionInSourceDictionary()
    {
        var initialPosition = new NVector(1, 2);
        var updatedPosition = new NVector(5, 7);
        var state = new Dictionary<string, object> { ["Position"] = initialPosition };

        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Get",
            (object[] args) => ((IDictionary<string, object>)args[0])["Position"]).Execute();
        Ioc.Resolve<App.ICommand>("IoC.Register", "IMovingObject.Position.Set",
            (object[] args) => new SetPositionCommand((IDictionary<string, object>)args[0], (NVector)args[1])).Execute();

        new RegisterAdapterCommand<IMovingObject>().Execute();
        var adapter = Ioc.Resolve<IMovingObject>("Adapters.IMovingObject", state);

        Assert.Equal(initialPosition, adapter.Position);

        adapter.Position = updatedPosition;

        Assert.Equal(updatedPosition, (NVector)state["Position"]);
        Assert.Equal(updatedPosition, adapter.Position);
    }

    [Fact]
    public void RegisterAdapterCommand_RegistersCustomStrategy_FromAdapterAttribute()
    {
        // arrange (подготовка команды регистрации для интерфейса с кастомным атрибутом)
        var command = new RegisterAdapterCommand<ICalculatedMovingObject>();
        var state = new Dictionary<string, object>();

        // act (выполнение регистрации)
        command.Execute();

        // assert (проверка автоматической регистрации стратегии из атрибута)
        var adapter = Ioc.Resolve<ICalculatedMovingObject>("Adapters.ICalculatedMovingObject", state);
        Assert.NotNull(adapter);
        Assert.Equal(new NVector(2, -5), adapter.Velocity);
    }

    [Fact]
    public void RegisterAdapterCommand_RegistersInheritedAttributeStrategy()
    {
        new RegisterAdapterCommand<IInheritedCalculatedMovingObject>().Execute();

        var adapter = Ioc.Resolve<IInheritedCalculatedMovingObject>(
            "Adapters.IInheritedCalculatedMovingObject",
            new Dictionary<string, object>()
        );

        Assert.Equal(new NVector(2, -5), adapter.Velocity);
    }

    [Fact]
    public void RegisterAdapterCommand_AcceptsCustomTemplateWithInlineFactory()
    {
        const string adapterTemplate = """
            using System.Collections.Generic;
            using App;
            using SpaceBattle.Lib;

            namespace SpaceBattle.Lib.Generated;

            public class {{AdapterClassName}} : {{InterfaceName}}
            {
                private readonly IDictionary<string, object> _obj;

                public {{AdapterClassName}}(IDictionary<string, object> obj) => _obj = obj;

            {{Properties}}
            }

            public class {{FactoryClassName}} : IAdapterFactory<{{InterfaceName}}>
            {
                public {{InterfaceName}} Create(IDictionary<string, object> obj) => new {{AdapterClassName}}(obj);
            }
            """;
        Ioc.Resolve<App.ICommand>("IoC.Register", "Adapters.Template", (object[] _) => adapterTemplate).Execute();

        new RegisterAdapterCommand<IMovingObject>().Execute();

        var adapter = Ioc.Resolve<IMovingObject>("Adapters.IMovingObject", new Dictionary<string, object>());
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }

    private sealed class SetPositionCommand(IDictionary<string, object> state, NVector position) : App.ICommand
    {
        public void Execute() => state["Position"] = position;
    }
}

public interface IInheritedCalculatedMovingObject : ICalculatedMovingObject { }
