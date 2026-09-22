using Microsoft.OpenApi;
using System.Globalization;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace EnfusionCodegen.Core.OpenApi;

internal static class OpenApiInputMapper
{
    public static OpenApiDocument Map(Microsoft.OpenApi.OpenApiDocument document) => new()
    {
        Info = new OpenApiInfo { Title = document.Info?.Title },
        Components = new OpenApiComponents
        {
            Schemas = (document.Components?.Schemas ?? new Dictionary<string, IOpenApiSchema>())
                .ToDictionary(schema => schema.Key, schema => Map(schema.Value)),
        },
        Paths = document.Paths?.ToDictionary(path => path.Key, path => Map(path.Value))
            ?? new Dictionary<string, OpenApiPathItem>(),
    };

    private static OpenApiPathItem Map(IOpenApiPathItem pathItem) => new()
    {
        Operations = pathItem is Microsoft.OpenApi.OpenApiPathItem item
            ? (item.Operations ?? new Dictionary<HttpMethod, Microsoft.OpenApi.OpenApiOperation>())
                .Where(operation => TryMapMethod(operation.Key, out _))
                .ToDictionary(operation => MapMethod(operation.Key), operation => Map(operation.Value))
            : new Dictionary<OpenApiHttpMethod, OpenApiOperation>(),
    };

    private static OpenApiOperation Map(Microsoft.OpenApi.OpenApiOperation operation) => new()
    {
        OperationId = operation.OperationId,
        Tags = operation.Tags?.Select(tag => tag.Name).Where(name => !string.IsNullOrEmpty(name)).Cast<string>().ToList() ?? [],
        Parameters = operation.Parameters?.Select(Map).ToList() ?? [],
        RequestBody = operation.RequestBody is null ? null : Map(operation.RequestBody),
        Responses = operation.Responses?.ToDictionary(response => response.Key, response => Map(response.Value))
            ?? new Dictionary<string, OpenApiResponse>(),
    };

    private static OpenApiParameter Map(IOpenApiParameter parameter) => new()
    {
        Name = parameter.Name ?? string.Empty,
        Location = parameter.In switch
        {
            ParameterLocation.Path => OpenApiParameterLocation.Path,
            ParameterLocation.Query => OpenApiParameterLocation.Query,
            _ => OpenApiParameterLocation.Other,
        },
        Schema = parameter.Schema is null ? new OpenApiSchema() : Map(parameter.Schema),
    };

    private static OpenApiRequestBody Map(IOpenApiRequestBody requestBody) => new()
    {
        Content = requestBody.Content?.ToDictionary(media => media.Key, media => Map(media.Value))
            ?? new Dictionary<string, OpenApiMediaType>(),
    };

    private static OpenApiResponse Map(IOpenApiResponse response) => new()
    {
        Content = response.Content?.ToDictionary(media => media.Key, media => Map(media.Value))
            ?? new Dictionary<string, OpenApiMediaType>(),
    };

    private static OpenApiMediaType Map(IOpenApiMediaType mediaType) => new()
    {
        Schema = mediaType.Schema is null ? null : Map(mediaType.Schema),
    };

    private static OpenApiSchema Map(IOpenApiSchema schema) => new()
    {
        Types = MapTypes(schema.Type, IsMarkedNullable(schema.Extensions)),
        ReferenceName = schema is OpenApiSchemaReference reference ? reference.Reference.Id : null,
        Properties = schema.Properties?.ToDictionary(property => property.Key, property => Map(property.Value))
            ?? new Dictionary<string, OpenApiSchema>(),
        Items = schema.Items is null ? null : Map(schema.Items),
        IntegerEnumValues = schema.Enum?.Select(MapIntegerEnumValue).Where(value => value.HasValue).Select(value => value!.Value).ToList() ?? [],
        StringEnumValues = schema.Enum?.OfType<JsonValue>()
            .Select(value => value.TryGetValue<string>(out var text) ? text : null)
            .Where(text => text is not null && !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            .Cast<string>()
            .ToList() ?? [],
        AllOf = schema.AllOf?.Select(reference => reference is OpenApiSchemaReference schemaReference ? schemaReference.Reference.Id : null)
            .Where(name => !string.IsNullOrEmpty(name)).Cast<string>().ToList() ?? [],
        Extensions = MapExtensions(schema.Extensions),
    };

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> MapExtensions(IDictionary<string, IOpenApiExtension>? extensions) =>
        extensions?
            .Where(extension => extension.Value is JsonNodeExtension { Node: JsonArray })
            .ToDictionary(
                extension => extension.Key,
                extension => (IReadOnlyList<string>)((JsonNodeExtension)extension.Value).Node.AsArray()
                    .Select(value => value?.GetValue<string>()).Where(value => value is not null).Cast<string>().ToList())
            ?? new Dictionary<string, IReadOnlyList<string>>();

    private static int? MapIntegerEnumValue(JsonNode? value)
    {
        if (value is not JsonValue jsonValue)
        {
            return null;
        }

        if (jsonValue.TryGetValue<int>(out var integer))
        {
            return integer;
        }

        return jsonValue.TryGetValue<string>(out var text)
            && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer)
            ? integer
            : null;
    }

    private static bool IsMarkedNullable(IDictionary<string, IOpenApiExtension>? extensions)
    {
        return extensions?.TryGetValue("x-enfusion-codegen-nullable", out var extension) == true
            && extension is JsonNodeExtension { Node: JsonValue value }
            && value.TryGetValue<bool>(out var isNullable)
            && isNullable;
    }

    private static OpenApiSchemaType MapTypes(JsonSchemaType? types, bool isNullable)
    {
        var mapped = OpenApiSchemaType.None;
        if (types is not null)
        {
            foreach (var (nativeType, inputType) in new[]
                     {
                         (JsonSchemaType.Null, OpenApiSchemaType.Null),
                         (JsonSchemaType.Boolean, OpenApiSchemaType.Boolean),
                         (JsonSchemaType.Integer, OpenApiSchemaType.Integer),
                         (JsonSchemaType.Number, OpenApiSchemaType.Number),
                         (JsonSchemaType.String, OpenApiSchemaType.String),
                         (JsonSchemaType.Object, OpenApiSchemaType.Object),
                         (JsonSchemaType.Array, OpenApiSchemaType.Array),
                     })
            {
                if (types.Value.HasFlag(nativeType))
                {
                    mapped |= inputType;
                }
            }
        }

        return isNullable ? mapped | OpenApiSchemaType.Null : mapped;
    }

    private static bool TryMapMethod(HttpMethod method, out OpenApiHttpMethod mapped)
    {
        mapped = method.Method.ToUpperInvariant() switch
        {
            "GET" => OpenApiHttpMethod.Get,
            "POST" => OpenApiHttpMethod.Post,
            "PUT" => OpenApiHttpMethod.Put,
            "DELETE" => OpenApiHttpMethod.Delete,
            "PATCH" => OpenApiHttpMethod.Patch,
            _ => default,
        };
        return method.Method is "GET" or "POST" or "PUT" or "DELETE" or "PATCH";
    }

    private static OpenApiHttpMethod MapMethod(HttpMethod method)
    {
        TryMapMethod(method, out var mapped);
        return mapped;
    }
}
