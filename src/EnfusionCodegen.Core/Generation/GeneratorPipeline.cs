using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using EnfusionCodegen.Core.Writers;
using System.Text.RegularExpressions;

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
        (classes, enums, operations) = PrefixSchemaTypes(classes, enums, operations, prefix);
        var callbacks = CallbackModelBuilder.Build(operations, string.Empty);
        var callbackClassByResponseType = callbacks.ToDictionary(c => c.ResponseModelName, c => c.Name);

        foreach (var skipped in skippedPatchPaths)
        {
            Console.WriteLine($"warning: skipping PATCH {skipped} — RestContext has no PATCH verb");
        }

        foreach (var skipped in skippedSchemas)
        {
            Console.WriteLine($"warning: skipping schema {skipped} — unsupported schema shape (not object or enum)");
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
        files.Add(new GeneratedFile($"Api/{prefix}ApiConfigDto.c", BoilerplateWriter.WriteApiConfigScaffold(prefix), OverwriteIfExists: true));
        files.Add(new GeneratedFile($"Api/{prefix}Api_Base.c", BoilerplateWriter.WriteApiBaseScaffold(prefix), OverwriteIfExists: false));

        foreach (var tagGroup in operations.GroupBy(o => o.Tag))
        {
            var content = ApiTagFileWriter.Write(tagGroup.Key, tagGroup.ToList(), prefix, callbackClassByResponseType);
            files.Add(new GeneratedFile($"Api/{prefix}Api_{tagGroup.Key}.c", content, OverwriteIfExists: true));
        }

        return files;
    }

    private static (IReadOnlyList<EsClass> Classes, IReadOnlyList<EsEnum> Enums, IReadOnlyList<EsOperation> Operations) PrefixSchemaTypes(
        IReadOnlyList<EsClass> classes,
        IReadOnlyList<EsEnum> enums,
        IReadOnlyList<EsOperation> operations,
        string prefix)
    {
        var schemaNames = classes.Select(esClass => esClass.Name)
            .Concat(enums.Select(esEnum => esEnum.Name))
            .OrderByDescending(name => name.Length)
            .ToList();
        var schemaNamePattern = schemaNames.Count == 0
            ? null
            : new Regex(string.Join("|", schemaNames.Select(Regex.Escape)));

        EsType PrefixType(EsType type)
        {
            var typeName = schemaNamePattern?.Replace(type.TypeName, match => prefix + match.Value) ?? type.TypeName;
            return new EsType
            {
                TypeName = typeName,
                BaseTypeName = type.BaseTypeName,
                DefaultValueLiteral = type.DefaultValueLiteral,
                IsEnum = type.IsEnum,
                IsArray = type.IsArray,
                IsReference = type.IsReference,
                IsNullable = type.IsNullable,
            };
        }

        var prefixedClasses = classes
            .Select(esClass => esClass with
            {
                Name = prefix + esClass.Name,
                Properties = esClass.Properties.Select(property => property with { Type = PrefixType(property.Type) }).ToList(),
            })
            .ToList();
        var prefixedEnums = enums.Select(esEnum => esEnum with { Name = prefix + esEnum.Name }).ToList();
        var prefixedOperations = operations
            .Select(operation => operation with
            {
                Parameters = operation.Parameters.Select(parameter => parameter with { Type = PrefixType(parameter.Type) }).ToList(),
                RequestBodyType = operation.RequestBodyType is null ? null : PrefixType(operation.RequestBodyType),
                ResponseType = operation.ResponseType is null ? null : PrefixType(operation.ResponseType),
            })
            .ToList();

        return (prefixedClasses, prefixedEnums, prefixedOperations);
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
