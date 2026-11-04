using System;
using System.Reflection;
using System.Text;

namespace SpaceBattle.Lib;

public class AdapterCodeGenerator
{
    public string Generate(Type interfaceType)
    {
        var sb = new StringBuilder();
        string className = interfaceType.Name.StartsWith("I")
            ? interfaceType.Name.Substring(1) + "Adapter"
            : interfaceType.Name + "Adapter";

        sb.AppendLine("using System;");
        sb.AppendLine("using SpaceBattle.Lib;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine($"public class {className} : {interfaceType.Name}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IDictionary<string, object> _obj;");
        sb.AppendLine($"    public {className}(IDictionary<string, object> obj) => _obj = obj;");
        sb.AppendLine();

        foreach (var prop in interfaceType.GetProperties())
        {
            string propType = prop.PropertyType.Name;
            sb.AppendLine($"    public {propType} {prop.Name}");
            sb.AppendLine("    {");

            // генерация тела геттера
            if (prop.CanRead)
            {
                sb.AppendLine($"        get => IoC.Resolve<{propType}>(\"{interfaceType.Name}.{prop.Name}.Get\", _obj);");
            }

            // генерация тела сеттера
            if (prop.CanWrite)
            {
                sb.AppendLine($"        set => IoC.Resolve<ICommand>(\"{interfaceType.Name}.{prop.Name}.Set\", _obj, value).Execute();");
            }

            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
