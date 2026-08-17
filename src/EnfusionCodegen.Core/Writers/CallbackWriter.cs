using EnfusionCodegen.Core.Model;

namespace EnfusionCodegen.Core.Writers;

public static class CallbackWriter
{
    public static string Write(EsCallbackClass callback, string prefix)
    {
        return
            $"class {callback.Name} : {prefix}BaseRestCallback\n" +
            "{\n" +
            $"\toverride {prefix}EApiStatusCode ExtractData(string data, int dataSize, out JsonApiStruct resultData)\n" +
            "\t{\n" +
            $"\t\tresultData = new {callback.ResponseModelName}();\n" +
            "\t\tresultData.ExpandFromRAW(data);\n" +
            "\t\tif (!resultData)\n" +
            $"\t\t\treturn {prefix}EApiStatusCode.ERROR;\n" +
            $"\t\treturn {prefix}EApiStatusCode.SUCCESS;\n" +
            "\t}\n" +
            "}\n";
    }
}
