using System.Text;
using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.Writers;

public static class EnumWriter
{
    public static string Write(EsEnum esEnum)
    {
        var sb = new StringBuilder();
        sb.Append($"enum {esEnum.Name}\n{{\n");

        foreach (var member in esEnum.Members)
        {
            sb.Append($"\t{member.Name} = {member.Value},\n");
        }

        sb.Append("}\n");
        return sb.ToString();
    }
}
