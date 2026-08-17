using EnfusionCodegen.Core.Model;
using EnfusionCodegen.Core.Writers;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class CallbackWriterTests
{
    [Fact]
    public void Write_EmitsSubclassWithExtractDataDeserializingResponseModel()
    {
        var callback = new EsCallbackClass("ELIFE_CharacterDtoResultDtoCallback", "CharacterDtoResultDto");

        var result = CallbackWriter.Write(callback, "ELIFE_");

        Assert.Equal(
            "class ELIFE_CharacterDtoResultDtoCallback : ELIFE_BaseRestCallback\n" +
            "{\n" +
            "\toverride ELIFE_EApiStatusCode ExtractData(string data, int dataSize, out JsonApiStruct resultData)\n" +
            "\t{\n" +
            "\t\tresultData = new CharacterDtoResultDto();\n" +
            "\t\tresultData.ExpandFromRAW(data);\n" +
            "\t\tif (!resultData)\n" +
            "\t\t\treturn ELIFE_EApiStatusCode.ERROR;\n" +
            "\t\treturn ELIFE_EApiStatusCode.SUCCESS;\n" +
            "\t}\n" +
            "}\n",
            result);
    }
}
