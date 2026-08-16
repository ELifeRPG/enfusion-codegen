using System.Linq;
using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.ModelBuilder;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class CallbackModelBuilderTests
{
    [Fact]
    public void Build_CreatesOneCallbackPerDistinctResponseType()
    {
        var operations = new[]
        {
            OperationWithResponse("OpA", "CharacterDtoResultDto"),
            OperationWithResponse("OpB", "CharacterDtoResultDto"),
            OperationWithResponse("OpC", "SessionDtoResultDto"),
        };

        var callbacks = CallbackModelBuilder.Build(operations, "ELIFE_");

        Assert.Equal(2, callbacks.Count);
        Assert.Contains(callbacks, c => c.Name == "ELIFE_CharacterDtoResultDtoCallback" && c.ResponseModelName == "CharacterDtoResultDto");
        Assert.Contains(callbacks, c => c.Name == "ELIFE_SessionDtoResultDtoCallback" && c.ResponseModelName == "SessionDtoResultDto");
    }

    [Fact]
    public void Build_IgnoresOperationsWithoutResponseType()
    {
        var operations = new[] { OperationWithResponse("OpA", null) };

        var callbacks = CallbackModelBuilder.Build(operations, "ELIFE_");

        Assert.Empty(callbacks);
    }

    [Fact]
    public void Build_IgnoresOperationsWithNonReferenceResponseType()
    {
        // e.g. an inline array response ("ref array<ref CharacterDto>") or an
        // inline object response ("class") is not a $ref to a named schema, so
        // there's no DTO class to build a callback around — and using the raw
        // TypeName verbatim would otherwise produce a garbage class/file name.
        var operation = new EsOperation(
            "OpA",
            "Tag",
            EsHttpVerb.Get,
            "path",
            Array.Empty<EsParameter>(),
            null,
            new EsType { TypeName = "ref array<ref CharacterDto>", IsArray = true, IsReference = false });

        var callbacks = CallbackModelBuilder.Build(new[] { operation }, "ELIFE_");

        Assert.Empty(callbacks);
    }

    private static EsOperation OperationWithResponse(string name, string? responseTypeName) => new(
        name,
        "Tag",
        EsHttpVerb.Get,
        "path",
        Array.Empty<EsParameter>(),
        null,
        responseTypeName is null ? null : new EsType { TypeName = responseTypeName, IsReference = true });
}
