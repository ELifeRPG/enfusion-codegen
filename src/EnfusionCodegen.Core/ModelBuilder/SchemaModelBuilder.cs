using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.OpenApi;

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
            if (schema.IntegerEnumValues.Count > 0 || schema.StringEnumValues.Count > 0)
            {
                enums.Add(BuildEnum(name, schema));
            }
            else if (schema.HasType(OpenApiSchemaType.Object))
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
            .Select(property => new { property.Key, Type = EsType.FromSchema(property.Value) })
            // Enforce Script has no anonymous object field type. Preserve named
            // object references, but omit free-form objects such as additionalData.
            .Where(property => property.Type.TypeName != "class")
            .Select(property => new EsProperty(property.Key, property.Type))
            .ToList();

        return new EsClass(name, "JsonApiStruct", properties);
    }

    private static EsEnum BuildEnum(string name, OpenApiSchema schema)
    {
        var values = schema.IntegerEnumValues.Count > 0
            ? schema.IntegerEnumValues
            : Enumerable.Range(0, schema.StringEnumValues.Count).ToList();
        var names = GetEnumNames(schema, values.Count);
        var members = values
            .Select((value, i) => new EsEnumMember(names[i], value))
            .ToList();

        return new EsEnum(name, members);
    }

    private static IReadOnlyList<string> GetEnumNames(OpenApiSchema schema, int count)
    {
        // x-enum-varnames (OpenAPI Generator convention) takes precedence over
        // x-enumNames (NSwag convention) when a schema carries both.
        if (schema.Extensions.TryGetValue("x-enum-varnames", out var varnames))
        {
            return varnames;
        }

        if (schema.Extensions.TryGetValue("x-enumNames", out var names))
        {
            return names;
        }

        return Enumerable.Range(0, count).Select(i => $"Value{i}").ToList();
    }
}
