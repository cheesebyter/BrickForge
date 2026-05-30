namespace BrickForge.Api.Dtos;

/// <summary>Request body for creating a new generation job.</summary>
public sealed record CreateJobRequest(
    string Prompt,
    int? TargetParts = null,
    string? Difficulty = null,
    string[]? OutputFormats = null
);

/// <summary>Response returned when a job is successfully created.</summary>
public sealed record CreateJobResponse(string JobId, string Status);

/// <summary>Full job status returned by GET /api/generation-jobs/{id}.</summary>
public sealed record JobStatusResponse(
    string JobId,
    string Status,
    string? TemplateName,
    int? TargetParts,
    int? ActualParts,
    double? ValidationScore,
    string? ErrorMessage,
    DateTimeOffset CreatedAt
);

/// <summary>Metadata for a single generated file (no internal path exposed).</summary>
public sealed record JobFileDto(
    string FileId,
    string FileType,
    string FileName
);

/// <summary>Validation summary returned by GET /api/generation-jobs/{id}/validation.</summary>
public sealed record ValidationSummaryResponse(
    bool Valid,
    double Score,
    string RawJson
);
