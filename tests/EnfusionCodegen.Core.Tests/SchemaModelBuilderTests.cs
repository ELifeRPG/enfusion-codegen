using System.Linq;
using EnfusionCodegen.Core.ModelBuilder;
using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class SchemaModelBuilderTests
{
    private static (IReadOnlyList<Core.Model.EsClass> Classes, IReadOnlyList<Core.Model.EsEnum> Enums, IReadOnlyList<string> SkippedSchemas) BuildFromFixture()
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
        var (_, enums, _) = SchemaModelBuilder.Build(MinimalDocumentWithEnum("x-enumNames"));
        Assert.Equal(new[] { "Information", "Success", "Warning", "Error" }, enums.Single().Members.Select(m => m.Name));
        Assert.Equal(new[] { 0, 1, 2, 3 }, enums.Single().Members.Select(m => m.Value));
    }

    [Fact]
    public void Build_CreatesEnumWithMembersFromXEnumVarnamesWhenPresent()
    {
        var (_, enums, _) = SchemaModelBuilder.Build(MinimalDocumentWithEnum("x-enum-varnames"));
        Assert.Equal(new[] { "Information", "Success", "Warning", "Error" }, enums.Single().Members.Select(m => m.Name));
    }

    [Fact]
    public void Build_CreatesOrdinalEnumFromStringValues()
    {
        var document = MinimalDocumentWithSchema("HealthStatus", new OpenApiSchema
        {
            StringEnumValues = ["unknown", "healthy", "degraded", "unhealthy"],
            Extensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["x-enum-varnames"] = ["Unknown", "Healthy", "Degraded", "Unhealthy"],
            },
        });

        var (_, enums, _) = SchemaModelBuilder.Build(document);

        var healthStatus = Assert.Single(enums);
        Assert.Equal(["Unknown", "Healthy", "Degraded", "Unhealthy"], healthStatus.Members.Select(member => member.Name));
        Assert.Equal([0, 1, 2, 3], healthStatus.Members.Select(member => member.Value));
    }

    [Fact]
    public void Build_PrefersXEnumVarnames_WhenBothExtensionsPresent()
    {
        var document = MinimalDocumentWithSchema("BothExtensionsDto", new OpenApiSchema
        {
            Types = OpenApiSchemaType.Integer,
            IntegerEnumValues = [0, 1],
            Extensions = new Dictionary<string, IReadOnlyList<string>>
            {
                ["x-enum-varnames"] = ["FromVarnames0", "FromVarnames1"],
                ["x-enumNames"] = ["FromEnumNames0", "FromEnumNames1"],
            },
        });

        var (_, enums, _) = SchemaModelBuilder.Build(document);
        Assert.Equal(new[] { "FromVarnames0", "FromVarnames1" }, enums.Single().Members.Select(m => m.Name));
    }

    [Fact]
    public void Build_FallsBackToGeneratedNames_WhenXEnumNamesMissing()
    {
        var (_, enums, _) = SchemaModelBuilder.Build(MinimalDocumentWithSchema("UnnamedEnumDto", new OpenApiSchema
        {
            Types = OpenApiSchemaType.Integer,
            IntegerEnumValues = [0, 1],
        }));
        Assert.Equal(new[] { "Value0", "Value1" }, enums.Single().Members.Select(m => m.Name));
    }

    [Fact]
    public void Build_ReportsAllOfComposedSchemaAsSkipped_AndDoesNotSilentlyDropIt()
    {
        var (classes, enums, skipped) = SchemaModelBuilder.Build(MinimalDocumentWithSchema("ComposedDto", new OpenApiSchema { AllOf = ["BaseDto"] }));
        Assert.DoesNotContain(classes, c => c.Name == "ComposedDto");
        Assert.DoesNotContain(enums, e => e.Name == "ComposedDto");
        Assert.Contains("ComposedDto", skipped);
    }

    [Fact]
    public void Build_SkipsInlineObjectProperties()
    {
        var document = MinimalDocumentWithSchema("OpenBankAccountRequestDto", new OpenApiSchema
        {
            Types = OpenApiSchemaType.Object,
            Properties = new Dictionary<string, OpenApiSchema>
            {
                ["additionalData"] = new() { Types = OpenApiSchemaType.Object | OpenApiSchemaType.Null },
                ["characterId"] = new() { Types = OpenApiSchemaType.String | OpenApiSchemaType.Null },
            },
        });

        var (classes, _, _) = SchemaModelBuilder.Build(document);

        Assert.Equal(["characterId"], classes.Single().Properties.Select(property => property.JsonName));
    }

    private static OpenApiDocument MinimalDocumentWithEnum(string extensionName) => MinimalDocumentWithSchema("MessageTypeDtoDto", new OpenApiSchema
    {
        Types = OpenApiSchemaType.Integer,
        IntegerEnumValues = [0, 1, 2, 3],
        Extensions = new Dictionary<string, IReadOnlyList<string>> { [extensionName] = ["Information", "Success", "Warning", "Error"] },
    });

    private static OpenApiDocument MinimalDocumentWithSchema(string name, OpenApiSchema schema) => new()
    {
        Components = new OpenApiComponents { Schemas = new Dictionary<string, OpenApiSchema> { [name] = schema } },
    };
}
