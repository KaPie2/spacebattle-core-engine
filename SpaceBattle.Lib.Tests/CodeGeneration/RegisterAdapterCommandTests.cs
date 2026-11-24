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
        var adapterPlural = Ioc.Resolve<IMovingObject>("Adapters.IMovingObject", state);
        Assert.NotNull(adapterPlural);
        Assert.IsAssignableFrom<IMovingObject>(adapterPlural);
        var registrationType = adapterPlural.GetType().Assembly.GetType(
            "SpaceBattle.Lib.Generated.MovingObjectAdapterRegistrationCommand",
            throwOnError: true
        );
        Assert.True(typeof(ICommand).IsAssignableFrom(registrationType));

        // assert (проверка разрешения по альтернативному ключу)
        var adapterSingular = Ioc.Resolve<IMovingObject>("Adapter.IMovingObject", state);
        Assert.NotNull(adapterSingular);
        Assert.IsAssignableFrom<IMovingObject>(adapterSingular);

        // assert (проверка разрешения свойства скорости)
        var velocity = adapterPlural.Velocity;
        Assert.Equal(new NVector(1, 2), velocity);

        // assert (проверка разрешения исходного кода сгенерированного адаптера)
        var sourcePlural = Ioc.Resolve<string>("Adapters.Source.IMovingObject");
        Assert.NotNull(sourcePlural);
        Assert.Contains("MovingObjectAdapter", sourcePlural);

        var sourceSingular = Ioc.Resolve<string>("Adapter.Source.IMovingObject");
        Assert.NotNull(sourceSingular);
        Assert.Contains("MovingObjectAdapter", sourceSingular);
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
        var velocity = adapter.Velocity;
        Assert.NotNull(velocity);
    }

    [Fact]
    public void RegisterAdapterCommand_RegistersInheritedAttributeStrategy()
    {
        new RegisterAdapterCommand<IInheritedCalculatedMovingObject>().Execute();

        var adapter = Ioc.Resolve<IInheritedCalculatedMovingObject>(
            "Adapters.IInheritedCalculatedMovingObject",
            new Dictionary<string, object>()
        );

        Assert.NotNull(adapter.Velocity);
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
        Ioc.Resolve<App.ICommand>("IoC.Register", "Adapter.Template", (object[] _) => adapterTemplate).Execute();

        new RegisterAdapterCommand<IMovingObject>().Execute();

        var adapter = Ioc.Resolve<IMovingObject>("Adapters.IMovingObject", new Dictionary<string, object>());
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }
}

public interface IInheritedCalculatedMovingObject : ICalculatedMovingObject { }
