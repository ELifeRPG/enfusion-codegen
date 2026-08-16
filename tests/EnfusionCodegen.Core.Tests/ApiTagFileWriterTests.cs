using System.Collections.Generic;
using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.Writers;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class ApiTagFileWriterTests
{
    private static readonly Dictionary<string, string> CallbackLookup = new()
    {
        ["CharacterDtoListResultDto"] = "ELIFE_CharacterDtoListResultDtoCallback",
        ["CharacterDtoResultDto"] = "ELIFE_CharacterDtoResultDtoCallback",
    };

    [Fact]
    public void Write_EmitsGetMethod_WithPositionalPathSubstitution()
    {
        var operation = new EsOperation(
            "GetAccountCharacters",
            "Account",
            EsHttpVerb.Get,
            "accounts/{0}/characters",
            new[] { new EsParameter("accountId", new EsType { TypeName = "string" }, IsPathParameter: true) },
            null,
            new EsType { TypeName = "CharacterDtoListResultDto", IsReference = true });

        var result = ApiTagFileWriter.Write("Account", new[] { operation }, "ELIFE_", CallbackLookup);

        Assert.Equal(
            "modded class ELIFE_Api\n" +
            "{\n" +
            "\tvoid GetAccountCharacters(string accountId, Managed instance = null, string functionName = \"\")\n" +
            "\t{\n" +
            "\t\tELIFE_CharacterDtoListResultDtoCallback cbx = new ELIFE_CharacterDtoListResultDtoCallback;\n" +
            "\t\tcbx.SetCallback(instance, functionName, accountId);\n" +
            "\t\tGetElifeApi().GET(cbx, string.Format(\"accounts/%1/characters\", accountId));\n" +
            "\t}\n" +
            "}\n",
            result);
    }

    [Fact]
    public void Write_EmitsPostMethod_WithBodyPackAndAsString()
    {
        var operation = new EsOperation(
            "CreateCharacter",
            "Character",
            EsHttpVerb.Post,
            "characters",
            Array.Empty<EsParameter>(),
            new EsType { TypeName = "CharacterDto" },
            new EsType { TypeName = "CharacterDtoResultDto", IsReference = true });

        var result = ApiTagFileWriter.Write("Character", new[] { operation }, "ELIFE_", CallbackLookup);

        Assert.Equal(
            "modded class ELIFE_Api\n" +
            "{\n" +
            "\tvoid CreateCharacter(CharacterDto body, Managed instance = null, string functionName = \"\")\n" +
            "\t{\n" +
            "\t\tELIFE_CharacterDtoResultDtoCallback cbx = new ELIFE_CharacterDtoResultDtoCallback;\n" +
            "\t\tcbx.SetCallback(instance, functionName);\n" +
            "\t\tbody.Pack();\n" +
            "\t\tGetElifeApi().POST(cbx, \"characters\", body.AsString());\n" +
            "\t}\n" +
            "}\n",
            result);
    }

    [Fact]
    public void Write_IntTypedPathParameter_OmitsAdditionalDataArgument()
    {
        // A non-string path parameter (e.g. an int vehicleId) can't be passed
        // where SetCallback expects a string additionalData argument — no
        // implicit int->string conversion in Enforce Script.
        var operation = new EsOperation(
            "GetVehicle",
            "Vehicle",
            EsHttpVerb.Get,
            "vehicles/{0}",
            new[] { new EsParameter("vehicleId", new EsType { TypeName = "int" }, IsPathParameter: true) },
            null,
            new EsType { TypeName = "CharacterDtoResultDto", IsReference = true });

        var result = ApiTagFileWriter.Write("Vehicle", new[] { operation }, "ELIFE_", CallbackLookup);

        Assert.Contains("cbx.SetCallback(instance, functionName);", result);
        Assert.DoesNotContain("cbx.SetCallback(instance, functionName, vehicleId);", result);
    }

    [Fact]
    public void Write_QueryParameters_AppearInSignatureAndPositionalPathSubstitution()
    {
        var operation = new EsOperation(
            "GetCompanies",
            "CompanyEndpoints",
            EsHttpVerb.Get,
            "companies?page={0}&name={1}",
            new[]
            {
                new EsParameter("page", new EsType { TypeName = "int" }, IsPathParameter: false),
                new EsParameter("name", new EsType { TypeName = "string" }, IsPathParameter: false),
            },
            null,
            new EsType { TypeName = "CharacterDtoListResultDto", IsReference = true });

        var result = ApiTagFileWriter.Write("CompanyEndpoints", new[] { operation }, "ELIFE_", CallbackLookup);

        Assert.Contains("void GetCompanies(int page, string name, Managed instance = null, string functionName = \"\")", result);
        Assert.Contains("GetElifeApi().GET(cbx, string.Format(\"companies?page=%1&name=%2\", page, name));", result);
    }

    [Fact]
    public void Write_BodilessPost_PassesEmptyStringAsDataArgument()
    {
        var operation = new EsOperation(
            "CreateCharacterSession",
            "Character",
            EsHttpVerb.Post,
            "characters/{0}/sessions",
            new[] { new EsParameter("characterId", new EsType { TypeName = "string" }, IsPathParameter: true) },
            null,
            new EsType { TypeName = "CharacterDtoResultDto", IsReference = true });

        var result = ApiTagFileWriter.Write("Character", new[] { operation }, "ELIFE_", CallbackLookup);

        Assert.Contains("GetElifeApi().POST(cbx, string.Format(\"characters/%1/sessions\", characterId), \"\");", result);
    }

    [Fact]
    public void Write_HandlesPathParametersBeyondIndex8()
    {
        // Test that {9} and higher placeholders are correctly converted to %10 and higher
        var pathParams = new List<EsParameter>();
        for (var i = 0; i < 10; i++)
        {
            pathParams.Add(new EsParameter($"param{i}", new EsType { TypeName = "string" }, IsPathParameter: true));
        }

        var pathTemplate = "a/{0}/b/{1}/c/{2}/d/{3}/e/{4}/f/{5}/g/{6}/h/{7}/i/{8}/j/{9}/k";
        var operation = new EsOperation(
            "GetDeepResource",
            "Resource",
            EsHttpVerb.Get,
            pathTemplate,
            pathParams,
            null,
            new EsType { TypeName = "CharacterDtoListResultDto", IsReference = true });

        var result = ApiMethodWriter.Write(operation, "ELIFE_", CallbackLookup);

        // Verify that {9} is converted to %10 (not left as literal {9})
        Assert.Contains("%10", result);
        Assert.DoesNotContain("{9}", result);
    }
}
