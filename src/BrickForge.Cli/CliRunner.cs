using BrickForge.Ai;
using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Options;
using Microsoft.Extensions.Configuration;

namespace BrickForge.Cli;

/// <summary>
/// Entry point dispatcher for the BrickForge CLI.
/// Loads configuration, wires services and dispatches to commands.
/// Supported commands: health, generate
/// </summary>
public static class CliRunner
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var (ollamaOptions, generationOptions) = LoadConfiguration();

        IOllamaClient ollamaClient;

        if (ollamaOptions.MockMode)
        {
            ollamaClient = new MockOllamaClient();
            Console.WriteLine("[info] MockMode enabled — no Ollama calls will be made.");
        }
        else
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(ollamaOptions.BaseUrl),
                Timeout = TimeSpan.FromSeconds(ollamaOptions.TimeoutSeconds)
            };
            ollamaClient = new OllamaClient(httpClient, ollamaOptions);
        }

        var promptAnalyzer = new PromptAnalysisService(ollamaClient, ollamaOptions, generationOptions);

        return args[0].ToLowerInvariant() switch
        {
            "health" => await HealthCommand.RunAsync(ollamaClient),
            "generate" => await RunGenerateAsync(args[1..], promptAnalyzer, ollamaOptions, generationOptions),
            "--help" or "-h" or "help" => RunHelp(),
            _ => RunUnknown(args[0])
        };
    }

    private static async Task<int> RunGenerateAsync(
        string[] args,
        IPromptAnalyzer promptAnalyzer,
        OllamaOptions ollamaOptions,
        GenerationOptions generationOptions)
    {
        var baseDir = AppContext.BaseDirectory;
        var partsDir = Path.Combine(baseDir, "data", "parts");

        SupportedPartsRegistry registry;
        TemplateRegistry templateRegistry;

        try
        {
            var partsJson = await File.ReadAllTextAsync(Path.Combine(partsDir, "supported-parts.json"));
            var colorsJson = await File.ReadAllTextAsync(Path.Combine(partsDir, "supported-colors.json"));
            var templateJson = await File.ReadAllTextAsync(Path.Combine(partsDir, "small_machine_template.json"));

            registry = SupportedPartsRegistry.FromJson(partsJson, colorsJson);
            templateRegistry = TemplateRegistry.FromJson(templateJson);
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine($"Failed to load data files from '{partsDir}': {ex.Message}");
            return 1;
        }

        var generator = new SmallMachineGenerator(registry);
        var validator = new BrickGraphValidator(registry);

        return await GenerateCommand.RunAsync(
            args,
            promptAnalyzer,
            generator,
            validator,
            templateRegistry,
            generationOptions,
            ollamaOptions.Model);
    }

    private static (OllamaOptions ollama, GenerationOptions generation) LoadConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();

        var ollama = configuration.GetSection("Ollama").Get<OllamaOptions>() ?? new OllamaOptions();
        var generation = configuration.GetSection("Generation").Get<GenerationOptions>() ?? new GenerationOptions();

        return (ollama, generation);
    }

    private static int RunHelp()
    {
        PrintUsage();
        return 0;
    }

    private static int RunUnknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintUsage();
        return 1;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("BrickForge CLI – MVP0");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  brickforge health             Check local Ollama availability");
        Console.WriteLine("  brickforge generate <prompt>  Generate a brick model from a text prompt");
        Console.WriteLine("  brickforge --help             Show this help");
    }
}
