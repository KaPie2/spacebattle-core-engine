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
        var generator = new AdapterCodeGenerator();
        string generatedCode = generator.Generate(typeof(IMovingObject));

        var compiler = new AdapterCompiler();
        string expectedFactoryName = "SpaceBattle.Lib.Generated.MovingObjectAdapterFactory";
        Type compiledFactoryType = compiler.Compile(generatedCode, expectedFactoryName);

        Assert.NotNull(compiledFactoryType);
        Assert.True(typeof(IAdapterFactory<IMovingObject>).IsAssignableFrom(compiledFactoryType));

        var factory = (IAdapterFactory<IMovingObject>)Activator.CreateInstance(compiledFactoryType)!;
        var state = new Dictionary<string, object>();
        var adapter = factory.Create(state);

        Assert.NotNull(adapter);
        Assert.IsAssignableFrom<IMovingObject>(adapter);
    }
}
