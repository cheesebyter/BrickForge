# GitHub Copilot Instructions – BrickForge

## Project Context

BrickForge is a local-first AI-assisted system for generating buildable brick-compatible models and simple building instructions.

Current scope:

- MVP0: technical proof of concept with CLI generation, local Ollama integration, minimal prompt analysis, one `small_machine` template, BrickGraph generation, minimal validation, LDraw/MPD export, CSV parts list and Markdown instructions.
- MVP1: agent-based architecture with Orchestrator, PromptAnalysisAgent, TemplateSelectionAgent, BrickPlanAgent, BrickGraphGeneratorAgent, ValidationAgent, RepairAgent, ExportAgentSuite, API/UI/monitoring/security/tests.

Primary generation chain:

```text
Prompt
  -> local AI analysis
  -> template / plan
  -> BrickGraph
  -> validation
  -> LDraw/MPD/LDR export
  -> CSV parts list
  -> Markdown instructions
```

## Core Principles

1. **Local-first AI**
   - Prefer Ollama and local models.
   - Do not introduce required external AI APIs.
   - External AI must be optional, disabled by default and explicitly configurable.

2. **Low operating cost**
   - Avoid paid SaaS dependencies.
   - Prefer local files, local database and local processing.
   - Avoid cloud-only architecture.

3. **Low licence dependency**
   - Prefer open formats and open tooling.
   - Use LDraw/MPD/LDR as the primary model export format.
   - Do not require BrickLink Studio for core functionality.
   - Keep LPub3D optional.

4. **Buildability before appearance**
   - A simple buildable model is better than an attractive invalid model.
   - AI may plan and suggest, but deterministic validation decides.

5. **Structured outputs**
   - LLM outputs must be JSON where used by program logic.
   - Validate all AI responses against schemas or strict DTO parsers.
   - Never trust raw AI text as executable logic.

6. **Security**
   - Do not execute code, commands, scripts, paths or file operations suggested by a prompt.
   - Treat prompts and AI responses as untrusted input.
   - Prevent path traversal in output/download logic.
   - Do not log secrets.

## Preferred Technology

Use the project’s intended stack unless explicitly asked otherwise:

- Language: C#
- Runtime: modern .NET
- MVP0 interface: CLI
- MVP1 interface: ASP.NET Core API and optional Blazor/WPF UI
- AI: Ollama HTTP API on Windows
- Storage MVP0: local file system
- Storage MVP1: PostgreSQL via Docker plus local output storage
- Export: LDraw/MPD/LDR, CSV, Markdown
- Tests: xUnit or NUnit; keep consistent with existing project setup

## Architecture Guidelines

Keep responsibilities separated.

Suggested projects:

```text
BrickForge.Cli
BrickForge.Api
BrickForge.Core
BrickForge.Ai
BrickForge.BrickGraph
BrickForge.Validation
BrickForge.Export
BrickForge.Templates
BrickForge.Infrastructure
```

For MVP0, keep the minimum useful subset:

```text
BrickForge.Cli
BrickForge.Core
BrickForge.Ai
BrickForge.BrickGraph
BrickForge.Export
```

Do not put AI calls, export logic, validation logic and CLI/API code into the same class.

## Dependency Direction

Recommended dependency direction:

```text
CLI/API
  -> Application/Core services
  -> AI / BrickGraph / Validation / Export / Infrastructure
```

Avoid circular dependencies. Core domain types should not depend on infrastructure.

## Domain Model Guidance

The central internal model is `BrickGraph`.

A BrickGraph should contain:

- model metadata
- part instances
- positions
- rotations
- colours
- steps
- optional connections
- validation metadata

Prefer explicit domain objects:

```csharp
BrickGraph
BrickModelMetadata
BrickPartInstance
BrickStep
BrickConnection
PromptAnalysisResult
GenerationJob
GeneratedFile
ValidationResult
ValidationIssue
```

Avoid passing anonymous dictionaries through core logic.

