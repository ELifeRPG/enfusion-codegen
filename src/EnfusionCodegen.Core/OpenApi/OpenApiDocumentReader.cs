using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace EnfusionCodegen.Core.OpenApi;

public class OpenApiDocumentReader
{
    private static readonly Regex NullableYamlType = new(
        @"^(?<indent>\s*)(?<key>[""']?type[""']?)\s*:\s*\[\s*(?:(?<type>[""'][^""']+[""']|[A-Za-z][\w-]*)\s*,\s*[""']?null[""']?|[""']?null[""']?\s*,\s*(?<type>[""'][^""']+[""']|[A-Za-z][\w-]*))\s*\](?<comment>\s+#.*)?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex NumericStringYamlType = new(
        @"^(?<indent>\s*)(?<key>[""']?type[""']?)\s*:\s*\[\s*(?:(?<type>[""']?(?:integer|number)[""']?)\s*,\s*[""']?string[""']?|[""']?string[""']?\s*,\s*(?<type>[""']?(?:integer|number)[""']?))\s*(?<nullable>,\s*[""']?null[""']?)?\s*\](?<comment>\s+#.*)?$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly OpenApiReaderSettings Settings = CreateSettings();

    private readonly HttpClient _httpClient = new();

    public (OpenApiDocument Document, OpenApiDiagnostic Diagnostic) Read(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return ReadInternal(stream);
    }

    public async Task<(OpenApiDocument Document, OpenApiDiagnostic Diagnostic)> ReadFromUriAsync(Uri uri)
    {
        await using var stream = await _httpClient.GetStreamAsync(uri);
        return ReadInternal(stream);
    }

    private static (OpenApiDocument Document, OpenApiDiagnostic Diagnostic) ReadInternal(Stream input)
    {
        using var textReader = new StreamReader(input, leaveOpen: true);
        var spec = NormalizeSchemaTypeUnions(textReader.ReadToEnd());
        using var normalizedInput = new MemoryStream(Encoding.UTF8.GetBytes(spec));
        var result = Microsoft.OpenApi.OpenApiDocument.LoadAsync(normalizedInput, settings: Settings).GetAwaiter().GetResult();
        return (result.Document is null ? new OpenApiDocument() : OpenApiInputMapper.Map(result.Document),
            result.Diagnostic is null ? new OpenApiDiagnostic() : MapDiagnostic(result.Diagnostic));
    }

    private static string NormalizeSchemaTypeUnions(string spec)
    {
        if (!spec.TrimStart().StartsWith('{'))
        {
            var normalized = NumericStringYamlType.Replace(spec, NormalizeNumericStringYamlType);
            return NullableYamlType.Replace(normalized, "${indent}${key}: ${type}${comment}\n${indent}nullable: true\n${indent}x-enfusion-codegen-nullable: true");
        }

        var root = JsonNode.Parse(spec);
        if (root is null)
        {
            return spec;
        }

        NormalizeSchemaTypeUnions(root);
        return root.ToJsonString();
    }

    private static string NormalizeNumericStringYamlType(Match match)
    {
        var normalized = $"{match.Groups["indent"].Value}{match.Groups["key"].Value}: {match.Groups["type"].Value}{match.Groups["comment"].Value}";
        return match.Groups["nullable"].Success
            ? $"{normalized}\n{match.Groups["indent"].Value}nullable: true\n{match.Groups["indent"].Value}x-enfusion-codegen-nullable: true"
            : normalized;
    }

    private static void NormalizeSchemaTypeUnions(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            NormalizeSchemaTypeUnion(obj);

            foreach (var child in obj.Select(property => property.Value))
            {
                if (child is not null)
                {
                    NormalizeSchemaTypeUnions(child);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array)
            {
                if (child is not null)
                {
                    NormalizeSchemaTypeUnions(child);
                }
            }
        }
    }

    private static void NormalizeSchemaTypeUnion(JsonObject schema)
    {
        if (schema["type"] is not JsonArray types || types.Count is < 1 or > 3)
        {
            return;
        }

        var typeNames = types
            .OfType<JsonValue>()
            .Select(value => value.TryGetValue<string>(out var type) ? type : null)
            .Where(type => type is not null)
            .Cast<string>()
            .ToList();
        var nullTypeCount = typeNames.Count(type => type == "null");
        var nonNullableTypes = typeNames.Where(type => type != "null").ToList();

        var selectedType = nonNullableTypes.Count == 1
            ? nonNullableTypes[0]
            : nonNullableTypes.Count == 2 && nonNullableTypes.Contains("string")
                ? nonNullableTypes.SingleOrDefault(type => type is "integer" or "number")
                : null;

        if (nullTypeCount > 1 || selectedType is null || types.Count != nonNullableTypes.Count + nullTypeCount)
        {
            return;
        }

        schema["type"] = selectedType;
        if (nullTypeCount == 1)
        {
            schema["nullable"] = true;
            schema["x-enfusion-codegen-nullable"] = true;
        }
    }

    private static OpenApiDiagnostic MapDiagnostic(Microsoft.OpenApi.Reader.OpenApiDiagnostic diagnostic)
    {
        return new OpenApiDiagnostic
        {
            Errors = diagnostic.Errors.Select(error => new OpenApiDiagnosticError(
                string.IsNullOrEmpty(error.Pointer) ? error.Message : $"{error.Pointer}: {error.Message}"))
                .ToList(),
        };
    }

    private static OpenApiReaderSettings CreateSettings()
    {
        var settings = new OpenApiReaderSettings();
        settings.AddYamlReader();
        return settings;
    }
}
