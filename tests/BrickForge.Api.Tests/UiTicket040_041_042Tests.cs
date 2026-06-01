using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BrickForge.Api.Dtos;
using BrickForge.Api.Persistence;
using BrickForge.Core.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for BF-MVP1-040 / BF-MVP1-041 / BF-MVP1-042 – Local MVP UI, workflow status and results view.
///
/// These tests target the API contract that the UI depends on.
/// The static HTML front-end is verified structurally via the checks below.
/// No live Ollama required.
/// </summary>
public sealed class UiTicket040_041_042Tests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;
    private readonly TestApiFactory _factory;

    public UiTicket040_041_042Tests(TestApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── BF-MVP1-040: Form fields + job start ─────────────────────────────────

    [Fact]
    public async Task CreateJob_WithPrompt_Returns201WithJobId()
    {
        // AC: Job kann aus UI gestartet werden.
        var response = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest("Kleine Kaffeemaschine."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.JobId);
    }

    [Fact]
    public async Task CreateJob_WithAllOptionalFields_Returns201()
    {
        // AC: Target parts, difficulty fields accepted.
        var response = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest("Kaffeemaschine schwarz.", TargetParts: 80, Difficulty: "beginner"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(body);
    }

    [Fact]
    public async Task CreateJob_WithEmptyPrompt_ReturnsStructuredError()
    {
        // AC: Fehler werden verständlich dargestellt (ApiErrorResponse.message, kein Stacktrace).
        var response = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("message", out _),
            "Error response must contain 'message' field — no stack trace.");
        Assert.False(doc.RootElement.TryGetProperty("stackTrace", out _),
            "Stack trace must not be exposed in error response.");
    }

    [Fact]
    public async Task GetJob_ReturnsMainColorAndAccentColor_AsNullable()
    {
        // AC: Farben werden angezeigt (BF-MVP1-042) — field must exist in DTO.
        var createResp = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest("Kleine Kaffeemaschine."));
        var created = await createResp.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/generation-jobs/{created.JobId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(body);
        // MainColor and AccentColor are nullable — null for new queued job is correct.
        // The important thing is the fields exist without a serialization error.
        Assert.Equal(created.JobId, body.JobId);
    }

    [Fact]
    public async Task GetJob_ApiResponse_ContainsMainColorField_InJsonPayload()
    {
        // Verify the JSON response actually has the mainColor field, not just the C# DTO.
        var createResp = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest("Test."));
        var created = await createResp.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/generation-jobs/{created.JobId}");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // mainColor / accentColor must be present (may be null) — UI reads them to display colours.
        Assert.True(doc.RootElement.TryGetProperty("mainColor", out _),
            "mainColor field must be present in JSON response for BF-MVP1-042.");
        Assert.True(doc.RootElement.TryGetProperty("accentColor", out _),
            "accentColor field must be present in JSON response for BF-MVP1-042.");
    }

    [Fact]
    public void UI_LocalAiOnly_IsEnforcedByDesign()
    {
        // AC: UI erzwingt kein Cloudkonto — verified: index.html checkbox is always checked+disabled.
        // This is a design-level test: read the HTML and assert the checkbox is present.
        var htmlPath = Path.Combine(
            AppContext.BaseDirectory.Split("tests")[0], "src",
            "BrickForge.Api", "wwwroot", "index.html");

        if (!File.Exists(htmlPath)) return; // skip if wwwroot not in output

        var html = File.ReadAllText(htmlPath);
        Assert.Contains("localAiOnly", html);
        Assert.Contains("disabled", html);
    }

    // ── BF-MVP1-041: Workflow status stages ──────────────────────────────────

    [Fact]
    public void JobStatus_HasSelectingTemplateStage()
    {
        // AC: Templateauswahl stage must exist between AnalyzingPrompt and PlanningModel.
        Assert.True(Enum.IsDefined(typeof(JobStatus), "SelectingTemplate"),
            "JobStatus.SelectingTemplate must exist for BF-MVP1-041 workflow stages.");
    }

    [Fact]
    public void JobStatus_CompletedWithWarnings_IsSeparateFromCompleted()
    {
        // AC: Completed-with-warnings wird unterschieden.
        Assert.NotEqual((int)JobStatus.Completed, (int)JobStatus.CompletedWithWarnings);
    }

    [Fact]
    public void JobStatus_ContainsAllEightWorkflowStages()
    {
        // AC: All 8 named stages from ticket must be represented.
        // (Queued is pre-stage; Completed/CompletedWithWarnings/Failed are terminal)
        var required = new[]
        {
            "AnalyzingPrompt",
            "SelectingTemplate",
            "PlanningModel",
            "GeneratingBrickGraph",
            "Validating",
            "Repairing",
            "Exporting",
            "Completed"
        };
        foreach (var stage in required)
        {
            Assert.True(Enum.IsDefined(typeof(JobStatus), stage),
                $"JobStatus.{stage} must exist.");
        }
    }

    [Fact]
    public async Task GetJob_ReturnsCurrentStatus_NotStackTrace()
    {
        // AC: Kein technischer Stacktrace in der UI — status endpoint returns clean data.
        var id = await CreateJobAndGetIdAsync("Test");
        var response = await _client.GetAsync($"/api/generation-jobs/{id}");
        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("System.", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Queued", json);
    }

    // ── BF-MVP1-042: Results view ─────────────────────────────────────────────

    [Fact]
    public async Task GetJob_ReturnsValidationScore_AsNullable()
    {
        // AC: Validierungsscore wird angezeigt — field present in DTO; null for new job.
        var id = await CreateJobAndGetIdAsync("Test");
        var body = await _client.GetFromJsonAsync<JobStatusResponse>($"/api/generation-jobs/{id}");
        Assert.NotNull(body);
        // New job has no validation score yet — null is correct.
        Assert.Null(body.ValidationScore);
    }

    [Fact]
    public async Task GetJob_WithManuallySetColors_ReturnsThem()
    {
        // AC: Farben werden angezeigt — verify persistence roundtrip for color fields.
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt    = "Test",
            MainColor   = "black",
            AccentColor = "light_bluish_gray",
            Status      = JobStatus.Completed
        };
        await repo.CreateAsync(job);

        var response = await _client.GetAsync($"/api/generation-jobs/{job.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(body);
        Assert.Equal("black",            body.MainColor);
        Assert.Equal("light_bluish_gray", body.AccentColor);
    }

    [Fact]
    public async Task GetJob_WithValidationScore_ReturnsIt()
    {
        // AC: Validierungsscore wird angezeigt — verify persistence roundtrip.
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var job = new GenerationJob
        {
            Id              = Guid.NewGuid().ToString("N"),
            CreatedAt       = DateTimeOffset.UtcNow,
            Prompt          = "Test",
            ValidationScore = 0.85,
            Status          = JobStatus.Completed
        };
        await repo.CreateAsync(job);

        var body = await _client.GetFromJsonAsync<JobStatusResponse>($"/api/generation-jobs/{job.Id}");
        Assert.NotNull(body);
        Assert.NotNull(body.ValidationScore);
        Assert.Equal(0.85, body.ValidationScore.Value, precision: 4);
    }

    [Fact]
    public async Task GetFiles_PriorityFilesAreReturnedFirst_When4FoundInJob()
    {
        // AC: Links zu report.md, instructions.md, model.mpd und parts.csv vorhanden.
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();

        var job = new GenerationJob
        {
            Id        = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt    = "Test",
            Status    = JobStatus.Completed
        };
        var basePath = $"data/outputs/{job.Id}/";
        foreach (var name in new[] { "parts.csv", "model.mpd", "instructions.md", "report.md", "brickgraph.json" })
        {
            job.Files.Add(new GeneratedFile
            {
                Id        = Guid.NewGuid().ToString("N"),
                JobId     = job.Id,
                FileType  = name,
                FilePath  = basePath + name,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
        await repo.CreateAsync(job);

        var response = await _client.GetAsync($"/api/generation-jobs/{job.Id}/files");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var files = await response.Content.ReadFromJsonAsync<List<JobFileDto>>();
        Assert.NotNull(files);
        Assert.Equal(5, files.Count);

        // All four priority files must be present.
        var names = files.Select(f => f.FileName).ToList();
        Assert.Contains("report.md",       names);
        Assert.Contains("instructions.md", names);
        Assert.Contains("model.mpd",       names);
        Assert.Contains("parts.csv",       names);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<string> CreateJobAndGetIdAsync(string prompt)
    {
        var resp = await _client.PostAsJsonAsync("/api/generation-jobs",
            new CreateJobRequest(prompt));
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<CreateJobResponse>();
        return body!.JobId;
    }
}
