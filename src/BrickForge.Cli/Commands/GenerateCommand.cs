using BrickForge.Ai.Analysis;
using BrickForge.Core.Options;

namespace BrickForge.Cli;

/// <summary>
/// Runs prompt analysis via local Ollama and reports the structured model briefing.
/// Full generation pipeline continues in BF-MVP0-007+.
/// Implements BF-MVP0-005.
/// </summary>
internal static class GenerateCommand
{
    internal static async Task<int> RunAsync(
        string[] args,
        IPromptAnalyzer promptAnalyzer,
        GenerationOptions generationOptions)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: brickforge generate <prompt>");
            return 1;
        }

        var userPrompt = string.Join(" ", args);

        Console.WriteLine("Analysing prompt...");

        var result = await promptAnalyzer.AnalyzeAsync(userPrompt);

        if (!result.IsSuccess)
        {
            Console.Error.WriteLine($"Prompt analysis failed: {result.ErrorMessage}");
            return 1;
        }

        var analysis = result.Value!;

        if (!analysis.Feasible)
        {
            Console.Error.WriteLine("This prompt cannot be fulfilled within MVP0 constraints.");
            if (analysis.Warnings.Count > 0)
                Console.Error.WriteLine($"Reason: {string.Join("; ", analysis.Warnings)}");
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine($"Model name   : {analysis.ModelName}");
        Console.WriteLine($"Category     : {analysis.ModelCategory}");
        Console.WriteLine($"Target parts : {analysis.TargetParts}");
        Console.WriteLine($"Main colour  : {analysis.MainColor}");
        Console.WriteLine($"Accent colour: {analysis.AccentColor}");

        if (analysis.Features.Count > 0)
            Console.WriteLine($"Features     : {string.Join(", ", analysis.Features)}");

        if (analysis.Warnings.Count > 0)
        {
            Console.WriteLine();
            foreach (var warning in analysis.Warnings)
                Console.WriteLine($"[warning] {warning}");
        }

        if (analysis.UsedFallback)
            Console.WriteLine("[info] Fallback analysis was used (AI response could not be parsed).");

        Console.WriteLine();
        Console.WriteLine("[info] Generation pipeline not yet fully implemented (BF-MVP0-007+).");

        return 0;
    }
}
