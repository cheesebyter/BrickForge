using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Options;
using BrickForge.Export;

namespace BrickForge.Cli;

/// <summary>
/// Runs the full MVP0 generation pipeline:
/// Prompt → analysis → template → BrickGraph → validation → export → files.
/// </summary>
internal static class GenerateCommand
{
    internal static async Task<int> RunAsync(
        string[] args,
        IPromptAnalyzer promptAnalyzer,
        SmallMachineGenerator generator,
        BrickGraphValidator validator,
        TemplateRegistry templateRegistry,
        GenerationOptions generationOptions,
        string modelName)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: brickforge generate <prompt>");
            return 1;
        }

        var userPrompt = string.Join(" ", args);

        // ── Step 1: Analyse prompt ────────────────────────────────────────────

        Console.WriteLine("[1/5] Analysing prompt...");

        var analysisResult = await promptAnalyzer.AnalyzeAsync(userPrompt);

        if (!analysisResult.IsSuccess)
        {
            Console.Error.WriteLine($"Prompt analysis failed: {analysisResult.ErrorMessage}");
            return 1;
        }

        var analysis = analysisResult.Value!;

        if (!analysis.Feasible)
        {
            Console.Error.WriteLine("This prompt cannot be fulfilled within MVP0 constraints.");
            if (analysis.Warnings.Count > 0)
                Console.Error.WriteLine($"Reason: {string.Join("; ", analysis.Warnings)}");
            return 1;
        }

        Console.WriteLine($"      Model: {analysis.ModelName}  |  Parts: {analysis.TargetParts}  |  Color: {analysis.MainColor}");

        if (analysis.UsedFallback)
            Console.WriteLine("      [info] Fallback analysis used (AI response could not be parsed).");

        // ── Step 2: Load template ─────────────────────────────────────────────

        Console.WriteLine("[2/5] Loading template...");

        var template = templateRegistry.FindTemplate(analysis.ModelCategory)
                       ?? templateRegistry.FindTemplate("small_machine");

        if (template is null)
        {
            Console.Error.WriteLine("No suitable template found. Check that small_machine_template.json is present.");
            return 1;
        }

        // ── Step 3: Generate BrickGraph ───────────────────────────────────────

        Console.WriteLine("[3/5] Generating BrickGraph...");

        var graph = generator.Generate(analysis, template);

        // ── Step 4: Validate ──────────────────────────────────────────────────

        Console.WriteLine("[4/5] Validating...");

        var validation = validator.Validate(graph);

        if (!validation.Valid)
        {
            Console.Error.WriteLine("Validation failed — high-severity issues prevent export:");
            foreach (var issue in validation.Issues)
                Console.Error.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
            return 1;
        }

        if (validation.Issues.Count > 0)
        {
            foreach (var issue in validation.Issues)
                Console.WriteLine($"  [warning] {issue.Code}: {issue.Message}");
        }

        // ── Step 5: Export ────────────────────────────────────────────────────

        Console.WriteLine("[5/5] Exporting files...");

        var jobId = Guid.NewGuid().ToString("N");
        var outputDir = ResolveOutputDirectory(generationOptions.OutputRoot, jobId);

        if (outputDir is null)
        {
            Console.Error.WriteLine("Output path could not be resolved safely. Aborting.");
            return 1;
        }

        Directory.CreateDirectory(outputDir.FullPath);

        var generatedFiles = new List<string>();

        // brickgraph.json
        await WriteFileAsync(outputDir.FullPath, "brickgraph.json", graph.ToJson(), generatedFiles);

        // validation.json
        await WriteFileAsync(outputDir.FullPath, "validation.json", validation.ToJson(), generatedFiles);

        // model.mpd
        var ldrawResult = new LDrawExporter().Export(graph);
        if (ldrawResult.Success)
            await WriteFileAsync(outputDir.FullPath, "model.mpd", ldrawResult.Content!, generatedFiles);
        else
            Console.WriteLine($"  [warning] LDraw export skipped: {ldrawResult.ErrorMessage}");

        // parts.csv
        var csvResult = new CsvPartsExporter().Export(graph);
        if (csvResult.Success)
            await WriteFileAsync(outputDir.FullPath, "parts.csv", csvResult.Content!, generatedFiles);
        else
            Console.WriteLine($"  [warning] CSV export skipped: {csvResult.ErrorMessage}");

        // instructions.md
        var mdResult = new MarkdownInstructionsExporter().Export(graph);
        if (mdResult.Success)
            await WriteFileAsync(outputDir.FullPath, "instructions.md", mdResult.Content!, generatedFiles);
        else
            Console.WriteLine($"  [warning] Markdown export skipped: {mdResult.ErrorMessage}");

        // report.md
        var reportData = new GenerationReportData
        {
            OriginalPrompt = userPrompt,
            AiModelName = analysis.UsedFallback ? null : modelName,
            AnalysisResult = analysis,
            ValidationResult = validation,
            GeneratedFiles = generatedFiles.AsReadOnly(),
            Timestamp = DateTimeOffset.UtcNow
        };

        var reportResult = new ReportExporter().Export(graph, reportData);
        if (reportResult.Success)
            await WriteFileAsync(outputDir.FullPath, "report.md", reportResult.Content!, generatedFiles);
        else
            Console.WriteLine($"  [warning] Report export skipped: {reportResult.ErrorMessage}");

        // ── Done ──────────────────────────────────────────────────────────────

        Console.WriteLine();
        Console.WriteLine($"Done. {generatedFiles.Count} file(s) written to:");
        Console.WriteLine($"  {outputDir.RelativePath}");
        Console.WriteLine();

        return 0;
    }

    // ── Path helpers ──────────────────────────────────────────────────────────

    private sealed record OutputDirectory(string FullPath, string RelativePath);

    /// <summary>
    /// Resolves and validates the output directory for a job.
    /// Returns null if the resolved path is not safely contained within <paramref name="outputRoot"/>.
    /// </summary>
    private static OutputDirectory? ResolveOutputDirectory(string outputRoot, string jobId)
    {
        var rootFull = Path.GetFullPath(outputRoot);
        var jobFull = Path.GetFullPath(Path.Combine(rootFull, jobId));

        // Safety check: job path must remain strictly under the output root
        if (!jobFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;

        return new OutputDirectory(jobFull, Path.Combine(outputRoot, jobId));
    }

    private static async Task WriteFileAsync(string directory, string fileName, string content, List<string> written)
    {
        var filePath = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF8);
        written.Add(fileName);
    }
}
