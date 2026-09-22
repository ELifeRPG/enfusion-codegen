using EnfusionCodegen.Core.OpenApi;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class OpenApiDocumentReaderTests
{
    private const string OpenApi32Spec =
        """
        {
          "openapi": "3.2.0",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "NullableName": {
                "type": ["string", "null"]
              },
              "Status": {
                "type": "integer",
                "enum": ["0", "1"],
                "x-enum-varnames": ["Ready", "Done"]
              }
            }
          }
        }
        """;

    private const string OpenApi30YamlNullableTypeArraySpec =
        """
        openapi: 3.0.3
        info:
          title: Test
          version: 1.0
        paths: {}
        components:
          schemas:
            NullableName:
              type: [string, "null"]
        """;

    private const string OpenApi30NullableTypeArraySpec =
        """
        {
          "openapi": "3.0.3",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "NullableName": { "type": ["string", "null"] },
              "NullableAmount": { "type": ["number", "string", "null"] }
            }
          }
        }
        """;

    private const string InvalidSchemaTypeSpec =
        """
        {
          "openapi": "3.2.0",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "Widget": { "type": { "invalid": true } }
            }
          }
        }
        """;

    private const string OpenApi30StringEnumSpec =
        """
        {
          "openapi": "3.0.0",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {},
          "components": {
            "schemas": {
              "HealthStatus": {
                "enum": ["unknown", "healthy"],
                "x-enum-varnames": ["Unknown", "Healthy"]
              }
            }
          }
        }
        """;

    [Fact]
    public async Task Read_SupportsOpenApi32()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, OpenApi32Spec);

            var (document, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            Assert.Empty(diagnostic.Errors);
            Assert.Equal("Test", document.Info.Title);
            Assert.True(document.Components.Schemas["NullableName"].HasType(OpenApiSchemaType.String));
            Assert.True(document.Components.Schemas["NullableName"].IsNullable);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Read_PreservesOpenApi32EnumExtensions()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, OpenApi32Spec);

            var (document, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            Assert.Empty(diagnostic.Errors);
            var status = document.Components.Schemas["Status"];
            Assert.Equal(new[] { 0, 1 }, status.IntegerEnumValues);
            Assert.Equal(new[] { "Ready", "Done" }, status.Extensions["x-enum-varnames"]);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Read_NormalizesYamlNullableTypeUnions()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, OpenApi30YamlNullableTypeArraySpec);

            var (document, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            Assert.Empty(diagnostic.Errors);
            Assert.True(document.Components.Schemas["NullableName"].HasType(OpenApiSchemaType.String));
            Assert.True(document.Components.Schemas["NullableName"].IsNullable);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Read_NormalizesNullableTypeUnionsForScalarReaders()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, OpenApi30NullableTypeArraySpec);

            var (document, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            Assert.Empty(diagnostic.Errors);
            var schema = document.Components.Schemas["NullableName"];
            Assert.True(schema.HasType(OpenApiSchemaType.String));
            Assert.True(schema.IsNullable);
            var amount = document.Components.Schemas["NullableAmount"];
            Assert.True(amount.HasType(OpenApiSchemaType.Number));
            Assert.True(amount.IsNullable);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Read_IncludesThePointerInDiagnostics()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, InvalidSchemaTypeSpec);

            var (_, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            var error = Assert.Single(diagnostic.Errors);
            Assert.Contains("/components/schemas/Widget/type", error.Message);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Read_MapsStringEnumValues()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, OpenApi30StringEnumSpec);

            var (document, diagnostic) = new OpenApiDocumentReader().Read(specPath);

            Assert.Empty(diagnostic.Errors);
            var healthStatus = document.Components.Schemas["HealthStatus"];
            Assert.Equal(["unknown", "healthy"], healthStatus.StringEnumValues);
            Assert.Equal(["Unknown", "Healthy"], healthStatus.Extensions["x-enum-varnames"]);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public void Read_ResolvesComponentSchemaReferences()
    {
        var reader = new OpenApiDocumentReader();

        var (document, _) = reader.Read("Fixtures/swagger.json");

        var characterDto = document.Components.Schemas["CharacterDto"];
        Assert.NotNull(characterDto.Properties["id"]);
        Assert.True(characterDto.Properties["id"].HasType(OpenApiSchemaType.String));
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
        Assert.NotNull(dataProperty.Items);
        Assert.NotEmpty(dataProperty.Items.Properties);
        Assert.Contains("firstName", dataProperty.Items.Properties.Keys);
    }
}
