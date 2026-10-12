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
    public void WriteBaseRestCallback_UsesSetOnSuccessInsteadOfObsoleteOverrides()
    {
        var result = BoilerplateWriter.WriteBaseRestCallback("ELIFE_");

        Assert.Contains("SetOnSuccess(HandleSuccess);", result);
        Assert.Contains("SetOnError(HandleError);", result);
        Assert.DoesNotContain("override void OnSuccess", result);
        Assert.DoesNotContain("override void OnError", result);
    }

    [Fact]
    public void WriteBaseRestCallback_KeepsInFlightCallbacksAliveUntilTheyFire()
    {
        var result = BoilerplateWriter.WriteBaseRestCallback("ELIFE_");

        Assert.Contains("protected static ref array<ref ELIFE_BaseRestCallback> s_aInFlight;", result);
        Assert.Contains("s_aInFlight.Insert(this);", result);
        Assert.Contains("s_aInFlight.RemoveItem(self);", result);
    }

    [Fact]
    public void WriteApiBaseScaffold_DeclaresSingletonWithGetInstanceAndGetContextHelper()
    {
        var result = BoilerplateWriter.WriteApiBaseScaffold("ELIFE_");

        Assert.Contains("class ELIFE_Api", result);
        Assert.Contains("static ELIFE_Api GetInstance()", result);
        Assert.Contains("RestContext GetElifeApi()", result);
    }

    [Fact]
    public void WriteApiConfigScaffold_DeclaresJsonApiStructWithPropertiesAndRegV()
    {
        var result = BoilerplateWriter.WriteApiConfigScaffold("ELIFE_");

        Assert.Contains("class ELIFE_ApiConfigDto : JsonApiStruct", result);
        Assert.Contains("void ELIFE_ApiConfigDto()", result);
        Assert.Contains("string serverUrl;", result);
        Assert.Contains("RegV(\"serverUrl\");", result);
    }
}
