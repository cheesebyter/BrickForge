namespace BrickForge.Cli;

/// <summary>
/// Stub for the generate command. Full implementation in BF-MVP0-005 and later.
/// </summary>
internal static class GenerateCommand
{
    internal static Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: brickforge generate <prompt>");
            return Task.FromResult(1);
        }

        Console.WriteLine("[generate] Generation pipeline not yet implemented (BF-MVP0-005+).");
        return Task.FromResult(0);
    }
}
