using BrickForge.BrickGraph.Validation;

namespace BrickForge.BrickGraph.Tests.Validation;

/// <summary>
/// Unit tests for <see cref="MvpQualityChecker"/> (BF-MVP1-033).
/// </summary>
public sealed class MvpQualityCheckerTests
{
    private static readonly string[] AllRequiredFiles =
    [
        "model.mpd",
        "parts.csv",
        "instructions.md",
        "brickgraph.json",
        "report.md",
        "validation.json"
    ];

    private readonly MvpQualityChecker _checker = new();

    // ── IsAcceptable – positive path ──────────────────────────────────────────

    [Fact]
    public void Check_WhenValidationPassesAndAllFilesPresent_ReturnsAcceptable()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);

        var result = _checker.Check(validation, AllRequiredFiles);

        Assert.True(result.IsAcceptable);
        Assert.Empty(result.FailedCriteria);
        Assert.Empty(result.FailureMessages);
    }

    // ── Criterion 2: High-severity validation issues ──────────────────────────

    [Fact]
    public void Check_WhenValidationHasHighSeverityIssue_ReturnsValidationFailed()
    {
        var issues = new List<ValidationIssue>
        {
            new() { Code = "EMPTY_PARTS", Message = "No parts", Severity = ValidationSeverity.High }
        };
        var validation = ValidationResult.FromIssues(issues, totalChecks: 6);

        var result = _checker.Check(validation, AllRequiredFiles);

        Assert.False(result.IsAcceptable);
        Assert.Contains("VALIDATION_FAILED", result.FailedCriteria);
    }

    // ── Criterion 1: Unsupported parts / colors ───────────────────────────────

    [Fact]
    public void Check_WhenUnsupportedPartIssuePresent_ReturnsUnsupportedPartsCriterion()
    {
        var issues = new List<ValidationIssue>
        {
            new() { Code = "UNSUPPORTED_PART", Message = "Bad part", Severity = ValidationSeverity.High }
        };
        var validation = ValidationResult.FromIssues(issues, totalChecks: 6);

        var result = _checker.Check(validation, AllRequiredFiles);

        Assert.False(result.IsAcceptable);
        Assert.Contains("UNSUPPORTED_PARTS", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenUnsupportedColorIssuePresent_ReturnsUnsupportedPartsCriterion()
    {
        var issues = new List<ValidationIssue>
        {
            new() { Code = "UNSUPPORTED_COLOR", Message = "Bad color", Severity = ValidationSeverity.High }
        };
        var validation = ValidationResult.FromIssues(issues, totalChecks: 6);

        var result = _checker.Check(validation, AllRequiredFiles);

        Assert.False(result.IsAcceptable);
        Assert.Contains("UNSUPPORTED_PARTS", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenMediumSeverityIssueOnly_DoesNotReportValidationFailed()
    {
        // Medium-severity issues do not make the model invalid – only High does.
        var issues = new List<ValidationIssue>
        {
            new() { Code = "NO_BASE_PARTS", Message = "No base", Severity = ValidationSeverity.Medium }
        };
        var validation = ValidationResult.FromIssues(issues, totalChecks: 6);

        var result = _checker.Check(validation, AllRequiredFiles);

        Assert.DoesNotContain("VALIDATION_FAILED", result.FailedCriteria);
    }

    // ── Criteria 3-6: Required output files ──────────────────────────────────

    [Fact]
    public void Check_WhenMpdFileMissing_ReturnsMissingMpdCriterion()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = AllRequiredFiles.Except(["model.mpd"]);

        var result = _checker.Check(validation, files);

        Assert.False(result.IsAcceptable);
        Assert.Contains("MISSING_MPD", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenPartsCsvMissing_ReturnsMissingPartsCsvCriterion()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = AllRequiredFiles.Except(["parts.csv"]);

        var result = _checker.Check(validation, files);

        Assert.False(result.IsAcceptable);
        Assert.Contains("MISSING_PARTS_CSV", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenInstructionsMissing_ReturnsMissingInstructionsCriterion()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = AllRequiredFiles.Except(["instructions.md"]);

        var result = _checker.Check(validation, files);

        Assert.False(result.IsAcceptable);
        Assert.Contains("MISSING_INSTRUCTIONS", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenBrickGraphJsonMissing_ReturnsMissingBrickGraphCriterion()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = AllRequiredFiles.Except(["brickgraph.json"]);

        var result = _checker.Check(validation, files);

        Assert.False(result.IsAcceptable);
        Assert.Contains("MISSING_BRICKGRAPH", result.FailedCriteria);
    }

    [Fact]
    public void Check_WhenAllRequiredFilesMissing_ReportsMultipleCriteria()
    {
        var validation = ValidationResult.FromIssues([], totalChecks: 6);

        var result = _checker.Check(validation, []);

        Assert.False(result.IsAcceptable);
        Assert.Contains("MISSING_MPD", result.FailedCriteria);
        Assert.Contains("MISSING_PARTS_CSV", result.FailedCriteria);
        Assert.Contains("MISSING_INSTRUCTIONS", result.FailedCriteria);
        Assert.Contains("MISSING_BRICKGRAPH", result.FailedCriteria);
        Assert.Equal(result.FailedCriteria.Count, result.FailureMessages.Count);
    }

    [Fact]
    public void Check_FileNamesAreCaseInsensitive()
    {
        // Some OS may produce uppercase file names – the check must be case-insensitive.
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = new[] { "MODEL.MPD", "PARTS.CSV", "INSTRUCTIONS.MD", "BRICKGRAPH.JSON" };

        var result = _checker.Check(validation, files);

        Assert.DoesNotContain("MISSING_MPD", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_PARTS_CSV", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_INSTRUCTIONS", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_BRICKGRAPH", result.FailedCriteria);
    }

    [Fact]
    public void Check_FileNamesCanBeFullPaths()
    {
        // The caller may pass full paths; only the file name portion is checked.
        var validation = ValidationResult.FromIssues([], totalChecks: 6);
        var files = new[]
        {
            @"data\outputs\job-123\model.mpd",
            @"data\outputs\job-123\parts.csv",
            @"data\outputs\job-123\instructions.md",
            @"data\outputs\job-123\brickgraph.json",
        };

        var result = _checker.Check(validation, files);

        Assert.DoesNotContain("MISSING_MPD", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_PARTS_CSV", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_INSTRUCTIONS", result.FailedCriteria);
        Assert.DoesNotContain("MISSING_BRICKGRAPH", result.FailedCriteria);
    }
}
