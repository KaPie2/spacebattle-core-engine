using System;
using System.Collections.Generic;
using System.Reflection;
using App;

namespace SpaceBattle.Lib;

public class RegisterAdapterCommand<T> : ICommand
{
    public void Execute()
    {
        var interfaceType = typeof(T);
        var generator = new AdapterCodeGenerator();
        var compiler = new AdapterCompiler();

        // генерация исходного кода адаптера и фабрики
        var sourceCode = generator.Generate(interfaceType);

        // определение имени типа фабрики
        string cleanName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;
        string factoryClassName = $"SpaceBattle.Lib.Generated.{cleanName}AdapterFactory";

        // компиляция сгенерированного кода в сборку памяти
        var compiledFactoryType = compiler.Compile(sourceCode, factoryClassName);

        // создание экземпляра фабрики
        var factory = (IAdapterFactory<T>)Activator.CreateInstance(compiledFactoryType)!;

        // регистрация фабрики адаптера по основному контракту
        Ioc.Resolve<App.ICommand>("IoC.Register", $"Adapters.{interfaceType.Name}", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            return factory.Create(dict);
        }).Execute();

        // регистрация фабрики адаптера по альтернативному ключу
        Ioc.Resolve<App.ICommand>("IoC.Register", $"Adapter.{interfaceType.Name}", (object[] args) =>
        {
            var dict = (IDictionary<string, object>)args[0];
            return factory.Create(dict);
        }).Execute();

        // регистрация сгенерированного исходного кода адаптера
        Ioc.Resolve<App.ICommand>("IoC.Register", $"Adapters.Source.{interfaceType.Name}", (object[] args) => sourceCode).Execute();
        Ioc.Resolve<App.ICommand>("IoC.Register", $"Adapter.Source.{interfaceType.Name}", (object[] args) => sourceCode).Execute();

        // регистрация стратегий вычислений из атрибутов адаптера
        foreach (var prop in interfaceType.GetProperties())
        {
            var attr = prop.GetCustomAttribute<AdapterAttribute>();
            if (attr != null && attr.StrategyType != null && !string.IsNullOrEmpty(attr.MethodName))
            {
                var method = attr.StrategyType.GetMethod(attr.MethodName, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    try
                    {
                        Ioc.Resolve<App.ICommand>("IoC.Register", $"{attr.TargetInterface.Name}.{attr.PropertyName}.{attr.MethodName}", (object[] args) =>
                        {
                            return method.Invoke(null, new object[] { args })!;
                        }).Execute();
                    }
                    catch
                    {
                        // пропуск при наличии существующей регистрации
                    }
                }
            }
        }
    }
}
