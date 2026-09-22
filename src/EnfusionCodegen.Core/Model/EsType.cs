using EnfusionCodegen.Core.OpenApi;

namespace EnfusionCodegen.Core.Model;

public class EsType
{
    public required string TypeName { get; init; }
    public string? BaseTypeName { get; init; }
    public string? DefaultValueLiteral { get; init; }
    public bool IsEnum { get; init; }
    public bool IsArray { get; init; }
    public bool IsReference { get; init; }
    public bool IsNullable { get; init; }

    public static EsType FromSchema(OpenApiSchema schema)
    {
        if (schema.HasType(OpenApiSchemaType.Array))
        {
            var itemType = schema.Items is null ? new EsType { TypeName = string.Empty } : FromSchema(schema.Items);
            var itemTypeName = (itemType.IsReference && !itemType.IsEnum) || itemType.BaseTypeName is not null
                ? $"ref {itemType.TypeName}"
                : itemType.TypeName;

            return new EsType
            {
                TypeName = $"ref array<{itemTypeName}>",
                DefaultValueLiteral = "{}",
                IsArray = true,
                IsNullable = schema.IsNullable,
            };
        }

        if (schema.IntegerEnumValues.Count > 0 || schema.StringEnumValues.Count > 0)
        {
            return new EsType
            {
                TypeName = schema.ReferenceName ?? (schema.IntegerEnumValues.Count > 0 ? "int" : "string"),
                IsEnum = true,
                IsReference = schema.ReferenceName is not null,
                IsNullable = schema.IsNullable,
            };
        }

        var nonNullableType = GetNonNullableType(schema.Types);
        if (nonNullableType != OpenApiSchemaType.None)
        {
            var typeName = nonNullableType switch
            {
                OpenApiSchemaType.Integer => "int",
                OpenApiSchemaType.Number => "float",
                OpenApiSchemaType.Boolean => "bool",
                OpenApiSchemaType.Object => schema.ReferenceName ?? "class",
                OpenApiSchemaType.String => "string",
                _ => string.Empty,
            };

            return new EsType
            {
                TypeName = typeName,
                BaseTypeName = nonNullableType == OpenApiSchemaType.Object ? "JsonApiStruct" : null,
                IsReference = nonNullableType == OpenApiSchemaType.Object && schema.ReferenceName is not null,
                IsNullable = schema.IsNullable,
            };
        }

        if (!string.IsNullOrEmpty(schema.ReferenceName))
        {
            return new EsType
            {
                TypeName = schema.ReferenceName,
                IsReference = true,
                IsNullable = schema.IsNullable,
            };
        }

        return new EsType { TypeName = string.Empty, IsNullable = schema.IsNullable };
    }

    private static OpenApiSchemaType GetNonNullableType(OpenApiSchemaType types) =>
        types & ~OpenApiSchemaType.Null;
}
