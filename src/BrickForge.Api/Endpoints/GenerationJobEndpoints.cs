using BrickForge.Api.Dtos;
using BrickForge.Core.Jobs;
using BrickForge.Core.Options;
using BrickForge.Core.Pipelines;
using Microsoft.Extensions.Options;

namespace BrickForge.Api.Endpoints;

/// <summary>
/// All generation-job endpoints mapped under /api/generation-jobs.
/// </summary>
public static class GenerationJobEndpoints
{
    private const int MaxPromptLength = 2000;
    private const int MinPromptLength = 1;

    public static void MapGenerationJobEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/generation-jobs").WithTags("GenerationJobs");

        group.MapPost("/", CreateJobAsync)
             .WithName("CreateGenerationJob")
             .WithSummary("Create a new generation job");

        group.MapGet("/{id}", GetJobAsync)
             .WithName("GetGenerationJob")
             .WithSummary("Get job status by ID");

        group.MapGet("/{id}/files", GetFilesAsync)
             .WithName("GetGenerationJobFiles")
             .WithSummary("List files produced by a job");

        group.MapGet("/{id}/validation", GetValidationAsync)
             .WithName("GetGenerationJobValidation")
             .WithSummary("Get validation result for a job");

        group.MapGet("/{id}/download", DownloadFileAsync)
             .WithName("DownloadGenerationJobFile")
             .WithSummary("Download a generated file by fileId");
    }

    // ── POST /api/generation-jobs ────────────────────────────────────────────

    private static async Task<IResult> CreateJobAsync(
        CreateJobRequest request,
        IJobRepository jobs,
        IJobQueue queue,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Prompt)
            || request.Prompt.Trim().Length < MinPromptLength)
        {
            return Results.BadRequest(new { error = "Prompt must not be empty." });
        }

        if (request.Prompt.Length > MaxPromptLength)
        {
            return Results.BadRequest(new { error = $"Prompt must not exceed {MaxPromptLength} characters." });
        }

        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = request.Prompt.Trim(),
            Status = JobStatus.Queued,
            TargetParts = request.TargetParts
        };

        await jobs.CreateAsync(job, ct);
        queue.Enqueue(job.Id);

        var response = new CreateJobResponse(job.Id, job.Status.ToString());
        return Results.Created($"/api/generation-jobs/{job.Id}", response);
    }

    // ── GET /api/generation-jobs/{id} ────────────────────────────────────────

    private static async Task<IResult> GetJobAsync(
        string id,
        IJobRepository jobs,
        CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(id, ct);
        if (job is null)
            return Results.NotFound(new { error = $"Job '{id}' not found." });

        var response = new JobStatusResponse(
            job.Id,
            job.Status.ToString(),
            job.TemplateName,
            job.TargetParts,
            job.ActualParts,
            job.ValidationScore,
            job.ErrorMessage,
            job.CreatedAt);

        return Results.Ok(response);
    }

    // ── GET /api/generation-jobs/{id}/files ──────────────────────────────────

    private static async Task<IResult> GetFilesAsync(
        string id,
        IJobRepository jobs,
        CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(id, ct);
        if (job is null)
            return Results.NotFound(new { error = $"Job '{id}' not found." });

        var files = job.Files.Select(f => new JobFileDto(
            f.Id,
            f.FileType,
            Path.GetFileName(f.FilePath))).ToList();

        return Results.Ok(files);
    }

    // ── GET /api/generation-jobs/{id}/validation ──────────────────────────────

    private static async Task<IResult> GetValidationAsync(
        string id,
        IJobRepository jobs,
        CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(id, ct);
        if (job is null)
            return Results.NotFound(new { error = $"Job '{id}' not found." });

        var validationFile = job.Files.FirstOrDefault(
            f => string.Equals(f.FileType, "validation.json", StringComparison.OrdinalIgnoreCase));

        if (validationFile is null)
            return Results.NotFound(new { error = "Validation result not yet available." });

        if (!File.Exists(validationFile.FilePath))
            return Results.NotFound(new { error = "Validation file not found on disk." });

        var rawJson = await File.ReadAllTextAsync(validationFile.FilePath, ct);

        // Parse valid/score from the file without executing any content.
        bool valid = false;
        double score = 0.0;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("IsValid", out var isValid))
                valid = isValid.GetBoolean();
            if (root.TryGetProperty("Score", out var scoreEl))
                score = scoreEl.GetDouble();
        }
        catch (System.Text.Json.JsonException)
        {
            // Return raw content even if re-parsing fails.
        }

        return Results.Ok(new ValidationSummaryResponse(valid, score, rawJson));
    }

    // ── GET /api/generation-jobs/{id}/download?fileId=X ──────────────────────

    private static async Task<IResult> DownloadFileAsync(
        string id,
        string fileId,
        IJobRepository jobs,
        IOptions<GenerationOptions> genOptions,
        CancellationToken ct)
    {
        var job = await jobs.GetByIdAsync(id, ct);
        if (job is null)
            return Results.NotFound(new { error = $"Job '{id}' not found." });

        // Resolve file from job metadata – never from user-supplied path.
        var file = job.Files.FirstOrDefault(
            f => string.Equals(f.Id, fileId, StringComparison.Ordinal));

        if (file is null)
            return Results.NotFound(new { error = $"File '{fileId}' not found for job '{id}'." });

        // Path security: verify the resolved path is within the configured output root.
        var outputRoot = Path.GetFullPath(genOptions.Value.OutputRoot);
        var fullPath = Path.GetFullPath(file.FilePath);

        if (!fullPath.StartsWith(outputRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !fullPath.StartsWith(outputRoot + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                detail: "Access denied.");
        }

        if (!File.Exists(fullPath))
            return Results.NotFound(new { error = "File not found on disk." });

        var contentType = GetContentType(file.FileType);
        var fileName = Path.GetFileName(fullPath);

        var bytes = await File.ReadAllBytesAsync(fullPath, ct);
        return Results.File(bytes, contentType, fileName);
    }

    private static string GetContentType(string fileType) => fileType.ToLowerInvariant() switch
    {
        var ft when ft.EndsWith(".json") => "application/json",
        var ft when ft.EndsWith(".csv") => "text/csv; charset=utf-8",
        var ft when ft.EndsWith(".md") => "text/markdown; charset=utf-8",
        var ft when ft.EndsWith(".mpd") || ft.EndsWith(".ldr") => "text/plain; charset=utf-8",
        _ => "application/octet-stream"
    };
}
