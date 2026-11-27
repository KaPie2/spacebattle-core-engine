using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using App;

namespace SpaceBattle.Lib;

public class AdapterCodeGenerator
{
    private readonly string? _template;

    public AdapterCodeGenerator(string? template = null)
    {
        _template = template;
    }

    private string GetAdapterTemplate()
    {
        if (!string.IsNullOrWhiteSpace(_template))
            return _template;

        // попытка разрешения шаблона через ioc
        try
        {
            var iocTemplate = Ioc.Resolve<string>("Adapters.Template");
            if (!string.IsNullOrWhiteSpace(iocTemplate))
                return iocTemplate;
        }
        catch
        {
            // пропуск при отсутствии регистрации в ioc
        }

        return ReadTemplate("AdapterTemplate.txt");
    }

    private static string ReadTemplate(string fileName)
    {
        var relativePath = Path.Combine("Templates", fileName);
        var outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        var templatePath = File.Exists(outputPath) ? outputPath : Path.GetFullPath(relativePath);
        return File.ReadAllText(templatePath);
    }

    private static (string Property, string Getter, string Setter) ReadPropertyTemplates()
    {
        const string getterMarker = "{{GetterTemplate}}";
        const string setterMarker = "{{SetterTemplate}}";
        var template = ReadTemplate("AdapterPropertyTemplate.txt");
        var getterStart = template.IndexOf(getterMarker, StringComparison.Ordinal);
        var setterStart = template.IndexOf(setterMarker, StringComparison.Ordinal);
        if (getterStart < 0 || setterStart < getterStart + getterMarker.Length)
            throw new InvalidDataException("В AdapterPropertyTemplate.txt должны быть секции GetterTemplate и SetterTemplate в указанном порядке.");

        return (
            template[..getterStart].TrimEnd(),
            template[(getterStart + getterMarker.Length)..setterStart].Trim('\r', '\n'),
            template[(setterStart + setterMarker.Length)..].Trim('\r', '\n')
        );
    }

    private static string GetAdapterClassName(Type interfaceType)
    {
        var name = interfaceType.Name;
        return name.StartsWith('I') ? $"{name[1..]}Adapter" : $"{name}Adapter";
    }

    internal static string GetRegistrationClassName(Type interfaceType) =>
        $"{GetAdapterClassName(interfaceType)}RegistrationCommand";

    internal static string GenerateRegistration(Type interfaceType)
    {
        return ReadTemplate("AdapterRegistrationTemplate.txt")
            .Replace("{{RegistrationClassName}}", GetRegistrationClassName(interfaceType))
            .Replace("{{FactoryClassName}}", $"{GetAdapterClassName(interfaceType)}Factory")
            .Replace("{{InterfaceName}}", interfaceType.FullName ?? interfaceType.Name)
            .Replace("{{InterfaceShortName}}", interfaceType.Name);
    }

    internal static IEnumerable<PropertyInfo> GetAdapterProperties(Type interfaceType)
    {
        return interfaceType.GetProperties()
            .Concat(interfaceType.GetInterfaces().SelectMany(parent => parent.GetProperties()))
            .DistinctBy(property => property.Name);
    }

    public string Generate(Type interfaceType)
    {
        // проверка типа интерфейса
        if (!interfaceType.IsInterface)
            throw new ArgumentException("Целевой тип обязан быть интерфейсом.", nameof(interfaceType));

        var interfaceName = interfaceType.FullName ?? interfaceType.Name;
        var adapterClassName = GetAdapterClassName(interfaceType);
        var factoryClassName = $"{adapterClassName}Factory";

        var (propertyTemplate, getterTemplate, setterTemplate) = ReadPropertyTemplates();
        var generatedProperties = new StringBuilder();
        foreach (var prop in GetAdapterProperties(interfaceType))
        {
            var propTypeName = prop.PropertyType.FullName ?? prop.PropertyType.Name;
            var accessors = new StringBuilder();
            if (prop.CanRead)
            {
                var adapterAttr = prop.GetCustomAttribute<AdapterAttribute>();
                var key = adapterAttr?.DependencyKey ?? $"{interfaceType.Name}.{prop.Name}.Get";
                accessors.AppendLine(getterTemplate
                    .Replace("{{PropertyType}}", propTypeName)
                    .Replace("{{DependencyKey}}", key));
            }
            if (prop.CanWrite)
            {
                accessors.AppendLine(setterTemplate.Replace("{{DependencyKey}}", $"{interfaceType.Name}.{prop.Name}.Set"));
            }

            generatedProperties.AppendLine(propertyTemplate
                .Replace("{{PropertyType}}", propTypeName)
                .Replace("{{PropertyName}}", prop.Name)
                .Replace("{{Accessors}}", accessors.ToString().TrimEnd()));
            generatedProperties.AppendLine();
        }

        var adapterTemplate = GetAdapterTemplate();
        if (adapterTemplate.Contains("{{Factory}}", StringComparison.Ordinal))
            adapterTemplate = adapterTemplate.Replace("{{Factory}}", ReadTemplate("AdapterFactoryTemplate.txt").TrimEnd());

        var code = adapterTemplate
            .Replace("{{AdapterClassName}}", adapterClassName)
            .Replace("{{InterfaceName}}", interfaceName)
            .Replace("{{FactoryClassName}}", factoryClassName)
            .Replace("{{Properties}}", generatedProperties.ToString().TrimEnd());

        return code.TrimEnd() + Environment.NewLine;
    }

    // генерация и сохранение исходного кода адаптера в файл
    public string GenerateToFile(Type interfaceType, string filePath)
    {
        // генерация c#-кода по шаблону
        var code = Generate(interfaceType);

        // запись сгенерированного текста в файл
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(filePath, code);

        return code;
    }
}
