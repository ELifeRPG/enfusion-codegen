using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace EnfusionCodegen.Core.OpenApi;

public class OpenApiDocumentReader
{
    private static readonly OpenApiReaderSettings Settings = new()
    {
        ReferenceResolution = ReferenceResolutionSetting.ResolveLocalReferences,
    };

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
        var document = new OpenApiStreamReader(Settings).Read(input, out var diagnostic);
        return (document, diagnostic);
    }
}
