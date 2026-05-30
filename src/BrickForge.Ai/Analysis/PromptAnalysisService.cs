using System.Text.Json;
using BrickForge.Ai.Prompts;
using BrickForge.Core.Ai;
using BrickForge.Core.Options;
using BrickForge.Core.Results;

namespace BrickForge.Ai.Analysis;

/// <summary>
/// AI-assisted prompt analyser that calls local Ollama and validates the JSON response.
/// Falls back to <see cref="FallbackPromptAnalyzer"/> on any AI failure or parse error.
/// </summary>
public sealed class PromptAnalysisService : IPromptAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "small_machine", "small_building", "small_vehicle", "display_object"
    };

    private static readonly HashSet<string> AllowedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "black", "white", "red", "blue", "yellow",
        "light_bluish_gray", "dark_bluish_gray", "transparent_clear"
    };

    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaOptions _ollamaOptions;
    private readonly GenerationOptions _generationOptions;
    private readonly FallbackPromptAnalyzer _fallback;

    public PromptAnalysisService(
        IOllamaClient ollamaClient,
        OllamaOptions ollamaOptions,
        GenerationOptions generationOptions)
    {
        _ollamaClient = ollamaClient;
        _ollamaOptions = ollamaOptions;
        _generationOptions = generationOptions;
        _fallback = new FallbackPromptAnalyzer(generationOptions);
    }

    /// <inheritdoc />
    public async Task<Result<PromptAnalysisResult>> AnalyzeAsync(
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var userMessage = $"Benutzereingabe:\n{userPrompt}";

        var generateResult = await _ollamaClient.GenerateAsync(
            PromptTemplates.PromptAnalysisSystemPrompt,
            userMessage,
            cancellationToken);

        if (!generateResult.IsSuccess)
            return Result<PromptAnalysisResult>.Success(_fallback.Analyze(userPrompt));

        var dto = TryParseDto(generateResult.Value!);
        if (dto is null)
            return Result<PromptAnalysisResult>.Success(_fallback.Analyze(userPrompt));

        return Result<PromptAnalysisResult>.Success(MapToResult(dto, userPrompt));
    }

    private PromptAnalysisResult MapToResult(PromptAnalysisDto dto, string userPrompt)
    {
        var rawParts = dto.TargetParts ?? _generationOptions.DefaultTargetParts;
        var targetParts = Math.Clamp(rawParts, 1, _generationOptions.MaxParts);

        var category = ValidateCategory(dto.ModelCategory) ?? "small_machine";
        var mainColor = ValidateColor(dto.MainColor) ?? "black";
        var accentColor = ValidateColor(dto.AccentColor) ?? "light_bluish_gray";

        var warnings = new List<string>(dto.Warnings ?? []);
        if (rawParts > _generationOptions.MaxParts)
            warnings.Add($"target_parts wurde von {rawParts} auf {_generationOptions.MaxParts} begrenzt.");

        return new PromptAnalysisResult
        {
            ModelName = string.IsNullOrWhiteSpace(dto.ModelName) ? "Brick Model" : dto.ModelName,
            ModelCategory = category,
            TargetParts = targetParts,
            MainColor = mainColor,
            AccentColor = accentColor,
            Features = (dto.Features ?? []).AsReadOnly(),
            Feasible = dto.Feasible ?? true,
            Warnings = warnings.AsReadOnly(),
            UsedFallback = false
        };
    }

    private static PromptAnalysisDto? TryParseDto(string json)
    {
        try
        {
            var cleaned = StripMarkdownFences(json.Trim());
            return JsonSerializer.Deserialize<PromptAnalysisDto>(cleaned, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Some LLMs wrap JSON in markdown code fences despite instructions. Strip them defensively.
    /// </summary>
    private static string StripMarkdownFences(string text)
    {
        if (!text.StartsWith("```"))
            return text;

        var newline = text.IndexOf('\n');
        if (newline < 0)
            return text;

        var end = text.LastIndexOf("```");
        if (end <= newline)
            return text;

        return text[(newline + 1)..end].Trim();
    }

    private static string? ValidateCategory(string? value) =>
        value is not null && AllowedCategories.Contains(value) ? value : null;

    private static string? ValidateColor(string? value) =>
        value is not null && AllowedColors.Contains(value) ? value : null;
}
