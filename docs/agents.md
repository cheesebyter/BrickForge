# BrickForge – Agenten-Übersicht

Dieses Dokument beschreibt die Agenten der BrickForge-Generierungspipeline.

---

## Architekturprinzip

BrickForge verwendet eine sequentielle Agentenpipeline. Jeder Agent hat eine klar abgegrenzte Verantwortung. Die Pipeline wird durch den `GenerationPipelineService` orchestriert.

```
Prompt
  └─► PromptAnalysisAgent
        └─► TemplateSelectionAgent
              └─► BrickGraphGeneratorAgent
                    └─► ValidationAgent
                          └─► RepairAgent (optional)
                                └─► ExportAgents
```

---

## Agenten im Detail

### PromptAnalysisAgent

**Klasse:** `BrickForge.Ai.Analysis.PromptAnalyzer` (implementiert `IPromptAnalyzer`)

**Aufgabe:** Analysiert den Nutzer-Prompt und extrahiert strukturierte Informationen für die Generierung.

**Ausgabe (`PromptAnalysisResult`):**
- `model_name` – Name des zu erzeugenden Modells
- `model_category` – Kategorie (z. B. `small_machine`)
- `target_parts` – Anzahl angestrebter Teile
- `main_color` – Hauptfarbe
- `accent_color` – Akzentfarbe
- `features` – Optionale Merkmale
- `feasible` – Ob der Prompt umsetzbar ist
- `used_fallback` – Ob der Fallback-Parser verwendet wurde

**Fallback:** Bei Ollama-Ausfall oder ungültigem JSON wird ein deterministischer Fallback-Parser verwendet.

**KI:** Ollama (lokal). Kein externer API-Aufruf.

---

### TemplateSelectionAgent

**Klasse:** `BrickForge.BrickGraph.Templates.TemplateRegistry`

**Aufgabe:** Wählt anhand der Analysekategorie ein passendes Modell-Template aus.

**MVP0/MVP1:** Unterstützt derzeit `small_machine`. Weitere Templates können registriert werden.

**Fallback:** Fällt auf `small_machine` zurück, wenn keine passende Kategorie gefunden wird.

**KI:** Keine.

---

### BrickGraphGeneratorAgent

**Klasse:** `BrickForge.BrickGraph.Generation.TemplateBasedGenerator`

**Aufgabe:** Erzeugt einen `BrickGraph` aus `PromptAnalysisResult` und `ModelTemplate`.

**Vorgehen:**
1. Basis (Bodenplatte) erstellen
2. Rechteckigen Hauptkörper aufbauen
3. Frontpanel einfügen
4. Dachschicht anlegen
5. Optionale Details aus `features` hinzufügen

**Determinismus:** Vollständig deterministisch. Kein Zufallsgenerator, keine KI.

---

### ValidationAgent

**Klasse:** `BrickForge.BrickGraph.Validation.BrickGraphValidator`

**Aufgabe:** Prüft den generierten `BrickGraph` anhand regelbasierter Checks.

**Pflichtchecks:**
| Check | Schweregrad |
|-------|------------|
| `MaxPartsCheck` | High |
| `SupportedPartCheck` | High |
| `AllowedColorCheck` | High |
| `StepAssignmentCheck` | High |
| `PositionAssignedCheck` | High |
| `NonEmptyPartsCheck` | High |
| `CollisionCheck` | Medium |
| `FloatingPartCheck` | Medium |
| `ConnectedStructureCheck` | Medium |

**Ausgabe:** `ValidationResult` mit Score (0.0–1.0) und Issue-Liste.

**KI:** Keine.

---

### RepairAgent

**Klasse:** `BrickForge.BrickGraph.Repair.BrickGraphRepairAgent`

**Aufgabe:** Versucht einen ungültigen `BrickGraph` zu reparieren, wenn Validierung fehlschlägt.

**Strategien:**
- Überzählige Teile entfernen
- Ungültige Farben durch Standardfarbe ersetzen
- Fehlende Step-Zuweisung korrigieren
- Ungültige Positionen auf Gitter setzen

**Verhalten:** Deterministisch. Kein LLM-Aufruf. Max. 1 Reparaturrunde pro Job.

---

### ExportAgents

Jeder Exporter implementiert ein eigenes Ausgabeformat:

| Agent | Ausgabe | Klasse |
|-------|---------|--------|
| `LDrawExporter` | `model.mpd` | `BrickForge.Export.LDrawExporter` |
| `CsvPartsExporter` | `parts.csv` | `BrickForge.Export.CsvPartsExporter` |
| `MarkdownInstructionsExporter` | `instructions.md` | `BrickForge.Export.MarkdownInstructionsExporter` |
| `HtmlInstructionsExporter` | `instructions.html` | `BrickForge.Export.HtmlInstructionsExporter` |
| `ReportExporter` | `report.md` | `BrickForge.Export.ReportExporter` |
| `GenerationJsonExporter` | `generation.json` | `BrickForge.Export.GenerationJsonExporter` |

**Regeln:**
- Exporters mutieren den `BrickGraph` nicht.
- Leere Graphen werden sicher behandelt (kein Absturz, Fehlerresultat).
- Ausgabedateien enthalten immer einen rechtlichen Hinweis.

---

## Metriken (BF-MVP1-044)

Der `GenerationPipelineService` erfasst pro Agent:
- `StartTime` / `EndTime` / `DurationMs`
- `LlmCalls` (Anzahl KI-Aufrufe)
- `Retries`
- `Success`
- `Confidence` / `FinalScore`

Die Metriken werden in `report.md` und `JobMetrics` (als JSON in der DB) gespeichert.

---

## Neue Agenten hinzufügen

1. Interface in `BrickForge.Core.Agents` definieren (falls nötig).
2. Implementierung in das zuständige Projekt (`BrickForge.Ai`, `BrickForge.BrickGraph`, `BrickForge.Export`).
3. Im `GenerationPipelineService` integrieren.
4. `AgentMetrics`-Eintrag in der Pipline ergänzen.
5. Unit-Tests mit Fake-Abhängigkeiten erstellen.
