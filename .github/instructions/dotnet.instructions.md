# .NET Instructions – BrickForge

## General

Use clear, maintainable C# for BrickForge.

Preferred principles:

- explicit domain models
- dependency injection
- async I/O
- typed options
- testable services
- small focused classes
- deterministic core logic

## Project Structure

Keep concerns separated:

```text
BrickForge.Cli              CLI entry point for MVP0
BrickForge.Api              API entry point for MVP1
BrickForge.Core             shared domain abstractions
BrickForge.Ai               Ollama client and AI prompt handling
BrickForge.BrickGraph       BrickGraph model and generation
BrickForge.Validation       rule-based validation
BrickForge.Export           LDraw/CSV/Markdown export
BrickForge.Templates        model templates
BrickForge.Infrastructure   persistence, file storage, logging
```

MVP0 may use a reduced subset, but avoid creating monolithic code that blocks MVP1.

## Dependency Direction

Recommended dependency direction:

```text
CLI/API
  -> Application/Core services
  -> AI / BrickGraph / Validation / Export / Infrastructure
```

Avoid circular dependencies.

Core domain types should not depend on infrastructure.

## Options and Configuration

Use typed options classes:

```csharp
GenerationOptions
OllamaOptions
ExportOptions
StorageOptions
ValidationOptions
```

Validate options at startup where possible.

Do not hardcode:

- Ollama URL
- model name
- output root
- max parts
- timeout values

## Error Handling

Use clear result types for expected domain failures.

Examples of expected failures:

- invalid prompt
- Ollama unavailable
- invalid AI JSON
- unsupported part
- invalid BrickGraph
- export failure due to invalid model

Unexpected failures may throw, but should be logged and converted into safe CLI/API errors.

## Async Guidance

Use async for:

- HTTP calls
- file I/O
- database calls
- long-running generation workflows

Pass `CancellationToken` through public async APIs.

Do not block async code with `.Result` or `.Wait()`.

## File System Guidance

All generated output must stay under the configured output root.

Always normalize and validate paths.

Do not use user prompt content directly as a file or directory name. Use generated job IDs and sanitized model slugs only.

## HTTP Client Guidance

For Ollama:

- use `HttpClientFactory` where applicable
- configure timeout
- handle non-success status codes
- support cancellation
- separate request/response DTOs
- log failures without leaking sensitive data

## Domain Naming

Prefer names such as:

```csharp
BrickGraph
BrickPartInstance
BrickStep
BrickConnection
PromptAnalysisResult
GenerationRequest
GenerationResult
ValidationResult
ValidationIssue
GeneratedFile
```

Avoid ambiguous names like `Data`, `Info`, `Manager`, `Helper` unless justified.

## Serialization

Use System.Text.Json unless the project already uses another serializer.

Prefer explicit DTOs for JSON boundaries.

For AI JSON, parse defensively and validate before use.

## Logging

Include relevant context:

- JobId / RunId
- AgentName if applicable
- current workflow state
- duration
- validation score
- file type being exported

Do not log:

- secrets
- full local paths in user-facing logs
- excessive raw AI output unless debug mode explicitly enables it

## CLI Guidance

For MVP0 CLI:

- provide `generate` command
- provide `health` command
- return non-zero exit code on failure
- print output directory on success
- keep error messages short and actionable

## API Guidance

For MVP1 API:

Use endpoints aligned with the Pflichtenheft:

```http
POST /api/generation-jobs
GET  /api/generation-jobs/{id}
GET  /api/generation-jobs/{id}/files
GET  /api/generation-jobs/{id}/validation
GET  /api/generation-jobs/{id}/download
```

API errors should be structured and should not expose stack traces.

## Formatting

Follow standard C# formatting.

Prefer:

- file-scoped namespaces if used consistently
- nullable reference types
- `record` or `record class` for immutable DTOs
- explicit access modifiers
- meaningful XML comments only for public APIs that need them

## Avoid

Avoid:

- large static service locators
- global mutable state
- AI calls from constructors
- file writes from domain objects
- validation bypasses
- cloud dependencies in default flow