## AI Integration Guidance

When implementing AI-related features:

- Use an `IOllamaClient` abstraction.
- Keep model name, base URL, timeout and temperature configurable.
- Add health checks for Ollama availability.
- Use cancellation tokens.
- Use retry only where safe.
- Validate JSON output before returning domain objects.
- Provide deterministic fallback behaviour where possible.

Do not let AI:

- choose arbitrary file paths
- execute commands
- generate code that is executed
- bypass validation
- write directly to output files without validation

## Prompt Analysis Guidance

Prompt analysis should extract a structure similar to:

```json
{
  "model_name": "Simple Coffee Machine",
  "model_category": "small_machine",
  "target_parts": 50,
  "main_color": "black",
  "accent_color": "light_bluish_gray",
  "features": ["cup", "front_panel"],
  "feasible": true
}
```

Rules:

- Cap `target_parts` at the configured maximum.
- Use defaults for missing values.
- Mark impossible requests as infeasible.
- Prefer deterministic fallback parsing if the LLM fails.

## Template Guidance

For MVP0, implement only the `small_machine` template unless a ticket explicitly asks for more.

For MVP1, templates should be data-driven where possible:

```text
small_machine_template
small_building_template
small_vehicle_template
furniture_template
display_object_template
```

Templates should define:

- default dimensions
- supported part families
- subassemblies
- budget distribution
- validation assumptions

## BrickGraph Generation Guidance

Generation should be deterministic where possible.

For MVP0:

- create a base
- create a rectangular main body
- create a front panel
- create a top layer
- add one or two simple details if possible

Every part must have:

- instance ID
- part number
- part name
- colour
- position
- rotation
- step

Avoid overengineering geometric realism in MVP0.

## Validation Guidance

Validation must be rule-based.

MVP0 minimum checks:

- part count <= max
- all parts supported
- all colours allowed
- every part has a valid step
- positions are set
- parts list is not empty

MVP1 validation may include:

- collision checks
- floating part checks
- connected structure checks
- connection density
- export syntax pre-check
- high/medium/low severity issues

High-severity issues should make the model invalid.

## Export Guidance

Exporters must not mutate the BrickGraph.

Required MVP0 exports:

```text
model.mpd
parts.csv
instructions.md
brickgraph.json
validation.json
```

Optional:

```text
report.md
```

LDraw export should include:

- header
- generated-by notice
- legal disclaimer where appropriate
- `0 STEP` commands
- one line per part instance

Markdown instructions must clearly state that the output is not an official LEGO instruction.

## Security Rules

Never implement features that:

- execute user prompt content
- use prompt content as a raw file path
- allow downloads outside the job output folder
- expose local machine paths in user-facing output
- require cloud services for the default flow
- silently send prompts to external services

Always validate:

- prompt length
- file paths
- output file names
- JSON from AI
- generated model data before export

## Testing Guidance

Use tests for:

- configuration loading
- Ollama client error handling
- prompt analysis parsing
- fallback prompt analysis
- BrickGraph serialization
- supported parts lookup
- validation rules
- CSV aggregation
- LDraw export line generation
- Markdown instruction generation
- full MVP0 end-to-end flow

For AI-dependent tests, provide a mock mode. CI/local automated tests must not require a running Ollama instance unless explicitly marked as integration tests.

## Code Style

- Prefer clear domain names over abbreviations.
- Use nullable reference types where possible.
- Use async APIs for I/O and HTTP.
- Accept `CancellationToken` in long-running operations.
- Keep classes small and focused.
- Avoid static global state except for immutable constants.
- Avoid magic strings; use constants or enums.
- Return typed results rather than throwing for expected validation failures.

## Documentation Expectations

When adding or changing a feature, update relevant docs:

```text
docs/ai/prompt-template.md
docs/mvp0-setup.md
docs/architecture.md
docs/agents.md
docs/legal-notes.md
```

Do not add claims that the generated output is official LEGO content.
