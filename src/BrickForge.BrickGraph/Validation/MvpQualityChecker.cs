namespace BrickForge.BrickGraph.Validation;

/// <summary>
/// Checks whether a generated model result meets the MVP 1 quality definition (BF-MVP1-033).
///
/// A model is MVP-acceptable when:
/// 1. It uses only supported parts (no UNSUPPORTED_PART or UNSUPPORTED_COLOR issues).
/// 2. It has no high-severity validation errors (ValidationResult.Valid == true).
/// 3. It is LDraw/MPD exportable (model.mpd present in generated files).
/// 4. It has a comprehensible parts list (parts.csv present).
/// 5. It has a simple step-by-step instruction (instructions.md present).
/// 6. It has a BrickGraph record (brickgraph.json present).
///
/// Criterion "no external AI costs" is guaranteed by the local-only Ollama policy enforced
/// at configuration level and is therefore not a runtime check.
/// </summary>
public sealed class MvpQualityChecker
{
    /// <summary>Required output files for a model to be considered exportable and documented.</summary>
    private static readonly IReadOnlyList<(string FileName, string CriterionCode, string Message)> RequiredFiles =
    [
        ("model.mpd",        "MISSING_MPD",          "LDraw/MPD-Exportdatei (model.mpd) fehlt. Das Modell ist nicht exportierbar."),
        ("parts.csv",        "MISSING_PARTS_CSV",     "Teileliste (parts.csv) fehlt. Das Modell hat keine nachvollziehbare Teileliste."),
        ("instructions.md",  "MISSING_INSTRUCTIONS",  "Schritt-für-Schritt-Anleitung (instructions.md) fehlt."),
        ("brickgraph.json",  "MISSING_BRICKGRAPH",    "BrickGraph-Datei (brickgraph.json) fehlt."),
    ];

    /// <summary>
    /// Evaluates the MVP quality criteria for a completed generation result.
    /// </summary>
    /// <param name="validation">The validation result produced by <see cref="BrickGraphValidator"/>.</param>
    /// <param name="generatedFileNames">
    ///   The file names (not paths) of all files produced by the export pipeline.
    ///   Typically: model.mpd, parts.csv, instructions.md, report.md, brickgraph.json, validation.json.
    /// </param>
    public MvpQualityResult Check(
        ValidationResult validation,
        IEnumerable<string> generatedFileNames)
    {
        var failedCriteria = new List<string>();
        var failureMessages = new List<string>();

        // Criterion 1: Supported parts only
        if (validation.Issues.Any(i => i.Code is "UNSUPPORTED_PART" or "UNSUPPORTED_COLOR"))
        {
            failedCriteria.Add("UNSUPPORTED_PARTS");
            failureMessages.Add("Das Modell enthält nicht unterstützte Teile oder Farben.");
        }

        // Criterion 2: No high-severity validation errors
        if (!validation.Valid)
        {
            failedCriteria.Add("VALIDATION_FAILED");
            failureMessages.Add("Das Modell hat kritische Validierungsfehler (High-Severity).");
        }

        // Criteria 3-6: Required output files present
        var fileSet = new HashSet<string>(
            generatedFileNames.Select(f => Path.GetFileName(f).ToLowerInvariant()),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (fileName, code, message) in RequiredFiles)
        {
            if (!fileSet.Contains(fileName.ToLowerInvariant()))
            {
                failedCriteria.Add(code);
                failureMessages.Add(message);
            }
        }

        return new MvpQualityResult
        {
            FailedCriteria = failedCriteria.AsReadOnly(),
            FailureMessages = failureMessages.AsReadOnly()
        };
    }
}
