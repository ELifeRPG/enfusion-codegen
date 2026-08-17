using System.Linq;
using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using Microsoft.OpenApi.Models;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class OperationModelBuilderTests
{
    private static (System.Collections.Generic.IReadOnlyList<EsOperation> Operations,
        System.Collections.Generic.IReadOnlyList<string> SkippedPatchPaths) BuildFromFixture()
    {
        var (document, _) = new OpenApiDocumentReader().Read("Fixtures/swagger.json");
        return OperationModelBuilder.Build(document);
    }

    [Fact]
    public void Build_MapsGetWithPathParameter_ToOperationWithPathParameterAndResponseType()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.PathTemplate == "accounts/{0}/characters" && o.Verb == EsHttpVerb.Get);

        Assert.Equal("Account", op.Tag);
        Assert.Equal("GetAccountCharacters", op.OperationName);
        Assert.Single(op.Parameters);
        Assert.True(op.Parameters[0].IsPathParameter);
        Assert.Equal("accountId", op.Parameters[0].Name);
        Assert.Equal("CharacterDtoListResultDto", op.ResponseType!.TypeName);
        Assert.Null(op.RequestBodyType);
    }

    [Fact]
    public void Build_MapsPostWithRequestBody_ToOperationWithRequestBodyType()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.PathTemplate == "characters" && o.Verb == EsHttpVerb.Post);

        Assert.Equal("Character", op.Tag);
        Assert.Equal("CreateCharacter", op.OperationName);
        Assert.Equal("CharacterDto", op.RequestBodyType!.TypeName);
        Assert.Equal("CharacterDtoResultDto", op.ResponseType!.TypeName);
    }

    [Fact]
    public void Build_UsesZeroBasedPositionalPlaceholders_ForPathTemplate()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.OperationName == "GetAccountCharacters");

        Assert.Equal("accounts/{0}/characters", op.PathTemplate);
    }

    [Fact]
    public void Build_MapsGetOnPlainCollection_KeepsSegmentPlural()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.PathTemplate == "companies" && o.Verb == EsHttpVerb.Get);

        Assert.Equal("CompanyEndpoints", op.Tag);
        Assert.Equal("GetCompanies", op.OperationName);
    }

    [Fact]
    public void Build_MapsPostWithPathParameterAndTrailingCollection_SingularizesBothSegments()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.PathTemplate == "characters/{0}/sessions" && o.Verb == EsHttpVerb.Post);

        Assert.Equal("Character", op.Tag);
        Assert.Equal("CreateCharacterSession", op.OperationName);
        Assert.Single(op.Parameters);
        Assert.True(op.Parameters[0].IsPathParameter);
        Assert.Equal("characterId", op.Parameters[0].Name);
    }

    [Fact]
    public void Build_MapsPostOnPlainTopLevelCollection_SingularizesSegment()
    {
        var (operations, _) = BuildFromFixture();

        var op = operations.Single(o => o.PathTemplate == "sessions" && o.Verb == EsHttpVerb.Post);

        Assert.Equal("Session", op.Tag);
        Assert.Equal("CreateSession", op.OperationName);
    }

    [Fact]
    public void Build_AppendsQueryParameters_AsPositionalPlaceholdersOnThePathTemplate()
    {
        var document = MinimalDocumentWithQueryParameters();

        var (operations, _) = OperationModelBuilder.Build(document);

        var op = operations.Single();
        Assert.Equal("companies?page={0}&name={1}", op.PathTemplate);
        Assert.Equal(2, op.Parameters.Count);
        Assert.Equal("page", op.Parameters[0].Name);
        Assert.False(op.Parameters[0].IsPathParameter);
        Assert.Equal("name", op.Parameters[1].Name);
        Assert.False(op.Parameters[1].IsPathParameter);
    }

    [Fact]
    public void Build_UntaggedOperation_DoesNotThrow_AndGetsDefaultTag()
    {
        var document = MinimalDocumentWithUntaggedOperation();

        var (operations, _) = OperationModelBuilder.Build(document);

        var op = operations.Single();
        Assert.Equal("Default", op.Tag);
    }

    [Fact]
    public void Build_SkipsPatchOperations_AndRecordsThePath()
    {
        var document = MinimalDocumentWithPatchOperation();

        var (operations, skippedPatchPaths) = OperationModelBuilder.Build(document);

        Assert.Empty(operations);
        Assert.Contains("/widgets/{widgetId}", skippedPatchPaths);
    }

    private static OpenApiDocument MinimalDocumentWithQueryParameters()
    {
        var operation = new OpenApiOperation
        {
            Tags = new List<OpenApiTag> { new() { Name = "CompanyEndpoints" } },
            Parameters = new List<OpenApiParameter>
            {
                new() { Name = "page", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "integer" } },
                new() { Name = "name", In = ParameterLocation.Query, Schema = new OpenApiSchema { Type = "string" } },
            },
            Responses = new OpenApiResponses(),
        };

        return new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/companies"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Get] = operation },
                },
            },
            Components = new OpenApiComponents(),
        };
    }

    private static OpenApiDocument MinimalDocumentWithUntaggedOperation()
    {
        var operation = new OpenApiOperation
        {
            Responses = new OpenApiResponses(),
        };

        return new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/widgets"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Get] = operation },
                },
            },
            Components = new OpenApiComponents(),
        };
    }

    private static OpenApiDocument MinimalDocumentWithPatchOperation()
    {
        var operation = new OpenApiOperation
        {
            Tags = new List<OpenApiTag> { new() { Name = "Widget" } },
            Parameters = new List<OpenApiParameter>
            {
                new() { Name = "widgetId", In = ParameterLocation.Path, Schema = new OpenApiSchema { Type = "string" } },
            },
            Responses = new OpenApiResponses(),
        };

        return new OpenApiDocument
        {
            Paths = new OpenApiPaths
            {
                ["/widgets/{widgetId}"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Patch] = operation },
                },
            },
            Components = new OpenApiComponents(),
        };
    }
}
