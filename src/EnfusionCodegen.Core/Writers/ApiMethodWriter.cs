using System.Text;
using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.Writers;

public static class ApiMethodWriter
{
    public static string Write(
        EsOperation operation,
        string prefix,
        IReadOnlyDictionary<string, string> callbackClassByResponseType)
    {
        var sb = new StringBuilder();
        var pathParams = operation.Parameters.Where(p => p.IsPathParameter).ToList();
        var queryParams = operation.Parameters.Where(p => !p.IsPathParameter).ToList();
        var positionalParams = pathParams.Concat(queryParams).ToList();
        var callbackClass = operation.ResponseType is not null && operation.ResponseType.IsReference
            ? callbackClassByResponseType[operation.ResponseType.TypeName]
            : $"{prefix}BaseRestCallback";

        var signatureParams = new List<string>();
        if (operation.RequestBodyType is not null)
        {
            signatureParams.Add($"{operation.RequestBodyType.TypeName} body");
        }
        signatureParams.AddRange(pathParams.Select(p => $"{p.Type.TypeName} {p.Name}"));
        signatureParams.AddRange(queryParams.Select(p => $"{p.Type.TypeName} {p.Name}"));
        signatureParams.Add("Managed instance = null");
        signatureParams.Add("string functionName = \"\"");

        sb.Append($"\tvoid {operation.OperationName}({string.Join(", ", signatureParams)})\n\t{{\n");
        sb.Append($"\t\t{callbackClass} cbx = new {callbackClass};\n");

        var additionalDataArg = pathParams.Count > 0 && pathParams[0].Type.TypeName == "string"
            ? $", {pathParams[0].Name}"
            : string.Empty;
        sb.Append($"\t\tcbx.SetCallback(instance, functionName{additionalDataArg});\n");

        if (operation.RequestBodyType is not null)
        {
            sb.Append("\t\tbody.Pack();\n");
        }

        var formattedPath = FormatPathForStringFormat(operation.PathTemplate);
        var pathExpression = positionalParams.Count == 0
            ? $"\"{operation.PathTemplate}\""
            : $"string.Format(\"{formattedPath}\", {string.Join(", ", positionalParams.Select(p => p.Name))})";

        var dataArgument = operation.RequestBodyType is not null
            ? ", body.AsString()"
            : operation.Verb != EsHttpVerb.Get ? ", \"\"" : string.Empty;
        var verbName = operation.Verb.ToString().ToUpperInvariant();

        sb.Append($"\t\tGetElifeApi().{verbName}(cbx, {pathExpression}{dataArgument});\n");
        sb.Append("\t}\n");

        return sb.ToString();
    }

    private static string FormatPathForStringFormat(string pathTemplate)
    {
        // EsOperation stores {0}, {1}, ... zero-based placeholders internally;
        // Enforce Script's string.Format uses 1-based %1, %2, ... placeholders.
        return System.Text.RegularExpressions.Regex.Replace(
            pathTemplate,
            @"\{(\d+)\}",
            m => $"%{int.Parse(m.Groups[1].Value) + 1}");
    }
}
