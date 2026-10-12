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

        // The callback is returned so callers can read GetHttpCode() from their handler.
        sb.Append($"\t{callbackClass} {operation.OperationName}({string.Join(", ", signatureParams)})\n\t{{\n");
        sb.Append($"\t\t{callbackClass} cbx = new {callbackClass};\n");

        var additionalDataArg = pathParams.Count > 0 && pathParams[0].Type.TypeName == "string"
            ? $", {pathParams[0].Name}"
            : string.Empty;
        sb.Append($"\t\tcbx.SetCallback(instance, functionName{additionalDataArg});\n");

        if (operation.RequestBodyType is not null)
        {
            sb.Append("\t\tbody.Pack();\n");
        }

        var dataArgument = operation.RequestBodyType is not null
            ? ", body.AsString()"
            : operation.Verb != EsHttpVerb.Get ? ", \"\"" : string.Empty;
        var verbName = operation.Verb.ToString().ToUpperInvariant();

        if (queryParams.Count == 0)
        {
            sb.Append($"\t\tGetElifeApi().{verbName}(cbx, {PathExpression(operation.PathTemplate, pathParams)}{dataArgument});\n");
        }
        else
        {
            AppendRouteWithQuery(sb, operation.PathTemplate, pathParams, queryParams);
            sb.Append($"\t\tGetElifeApi().{verbName}(cbx, requestRoute{dataArgument});\n");
        }
        sb.Append("\t\treturn cbx;\n");
        sb.Append("\t}\n");

        return sb.ToString();
    }

    private static string PathExpression(string pathTemplate, IReadOnlyList<EsParameter> pathParams)
    {
        return pathParams.Count == 0
            ? $"\"{pathTemplate}\""
            : $"string.Format(\"{FormatPathForStringFormat(pathTemplate)}\", {string.Join(", ", pathParams.Select(p => p.Name))})";
    }

    // Empty string params are left out: ASP.NET treats "?since=" as an unparseable value (400), not a missing one.
    private static void AppendRouteWithQuery(
        StringBuilder sb,
        string pathTemplate,
        IReadOnlyList<EsParameter> pathParams,
        IReadOnlyList<EsParameter> queryParams)
    {
        var path = pathTemplate[..pathTemplate.IndexOf('?')];
        sb.Append($"\t\tstring requestRoute = {PathExpression(path, pathParams)};\n");
        sb.Append("\t\tstring requestQuery = \"\";\n");

        foreach (var param in queryParams)
        {
            var append = $"requestQuery += string.Format(\"&{param.Name}=%1\", {param.Name});\n";
            if (param.Type.TypeName == "string")
            {
                sb.Append($"\t\tif ({param.Name} != \"\")\n\t\t\t{append}");
            }
            else
            {
                sb.Append($"\t\t{append}");
            }
        }

        sb.Append("\t\tif (requestQuery != \"\")\n");
        sb.Append("\t\t\trequestRoute += \"?\" + requestQuery.Substring(1, requestQuery.Length() - 1);\n");
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
