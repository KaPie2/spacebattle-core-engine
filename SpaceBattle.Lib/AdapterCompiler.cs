using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SpaceBattle.Lib;

public class AdapterCompiler
{
    public Type Compile(string sourceCode, string className)
    {
        // текст в синтаксическое дерево C#
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // сбор ссылок на все загруженные сборки
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>();

        // настройка параметров компиляции (создание dll)
        var compilation = CSharpCompilation.Create(
            $"DynamicAdapter_{Guid.NewGuid()}",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // компиляция кода в поток памяти
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = string.Join("\n", result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage()));
            throw new InvalidOperationException($"Ошибка компиляции адаптера:\n{errors}");
        }

        // загрузка готовой сборки из памяти и возврат типа
        ms.Seek(0, SeekOrigin.Begin);
        var assembly = Assembly.Load(ms.ToArray());
        return assembly.GetType(className) ?? throw new TypeLoadException($"Тип {className} не найден в сгенерированной сборке.");
    }
}
