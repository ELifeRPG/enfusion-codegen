using Microsoft.OpenApi.Models;

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
        if (schema.Type == "array")
        {
            var itemType = FromSchema(schema.Items);
            var itemTypeName = (itemType.IsReference && !itemType.IsEnum) || itemType.BaseTypeName is not null
                ? $"ref {itemType.TypeName}"
                : itemType.TypeName;

            return new EsType
            {
                TypeName = $"ref array<{itemTypeName}>",
                DefaultValueLiteral = "{}",
                IsArray = true,
                IsNullable = schema.Nullable,
            };
        }

        if (schema.Type == "integer" && schema.Enum.Count > 0)
        {
            return new EsType
            {
                TypeName = schema.Reference?.Id ?? "int",
                IsEnum = true,
                IsReference = schema.Reference is not null,
                IsNullable = schema.Nullable,
            };
        }

        if (!string.IsNullOrEmpty(schema.Type))
        {
            var typeName = schema.Type switch
            {
                "integer" => "int",
                "number" => "float",
                "boolean" => "bool",
                "object" => schema.Reference?.Id ?? "class",
                _ => schema.Type,
            };

            return new EsType
            {
                TypeName = typeName,
                BaseTypeName = schema.Type == "object" ? "JsonApiStruct" : null,
                IsReference = schema.Type == "object" && schema.Reference is not null,
                IsNullable = schema.Nullable,
            };
        }

        if (!string.IsNullOrEmpty(schema.Reference?.Id))
        {
            return new EsType
            {
                TypeName = schema.Reference.Id,
                IsReference = true,
                IsNullable = schema.Nullable,
            };
        }

        return new EsType { TypeName = string.Empty, IsNullable = schema.Nullable };
    }
}
