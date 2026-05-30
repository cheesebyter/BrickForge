namespace BrickForge.Cli;

/// <summary>
/// Entry point dispatcher for the BrickForge CLI.
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

        return args[0].ToLowerInvariant() switch
        {
            "health" => await HealthCommand.RunAsync(args[1..]),
            "generate" => await GenerateCommand.RunAsync(args[1..]),
            "--help" or "-h" or "help" => RunHelp(),
            _ => RunUnknown(args[0])
        };
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
