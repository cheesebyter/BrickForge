using BrickForge.Ai;

namespace BrickForge.Cli;

/// <summary>
/// Checks whether the local Ollama service is reachable and reports the status.
/// Implements BF-MVP0-003.
/// </summary>
internal static class HealthCommand
{
    internal static async Task<int> RunAsync(IOllamaClient ollamaClient)
    {
        Console.WriteLine("Checking Ollama availability...");

        var available = await ollamaClient.IsAvailableAsync();

        if (available)
        {
            Console.WriteLine("Ollama: available");
            return 0;
        }

        Console.Error.WriteLine("Ollama: not available");
        Console.Error.WriteLine("Make sure Ollama is running on the configured address (default: http://localhost:11434).");
        Console.Error.WriteLine("Start Ollama with: ollama serve");
        return 1;
    }
}
