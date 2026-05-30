using System.Net;
using System.Text;
using BrickForge.Ai;
using BrickForge.Core.Options;

namespace BrickForge.Ai.Tests;

/// <summary>
/// Unit tests for <see cref="OllamaClient"/>.
/// All tests use a fake HTTP handler – no live Ollama instance is required.
/// </summary>
public sealed class OllamaClientTests
{
    // ── IsAvailableAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaReturns200_ReturnsTrue()
    {
        var client = BuildClient(HttpStatusCode.OK, """{"models":[]}""");

        var result = await client.IsAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaReturns500_ReturnsFalse()
    {
        var client = BuildClient(HttpStatusCode.InternalServerError, string.Empty);

        var result = await client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaIsUnreachable_ReturnsFalse()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused"));
        var client = BuildClientWithHandler(handler);

        var result = await client.IsAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenCancelled_ReturnsFalse()
    {
        var handler = new ThrowingHttpMessageHandler(new OperationCanceledException());
        var client = BuildClientWithHandler(handler);

        var result = await client.IsAvailableAsync(CancellationToken.None);

        Assert.False(result);
    }

    // ── GenerateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateAsync_WhenValidResponse_ReturnsResponseText()
    {
        const string expected = """{"model_name":"Test"}""";
        var body = $$"""{"model":"llama3.1:8b","response":{{JsonString(expected)}},"done":true}""";
        var client = BuildClient(HttpStatusCode.OK, body);

        var result = await client.GenerateAsync("system", "user");

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.Value);
    }

    [Fact]
    public async Task GenerateAsync_WhenOllamaReturnsHttpError_ReturnsFailure()
    {
        var client = BuildClient(HttpStatusCode.ServiceUnavailable, string.Empty);

        var result = await client.GenerateAsync("system", "user");

        Assert.False(result.IsSuccess);
        Assert.Contains("503", result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_WhenOllamaIsUnreachable_ReturnsFailure()
    {
        var handler = new ThrowingHttpMessageHandler(new HttpRequestException("No connection"));
        var client = BuildClientWithHandler(handler);

        var result = await client.GenerateAsync("system", "user");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GenerateAsync_WhenTimeout_ReturnsTimeoutFailure()
    {
        var handler = new ThrowingHttpMessageHandler(
            new TaskCanceledException("timeout", new TimeoutException()));
        var client = BuildClientWithHandler(handler);

        var result = await client.GenerateAsync("system", "user");

        Assert.False(result.IsSuccess);
        Assert.Contains("timed out", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_WhenCancelledByToken_ReturnsCancellationFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Use a handler that honours the cancellation token
        var handler = new CancellingHttpMessageHandler();
        var client = BuildClientWithHandler(handler);

        var result = await client.GenerateAsync("system", "user", cts.Token);

        Assert.False(result.IsSuccess);
        Assert.Contains("cancel", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_WhenResponseMissingResponseField_ReturnsFailure()
    {
        var body = """{"model":"llama3.1:8b","done":true}""";
        var client = BuildClient(HttpStatusCode.OK, body);

        var result = await client.GenerateAsync("system", "user");

        Assert.False(result.IsSuccess);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OllamaClient BuildClient(HttpStatusCode statusCode, string responseBody)
    {
        var handler = new FakeHttpMessageHandler(statusCode, responseBody);
        return BuildClientWithHandler(handler);
    }

    private static OllamaClient BuildClientWithHandler(HttpMessageHandler handler)
    {
        var options = new OllamaOptions
        {
            BaseUrl = "http://localhost:11434",
            Model = "llama3.1:8b",
            TimeoutSeconds = 30,
            Temperature = 0.2
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
        };

        return new OllamaClient(httpClient, options);
    }

    /// <summary>Wraps a JSON string value for embedding in a larger JSON string.</summary>
    private static string JsonString(string value) =>
        System.Text.Json.JsonSerializer.Serialize(value);
}

// ── Fake HTTP handlers ────────────────────────────────────────────────────────

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _content;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string content)
    {
        _statusCode = statusCode;
        _content = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromResult(new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_content, Encoding.UTF8, "application/json")
        });
}

internal sealed class ThrowingHttpMessageHandler : HttpMessageHandler
{
    private readonly Exception _exception;

    public ThrowingHttpMessageHandler(Exception exception)
    {
        _exception = exception;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        Task.FromException<HttpResponseMessage>(_exception);
}

internal sealed class CancellingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
