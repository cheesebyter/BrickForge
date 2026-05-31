using BrickForge.Ai.Analysis;
using BrickForge.BrickGraph.Generation;
using BrickForge.BrickGraph.Templates;
using BrickForge.BrickGraph.Validation;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;
using BrickForge.Core.Pipelines;
using BrickForge.Export;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Services;

/// <summary>
/// Runs the full generation pipeline for a queued job.
/// Mirrors the MVP0 GenerateCommand, updating job status in the repository at each step.
/// </summary>
public sealed class GenerationPipelineService : IGenerationPipelineService
{
    private readonly IJobRepository _jobs;
    private readonly IPromptAnalyzer _promptAnalyzer;
    private readonly SmallMachineGenerator _generator;
    private readonly BrickGraphValidator _validator;
    private readonly TemplateRegistry _templateRegistry;
    private readonly GenerationOptions _generationOptions;
    private readonly ILogger<GenerationPipelineService> _logger;

    public GenerationPipelineService(
        IJobRepository jobs,
        IPromptAnalyzer promptAnalyzer,
        SmallMachineGenerator generator,
        BrickGraphValidator validator,
        TemplateRegistry templateRegistry,
        IOptions<GenerationOptions> genOptions,
        ILogger<GenerationPipelineService> logger)
    {
        _jobs = jobs;
        _promptAnalyzer = promptAnalyzer;
        _generator = generator;
        _validator = validator;
        _templateRegistry = templateRegistry;
        _generationOptions = genOptions.Value;
        _logger = logger;
    }

    public async Task RunAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _logger.BeginScope(new { JobId = jobId });

