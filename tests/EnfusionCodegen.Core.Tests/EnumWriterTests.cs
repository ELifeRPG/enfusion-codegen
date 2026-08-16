using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.Writers;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class EnumWriterTests
{
    [Fact]
    public void Write_EmitsEnumWithExplicitValuesAndTrailingComma()
    {
        var esEnum = new EsEnum(
            "MessageTypeDto",
            new[]
            {
                new EsEnumMember("Information", 0),
                new EsEnumMember("Success", 1),
                new EsEnumMember("Warning", 2),
                new EsEnumMember("Error", 3),
            });

        var result = EnumWriter.Write(esEnum);

        Assert.Equal(
            "enum MessageTypeDto\n" +
            "{\n" +
            "\tInformation = 0,\n" +
            "\tSuccess = 1,\n" +
            "\tWarning = 2,\n" +
            "\tError = 3,\n" +
            "}\n",
            result);
    }
}
