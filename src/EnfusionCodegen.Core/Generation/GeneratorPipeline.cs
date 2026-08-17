using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using EnfusionCodegen.Core.Writers;

namespace EnfusionCodegen.Core.Generation;

public static class GeneratorPipeline
{
    public static async Task<IReadOnlyList<GeneratedFile>> Generate(string specPath, string prefix)
    {
        var reader = new OpenApiDocumentReader();

        var (document, diagnostic) = IsHttpUrl(specPath, out var uri)
            ? await reader.ReadFromUriAsync(uri!)
            : reader.Read(specPath);

        if (diagnostic.Errors.Count > 0)
        {
            var summary = string.Join("; ", diagnostic.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"failed to parse OpenAPI spec: {summary}");
        }

        var (classes, enums, skippedSchemas) = SchemaModelBuilder.Build(document);
        var (operations, skippedPatchPaths) = OperationModelBuilder.Build(document);
        var callbacks = CallbackModelBuilder.Build(operations, prefix);
        var callbackClassByResponseType = callbacks.ToDictionary(c => c.ResponseModelName, c => c.Name);

        foreach (var skipped in skippedPatchPaths)
        {
            Console.WriteLine($"warning: skipping PATCH {skipped} — RestContext has no PATCH verb");
        }

        foreach (var skipped in skippedSchemas)
        {
            Console.WriteLine($"warning: skipping schema {skipped} — unsupported schema shape (not object or integer enum)");
        }

        var emittedTypeNames = new HashSet<string>(classes.Select(c => c.Name).Concat(enums.Select(e => e.Name)));
        var missingReferences = callbacks
            .Where(c => !emittedTypeNames.Contains(c.ResponseModelName))
            .Select(c => c.ResponseModelName)
            .Distinct()
            .ToList();

        if (missingReferences.Count > 0)
        {
            var summary = string.Join(", ", missingReferences);
            throw new InvalidOperationException(
                $"error: the following schemas are referenced by generated callbacks but were never emitted as a class or enum: {summary}");
        }

        var files = new List<GeneratedFile>();

        foreach (var esClass in classes)
        {
            files.Add(new GeneratedFile($"Api/Structs/{esClass.Name}.c", StructWriter.Write(esClass), OverwriteIfExists: true));
        }

        foreach (var esEnum in enums)
        {
            files.Add(new GeneratedFile($"Api/Structs/{esEnum.Name}.c", EnumWriter.Write(esEnum), OverwriteIfExists: true));
        }

        foreach (var callback in callbacks)
        {
            files.Add(new GeneratedFile($"Api/Callbacks/{callback.Name}.c", CallbackWriter.Write(callback, prefix), OverwriteIfExists: true));
        }

        files.Add(new GeneratedFile($"Api/{prefix}BaseRestCallback.c", BoilerplateWriter.WriteBaseRestCallback(prefix), OverwriteIfExists: true));
        files.Add(new GeneratedFile($"Api/{prefix}Api_Base.c", BoilerplateWriter.WriteApiBaseScaffold(prefix), OverwriteIfExists: false));

        foreach (var tagGroup in operations.GroupBy(o => o.Tag))
        {
            var content = ApiTagFileWriter.Write(tagGroup.Key, tagGroup.ToList(), prefix, callbackClassByResponseType);
            files.Add(new GeneratedFile($"Api/{prefix}Api_{tagGroup.Key}.c", content, OverwriteIfExists: true));
        }

        return files;
    }

    private static bool IsHttpUrl(string specPath, out Uri? uri)
    {
        if (Uri.TryCreate(specPath, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null;
        return false;
    }
}
