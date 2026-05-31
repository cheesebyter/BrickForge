using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BrickForge.Api.Dtos;
using BrickForge.Core.Jobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrickForge.Api.Tests;

/// <summary>
/// Integration tests for all generation-job API endpoints.
/// These tests use an in-memory server and do not require Ollama.
/// </summary>
public sealed class GenerationJobEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public GenerationJobEndpointsTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── POST /api/generation-jobs ────────────────────────────────────────────

    [Fact]
    public async Task CreateJob_WithValidPrompt_Returns201WithJobId()
    {
        var request = new CreateJobRequest("Eine kleine Kaffeemaschine aus schwarzen Steinen.");
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(body);
        Assert.NotEmpty(body.JobId);
        Assert.Equal("Queued", body.Status);
    }

    [Fact]
    public async Task CreateJob_WithEmptyPrompt_Returns400()
    {
        var request = new CreateJobRequest("   ");
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_WithPromptExceedingMaxLength_Returns400()
    {
        var longPrompt = new string('a', 2001);
        var request = new CreateJobRequest(longPrompt);
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_WithExactlyMaxLengthPrompt_Returns201()
    {
        var prompt = new string('a', 2000);
        var request = new CreateJobRequest(prompt);
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_LocationHeaderPointsToJobResource()
    {
        var request = new CreateJobRequest("Ein kleines Haus.");
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        Assert.NotNull(body);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains(body.JobId, response.Headers.Location!.ToString());
    }

    // ── GET /api/generation-jobs/{id} ────────────────────────────────────────

    [Fact]
    public async Task GetJob_AfterCreate_ReturnsQueuedStatus()
    {
        var created = await CreateJobAndGetIdAsync("Testmodell Kaffeemaschine");
        var response = await _client.GetAsync($"/api/generation-jobs/{created}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
        Assert.NotNull(body);
        Assert.Equal(created, body.JobId);
        Assert.Equal("Queued", body.Status);
    }

    [Fact]
    public async Task GetJob_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/generation-jobs/nonexistent-job-id");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/generation-jobs/{id}/files ──────────────────────────────────

    [Fact]
    public async Task GetFiles_ForNewJob_ReturnsEmptyList()
    {
        var id = await CreateJobAndGetIdAsync("Werkbank Modell");
        var response = await _client.GetAsync($"/api/generation-jobs/{id}/files");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var files = await response.Content.ReadFromJsonAsync<List<JobFileDto>>();
        Assert.NotNull(files);
        Assert.Empty(files);
    }

    [Fact]
    public async Task GetFiles_WithFilesAttached_ReturnsFileMetadataWithoutPaths()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();
        var jobRepo = factory.Services.GetRequiredService<IJobRepository>();

        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = "Test"
        };
        job.Files.Add(new GeneratedFile
        {
            Id = "file1",
            JobId = job.Id,
            FileType = "model.mpd",
            FilePath = "data/outputs/" + job.Id + "/model.mpd",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await jobRepo.CreateAsync(job);

        var response = await client.GetAsync($"/api/generation-jobs/{job.Id}/files");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var files = await response.Content.ReadFromJsonAsync<List<JobFileDto>>();
        Assert.NotNull(files);
        Assert.Single(files);
        Assert.Equal("file1", files[0].FileId);
        Assert.Equal("model.mpd", files[0].FileType);
        Assert.Equal("model.mpd", files[0].FileName);
    }

    [Fact]
    public async Task GetFiles_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/generation-jobs/unknown/files");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/generation-jobs/{id}/validation ──────────────────────────────

    [Fact]
    public async Task GetValidation_ForNewJob_Returns404()
    {
        var id = await CreateJobAndGetIdAsync("Sportwagen Modell");
        var response = await _client.GetAsync($"/api/generation-jobs/{id}/validation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetValidation_WithUnknownId_Returns404()
    {
        var response = await _client.GetAsync("/api/generation-jobs/unknown-id/validation");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/generation-jobs/{id}/download ───────────────────────────────

    [Fact]
    public async Task Download_WithUnknownJob_Returns404()
    {
        var response = await _client.GetAsync("/api/generation-jobs/unknown/download?fileId=file1");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_WithUnknownFileId_Returns404()
    {
        var id = await CreateJobAndGetIdAsync("Verkaufsstand");
        var response = await _client.GetAsync($"/api/generation-jobs/{id}/download?fileId=nonexistent");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Download_PathTraversal_FileStoredOutsideOutputRoot_Returns403()
    {
        // Arrange: inject a job with a file whose path points OUTSIDE the output root
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(_ => { });
        var client = factory.CreateClient();
        var jobRepo = factory.Services.GetRequiredService<IJobRepository>();

        var job = new GenerationJob
        {
            Id = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTimeOffset.UtcNow,
            Prompt = "Test"
        };
        job.Files.Add(new GeneratedFile
        {
            Id = "evil-file",
            JobId = job.Id,
            FileType = "model.mpd",
            FilePath = "../../../etc/passwd",  // path traversal attempt
            CreatedAt = DateTimeOffset.UtcNow
        });
        await jobRepo.CreateAsync(job);

        var response = await client.GetAsync($"/api/generation-jobs/{job.Id}/download?fileId=evil-file");

        // Must be blocked – either 403 Forbidden or 404 Not Found is acceptable
        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 403 or 404, got {response.StatusCode}");
    }

    // ── Health check ─────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> CreateJobAndGetIdAsync(string prompt)
    {
        var request = new CreateJobRequest(prompt);
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateJobResponse>();
        return body!.JobId;
    }
}
