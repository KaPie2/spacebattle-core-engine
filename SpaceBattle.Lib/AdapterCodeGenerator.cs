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

    public const string DefaultTemplate = @"using System;
using System.Collections.Generic;
using App;
using SpaceBattle.Lib;

namespace SpaceBattle.Lib.Generated
{
    public class {{AdapterClassName}} : {{InterfaceName}}
    {
        private readonly IDictionary<string, object> _obj;

        public {{AdapterClassName}}(IDictionary<string, object> obj)
        {
            _obj = obj ?? throw new ArgumentNullException(nameof(obj));
        }

{{Properties}}
    }

    public class {{FactoryClassName}} : IAdapterFactory<{{InterfaceName}}>
    {
        public {{InterfaceName}} Create(IDictionary<string, object> obj)
        {
            return new {{AdapterClassName}}(obj);
        }
    }
}";

    public AdapterCodeGenerator(string? template = null)
    {
        _template = template;
    }

    // получение актуального шаблона
    private string GetTemplate()
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

        // попытка чтения шаблона из txt-файла
        var templateFile = Path.Combine(AppContext.BaseDirectory, "AdapterTemplate.txt");
        if (File.Exists(templateFile))
        {
            return File.ReadAllText(templateFile);
        }

        if (File.Exists("AdapterTemplate.txt"))
        {
            return File.ReadAllText("AdapterTemplate.txt");
        }

        return DefaultTemplate;
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

        // генерация свойств адаптера
        var sbProps = new StringBuilder();
        foreach (var prop in properties)
        {
            var propTypeName = prop.PropertyType.FullName ?? prop.PropertyType.Name;
            sbProps.AppendLine($"        public {propTypeName} {prop.Name}");
            sbProps.AppendLine("        {");
            if (prop.CanRead)
            {
                var adapterAttr = prop.GetCustomAttribute<AdapterAttribute>();
                if (adapterAttr != null)
                {
                    // вызов стратегии вычисления, заданной атрибутом
                    sbProps.AppendLine($"            get => Ioc.Resolve<{propTypeName}>(\"{adapterAttr.TargetInterface.Name}.{adapterAttr.PropertyName}.{adapterAttr.MethodName}\", _obj);");
                }
                else
                {
                    // вызов стандартной стратегии получения значения
                    sbProps.AppendLine($"            get => Ioc.Resolve<{propTypeName}>(\"{interfaceType.Name}.{prop.Name}.Get\", _obj);");
                }
            }
            if (prop.CanWrite)
            {
                // вызов команды установки значения
                sbProps.AppendLine($"            set => Ioc.Resolve<App.ICommand>(\"{interfaceType.Name}.{prop.Name}.Set\", _obj, value).Execute();");
            }
            sbProps.AppendLine("        }");
            sbProps.AppendLine();
        }

        // подстановка переменных в шаблон
        var template = GetTemplate();
        var code = template
            .Replace("{{AdapterClassName}}", adapterClassName)
            .Replace("{{InterfaceName}}", interfaceName)
            .Replace("{{FactoryClassName}}", factoryClassName)
            .Replace("{{Properties}}", sbProps.ToString().TrimEnd());

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
