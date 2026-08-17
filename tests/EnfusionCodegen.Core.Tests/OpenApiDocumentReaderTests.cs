using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class OpenApiDocumentReaderTests
{
    [Fact]
    public void Read_ResolvesComponentSchemaReferences()
    {
        var reader = new OpenApiDocumentReader();

        var (document, _) = reader.Read("Fixtures/swagger.json");

        var characterDto = document.Components.Schemas["CharacterDto"];
        Assert.NotNull(characterDto.Properties["id"]);
        Assert.Equal("string", characterDto.Properties["id"].Type);
    }

    [Fact]
    public void Read_ResolvesReferencedArrayItemProperties()
    {
        var reader = new OpenApiDocumentReader();

        var (document, _) = reader.Read("Fixtures/swagger.json");

        var listResult = document.Components.Schemas["CharacterDtoListResultDto"];
        var dataProperty = listResult.Properties["data"];

        // With reference resolution enabled, the referenced schema's own
        // Properties are populated on Items, not just its Reference.Id.
        Assert.NotEmpty(dataProperty.Items.Properties);
        Assert.Contains("firstName", dataProperty.Items.Properties.Keys);
    }
}
