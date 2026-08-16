using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.Writers;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class StructWriterTests
{
    [Fact]
    public void Write_EmitsClassWithRegVPerPropertyAndConstructor()
    {
        var esClass = new EsClass(
            "CharacterDto",
            "JsonApiStruct",
            new[]
            {
                new EsProperty("id", new EsType { TypeName = "string" }),
                new EsProperty("firstName", new EsType { TypeName = "string" }),
                new EsProperty("lastName", new EsType { TypeName = "string" }),
            });

        var result = StructWriter.Write(esClass);

        Assert.Equal(
            "class CharacterDto : JsonApiStruct\n" +
            "{\n" +
            "\tstring id;\n" +
            "\tstring firstName;\n" +
            "\tstring lastName;\n" +
            "\n" +
            "\tvoid CharacterDto()\n" +
            "\t{\n" +
            "\t\tRegV(\"id\");\n" +
            "\t\tRegV(\"firstName\");\n" +
            "\t\tRegV(\"lastName\");\n" +
            "\t}\n" +
            "}\n",
            result);
    }

    [Fact]
    public void Write_EmitsRefArrayFieldWithDefaultInitializerAndRegV()
    {
        var esClass = new EsClass(
            "CharacterDtoListResultDto",
            "JsonApiStruct",
            new[]
            {
                new EsProperty("messages", new EsType
                {
                    TypeName = "ref array<ref MessageDto>",
                    DefaultValueLiteral = "{}",
                    IsArray = true,
                }),
            });

        var result = StructWriter.Write(esClass);

        Assert.Contains("\tref array<ref MessageDto> messages = {};\n", result);
        Assert.Contains("\t\tRegV(\"messages\");\n", result);
    }
}
