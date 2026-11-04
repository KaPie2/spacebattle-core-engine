namespace SpaceBattle.Lib.Tests;

public class AdapterCompilerTests
{
    [Fact]
    public void Compiler_Should_Create_Valid_Type_From_Generated_Code()
    {
        // генерация строки кода
        var generator = new AdapterCodeGenerator();
        Type targetInterface = typeof(IMovingObject);
        string generatedCode = generator.Generate(targetInterface);

        // компиляция строки в реальный тип
        var compiler = new AdapterCompiler();
        string expectedClassName = "MovingObjectAdapter";
        Type compiledType = compiler.Compile(generatedCode, expectedClassName);

        Assert.NotNull(compiledType);

        // проверка, что сгенерированный класс действительно реализует IMovingObject
        Assert.True(typeof(IMovingObject).IsAssignableFrom(compiledType));

        // попытка создать экземпляр скомпилированного класса
        var state = new Dictionary<string, object>();
        var adapterInstance = Activator.CreateInstance(compiledType, state);

        Assert.NotNull(adapterInstance);
        Assert.IsAssignableFrom<IMovingObject>(adapterInstance);
    }
}
