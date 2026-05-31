using System.Net;
using System.Net.Http.Json;
using BrickForge.Api.Dtos;
using BrickForge.Core.Pipelines;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests that all API error responses (400/404/500) conform to the standardised
/// ApiErrorResponse shape required by BF-MVP1-039.
/// Verifies: Code, Message, CorrelationId present; no stacktrace in 500 body.
/// </summary>
public sealed class ApiErrorResponseTests : IClassFixture<ApiErrorResponseTests.ThrowingApiFactory>
{
    private readonly HttpClient _client;

    public ApiErrorResponseTests(ThrowingApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── 400 validation error ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateJob_EmptyPrompt_Returns400WithStandardErrorShape()
    {
        var request = new CreateJobRequest("  ");
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    [Fact]
    public async Task CreateJob_PromptTooLong_Returns400WithValidationErrorCode()
    {
        var request = new CreateJobRequest(new string('x', 9999));
        var response = await _client.PostAsJsonAsync("/api/generation-jobs", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("VALIDATION_ERROR", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    // ── 404 not-found error ──────────────────────────────────────────────────

    [Fact]
    public async Task GetJob_UnknownId_Returns404WithStandardErrorShape()
    {
        var response = await _client.GetAsync("/api/generation-jobs/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    [Fact]
    public async Task GetFiles_UnknownId_Returns404WithNotFoundCode()
    {
        var response = await _client.GetAsync("/api/generation-jobs/no-such-job/files");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    [Fact]
    public async Task GetValidation_UnknownId_Returns404WithNotFoundCode()
    {
        var response = await _client.GetAsync("/api/generation-jobs/no-such-job/validation");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("NOT_FOUND", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
    }

    // ── 500 internal error ───────────────────────────────────────────────────

    [Fact]
    public async Task UnhandledException_Returns500WithInternalErrorCode_NoStackTrace()
    {
        // /test/throw is registered by ThrowStartupFilter — simulates an uncaught exception.
        var response = await _client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("INTERNAL_ERROR", error.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));

        // Stacktrace must NOT appear in the response body.
        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("StackTrace", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at BrickForge", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnhandledException_Response_HasNullDetails()
    {
        var response = await _client.GetAsync("/test/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.Null(error.Details);
    }

    // ── Factory ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Extends Program startup with a /test/throw route (via IStartupFilter) to verify
    /// the global exception handler returns a standard 500 ApiErrorResponse.
    /// The throw middleware is added AFTER the full pipeline (including UseExceptionHandler),
    /// so unmatched requests fall through to it and exceptions are caught.
    /// </summary>
    public sealed class ThrowingApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                // Replace job queue with no-op so no background processing occurs.
                var queueDesc = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IJobQueue));
                if (queueDesc is not null) services.Remove(queueDesc);
                services.AddSingleton<IJobQueue, NoOpJobQueue>();

                // Add a startup filter that appends the /test/throw route AFTER
                // all Program.cs middleware (including UseExceptionHandler).
                services.AddSingleton<IStartupFilter, ThrowRouteStartupFilter>();
            });
        }
    }

    /// <summary>
    /// Adds a terminal /test/throw middleware at the END of the pipeline.
    /// Because UseExceptionHandler is already registered (by Program.cs via next()),
    /// it wraps all downstream middleware including this throw route.
    /// </summary>
    private sealed class ThrowRouteStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                // Register the full Program.cs pipeline first (includes UseExceptionHandler).
                next(app);

                // Append after all endpoints: only runs for unmatched paths.
                app.Use(async (ctx, nextMiddleware) =>
                {
                    if (ctx.Request.Path.StartsWithSegments("/test/throw"))
                        throw new InvalidOperationException(
                            "Simulated unhandled exception for testing the global error handler.");
                    await nextMiddleware(ctx);
                });
            };
        }
    }
}
