using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SpaceBattle.Lib;

public class AdapterCodeGenerator
{
    public string Generate(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
            throw new ArgumentException("Целевой тип обязан быть интерфейсом.", nameof(interfaceType));

        var interfaceName = interfaceType.FullName ?? interfaceType.Name;
        var cleanName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;

        var adapterClassName = $"{cleanName}Adapter";
        var factoryClassName = $"{cleanName}AdapterFactory";

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using App;");
        sb.AppendLine("using SpaceBattle.Lib;");
        sb.AppendLine();
        sb.AppendLine("namespace SpaceBattle.Lib.Generated");
        sb.AppendLine("{");

        // Класс Адаптера
        sb.AppendLine($"    public class {adapterClassName} : {interfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IDictionary<string, object> _obj;");
        sb.AppendLine();
        sb.AppendLine($"        public {adapterClassName}(IDictionary<string, object> obj)");
        sb.AppendLine("        {");
        sb.AppendLine("            _obj = obj ?? throw new ArgumentNullException(nameof(obj));");
        sb.AppendLine("        }");
        sb.AppendLine();

        var properties = new List<PropertyInfo>(interfaceType.GetProperties());
        foreach (var parentInterface in interfaceType.GetInterfaces())
        {
            properties.AddRange(parentInterface.GetProperties());
        }

        foreach (var prop in properties)
        {
            var propTypeName = prop.PropertyType.FullName ?? prop.PropertyType.Name;
            sb.AppendLine($"        public {propTypeName} {prop.Name}");
            sb.AppendLine("        {");
            if (prop.CanRead)
            {
                sb.AppendLine($"            get => Ioc.Resolve<{propTypeName}>(\"{interfaceType.Name}.{prop.Name}.Get\", _obj);");
            }
            if (prop.CanWrite)
            {
                sb.AppendLine($"            set => Ioc.Resolve<App.ICommand>(\"{interfaceType.Name}.{prop.Name}.Set\", _obj, value).Execute();");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // Класс Фабрики
        sb.AppendLine($"    public class {factoryClassName} : IAdapterFactory<{interfaceName}>");
        sb.AppendLine("    {");
        sb.AppendLine($"        public {interfaceName} Create(IDictionary<string, object> obj)");
        sb.AppendLine("        {");
        sb.AppendLine($"            return new {adapterClassName}(obj);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
