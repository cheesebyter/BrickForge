namespace BrickForge.Cli;

/// <summary>
/// Stub for the health command. Full implementation in BF-MVP0-003.
/// </summary>
internal static class HealthCommand
{
    internal static Task<int> RunAsync(string[] args)
    {
        Console.WriteLine("[health] Ollama health check not yet implemented (BF-MVP0-003).");
        return Task.FromResult(0);
    }
}
