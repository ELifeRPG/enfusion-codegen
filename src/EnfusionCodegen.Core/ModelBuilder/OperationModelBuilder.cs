using System.Text;
using EnfusionCodegen.Core.Model;
using Microsoft.OpenApi.Models;

namespace EnfusionCodegen.Core.ModelBuilder;

public static class OperationModelBuilder
{
    public static (IReadOnlyList<EsOperation> Operations, IReadOnlyList<string> SkippedPatchPaths) Build(OpenApiDocument document)
    {
        var operations = new List<EsOperation>();
        var skipped = new List<string>();

        foreach (var (path, item) in document.Paths)
        {
            foreach (var (verb, operation) in item.Operations)
            {
                if (verb == OperationType.Patch)
                {
                    skipped.Add(path);
                    continue;
                }

                if (verb is not (OperationType.Get or OperationType.Post or OperationType.Put or OperationType.Delete))
                {
                    continue;
                }

                operations.Add(BuildOperation(path, verb, operation));
            }
        }

        return (operations, skipped);
    }

    private static EsOperation BuildOperation(string path, OperationType verb, OpenApiOperation operation)
    {
        var parameters = new List<EsParameter>();
        var pathTemplate = new StringBuilder();
        var segments = path.TrimStart('/').Split('/');
        var positionalIndex = 0;
        var pathParamNames = operation.Parameters
            .Where(p => p.In == ParameterLocation.Path)
            .ToDictionary(p => p.Name, p => p);

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                var name = segment[1..^1];
                var openApiParam = pathParamNames[name];
                parameters.Add(new EsParameter(name, EsType.FromSchema(openApiParam.Schema), IsPathParameter: true));
                pathTemplate.Append($"{{{positionalIndex}}}");
                positionalIndex++;
            }
            else
            {
                pathTemplate.Append(segment);
            }

            if (i < segments.Length - 1)
            {
                pathTemplate.Append('/');
            }
        }

        var queryParams = operation.Parameters
            .Where(p => p.In == ParameterLocation.Query)
            .ToList();

        for (var i = 0; i < queryParams.Count; i++)
        {
            var openApiParam = queryParams[i];
            pathTemplate.Append(i == 0 ? '?' : '&');
            pathTemplate.Append($"{openApiParam.Name}={{{positionalIndex}}}");
            parameters.Add(new EsParameter(openApiParam.Name, EsType.FromSchema(openApiParam.Schema), IsPathParameter: false));
            positionalIndex++;
        }

        EsType? requestBodyType = null;
        if (operation.RequestBody?.Content.TryGetValue("application/json", out var requestMedia) == true)
        {
            requestBodyType = EsType.FromSchema(requestMedia.Schema);
        }

        EsType? responseType = null;
        var successResponse = operation.Responses.FirstOrDefault(r => r.Key.StartsWith('2'));
        if (successResponse.Value?.Content.TryGetValue("application/json", out var responseMedia) == true)
        {
            responseType = EsType.FromSchema(responseMedia.Schema);
        }

        return new EsOperation(
            BuildOperationName(verb, operation, path),
            operation.Tags.FirstOrDefault()?.Name ?? "Default",
            MapVerb(verb),
            pathTemplate.ToString(),
            parameters,
            requestBodyType,
            responseType);
    }

    private static string BuildOperationName(OperationType verb, OpenApiOperation operation, string path)
    {
        if (!string.IsNullOrEmpty(operation.OperationId))
        {
            return operation.OperationId;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var nameParts = new List<string>();

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                continue;
            }

            // A segment immediately owning a path parameter refers to a single resource
            // (e.g. "accounts/{accountId}" -> the one account), so singularize it.
            var isFollowedByPathParameter = i + 1 < segments.Length
                && segments[i + 1].StartsWith('{') && segments[i + 1].EndsWith('}');

            // POST against a collection segment creates a single new instance of it
            // (e.g. POST "characters" -> creates one Character), so singularize it too.
            var isLastSegment = i == segments.Length - 1;
            var shouldSingularize = isFollowedByPathParameter || (isLastSegment && verb == OperationType.Post);

            var word = shouldSingularize ? Singularize(segment) : segment;
            nameParts.Add(char.ToUpperInvariant(word[0]) + word[1..]);
        }

        return $"{VerbPrefix(verb)}{string.Concat(nameParts)}";
    }

    // Pragmatic heuristic scoped to this API's naming vocabulary (plain trailing
    // "s"/"ies" pluralization) — it does not handle irregular plurals or words
    // ending in "-sis"/"-us" (e.g. "analyses", "statuses" would not singularize correctly).
    private static string Singularize(string word)
    {
        if (word.EndsWith("ies", StringComparison.OrdinalIgnoreCase) && word.Length > 3)
        {
            return word[..^3] + "y";
        }

        if ((word.EndsWith('s') || word.EndsWith('S'))
            && !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase)
            && word.Length > 1)
        {
            return word[..^1];
        }

        return word;
    }

    private static string VerbPrefix(OperationType verb) => verb switch
    {
        OperationType.Get => "Get",
        OperationType.Post => "Create",
        OperationType.Put => "Update",
        OperationType.Delete => "Delete",
        _ => verb.ToString(),
    };

    private static EsHttpVerb MapVerb(OperationType verb) => verb switch
    {
        OperationType.Get => EsHttpVerb.Get,
        OperationType.Post => EsHttpVerb.Post,
        OperationType.Put => EsHttpVerb.Put,
        OperationType.Delete => EsHttpVerb.Delete,
        _ => throw new ArgumentOutOfRangeException(nameof(verb)),
    };
}
