using System.Linq;
using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class OperationModelBuilderTests
{
    private static (IReadOnlyList<EsOperation> Operations, IReadOnlyList<string> SkippedPatchPaths) BuildFromFixture()
    {
        var (document, _) = new OpenApiDocumentReader().Read("Fixtures/swagger.json");
        return OperationModelBuilder.Build(document);
    }

    [Fact]
    public void Build_MapsGetWithPathParameter_ToOperationWithPathParameterAndResponseType()
    {
        var op = BuildFromFixture().Operations.Single(o => o.PathTemplate == "accounts/{0}/characters" && o.Verb == EsHttpVerb.Get);
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
        var op = BuildFromFixture().Operations.Single(o => o.PathTemplate == "characters" && o.Verb == EsHttpVerb.Post);
        Assert.Equal("Character", op.Tag);
        Assert.Equal("CreateCharacter", op.OperationName);
        Assert.Equal("CharacterDto", op.RequestBodyType!.TypeName);
        Assert.Equal("CharacterDtoResultDto", op.ResponseType!.TypeName);
    }

    [Fact]
    public void Build_UsesZeroBasedPositionalPlaceholders_ForPathTemplate() =>
        Assert.Equal("accounts/{0}/characters", BuildFromFixture().Operations.Single(o => o.OperationName == "GetAccountCharacters").PathTemplate);

    [Fact]
    public void Build_MapsGetOnPlainCollection_KeepsSegmentPlural()
    {
        var op = BuildFromFixture().Operations.Single(o => o.PathTemplate == "companies" && o.Verb == EsHttpVerb.Get);
        Assert.Equal("CompanyEndpoints", op.Tag);
        Assert.Equal("GetCompanies", op.OperationName);
    }

    [Fact]
    public void Build_MapsPostWithPathParameterAndTrailingCollection_SingularizesBothSegments()
    {
        var op = BuildFromFixture().Operations.Single(o => o.PathTemplate == "characters/{0}/sessions" && o.Verb == EsHttpVerb.Post);
        Assert.Equal("Character", op.Tag);
        Assert.Equal("CreateCharacterSession", op.OperationName);
        Assert.Single(op.Parameters);
        Assert.True(op.Parameters[0].IsPathParameter);
        Assert.Equal("characterId", op.Parameters[0].Name);
    }

    [Fact]
    public void Build_MapsPostOnPlainTopLevelCollection_SingularizesSegment()
    {
        var op = BuildFromFixture().Operations.Single(o => o.PathTemplate == "sessions" && o.Verb == EsHttpVerb.Post);
        Assert.Equal("Session", op.Tag);
        Assert.Equal("CreateSession", op.OperationName);
    }

    [Fact]
    public void Build_AppendsQueryParameters_AsPositionalPlaceholdersOnThePathTemplate()
    {
        var op = OperationModelBuilder.Build(MinimalDocument("/companies", OpenApiHttpMethod.Get, new OpenApiOperation
        {
            Tags = ["CompanyEndpoints"],
            Parameters =
            [
                new OpenApiParameter { Name = "page", Location = OpenApiParameterLocation.Query, Schema = new OpenApiSchema { Types = OpenApiSchemaType.Integer } },
                new OpenApiParameter { Name = "name", Location = OpenApiParameterLocation.Query, Schema = new OpenApiSchema { Types = OpenApiSchemaType.String } },
            ],
        })).Operations.Single();

        Assert.Equal("companies?page={0}&name={1}", op.PathTemplate);
        Assert.Equal(2, op.Parameters.Count);
        Assert.Equal("page", op.Parameters[0].Name);
        Assert.False(op.Parameters[0].IsPathParameter);
        Assert.Equal("name", op.Parameters[1].Name);
        Assert.False(op.Parameters[1].IsPathParameter);
    }

    [Fact]
    public void Build_UntaggedOperation_DoesNotThrow_AndGetsDefaultTag() =>
        Assert.Equal("Default", OperationModelBuilder.Build(MinimalDocument("/widgets", OpenApiHttpMethod.Get, new OpenApiOperation())).Operations.Single().Tag);

    [Fact]
    public void Build_SkipsPatchOperations_AndRecordsThePath()
    {
        var (operations, skipped) = OperationModelBuilder.Build(MinimalDocument("/widgets/{widgetId}", OpenApiHttpMethod.Patch, new OpenApiOperation
        {
            Tags = ["Widget"],
            Parameters = [new OpenApiParameter { Name = "widgetId", Location = OpenApiParameterLocation.Path, Schema = new OpenApiSchema { Types = OpenApiSchemaType.String } }],
        }));
        Assert.Empty(operations);
        Assert.Contains("/widgets/{widgetId}", skipped);
    }

    private static OpenApiDocument MinimalDocument(string path, OpenApiHttpMethod method, OpenApiOperation operation) => new()
    {
        Paths = new Dictionary<string, OpenApiPathItem>
        {
            [path] = new OpenApiPathItem { Operations = new Dictionary<OpenApiHttpMethod, OpenApiOperation> { [method] = operation } },
        },
    };
}
