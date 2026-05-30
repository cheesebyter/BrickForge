# Fix Bug Prompt – BrickForge

## Role

You are fixing a defect in BrickForge.

BrickForge is a local-first AI-assisted brick model generation system. Follow `.github/copilot-instructions.md`.

## Bug Description

```text
{{BUG_DESCRIPTION}}
```

Observed behaviour:

```text
{{OBSERVED_BEHAVIOUR}}
```

Expected behaviour:

```text
{{EXPECTED_BEHAVIOUR}}
```

Relevant logs, stack traces or files:

```text
{{RELEVANT_CONTEXT}}
```

## Investigation Rules

Before changing code:

1. Identify the affected module: CLI/API/UI, AI/Ollama, Prompt analysis, Template selection, BrickGraph generation, Validation, Repair, Export, Storage or Security.
2. Reproduce the issue with the smallest possible input.
3. Determine whether the bug is an input validation issue, configuration issue, AI output parsing issue, deterministic logic issue, file/path issue, export formatting issue or test data issue.
4. Add or update a failing test before fixing where practical.

## Project Constraints

Do not fix bugs by:

- bypassing validation
- accepting arbitrary AI output
- executing AI-generated code
- disabling security checks
- hardcoding local machine paths
- adding required cloud services
- hiding errors without reporting them
- broadening file access outside the job output folder

## Common Bug Areas

### Ollama / AI

Check:

- Is Ollama reachable?
- Is the configured model available?
- Is timeout handled?
- Is the response valid JSON?
- Is fallback analysis triggered correctly?
- Is the error user-readable?

### BrickGraph

Check:

- Are all parts assigned a step?
- Are positions and rotations valid?
- Is `actual_parts` correct?
- Are supported part IDs used?
- Is the model empty?
- Does it exceed max parts?

### Validation

Check:

- Are severity levels correct?
- Does high severity invalidate the model?
- Are issues reported with enough detail?
- Are false positives caused by an overly strict rule?

### Export

Check:

- Is the output directory correct?
- Are files UTF-8 encoded?
- Is CSV aggregation correct?
- Are LDraw lines formatted correctly?
- Are STEP markers written?
- Is Markdown readable?

### Security

Check:

- Is path traversal prevented?
- Are user inputs sanitized?
- Are secrets omitted from logs?
- Are user-facing errors safe?

## Testing Requirements

Add or update tests that prove:

- the bug existed
- the fix works
- the edge case remains covered
- no validation/security rule was weakened unintentionally

For AI bugs, use mocked responses where possible.

## Output Expected From You

Provide:

1. Root cause summary.
2. Code fix.
3. Regression test.
4. Any config/doc updates.
5. Remaining limitations, if any.

## Acceptance Checklist

The bug fix is complete only if:

- The issue is reproducible before the fix or clearly explained.
- A regression test exists where practical.
- The system still runs locally.
- No required external service was introduced.
- Validation and security constraints remain intact.
- Error messages are clear and not misleading.
