namespace EnfusionCodegen.Core.OpenApi;

public sealed class OpenApiDocument
{
    public OpenApiInfo Info { get; init; } = new();
    public OpenApiComponents Components { get; init; } = new();
    public IReadOnlyDictionary<string, OpenApiPathItem> Paths { get; init; } = new Dictionary<string, OpenApiPathItem>();
}

public sealed class OpenApiInfo
{
    public string? Title { get; init; }
}

public sealed class OpenApiComponents
{
    public IReadOnlyDictionary<string, OpenApiSchema> Schemas { get; init; } = new Dictionary<string, OpenApiSchema>();
}

[Flags]
public enum OpenApiSchemaType
{
    None = 0,
    Null = 1,
    Boolean = 2,
    Integer = 4,
    Number = 8,
    String = 16,
    Object = 32,
    Array = 64,
}

public sealed class OpenApiSchema
{
    public OpenApiSchemaType Types { get; init; }
    public string? ReferenceName { get; init; }
    public IReadOnlyDictionary<string, OpenApiSchema> Properties { get; init; } = new Dictionary<string, OpenApiSchema>();
    public OpenApiSchema? Items { get; init; }
    public IReadOnlyList<int> IntegerEnumValues { get; init; } = [];
    public IReadOnlyList<string> StringEnumValues { get; init; } = [];
    public IReadOnlyList<string> AllOf { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Extensions { get; init; } = new Dictionary<string, IReadOnlyList<string>>();

    public bool IsNullable => Types.HasFlag(OpenApiSchemaType.Null);
    public bool HasType(OpenApiSchemaType type) => Types.HasFlag(type);
}

public enum OpenApiHttpMethod
{
    Get,
    Post,
    Put,
    Delete,
    Patch,
}

public sealed class OpenApiPathItem
{
    public IReadOnlyDictionary<OpenApiHttpMethod, OpenApiOperation> Operations { get; init; } = new Dictionary<OpenApiHttpMethod, OpenApiOperation>();
}

public sealed class OpenApiOperation
{
    public string? OperationId { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyList<OpenApiParameter> Parameters { get; init; } = [];
    public OpenApiRequestBody? RequestBody { get; init; }
    public IReadOnlyDictionary<string, OpenApiResponse> Responses { get; init; } = new Dictionary<string, OpenApiResponse>();
}

public enum OpenApiParameterLocation
{
    Query,
    Path,
    Other,
}

public sealed class OpenApiParameter
{
    public required string Name { get; init; }
    public OpenApiParameterLocation Location { get; init; }
    public required OpenApiSchema Schema { get; init; }
}

public sealed class OpenApiRequestBody
{
    public IReadOnlyDictionary<string, OpenApiMediaType> Content { get; init; } = new Dictionary<string, OpenApiMediaType>();
}

public sealed class OpenApiResponse
{
    public IReadOnlyDictionary<string, OpenApiMediaType> Content { get; init; } = new Dictionary<string, OpenApiMediaType>();
}

public sealed class OpenApiMediaType
{
    public OpenApiSchema? Schema { get; init; }
}

public sealed class OpenApiDiagnostic
{
    public IReadOnlyList<OpenApiDiagnosticError> Errors { get; init; } = [];
}

public sealed record OpenApiDiagnosticError(string Message);
