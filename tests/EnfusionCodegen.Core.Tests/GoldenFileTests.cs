using System.Linq;
using EnfusionCodegen.Core.Generation;
using Xunit;

namespace EnfusionCodegen.Core.Tests;

public class GoldenFileTests
{
    [Theory]
    [InlineData("Api/Structs/ELIFE_CharacterDto.c")]
    [InlineData("Api/Structs/ELIFE_MessageTypeDto.c")]
    [InlineData("Api/Structs/ELIFE_CharacterDtoListResultDto.c")]
    [InlineData("Api/Callbacks/ELIFE_CharacterDtoResultDtoCallback.c")]
    [InlineData("Api/ELIFE_BaseRestCallback.c")]
    [InlineData("Api/ELIFE_Api_Character.c")]
    public async Task Generate_MatchesCheckedInExpectedOutput(string relativePath)
    {
        var files = await GeneratorPipeline.Generate("Fixtures/swagger.json", "ELIFE_");
        var generated = files.Single(f => f.RelativePath == relativePath);
        var expected = File.ReadAllText(Path.Combine("Fixtures", "Expected", relativePath));

        Assert.Equal(expected, generated.Content);
    }

    [Fact]
    public async Task Generate_ScaffoldsApiBaseWithOverwriteIfExistsFalse()
    {
        var files = await GeneratorPipeline.Generate("Fixtures/swagger.json", "ELIFE_");
        var apiBase = files.Single(f => f.RelativePath == "Api/ELIFE_Api_Base.c");

        Assert.False(apiBase.OverwriteIfExists);
    }
}
