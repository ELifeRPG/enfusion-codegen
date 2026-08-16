using EnfusionCodegen.Core;
using EnfusionCodegen.Core.Generation;

namespace EnfusionCodegen.Cli;

public static class GenerateCommand
{
    public static async Task<int> RunAsync(string specPath, string outputDirectory, string prefix)
    {
        try
        {
            var files = await GeneratorPipeline.Generate(specPath, prefix);
            var (written, skipped) = OutputWriter.WriteAll(outputDirectory, files);

            foreach (var path in written)
            {
                Console.WriteLine($"wrote {path}");
            }

            foreach (var path in skipped)
            {
                Console.WriteLine($"skipped {path} (already exists)");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }
}
