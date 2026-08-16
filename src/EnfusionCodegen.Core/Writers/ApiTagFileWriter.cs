using System.Text;
using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.Writers;

public static class ApiTagFileWriter
{
    public static string Write(
        string tag,
        IReadOnlyList<EsOperation> operationsForTag,
        string prefix,
        IReadOnlyDictionary<string, string> callbackClassByResponseType)
    {
        var sb = new StringBuilder();
        sb.Append($"modded class {prefix}Api\n{{\n");

        for (var i = 0; i < operationsForTag.Count; i++)
        {
            sb.Append(ApiMethodWriter.Write(operationsForTag[i], prefix, callbackClassByResponseType));
            if (i < operationsForTag.Count - 1)
            {
                sb.Append('\n');
            }
        }

        sb.Append("}\n");
        return sb.ToString();
    }
}
