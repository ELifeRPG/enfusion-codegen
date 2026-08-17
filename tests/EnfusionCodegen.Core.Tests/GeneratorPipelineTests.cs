using System.Linq;
using System.Threading.Tasks;
using EnfusionCodegen.Core.Generation;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class GeneratorPipelineTests
{
    // A response schema composed via allOf (no top-level "type") isn't
    // supported by SchemaModelBuilder and is reported as skipped — but the
    // operation still $refs it by name, so CallbackModelBuilder still builds
    // a callback around it. GeneratorPipeline must catch that mismatch
    // (a callback pointing at a class/enum that was never emitted) rather
    // than silently writing a callback file with a dangling reference.
    private const string SpecWithCallbackReferencingSkippedSchema =
        """
        {
          "openapi": "3.0.1",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {
            "/widgets": {
              "get": {
                "tags": ["Widget"],
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/WidgetDto" }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": {
            "schemas": {
              "WidgetDto": {
                "allOf": [
                  { "$ref": "#/components/schemas/BaseDto" }
                ]
              },
              "BaseDto": {
                "type": "object",
                "properties": {
                  "id": { "type": "string" }
                }
              }
            }
          }
        }
        """;

    // Structural errors (e.g. an unresolvable $ref) are reported by
    // OpenApiStreamReader through the OpenApiDiagnostic rather than by
    // throwing — GeneratorPipeline must surface those instead of discarding
    // the diagnostic and reporting a clean, empty success.
    private const string SpecWithUnresolvableRef =
        """
        {
          "openapi": "3.0.1",
          "info": { "title": "Test", "version": "1.0" },
          "paths": {
            "/widgets": {
              "get": {
                "tags": ["Widget"],
                "responses": {
                  "200": {
                    "description": "Success",
                    "content": {
                      "application/json": {
                        "schema": { "$ref": "#/components/schemas/DoesNotExist" }
                      }
                    }
                  }
                }
              }
            }
          },
          "components": { "schemas": {} }
        }
        """;

    [Fact]
    public async Task Generate_SpecWithDiagnosticErrors_ThrowsInsteadOfSilentlySucceeding()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, SpecWithUnresolvableRef);

            var exception = await Assert.ThrowsAsync<System.InvalidOperationException>(
                () => GeneratorPipeline.Generate(specPath, "ELIFE_"));

            Assert.Contains("DoesNotExist", exception.Message);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }

    [Fact]
    public async Task Generate_ThrowsWhenACallbackReferencesASchemaThatWasNeverEmitted()
    {
        var specPath = System.IO.Path.GetTempFileName();
        try
        {
            await System.IO.File.WriteAllTextAsync(specPath, SpecWithCallbackReferencingSkippedSchema);

            var exception = await Assert.ThrowsAsync<System.InvalidOperationException>(
                () => GeneratorPipeline.Generate(specPath, "ELIFE_"));

            Assert.Contains("WidgetDto", exception.Message);
        }
        finally
        {
            System.IO.File.Delete(specPath);
        }
    }
}
