using System.Net;
using System.Text;
using System.Text.Json;
using BrickForge.Ai.Dto;
using BrickForge.Core.Options;
using BrickForge.Core.Results;

namespace BrickForge.Ai;

/// <summary>
/// HTTP client for the local Ollama REST API.
/// All AI calls stay on the local machine. No external API is contacted.
/// </summary>
public sealed class OllamaClient : IOllamaClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    /// <param name="httpClient">
    /// Pre-configured client. The caller is responsible for setting
    /// <see cref="HttpClient.BaseAddress"/> and <see cref="HttpClient.Timeout"/>.
    /// </param>
    public OllamaClient(HttpClient httpClient, OllamaOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new OllamaGenerateRequest
            {
                Model = _options.PlanningModel,
                Prompt = userPrompt,
                System = systemPrompt,
                Stream = false,
                Options = new OllamaModelOptions { Temperature = _options.Temperature }
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync("/api/generate", content, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return Result<string>.Failure(
                    $"Ollama returned HTTP {(int)httpResponse.StatusCode} {httpResponse.StatusCode}.");
            }

            var body = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
            var generateResponse = JsonSerializer.Deserialize<OllamaGenerateResponse>(body, JsonOptions);

            if (generateResponse?.Response is null)
                return Result<string>.Failure("Ollama returned an empty or unrecognised response.");

            return Result<string>.Success(generateResponse.Response);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return Result<string>.Failure(
                $"Ollama request timed out after {_options.TimeoutSeconds} seconds.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<string>.Failure("Ollama request was cancelled.");
        }
        catch (HttpRequestException ex)
        {
            return Result<string>.Failure($"Ollama is not reachable: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<string>.Failure($"Ollama request failed unexpectedly: {ex.Message}");
        }
    }
}
