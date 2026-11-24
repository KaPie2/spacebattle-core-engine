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
            var iocTemplate = Ioc.Resolve<string>("Adapter.Template");
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
        var outputPath = Path.Combine(AppContext.BaseDirectory, fileName);
        var templatePath = File.Exists(outputPath) ? outputPath : Path.GetFullPath(fileName);
        return File.ReadAllText(templatePath);
    }

    public string Generate(Type interfaceType)
    {
        // проверка типа интерфейса
        if (!interfaceType.IsInterface)
            throw new ArgumentException("Целевой тип обязан быть интерфейсом.", nameof(interfaceType));

        var interfaceName = interfaceType.FullName ?? interfaceType.Name;
        var cleanName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;

        var adapterClassName = $"{cleanName}Adapter";
        var factoryClassName = $"{cleanName}AdapterFactory";

        // получение уникальных свойств с учетом унаследованных интерфейсов
        var properties = new List<PropertyInfo>(interfaceType.GetProperties());
        foreach (var parentInterface in interfaceType.GetInterfaces())
        {
            properties.AddRange(parentInterface.GetProperties());
        }
        properties = properties.GroupBy(p => p.Name).Select(g => g.First()).ToList();

        var propertyTemplate = ReadTemplate("AdapterPropertyTemplate.txt").TrimEnd();
        var getterTemplate = ReadTemplate("AdapterGetterTemplate.txt").TrimEnd();
        var setterTemplate = ReadTemplate("AdapterSetterTemplate.txt").TrimEnd();
        var generatedProperties = new StringBuilder();
        foreach (var prop in properties)
        {
            var propTypeName = prop.PropertyType.FullName ?? prop.PropertyType.Name;
            var accessors = new StringBuilder();
            if (prop.CanRead)
            {
                var adapterAttr = prop.GetCustomAttribute<AdapterAttribute>();
                var key = adapterAttr == null
                    ? $"{interfaceType.Name}.{prop.Name}.Get"
                    : $"{adapterAttr.TargetInterface.Name}.{adapterAttr.PropertyName}.{adapterAttr.MethodName}";
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

        // подстановка переменных в шаблон
        var code = GetAdapterTemplate()
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
