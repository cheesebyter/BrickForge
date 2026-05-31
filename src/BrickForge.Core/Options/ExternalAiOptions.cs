namespace BrickForge.Core.Options;

/// <summary>
/// Configuration for optional external AI fallback services.
///
/// <para>
/// <b>BF-MVP1-020 §20.4:</b> External AI fallbacks are allowed but not required.
/// They must be explicitly enabled via configuration and are never activated automatically.
/// When used, all external API calls are logged.
/// </para>
/// <para>
/// Default configuration: all external services <b>disabled</b>.
/// No external AI API is called unless <see cref="Enabled"/> is explicitly set to <c>true</c>.
/// </para>
/// </summary>
public sealed class ExternalAiOptions
{
    /// <summary>
    /// When false (the default), no external AI API is ever called.
    /// Must be set to true explicitly in configuration to enable.
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Optional URL of the external AI API endpoint.
    /// Only used when <see cref="Enabled"/> is true.
    /// </summary>
    public string? ApiUrl { get; init; }

    /// <summary>
    /// Maximum cost (in units defined by the external provider) allowed per generation job.
    /// Zero or negative means no limit is enforced.
    /// Only evaluated when <see cref="Enabled"/> is true.
    /// </summary>
    public decimal MaxCostPerJob { get; init; } = 0m;

    /// <summary>
    /// When true, each use of the external AI API is written to the application log.
    /// Strongly recommended when <see cref="Enabled"/> is true.
    /// </summary>
    public bool LogExternalUsage { get; init; } = true;
}
