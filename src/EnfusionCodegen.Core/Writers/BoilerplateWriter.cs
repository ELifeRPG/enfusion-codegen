namespace EnfusionCodegen.Core.Writers;

public static class BoilerplateWriter
{
    public static string WriteBaseRestCallback(string prefix)
    {
        return
            $"class {prefix}BaseRestCallback : RestCallback\n" +
            "{\n" +
            "\tprotected ref ScriptCallQueue m_Invoker;\n" +
            "\tprotected Managed m_InvokeInstance;\n" +
            "\tprotected string m_InvokeMethodName;\n" +
            "\tprotected string m_InvokeAdditionalData;\n" +
            "\n" +
            "\tvoid SetCallback(Managed instance, string functionName, string additionalData = \"\")\n" +
            "\t{\n" +
            "\t\tm_Invoker = new ScriptCallQueue();\n" +
            "\t\tm_InvokeInstance = instance;\n" +
            "\t\tm_InvokeMethodName = functionName;\n" +
            "\t\tm_InvokeAdditionalData = additionalData;\n" +
            "\t}\n" +
            "\n" +
            $"\t{prefix}EApiStatusCode ExtractData(string data, int dataSize, out JsonApiStruct resultData)\n" +
            "\t{\n" +
            $"\t\treturn {prefix}EApiStatusCode.SUCCESS;\n" +
            "\t}\n" +
            "\n" +
            "\toverride void OnSuccess(string data, int dataSize)\n" +
            "\t{\n" +
            "\t\tJsonApiStruct resultData;\n" +
            $"\t\t{prefix}EApiStatusCode status = ExtractData(data, dataSize, resultData);\n" +
            "\t\tInvokeCallback(status, resultData);\n" +
            "\t}\n" +
            "\n" +
            "\toverride void OnError( int errorCode )\n" +
            "\t{\n" +
            $"\t\tInvokeCallback({prefix}EApiStatusCode.ERROR, null);\n" +
            "\t}\n" +
            "\n" +
            $"\tprotected void InvokeCallback({prefix}EApiStatusCode statusCode, JsonApiStruct data)\n" +
            "\t{\n" +
            "\t\tif (m_Invoker && m_InvokeInstance && m_InvokeMethodName)\n" +
            "\t\t{\n" +
            "\t\t\tif (m_InvokeAdditionalData != \"\")\n" +
            "\t\t\t{\n" +
            "\t\t\t\tm_Invoker.CallByName(m_InvokeInstance, m_InvokeMethodName, statusCode, data, m_InvokeAdditionalData);\n" +
            "\t\t\t}\n" +
            "\t\t\telse\n" +
            "\t\t\t{\n" +
            "\t\t\t\tm_Invoker.CallByName(m_InvokeInstance, m_InvokeMethodName, statusCode, data);\n" +
            "\t\t\t}\n" +
            "\t\t\tm_Invoker.Tick(1);\n" +
            "\t\t}\n" +
            "\t}\n" +
            "}\n" +
            "\n" +
            $"enum {prefix}EApiStatusCode\n" +
            "{\n" +
            "\tSUCCESS,\n" +
            "\tERROR\n" +
            "}\n";
    }

    public static string WriteApiBaseScaffold(string prefix)
    {
        return
            $"class {prefix}Api\n" +
            "{\n" +
            $"\tprotected static ref {prefix}Api s_Instance;\n" +
            "\tprotected static string serverURL;\n" +
            "\n" +
            "\tprotected const string CONFIG_FILE_PATH = \"$profile:ELifeRPG.json\";\n" +
            "\tprotected const string WORKBENCH_DEFAULT_SERVER_URL = \"http://127.0.0.1:5200/\";\n" +
            "\n" +
            "\t//------------------------------------------------------------------------------------------------\n" +
            $"\tstatic {prefix}Api GetInstance()\n" +
            "\t{\n" +
            "\t\treturn s_Instance;\n" +
            "\t}\n" +
            "\n" +
            "\t//------------------------------------------------------------------------------------------------\n" +
            $"\tstatic void Initialize()\n" +
            "\t{\n" +
            $"\t\ts_Instance = new {prefix}Api();\n" +
            "\t}\n" +
            "\n" +
            "\t//------------------------------------------------------------------------------------------------\n" +
            "\tRestContext GetElifeApi()\n" +
            "\t{\n" +
            "\t\tRestContext ctx = GetGame().GetRestApi().GetContext(serverURL);\n" +
            "\t\tctx.SetHeaders(\"Content-Type,application/json\");\n" +
            "\t\treturn ctx;\n" +
            "\t}\n" +
            "\n" +
            "\t//------------------------------------------------------------------------------------------------\n" +
            "\tprotected string ParseServerUrlFromProfile()\n" +
            "\t{\n" +
            "\t\tif (!FileIO.FileExist(CONFIG_FILE_PATH))\n" +
            "\t\t{\n" +
            "#ifdef WORKBENCH\n" +
            "\t\t\treturn WORKBENCH_DEFAULT_SERVER_URL;\n" +
            "#else\n" + 
            "\t\t\treturn \"\";\n" +
            "#endif\n" + 
            "\t\t}\n" +
            "\n" +
            "\t\tELIFE_ApiConfigDto config = new ELIFE_ApiConfigDto();\n" +
            "\t\tconfig.ExpandFromRAW(SCR_FileIOHelper.GetFileStringContent(CONFIG_FILE_PATH));\n" +
            "\n" +
            "\t\treturn config.serverUrl;\n" +
            "\t}\n" +
            "\n" +
            "\t//------------------------------------------------------------------------------------------------\n" +
            "\tvoid ELIFE_Api()\n" +
            "\t{\n" +
            "\t\tserverURL = ParseServerUrlFromProfile();\n" +
            "\n" +
            "\t\tif (serverURL != \"\")\n" +
            "\t\t\treturn;\n" +
            "\n" +
            "#ifdef WORKBENCH\n" +
            "\t\tPrint(\"ELIFE_Api | ServerUrl not configured.\", LogLevel.WARNING);\n" +
            "#else\n" +
            "\t\tPrint(\"ELIFE_Api | ServerUrl not configured - refusing to start.\", LogLevel.ERROR);\n" +
            "\t\tGetGame().RequestClose();\n" +
            "#endif\n" +
            "\t}\n" +
            "}\n";
    }
    
    public static string WriteApiConfigScaffold(string prefix)
    {
        return
            $"class {prefix}ApiConfigDto : JsonApiStruct\n" +
            "{\n" +
            "\tstring serverUrl;\n" +
            "\n" +
            $"\tvoid {prefix}ApiConfigDto()" +
            "\t{\n" +
            "\t\tRegV(\"serverUrl\");\n" +
            "\t}\n" +
            "}\n";
    }
}
