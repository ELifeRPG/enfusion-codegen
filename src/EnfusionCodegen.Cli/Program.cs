using System.CommandLine;
using System.CommandLine.Invocation;
using EnfusionCodegen.Cli;

var specArgument = new Argument<string>("source", "File path or URL to the OpenAPI spec.");
var outputOption = new Option<string>("--output", () => Path.Combine(Directory.GetCurrentDirectory(), "out"), "Destination folder.");
var prefixOption = new Option<string>("--prefix", () => "", "Prefix applied to the client singleton, callback classes, and base callback/status-enum names.");

var generateCommand = new Command("generate", "Generates an Enforce Script API client from an OpenAPI spec.")
{
    specArgument,
    outputOption,
    prefixOption,
};

generateCommand.SetHandler(async (InvocationContext context) =>
{
    var spec = context.ParseResult.GetValueForArgument(specArgument);
    var output = context.ParseResult.GetValueForOption(outputOption)!;
    var prefix = context.ParseResult.GetValueForOption(prefixOption)!;
    context.ExitCode = await GenerateCommand.RunAsync(spec, output, prefix);
});

var rootCommand = new RootCommand { generateCommand };
return await rootCommand.InvokeAsync(args);
