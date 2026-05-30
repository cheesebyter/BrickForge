using BrickForge.BrickGraph.Model;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Validation;

namespace BrickForge.BrickGraph.Tests.Validation;

/// <summary>
/// Unit tests for <see cref="BrickGraphValidator"/> (BF-MVP0-012).
/// </summary>
public sealed class BrickGraphValidatorTests
{
    private static readonly SupportedPartsRegistry Registry = BuildRegistry();
    private readonly BrickGraphValidator _validator = new(Registry);

    // ── NonEmptyPartsCheck ────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenPartsListIsEmpty_ReturnsHighSeverityIssue()
    {
        var graph = new BrickGraph();

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i =>
            i.Code == "EMPTY_PARTS" && i.Severity == ValidationSeverity.High);
    }

    // ── MaxPartsCheck ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenPartsExceedMaximum_ReturnsHighSeverityIssue()
    {
        var graph = new BrickGraph();
        for (int i = 1; i <= 81; i++)
            graph.AddPart(MakeValidPart($"part_{i:000}"));

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i => i.Code == "TOO_MANY_PARTS");
    }

    [Fact]
    public void Validate_WhenPartsEqualsMaximum_DoesNotReportTooManyParts()
    {
        var graph = new BrickGraph();
        for (int i = 1; i <= 80; i++)
            graph.AddPart(MakeValidPart($"part_{i:000}"));

        var result = _validator.Validate(graph);

        Assert.DoesNotContain(result.Issues, i => i.Code == "TOO_MANY_PARTS");
    }

    // ── SupportedPartCheck ────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenPartIsUnsupported_ReturnsHighSeverityIssue()
    {
        var graph = new BrickGraph();
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "part_001",
            PartNumber = "9999", // not supported
            PartName = "Unknown Brick",
            Color = "black",
            Position = [0f, 0f, 0f],
            Step = 1
        });

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i =>
            i.Code == "UNSUPPORTED_PART" && i.Severity == ValidationSeverity.High);
    }

    // ── AllowedColorCheck ─────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenColorIsUnsupported_ReturnsHighSeverityIssue()
    {
        var graph = new BrickGraph();
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "part_001",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "neon_pink", // not supported
            Position = [0f, 0f, 0f],
            Step = 1
        });

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i =>
            i.Code == "UNSUPPORTED_COLOR" && i.Severity == ValidationSeverity.High);
    }

    // ── StepAssignmentCheck ───────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenPartHasStepZero_ReturnsHighSeverityIssue()
    {
        var graph = new BrickGraph();
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "part_001",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Position = [0f, 0f, 0f],
            Step = 0 // invalid
        });

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
        Assert.Contains(result.Issues, i =>
            i.Code == "INVALID_STEP" && i.Severity == ValidationSeverity.High);
    }

    // ── PositionAssignedCheck ─────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenPositionHasWrongLength_ReturnsMediumSeverityIssue()
    {
        var graph = new BrickGraph();
        graph.AddPart(new BrickPartInstance
        {
            InstanceId = "part_001",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Position = [0f, 0f], // wrong length
            Step = 1
        });

        var result = _validator.Validate(graph);

        Assert.Contains(result.Issues, i =>
            i.Code == "INVALID_POSITION" && i.Severity == ValidationSeverity.Medium);
    }

    // ── Score and Valid ───────────────────────────────────────────────────────

    [Fact]
    public void Validate_WhenGraphIsValid_ReturnsValidTrueAndScoreOne()
    {
        var graph = new BrickGraph();
        graph.AddPart(MakeValidPart("part_001"));

        var result = _validator.Validate(graph);

        Assert.True(result.Valid);
        Assert.Equal(1.0f, result.Score, precision: 4);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void Validate_WhenHighSeverityIssueExists_ValidIsFalse()
    {
        var graph = new BrickGraph(); // empty → HIGH severity

        var result = _validator.Validate(graph);

        Assert.False(result.Valid);
    }

    [Fact]
    public void Validate_Score_IsProportionalToPassedChecks()
    {
        var graph = new BrickGraph(); // empty → EMPTY_PARTS (1 failure of 6)

        var result = _validator.Validate(graph);

        Assert.True(result.Score < 1.0f);
        Assert.True(result.Score >= 0.0f);
    }

    // ── JSON serialization ────────────────────────────────────────────────────

    [Fact]
    public void ValidationResult_ToJson_ContainsRequiredFields()
    {
        var graph = new BrickGraph();
        graph.AddPart(MakeValidPart("part_001"));

        var result = _validator.Validate(graph);
        var json = result.ToJson();

        Assert.Contains("\"valid\"", json);
        Assert.Contains("\"score\"", json);
        Assert.Contains("\"issues\"", json);
    }

    [Fact]
    public void ValidationResult_RoundTrip_PreservesData()
    {
        var graph = new BrickGraph();
        graph.AddPart(MakeValidPart("part_001"));

        var result = _validator.Validate(graph);
        var json = result.ToJson();
        var restored = ValidationResult.FromJson(json);

        Assert.NotNull(restored);
        Assert.Equal(result.Valid, restored.Valid);
        Assert.Equal(result.Score, restored.Score, precision: 4);
        Assert.Equal(result.Issues.Count, restored.Issues.Count);
    }

    [Fact]
    public void ValidationResult_FromJson_WhenInvalid_ReturnsNull()
    {
        var result = ValidationResult.FromJson("not json {{");

        Assert.Null(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static BrickPartInstance MakeValidPart(string instanceId) =>
        new()
        {
            InstanceId = instanceId,
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "black",
            Position = [0f, 0f, 0f],
            Step = 1
        };

    private static SupportedPartsRegistry BuildRegistry()
    {
        const string parts = """
            [
              { "part_number": "3001", "part_name": "Brick 2 x 4" },
              { "part_number": "3004", "part_name": "Brick 1 x 2" }
            ]
            """;
        const string colors = """["black","white","light_bluish_gray"]""";
        return SupportedPartsRegistry.FromJson(parts, colors);
    }
}
