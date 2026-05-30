# BrickForge – Sample Prompts

This directory contains sample prompts for manual and integration testing.

## kaffeemaschine.txt

**Primary golden sample for MVP0.**

Prompt:
```
Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. Das Modell soll einfach und stabil sein.
```

Expected analysis output:
- model_category: `small_machine`
- main_color: `black`
- accent_color: `light_bluish_gray`
- target_parts: ~50
- feasible: `true`

Expected output files (in `data/outputs/{jobId}/`):
- `brickgraph.json`
- `validation.json`
- `model.mpd`
- `parts.csv`
- `instructions.md`
- `report.md`

## Running the sample

```bash
# With real Ollama
dotnet run --project src/BrickForge.Cli -- generate "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse."

# With mock mode (no Ollama required)
# Set Ollama:MockMode=true in appsettings.json, then:
dotnet run --project src/BrickForge.Cli -- generate "Kaffeemaschine"
```
