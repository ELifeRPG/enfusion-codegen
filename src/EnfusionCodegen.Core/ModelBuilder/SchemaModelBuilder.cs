using EnfusionCodegen.Core.Model;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace EnfusionCodegen.Core.ModelBuilder;

public static class SchemaModelBuilder
{
    public static (IReadOnlyList<EsClass> Classes, IReadOnlyList<EsEnum> Enums, IReadOnlyList<string> SkippedSchemas) Build(OpenApiDocument document)
    {
        var classes = new List<EsClass>();
        var enums = new List<EsEnum>();
        var skipped = new List<string>();

        foreach (var (name, schema) in document.Components.Schemas)
        {
            if (schema.Type == "integer" && schema.Enum.Count > 0)
            {
                enums.Add(BuildEnum(name, schema));
            }
            else if (schema.Type == "object")
            {
                classes.Add(BuildClass(name, schema));
            }
            else
            {
                skipped.Add(name);
            }
        }

        return (classes, enums, skipped);
    }

    private static EsClass BuildClass(string name, OpenApiSchema schema)
    {
        var properties = schema.Properties
            .Select(p => new EsProperty(p.Key, EsType.FromSchema(p.Value)))
            .ToList();

        return new EsClass(name, "JsonApiStruct", properties);
    }

    private static EsEnum BuildEnum(string name, OpenApiSchema schema)
    {
        var names = GetEnumNames(schema, schema.Enum.Count);
        var members = schema.Enum
            .Cast<OpenApiInteger>()
            .Select((v, i) => new EsEnumMember(names[i], v.Value))
            .ToList();

        return new EsEnum(name, members);
    }

    private static IReadOnlyList<string> GetEnumNames(OpenApiSchema schema, int count)
    {
        // x-enum-varnames (OpenAPI Generator convention) takes precedence over
        // x-enumNames (NSwag convention) when a schema carries both.
        if (schema.Extensions.TryGetValue("x-enum-varnames", out var varnamesRaw) && varnamesRaw is OpenApiArray varnamesArray)
        {
            return varnamesArray.Cast<OpenApiString>().Select(s => s.Value).ToList();
        }

        if (schema.Extensions.TryGetValue("x-enumNames", out var raw) && raw is OpenApiArray array)
        {
            return array.Cast<OpenApiString>().Select(s => s.Value).ToList();
        }

        return Enumerable.Range(0, count).Select(i => $"Value{i}").ToList();
    }
}
