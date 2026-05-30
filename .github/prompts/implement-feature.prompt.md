# Implement Feature Prompt – BrickForge

## Role

You are implementing a feature for BrickForge, a local-first AI-assisted system for generating buildable brick-compatible models and simple building instructions.

Follow the repository instructions in `.github/copilot-instructions.md`.

## Feature Input

Implement the following ticket or feature:

```text
{{FEATURE_DESCRIPTION}}
```

Relevant scope:

```text
{{SCOPE_OR_TICKET_ID}}
```

## Project Constraints

Respect these constraints:

- Default flow must run locally.
- Ollama/local AI is preferred.
- Do not introduce required external AI APIs.
- Do not introduce paid SaaS dependencies.
- Do not execute user prompt content.
- Treat AI output as untrusted data.
- Validate structured AI output before using it.
- Keep BrickGraph as the central internal model.
- Keep export formats open: LDraw/MPD/LDR, CSV, Markdown.
- Do not claim generated output is an official LEGO instruction.

## Expected Approach

Before coding, identify:

1. Which project/module should contain the change.
2. Which existing interfaces or domain models should be reused.
3. Whether this belongs in CLI/API/UI, AI, BrickGraph, Templates, Validation, Export or Infrastructure.
4. Which tests should be added or updated.
5. Which documentation should be updated.

## Implementation Rules

### Domain

Use explicit domain types. Avoid passing loosely typed dictionaries through core logic unless handling raw JSON boundaries.

### AI

If the feature uses AI:

- Use the configured Ollama client abstraction.
- Use structured prompts.
- Request JSON output where data is consumed programmatically.
- Validate JSON against a schema or typed parser.
- Provide deterministic fallback where suitable.
- Do not execute AI-generated code.

### BrickGraph

If the feature changes model generation:

- Ensure every part has an instance ID.
- Ensure every part has part number, name, colour, position, rotation and step.
- Ensure generated model respects the configured part limit.
- Ensure unsupported parts and colours are rejected or replaced intentionally.
- Ensure validation can run after generation.

### Export

If the feature changes output generation:

- Do not mutate the BrickGraph.
- Keep exports deterministic.
- Ensure UTF-8 output.
- Ensure generated files are written only below the configured output directory.
- Include legal/disclaimer text where relevant.

### Security

Validate all input. Prevent path traversal. Do not log secrets. Do not expose internal absolute paths in user-facing output.

## Testing Requirements

Add or update tests for:

- happy path
- invalid input
- boundary values
- configured limits
- expected failures
- serialization/export output if applicable

For AI-related features:

- Add mock-based tests.
- Do not require a live Ollama instance for normal unit tests.
- Integration tests may use live Ollama if clearly marked.

## Output Expected From You

Provide:

1. Code changes.
2. Tests.
3. Any required config changes.
4. Documentation updates.
5. A short summary of behaviour and limitations.

## Acceptance Checklist

The feature is complete only if:

- It builds successfully.
- Tests pass.
- The feature works locally without external AI APIs.
- Generated files stay inside the configured output folder.
- AI outputs are validated.
- Errors are handled with clear messages.
- Documentation is updated where applicable.
