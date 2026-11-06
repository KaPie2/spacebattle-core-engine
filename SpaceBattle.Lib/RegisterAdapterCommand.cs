using System;
using System.Collections.Generic;
using System.Linq;
using App;

namespace SpaceBattle.Lib;

public class RegisterAdapterCommand<T> : ICommand
{
    public void Execute()
    {
        var interfaceType = typeof(T);
        var generator = new AdapterCodeGenerator();
        var compiler = new AdapterCompiler();

        // генерирация кода (сразу 2 класса: адаптер и фабрика)
        var sourceCode = generator.Generate(interfaceType);

        // имя сгенерированного класса фабрики
        string cleanName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
        string factoryClassName = $"SpaceBattle.Lib.Generated.{cleanName}AdapterFactory";

        // компиляция
        var compiledFactoryType = compiler.Compile(sourceCode, factoryClassName);

        // создание экземпляра фабрики через Activator
        var factory = (IAdapterFactory<T>)Activator.CreateInstance(compiledFactoryType)!;

        // регистрация в IoC
        Ioc.Resolve<App.ICommand>("IoC.Register", $"Adapter.{interfaceType.Name}", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            return factory.Create(dict);
        }).Execute();
    }
}
