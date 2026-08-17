using System.Text;
using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.Writers;

public static class StructWriter
{
    public static string Write(EsClass esClass)
    {
        var sb = new StringBuilder();
        sb.Append($"class {esClass.Name} : {esClass.BaseTypeName}\n{{\n");

        foreach (var property in esClass.Properties)
        {
            var defaultInitializer = property.Type.DefaultValueLiteral is null
                ? string.Empty
                : $" = {property.Type.DefaultValueLiteral}";
            sb.Append($"\t{property.Type.TypeName} {property.JsonName}{defaultInitializer};\n");
        }

        sb.Append($"\n\tvoid {esClass.Name}()\n\t{{\n");
        foreach (var property in esClass.Properties)
        {
            sb.Append($"\t\tRegV(\"{property.JsonName}\");\n");
        }
        sb.Append("\t}\n}\n");

        return sb.ToString();
    }
}