        var job = await _jobs.GetByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            _logger.LogWarning("Job {JobId} not found, skipping pipeline.", jobId);
            return;
        }

        try
        {
            // Step 1: Analyze prompt
            job.Status = JobStatus.AnalyzingPrompt;
            await _jobs.UpdateAsync(job, cancellationToken);

            var analysisResult = await _promptAnalyzer.AnalyzeAsync(job.Prompt, cancellationToken);
            if (!analysisResult.IsSuccess)
            {
                await FailJobAsync(job, analysisResult.ErrorMessage ?? "Prompt analysis failed.", cancellationToken);
                return;
            }

            var analysis = analysisResult.Value!;

            if (!analysis.Feasible)
            {
                await FailJobAsync(
                    job,
                    "Prompt is not feasible: " + string.Join("; ", analysis.Warnings),
                    cancellationToken);
                return;
            }

            _logger.LogInformation("Prompt analyzed. Model: {ModelName}, Parts: {Parts}",
                analysis.ModelName, analysis.TargetParts);

            // Step 2: Plan model (select template)
            job.Status = JobStatus.PlanningModel;
            job.TargetParts = analysis.TargetParts;
            job.TemplateName = analysis.ModelCategory;
            await _jobs.UpdateAsync(job, cancellationToken);

            var template = _templateRegistry.FindTemplate(analysis.ModelCategory)
                           ?? _templateRegistry.FindTemplate("small_machine");

            if (template is null)
            {
                await FailJobAsync(job, "No suitable template found.", cancellationToken);
                return;
            }

            // Step 3: Generate BrickGraph
            job.Status = JobStatus.GeneratingBrickGraph;
            await _jobs.UpdateAsync(job, cancellationToken);

            var graph = _generator.Generate(analysis, template);

            // Step 4: Validate
            job.Status = JobStatus.Validating;
            await _jobs.UpdateAsync(job, cancellationToken);

            var validation = _validator.Validate(graph);
            job.ValidationScore = validation.Score;

            if (!validation.Valid)
            {
                _logger.LogWarning("Validation failed for job {JobId}: {IssueCount} high-severity issues.",
                    jobId, validation.Issues.Count);
                await FailJobAsync(job, "Validation failed: high-severity issues detected.", cancellationToken);
                return;
            }

            // Step 5: Export
            job.Status = JobStatus.Exporting;
            job.ActualParts = graph.Parts.Count;
            await _jobs.UpdateAsync(job, cancellationToken);

            var outputDir = ResolveOutputDirectory(_generationOptions.OutputRoot, jobId);
            if (outputDir is null)
            {
                await FailJobAsync(job, "Output path could not be resolved safely.", cancellationToken);
                return;
            }

            Directory.CreateDirectory(outputDir);
            job.OutputPath = Path.Combine(_generationOptions.OutputRoot, jobId);

            var generatedFileNames = new List<string>();

            // brickgraph.json
            await WriteFileAndRecordAsync(job, outputDir, "brickgraph.json", graph.ToJson(),
                generatedFileNames, cancellationToken);

            // validation.json
            await WriteFileAndRecordAsync(job, outputDir, "validation.json", validation.ToJson(),
                generatedFileNames, cancellationToken);

            // model.mpd
            var ldrawResult = new LDrawExporter().Export(graph);
            if (ldrawResult.Success)
                await WriteFileAndRecordAsync(job, outputDir, "model.mpd", ldrawResult.Content!,
                    generatedFileNames, cancellationToken);
            else
                _logger.LogWarning("LDraw export skipped: {Error}", ldrawResult.ErrorMessage);

            // parts.csv
            var csvResult = new CsvPartsExporter().Export(graph);
            if (csvResult.Success)
                await WriteFileAndRecordAsync(job, outputDir, "parts.csv", csvResult.Content!,
                    generatedFileNames, cancellationToken);
            else
                _logger.LogWarning("CSV export skipped: {Error}", csvResult.ErrorMessage);

            // instructions.md
            var mdResult = new MarkdownInstructionsExporter().Export(graph);
            if (mdResult.Success)
                await WriteFileAndRecordAsync(job, outputDir, "instructions.md", mdResult.Content!,
                    generatedFileNames, cancellationToken);
            else
                _logger.LogWarning("Markdown export skipped: {Error}", mdResult.ErrorMessage);

            // report.md
            var reportData = new GenerationReportData
            {
                OriginalPrompt = job.Prompt,
                AiModelName = analysis.UsedFallback ? null : "ollama",
                AnalysisResult = analysis,
                ValidationResult = validation,
                GeneratedFiles = generatedFileNames.AsReadOnly(),
                Timestamp = DateTimeOffset.UtcNow
            };

            var reportResult = new ReportExporter().Export(graph, reportData);
            if (reportResult.Success)
                await WriteFileAndRecordAsync(job, outputDir, "report.md", reportResult.Content!,
                    generatedFileNames, cancellationToken);
            else
                _logger.LogWarning("Report export skipped: {Error}", reportResult.ErrorMessage);

            // Final status
            job.Status = validation.Issues.Count > 0
                ? JobStatus.CompletedWithWarnings
                : JobStatus.Completed;
            await _jobs.UpdateAsync(job, cancellationToken);

            _logger.LogInformation("Job {JobId} completed with {FileCount} file(s).", jobId, generatedFileNames.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Job {JobId} was cancelled.", jobId);
            await FailJobAsync(job, "Job was cancelled.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed unexpectedly.", jobId);
            await FailJobAsync(job, ex.Message, CancellationToken.None);
        }
    }

    private async Task FailJobAsync(GenerationJob job, string errorMessage, CancellationToken cancellationToken)
    {
        job.Status = JobStatus.Failed;
        job.ErrorMessage = errorMessage;
        try
        {
            await _jobs.UpdateAsync(job, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update job {JobId} to Failed status.", job.Id);
        }
    }

    private async Task WriteFileAndRecordAsync(
        GenerationJob job,
        string outputDir,
        string fileName,
        string content,
        List<string> fileNames,
        CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(outputDir, fileName);
        await File.WriteAllTextAsync(filePath, content, System.Text.Encoding.UTF8, cancellationToken);
        fileNames.Add(fileName);

        job.Files.Add(new GeneratedFile
        {
            Id = Guid.NewGuid().ToString("N"),
            JobId = job.Id,
            FileType = fileName,
            FilePath = filePath,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _jobs.UpdateAsync(job, cancellationToken);
    }

    private static string? ResolveOutputDirectory(string outputRoot, string jobId)
    {
        var rootFull = Path.GetFullPath(outputRoot);
        var jobFull = Path.GetFullPath(Path.Combine(rootFull, jobId));

        if (!jobFull.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            return null;

        return jobFull;
    }
}
