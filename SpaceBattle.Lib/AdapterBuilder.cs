using App;

namespace SpaceBattle.Lib;

public class AdapterBuilder
{
    private readonly AdapterCodeGenerator _generator = new();
    private readonly AdapterCompiler _compiler = new();

    public void BuildAndRegister(Type interfaceType)
    {
        string key = $"Adapter.{interfaceType.Name}";

        // генерация кода
        string code = _generator.Generate(interfaceType);

        // вычисление имя класса (совпадает с логикой генератора)
        string className = interfaceType.Name.StartsWith("I")
            ? interfaceType.Name.Substring(1) + "Adapter"
            : interfaceType.Name + "Adapter";

        // компиляция типа
        Type compiledType = _compiler.Compile(code, className);

        // регистрация быстрой функции-фабрики в IoC-контейнере
        Ioc.Resolve<App.ICommand>("IoC.Register", key, (object[] args) =>
        {
            var state = (IDictionary<string, object>)args[0];
            return Activator.CreateInstance(compiledType, state);
        }).Execute();
    }
}
