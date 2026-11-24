using App;

namespace SpaceBattle.Lib;

public class RegisterAdapterCommand<T> : ICommand
{
    public void Execute()
    {
        var interfaceType = typeof(T);
        var (factory, sourceCode) = AdapterFactoryBuilder.Build<T>();
        Func<object[], object> createAdapter = args => factory.Create((IDictionary<string, object>)args[0])!;
        Func<object[], object> getSource = _ => sourceCode;

        Register($"Adapters.{interfaceType.Name}", createAdapter);
        Register($"Adapter.{interfaceType.Name}", createAdapter);
        Register($"Adapters.Source.{interfaceType.Name}", getSource);
        Register($"Adapter.Source.{interfaceType.Name}", getSource);

        foreach (var (key, strategy) in AdapterFactoryBuilder.GetAttributeStrategies(interfaceType))
        {
            try
            {
                Register(key, strategy);
            }
            catch
            {
                // Сохраняем ранее зарегистрированную стратегию.
            }
        }
    }

    private static void Register(string key, Func<object[], object> strategy)
    {
        Ioc.Resolve<App.ICommand>("IoC.Register", key, strategy).Execute();
    }
}
