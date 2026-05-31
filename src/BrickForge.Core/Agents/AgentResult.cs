namespace BrickForge.Core.Agents;

/// <summary>
/// The typed outcome of an agent run.
/// Prefer <see cref="Success"/> and <see cref="Failure"/> factory methods.
/// </summary>
public sealed class AgentResult<T>
{
    public bool    IsSuccess    { get; private init; }
    public T?      Value        { get; private init; }
    public string? ErrorMessage { get; private init; }

    /// <summary>Creates a successful result with the given value.</summary>
    public static AgentResult<T> Success(T value)
        => new() { IsSuccess = true, Value = value };

    /// <summary>Creates a failed result with the given error message.</summary>
    public static AgentResult<T> Failure(string error)
        => new() { IsSuccess = false, ErrorMessage = error };
}
