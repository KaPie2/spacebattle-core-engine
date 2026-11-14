using System;
using System.IO;
using System.Linq;
using System.Reflection;
using App;
using App.Scopes;
using Xunit;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Tests;

public class AdapterGeneratorTests
{
    public AdapterGeneratorTests()
    {
        // инициализация скоупов и установка тестового контекста для изоляции
        new InitCommand().Execute();
        var testScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", testScope).Execute();
    }

    [Fact]
    public void AdapterBuilder_ThrowsArgumentException_WhenNotInterface()
    {
        // arrange (подготовка генератора кода)
        var generator = new AdapterCodeGenerator();

        // act & assert (проверка выброса исключения для неинтерфейсного типа)
        Assert.Throws<ArgumentException>(() => generator.Generate(typeof(NVector)));
    }

    [Fact]
    public void AdapterBuilder_GeneratesValidProperties_ForMovingObject()
    {
        // arrange (подготовка генератора кода)
        var generator = new AdapterCodeGenerator();

        // act (генерация кода адаптера для imovingobject)
        var generatedCode = generator.Generate(typeof(IMovingObject));

        // assert (проверка наличия объявлений классов адаптера и фабрики)
        Assert.Contains("public class MovingObjectAdapter : SpaceBattle.Lib.IMovingObject", generatedCode);
        Assert.Contains("public class MovingObjectAdapterFactory : IAdapterFactory<SpaceBattle.Lib.IMovingObject>", generatedCode);

        // assert (проверка формирования вызовов ioc.resolve для свойств)
        Assert.Contains("get => Ioc.Resolve<SpaceBattle.Lib.NVector>(\"IMovingObject.Position.Get\", _obj);", generatedCode);
        Assert.Contains("set => Ioc.Resolve<App.ICommand>(\"IMovingObject.Position.Set\", _obj, value).Execute();", generatedCode);

        // assert (проверка формирования вызова стратегии по атрибуту adapter)
        Assert.Contains("get => Ioc.Resolve<SpaceBattle.Lib.NVector>(\"IMovingObject.Velocity.GetVelocityByAngle\", _obj);", generatedCode);
    }

    [Fact]
    public void Reflection_Reads_AdapterAttribute_Metadata()
    {
        // arrange (получение типа интерфейса через рефлексию)
        Type targetType = typeof(IMovingObject);

        // act (получение свойства velocity и атрибута adapter)
        var velocityProp = targetType.GetProperty("Velocity");
        Assert.NotNull(velocityProp);
        var attr = velocityProp.GetCustomAttribute<AdapterAttribute>();

        // assert (проверка метаданных атрибута adapter)
        Assert.NotNull(attr);
        Assert.Equal(typeof(IMovingObject), attr.TargetInterface);
        Assert.Equal("Velocity", attr.PropertyName);
        Assert.Equal(typeof(MovementCalculations), attr.StrategyType);
        Assert.Equal("GetVelocityByAngle", attr.MethodName);
    }

    [Fact]
    public void GenerateToFile_CreatesTxtFileWithGeneratedCode()
    {
        // arrange (подготовка генератора и пути к временному файлу)
        var generator = new AdapterCodeGenerator();
        var tempFile = Path.Combine(Path.GetTempPath(), $"Adapter_{Guid.NewGuid()}.txt");

        try
        {
            // act (генерация кода в txt-файл)
            var generatedCode = generator.GenerateToFile(typeof(IMovingObject), tempFile);

            // assert (проверка создания файла и совпадения содержимого)
            Assert.True(File.Exists(tempFile));
            var fileContent = File.ReadAllText(tempFile);
            Assert.Equal(generatedCode, fileContent);
            Assert.Contains("MovingObjectAdapter", fileContent);
        }
        finally
        {
            // очистка временного файла
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void AdapterCodeGenerator_UsesCustomTemplate_WhenProvided()
    {
        // arrange (подготовка кастомного шаблона)
        var customTemplate = "// Custom template\npublic class {{AdapterClassName}} : {{InterfaceName}} {\n{{Properties}}\n}";
        var generator = new AdapterCodeGenerator(customTemplate);

        // act (генерация кода по кастомному шаблону)
        var code = generator.Generate(typeof(IMovingObject));

        // assert (проверка применения кастомного шаблона)
        Assert.StartsWith("// Custom template", code);
        Assert.Contains("MovingObjectAdapter", code);
    }

    [Fact]
    public void AdapterCodeGenerator_ResolvesTemplateFromIoC_WhenRegistered()
    {
        // arrange (регистрация изолированного скоупа и шаблона в ioc)
        var originalScope = Ioc.Resolve<object>("IoC.Scope.Current");
        var isolatedScope = Ioc.Resolve<object>("IoC.Scope.Create");
        Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", isolatedScope).Execute();

        try
        {
            var iocTemplate = "// IoC template: {{AdapterClassName}}\n{{Properties}}";
            Ioc.Resolve<App.ICommand>("IoC.Register", "Adapter.Template", (object[] args) => iocTemplate).Execute();

            var generator = new AdapterCodeGenerator();

            // act (генерация кода)
            var code = generator.Generate(typeof(IMovingObject));

            // assert (проверка использования шаблона из ioc)
            Assert.StartsWith("// IoC template: MovingObjectAdapter", code);
        }
        finally
        {
            // восстановление исходного скоупа для предотвращения утечки состояния
            Ioc.Resolve<App.ICommand>("IoC.Scope.Current.Set", originalScope).Execute();
        }
    }
}
