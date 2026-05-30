# Testing Instructions – BrickForge

## Purpose

Tests must make BrickForge reliable despite AI variability.

The default test suite must be deterministic and must not depend on external AI APIs or paid services.

## Test Layers

Use these layers:

1. Unit tests
   - fast
   - deterministic
   - no network
   - no real Ollama
   - no persistent local state

2. Integration tests
   - may use temp directories
   - may use test configuration
   - may use a mocked Ollama server
   - may use local database if clearly marked

3. Manual/local tests
   - may require real Ollama
   - should be documented separately

## AI Testing

Do not require a live Ollama instance for normal unit tests.

Use:

- fake `IOllamaClient`
- fixed JSON responses
- malformed JSON responses
- timeout simulation
- cancellation simulation

Test cases:

- valid JSON
- invalid JSON
- schema mismatch
- empty response
- model unavailable
- fallback prompt analysis

## BrickGraph Tests

Test at minimum:

- create empty graph
- add part
- add step
- serialize graph
- deserialize graph
- count parts
- reject unsupported part
- reject invalid colour
- ensure each generated part has a step

Example assertions:

```text
actual_parts equals parts.Count
all part IDs are unique
all step numbers are >= 1
no empty generated model is considered valid
```

## Validation Tests

Each validation rule should have its own test.

Minimum MVP0 rules:

- MaxPartsCheck
- SupportedPartCheck
- AllowedColorCheck
- StepAssignmentCheck
- PositionAssignedCheck
- NonEmptyPartsCheck

MVP1 rules may additionally test:

- CollisionCheck
- FloatingPartCheck
- ConnectedStructureCheck
- ExportSyntaxPreCheck

## Export Tests

### LDraw/MPD

Assert:

- file contains header
- file contains part lines
- file contains STEP markers
- generated file is UTF-8
- empty graph fails safely

### CSV

Assert:

- header exists
- equal parts are aggregated
- colours are part of aggregation key
- output is UTF-8

### Markdown Instructions

Assert:

- title exists
- legal disclaimer exists
- part list exists
- all steps are listed
- no claim of official LEGO origin appears

## File System Tests

Use temporary directories.

Clean up after test execution.

Test path traversal explicitly:

```text
../outside.txt
..\outside.txt
C:\Windows\...
/etc/passwd
```

Generated files must stay below output root.

## CLI Tests

For MVP0 CLI, test:

- `health`
- `generate`
- missing prompt
- invalid config
- Ollama unavailable
- successful output with mock AI

Use exit code assertions.

## API Tests

For MVP1 API, test:

- create generation job
- get job status
- list generated files
- get validation result
- download file
- download path traversal blocked
- unknown job returns 404

## Naming

Use descriptive names:

```csharp
GenerateAsync_WhenPromptIsCoffeeMachine_CreatesExpectedOutputFiles()
Validate_WhenPartHasUnsupportedColor_ReturnsHighSeverityIssue()
Export_WhenBrickGraphIsEmpty_ReturnsFailure()
```

## Fixtures and Builders

Prefer test builders for common objects:

```csharp
BrickGraphBuilder
PartInstanceBuilder
PromptAnalysisResultBuilder
ValidationIssueBuilder
```

Keep test data small.

## Golden Samples

Maintain sample prompts for:

1. Kaffeemaschine
2. kleines Gartenhaus
3. Werkbank
4. kleiner Sportwagen
5. Verkaufsstand

For MVP0, the primary golden sample is:

```text
Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. Das Modell soll einfach und stabil sein.
```

Expected MVP0 files:

```text
brickgraph.json
validation.json
model.mpd
parts.csv
instructions.md
report.md
```

## Test Acceptance

A feature is not complete until:

- relevant tests exist
- tests are deterministic
- expected failures are asserted
- test names explain the behaviour
- no test requires paid/cloud services
