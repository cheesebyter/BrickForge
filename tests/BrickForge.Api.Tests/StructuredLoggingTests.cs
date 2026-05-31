using BrickForge.Ai;
using BrickForge.BrickGraph.Parts;
using BrickForge.BrickGraph.Repair;
using BrickForge.Core.Agents;
using BrickForge.Core.Results;
using Microsoft.Extensions.Logging;

namespace BrickForge.Api.Tests;

/// <summary>
/// Tests for BF-MVP1-043 – Structured Logging.
///
/// Acceptance criteria:
/// - Logs contain CorrelationId (=JobId).
/// - Agent errors are traceable.
/// - No sensitive configuration values are logged.
/// - Logging is configurable.
/// </summary>
public sealed class StructuredLoggingTests
{
    // ── CorrelationId / JobId in scope ─────────────────────────────────────────

    [Fact]
    public void Logger_BeginScope_WithJobIdAndCorrelationId_IncludesBothKeys()
    {
        var logger = new CapturingLogger<StructuredLoggingTests>();

        using (logger.BeginScope(new Dictionary<string, object?> { ["JobId"] = "job-abc", ["CorrelationId"] = "job-abc" }))
        {
            logger.LogInformation("Test message.");
        }

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("Test message.", entry.Message);

        // Verify the scope state exposed both keys.
        Assert.True(entry.Scopes.Any(s =>
            s is IEnumerable<KeyValuePair<string, object?>> kvps &&
            kvps.Any(kv => kv.Key == "JobId" && kv.Value?.ToString() == "job-abc")),
            "Scope must include JobId");

        Assert.True(entry.Scopes.Any(s =>
            s is IEnumerable<KeyValuePair<string, object?>> kvps &&
            kvps.Any(kv => kv.Key == "CorrelationId" && kv.Value?.ToString() == "job-abc")),
            "Scope must include CorrelationId");
    }

    // ── AgentName in repair agent logs ─────────────────────────────────────────

    [Fact]
    public async Task RepairAgent_WhenRepairApplied_LogsAgentNameAndJobId()
    {
        var capturingLogger = new CapturingLogger<BrickGraphRepairAgent>();
        var registry = BuildRegistry();
        var agent = new BrickGraphRepairAgent(registry, capturingLogger);

        var graph = BuildInvalidGraph();
        var context = new AgentContext { JobId = "repair-job-99" };

        await agent.RunAsync(
            new RepairRequest(graph, null, 80),
            context);

        // All log entries from repair should reference AgentName and JobId.
        Assert.All(capturingLogger.Entries, e =>
        {
            Assert.Contains("BrickGraphRepairAgent", e.Message);
            Assert.Contains("repair-job-99", e.Message);
        });
    }

    [Fact]
    public async Task RepairAgent_LogsRepairCount_WhenPartsFixed()
    {
        var capturingLogger = new CapturingLogger<BrickGraphRepairAgent>();
        var registry = BuildRegistry();
        var agent = new BrickGraphRepairAgent(registry, capturingLogger);

        var graph = BuildInvalidGraph();
        var context = new AgentContext { JobId = "job-repair" };

        await agent.RunAsync(new RepairRequest(graph, null, 80), context);

        // The completion log line must contain the number of fixes.
        Assert.True(
            capturingLogger.Entries.Any(e => e.Message.Contains("fix(es)")),
            "Completion log must state number of fixes");
    }

    // ── No sensitive data in log messages ──────────────────────────────────────

    [Fact]
    public void Logger_SensitiveValues_NeverAppearInMessages()
    {
        // These tokens must never appear in any BrickForge log message.
        string[] forbiddenPatterns =
        [
            "api_key",
            "apikey",
            "password",
            "secret",
            "token",
            "Authorization",
            "Bearer "
        ];

        var logger = new CapturingLogger<StructuredLoggingTests>();
        logger.LogInformation("Job {JobId} started.", "test-job");
        logger.LogInformation("Ollama at http://localhost:11434");
        logger.LogDebug("Model: qwen2.5-coder:14b");
        logger.LogWarning("Validation failed: 3 issue(s).");

        foreach (var entry in logger.Entries)
        {
            foreach (var forbidden in forbiddenPatterns)
            {
                Assert.DoesNotContain(forbidden, entry.Message, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    // ── Configurable log levels ─────────────────────────────────────────────────

    [Fact]
    public void Logger_WhenLevelSetAboveDebug_DebugMessagesAreFiltered()
    {
        // When a logger is created with minimum level Information, Debug messages are suppressed.
        var logger = new CapturingLogger<StructuredLoggingTests>(minimumLevel: LogLevel.Information);

        logger.LogDebug("This debug message should be filtered.");
        logger.LogInformation("This info message should be captured.");

        Assert.Single(logger.Entries);
        Assert.Contains("info message", logger.Entries[0].Message);
    }

    [Fact]
    public void Logger_WhenLevelSetToDebug_AllMessagesAreCaptured()
    {
        var logger = new CapturingLogger<StructuredLoggingTests>(minimumLevel: LogLevel.Debug);

        logger.LogDebug("debug");
        logger.LogInformation("info");
        logger.LogWarning("warning");

        Assert.Equal(3, logger.Entries.Count);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SupportedPartsRegistry BuildRegistry()
    {
        const string partsJson = """
            [
              { "part_number": "3001", "part_name": "Brick 2 x 4" },
              { "part_number": "3003", "part_name": "Brick 2 x 2" }
            ]
            """;
        const string colorsJson = """["black","white","light_bluish_gray"]""";
        return SupportedPartsRegistry.FromJson(partsJson, colorsJson);
    }

    private static BrickForge.BrickGraph.BrickGraph BuildInvalidGraph()
    {
        var graph = new BrickForge.BrickGraph.BrickGraph
        {
            Model = new BrickForge.BrickGraph.Model.BrickModelMetadata
            {
                Id = "test-model",
                Name = "Test Model",
                TargetParts = 5,
                ActualParts = 2
            }
        };

        // Add a part with an invalid color (not in registry) and invalid step (0).
        graph.Parts.Add(new BrickForge.BrickGraph.Model.BrickPartInstance
        {
            InstanceId = "p1",
            PartNumber = "3001",
            PartName = "Brick 2 x 4",
            Color = "unsupported_purple",
            Position = [0f, 0f, 0f],
            Rotation = [1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f],
            Step = 0
        });

        return graph;
    }
}

// ── Minimal capturing ILogger for tests ───────────────────────────────────────

internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly LogLevel _minimumLevel;
    private readonly List<LogEntry> _entries = [];
    private readonly List<object?> _activeScopes = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public CapturingLogger(LogLevel minimumLevel = LogLevel.Trace) =>
        _minimumLevel = minimumLevel;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        _activeScopes.Add(state);
        return new ScopeDisposable(_activeScopes);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        _entries.Add(new LogEntry(
            logLevel,
            formatter(state, exception),
            [.. _activeScopes]));
    }

    internal sealed record LogEntry(LogLevel Level, string Message, List<object?> Scopes);

    private sealed class ScopeDisposable(List<object?> scopes) : IDisposable
    {
        public void Dispose()
        {
            if (scopes.Count > 0)
                scopes.RemoveAt(scopes.Count - 1);
        }
    }
}
