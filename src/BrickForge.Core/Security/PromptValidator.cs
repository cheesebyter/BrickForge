using BrickForge.Core.Results;

namespace BrickForge.Core.Security;

/// <summary>
/// Validates user-supplied prompts before they reach the AI pipeline.
/// Prompts are treated as untrusted plain-text data.  This validator
/// enforces structural constraints (length, non-empty) only – it never
/// interprets or executes any content the prompt may contain.
/// BF-MVP1-046.
/// </summary>
public static class PromptValidator
{
    /// <summary>Minimum non-whitespace characters required.</summary>
    public const int MinLength = 1;

    /// <summary>
    /// Validates <paramref name="prompt"/> against the supplied
    /// <paramref name="maxLength"/> cap.
    /// Returns a successful <see cref="Result"/> when the prompt is
    /// acceptable, or a failure result with a human-readable message.
    /// </summary>
    public static Result Validate(string? prompt, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return Result.Failure("Prompt must not be empty.");

        if (prompt.TrimStart().Length < MinLength)
            return Result.Failure("Prompt must not be empty.");

        if (prompt.Length > maxLength)
            return Result.Failure($"Prompt must not exceed {maxLength} characters.");

        return Result.Success();
    }
}
