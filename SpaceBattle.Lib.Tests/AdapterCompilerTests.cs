using System;
using System.Collections.Generic;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AdapterCompilerTests
{
    [Fact]
    public void RoslynCompiler_EmitsAssemblyAndCreatesInstance()
    {
        // arrange (генерация исходного кода для интерфейса)
        var generator = new AdapterCodeGenerator();
        string generatedCode = generator.Generate(typeof(IMovingObject));

        // act (компиляция кода в сборку)
        var compiler = new AdapterCompiler();
        string expectedFactoryName = "SpaceBattle.Lib.Generated.MovingObjectAdapterFactory";
        Type compiledFactoryType = compiler.Compile(generatedCode, expectedFactoryName);

        // assert (проверка типа фабрики и создание экземпляра адаптера)
        Assert.NotNull(compiledFactoryType);
        Assert.True(typeof(IAdapterFactory<IMovingObject>).IsAssignableFrom(compiledFactoryType));

        var factory = (IAdapterFactory<IMovingObject>)Activator.CreateInstance(compiledFactoryType)!;
        var state = new Dictionary<string, object>();
        var adapter = factory.Create(state);

        Assert.NotNull(adapter);
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }

    [Fact]
    public void Compile_ThrowsInvalidOperationException_OnCompilationError()
    {
        // arrange (подготовка некорректного синтаксиса c#)
        var compiler = new AdapterCompiler();
        string invalidSource = "class BrokenSyntax { invalid syntax }";

        // act & assert (проверка выброса исключения при ошибке компиляции)
        Assert.Throws<InvalidOperationException>(() => compiler.Compile(invalidSource, "BrokenSyntax"));
    }

    [Fact]
    public void Compile_ThrowsTypeLoadException_WhenClassDoesNotExist()
    {
        // arrange (подготовка корректного кода с запросом несуществующего типа)
        var compiler = new AdapterCompiler();
        string validSource = "namespace Test { public class ExistingClass {} }";

        // act & assert (проверка выброса исключения при отсутствии типа в сборке)
        Assert.Throws<TypeLoadException>(() => compiler.Compile(validSource, "Test.NonExistentClass"));
    }
}
