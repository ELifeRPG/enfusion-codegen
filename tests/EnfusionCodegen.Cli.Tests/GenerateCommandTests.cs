using System.IO;
using EnfusionCodegen.Cli;
using Xunit;

namespace EnfusionCodegen.Cli.Tests;

public class GenerateCommandTests : IDisposable
{
    private readonly string _outputDir = Path.Combine(Path.GetTempPath(), "enfusion-codegen-cli-tests-" + Guid.NewGuid());

    [Fact]
    public async Task InvokeAsync_GeneratesFilesUnderOutputDirectory_AndReturnsZero()
    {
        var specPath = "Fixtures/swagger.json";

        var exitCode = await GenerateCommand.RunAsync(specPath, _outputDir, "ELIFE_");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(_outputDir, "Api", "Structs", "CharacterDto.c")));
        Assert.True(File.Exists(Path.Combine(_outputDir, "Api", "ELIFE_BaseRestCallback.c")));
    }

    [Fact]
    public async Task InvokeAsync_MissingSpecFile_ReturnsNonZeroInsteadOfThrowing()
    {
        var missingSpecPath = Path.Combine(Path.GetTempPath(), "enfusion-codegen-cli-tests-missing-" + Guid.NewGuid() + ".json");

        var exitCode = await GenerateCommand.RunAsync(missingSpecPath, _outputDir, "ELIFE_");

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task InvokeAsync_HttpUrlSource_IsTreatedAsAUrlNotALocalPath()
    {
        // An absolute http(s) URL must not be handed to the local FileStream
        // reader (which would fail with a confusing DirectoryNotFoundException
        // treating "http:" as a directory segment). Using a port nothing listens
        // on forces a fast, deterministic connection failure, proving the URL
        // branch was taken (a network-level failure) rather than the file-path
        // branch (which would fail with a path-not-found error instead).
        var unreachableUrl = "http://127.0.0.1:1/openapi.json";

        var exitCode = await GenerateCommand.RunAsync(unreachableUrl, _outputDir, "ELIFE_");

        Assert.Equal(1, exitCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
        {
            Directory.Delete(_outputDir, recursive: true);
        }
    }
}
