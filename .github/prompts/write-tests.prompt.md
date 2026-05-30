# Write Tests Prompt – BrickForge

## Role

You are adding or improving tests for BrickForge.

BrickForge is a local-first AI-assisted system for generating brick-compatible models and instructions.

Follow `.github/copilot-instructions.md` and `.github/instructions/testing.instructions.md`.

## Test Target

Create or update tests for:

```text
{{TEST_TARGET}}
```

Relevant ticket or feature:

```text
{{TICKET_OR_FEATURE}}
```

## Testing Goals

Cover:

1. Happy path.
2. Invalid input.
3. Boundary values.
4. Configured limits.
5. Error handling.
6. Serialization/export behaviour where applicable.
7. Security-relevant cases where applicable.

## Project-Specific Test Areas

### Configuration

Test default values, invalid values, missing sections, output root handling and max part limits.

### Ollama / AI

Use mocks for normal tests.

Test:

- successful text response
- timeout
- invalid JSON
- schema mismatch
- fallback analysis
- unavailable Ollama
- cancellation token behaviour where possible

Do not require a live Ollama instance for normal unit tests.

### Prompt Analysis

Test:

- coffee machine prompt
- small building prompt
- vehicle prompt
- unknown prompt
- target part cap
- default colours
- infeasible request

### BrickGraph

Test:

- empty model invalidity
- part insertion
- step assignment
- serialization/deserialization
- actual part count
- supported parts only

### Validation

Test:

- max parts exceeded
- unsupported part
- unsupported colour
- missing step
- empty parts list
- valid minimal model

### Export

Test:

- MPD header generation
- MPD part line generation
- STEP command generation
- CSV aggregation
- Markdown instruction structure
- legal disclaimer presence
- output path safety

### Security

Test:

- path traversal attempts
- malicious prompt text is not executed
- invalid output file names
- oversized prompts
- unsafe absolute paths

## Test Design Rules

- Prefer deterministic tests.
- Avoid real time delays.
- Avoid network calls in unit tests.
- Use mocks/fakes for AI and file system where practical.
- Integration tests may touch the file system in temporary directories.
- Use clear test names describing expected behaviour.

Example naming style:

```csharp
GenerateAsync_WhenPromptIsCoffeeMachine_CreatesValidBrickGraph()
```

## Expected Output

Provide:

1. Test files/classes.
2. Any required test helpers or builders.
3. Mock/fake implementations where useful.
4. Minimal changes to production code only if required for testability.
5. Explanation of coverage.

## Acceptance Checklist

Tests are complete when:

- They run locally.
- They do not require external AI APIs.
- Unit tests do not require live Ollama.
- Temporary files are cleaned up.
- Failing cases assert meaningful errors.
- Security-relevant behaviour is covered where applicable.
