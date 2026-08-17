using EnfusionCodegen.Core.Writers;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class BoilerplateWriterTests
{
    [Fact]
    public void WriteBaseRestCallback_UsesPrefixForClassAndEnumNames()
    {
        var result = BoilerplateWriter.WriteBaseRestCallback("ELIFE_");

        Assert.Contains("class ELIFE_BaseRestCallback : RestCallback", result);
        Assert.Contains("enum ELIFE_EApiStatusCode", result);
        Assert.Contains("SUCCESS,", result);
        Assert.Contains("ERROR", result);
    }

    [Fact]
    public void WriteApiBaseScaffold_DeclaresSingletonWithGetInstanceAndGetContextHelper()
    {
        var result = BoilerplateWriter.WriteApiBaseScaffold("ELIFE_");

        Assert.Contains("class ELIFE_Api", result);
        Assert.Contains("static ELIFE_Api GetInstance()", result);
        Assert.Contains("protected RestContext GetElifeApi()", result);
        Assert.Contains("protected string BuildHeaders()", result); // extension point for future auth work
    }
}
