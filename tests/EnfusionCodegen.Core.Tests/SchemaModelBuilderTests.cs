using System.Linq;
using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class SchemaModelBuilderTests
{
    private static (System.Collections.Generic.IReadOnlyList<Core.Model.EsClass> Classes,
        System.Collections.Generic.IReadOnlyList<Core.Model.EsEnum> Enums,
        System.Collections.Generic.IReadOnlyList<string> SkippedSchemas) BuildFromFixture()
    {
        var (document, _) = new OpenApiDocumentReader().Read("Fixtures/swagger.json");
        return SchemaModelBuilder.Build(document);
    }

    [Fact]
    public void Build_CreatesClassWithPropertiesInDeclaredOrder()
    {
        var (classes, _, _) = BuildFromFixture();

        var characterDto = classes.Single(c => c.Name == "CharacterDto");

        Assert.Equal(new[] { "id", "firstName", "lastName" }, characterDto.Properties.Select(p => p.JsonName));
        Assert.Equal("JsonApiStruct", characterDto.BaseTypeName);
    }

    [Fact]
    public void Build_CreatesEnumWithMembersFromXEnumNamesWhenPresent()
    {
        var document = MinimalDocumentWithNamedEnum();

        var (_, builtEnums, _) = SchemaModelBuilder.Build(document);

        var builtEnum = builtEnums.Single(e => e.Name == "MessageTypeDtoDto");
        Assert.Equal(new[] { "Information", "Success", "Warning", "Error" }, builtEnum.Members.Select(m => m.Name));
        Assert.Equal(new[] { 0, 1, 2, 3 }, builtEnum.Members.Select(m => m.Value));
    }

    [Fact]
    public void Build_FallsBackToGeneratedNames_WhenXEnumNamesMissing()
    {
        var document = MinimalDocumentWithUnnamedEnum();

        var (_, builtEnums, _) = SchemaModelBuilder.Build(document);

        var builtEnum = builtEnums.Single(e => e.Name == "UnnamedEnumDto");
        Assert.Equal(new[] { "Value0", "Value1" }, builtEnum.Members.Select(m => m.Name));
    }

    [Fact]
    public void Build_ReportsAllOfComposedSchemaAsSkipped_AndDoesNotSilentlyDropIt()
    {
        var document = MinimalDocumentWithAllOfSchema();

        var (classes, enums, skippedSchemas) = SchemaModelBuilder.Build(document);

        Assert.DoesNotContain(classes, c => c.Name == "ComposedDto");
        Assert.DoesNotContain(enums, e => e.Name == "ComposedDto");
        Assert.Contains("ComposedDto", skippedSchemas);
    }

    private static Microsoft.OpenApi.Models.OpenApiDocument MinimalDocumentWithAllOfSchema()
    {
        var schema = new Microsoft.OpenApi.Models.OpenApiSchema
        {
            AllOf = new System.Collections.Generic.List<Microsoft.OpenApi.Models.OpenApiSchema>
            {
                new() { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Id = "BaseDto" } },
            },
        };

        return new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Components = new Microsoft.OpenApi.Models.OpenApiComponents
            {
                Schemas = new System.Collections.Generic.Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                {
                    ["ComposedDto"] = schema,
                },
            },
        };
    }

    private static Microsoft.OpenApi.Models.OpenApiDocument MinimalDocumentWithNamedEnum()
    {
        var schema = new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "integer",
            Enum = new System.Collections.Generic.List<Microsoft.OpenApi.Any.IOpenApiAny>
            {
                new Microsoft.OpenApi.Any.OpenApiInteger(0),
                new Microsoft.OpenApi.Any.OpenApiInteger(1),
                new Microsoft.OpenApi.Any.OpenApiInteger(2),
                new Microsoft.OpenApi.Any.OpenApiInteger(3),
            },
        };

        schema.Extensions["x-enumNames"] = new Microsoft.OpenApi.Any.OpenApiArray
        {
            new Microsoft.OpenApi.Any.OpenApiString("Information"),
            new Microsoft.OpenApi.Any.OpenApiString("Success"),
            new Microsoft.OpenApi.Any.OpenApiString("Warning"),
            new Microsoft.OpenApi.Any.OpenApiString("Error"),
        };

        return new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Components = new Microsoft.OpenApi.Models.OpenApiComponents
            {
                Schemas = new System.Collections.Generic.Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                {
                    ["MessageTypeDtoDto"] = schema,
                },
            },
        };
    }

    private static Microsoft.OpenApi.Models.OpenApiDocument MinimalDocumentWithUnnamedEnum()
    {
        var schema = new Microsoft.OpenApi.Models.OpenApiSchema
        {
            Type = "integer",
            Enum = new System.Collections.Generic.List<Microsoft.OpenApi.Any.IOpenApiAny>
            {
                new Microsoft.OpenApi.Any.OpenApiInteger(0),
                new Microsoft.OpenApi.Any.OpenApiInteger(1),
            },
        };

        return new Microsoft.OpenApi.Models.OpenApiDocument
        {
            Components = new Microsoft.OpenApi.Models.OpenApiComponents
            {
                Schemas = new System.Collections.Generic.Dictionary<string, Microsoft.OpenApi.Models.OpenApiSchema>
                {
                    ["UnnamedEnumDto"] = schema,
                },
            },
        };
    }
}
