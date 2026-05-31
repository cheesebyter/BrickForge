# BrickForge – Systemarchitektur

> Stand: MVP1 Phase 8 (Stabilisierung)

## Überblick

BrickForge ist ein lokal-first System zur KI-gestützten Generierung von Brick-Modellen.
Es erzeugt baubare, validierte Modelle und exportiert sie als LDraw/MPD, CSV-Teileliste und Markdown-Bauanleitung.

---

## Projektstruktur

```
BrickForge/
├── src/
│   ├── BrickForge.Cli            CLI-Einstiegspunkt (MVP0)
│   ├── BrickForge.Api            ASP.NET Core API (MVP1)
│   ├── BrickForge.Core           Gemeinsame Domaintypen, Optionen, Ergebnistypen
│   ├── BrickForge.Ai             Ollama-Client, Prompt-Analyse, Mock-Modus
│   ├── BrickForge.BrickGraph     BrickGraph-Modell, Generatoren, Validierung
│   ├── BrickForge.Export         LDraw/MPD, CSV, Markdown, Report, JSON-Exporter
│   └── BrickForge.Infrastructure SQLite-Repositories (Jobs, Katalog)
└── tests/
    ├── BrickForge.Core.Tests
    ├── BrickForge.Ai.Tests
    ├── BrickForge.BrickGraph.Tests
    ├── BrickForge.Export.Tests
    ├── BrickForge.Api.Tests
    └── BrickForge.Integration.Tests
```

---

## Generierungspipeline

```
Prompt (Benutzer)
  → PromptAnalysisService (Ollama / Fallback)
  → TemplateRegistry.FindTemplate()
  → SmallMachineGenerator / TemplateBasedGenerator
  → BrickGraph (internes Domainmodell)
  → BrickGraphValidator
  → [BrickGraphRepairAgent bei Reparaturversuch]
  → LDrawExporter → model.mpd
  → CsvPartsExporter → parts.csv
  → MarkdownInstructionsExporter → instructions.md
  → ReportExporter → report.md
  → GenerationJsonExporter → brickgraph.json, validation.json
```

---

## Abhängigkeitsrichtung

```
CLI / API
  → Application-Services (GenerationPipelineService)
  → Ai / BrickGraph / Validation / Export / Infrastructure
  → Core (keine Abhängigkeit nach oben)
```

Core-Domaintypen dürfen nicht von Infrastructure oder AI abhängen.

---

## KI-Integration

- **Standard:** Lokales Ollama über HTTP (konfigurierbare URL, Modell, Timeout)
- **Mock-Modus:** `OllamaOptions.MockMode = true` oder `MockOllamaClient` für Tests
- **Fallback:** `FallbackPromptAnalyzer` liefert deterministische Standardausgabe bei Fehler
- **Sicherheit:** AI-Ausgaben werden als untrusted data behandelt und gegen ein JSON-Schema validiert

---

## BrickGraph – Zentrales Domainmodell

```
BrickGraph
├── Model: BrickModelMetadata (ID, Name, TargetParts, ActualParts)
├── Parts: List<BrickPartInstance> (InstanceId, PartNumber, Color, Position, Rotation, Step)
└── Steps: List<BrickStep> (StepNumber, Label, PartInstanceIds)
```

Jeder Teil muss haben: InstanceId, PartNumber, Color, Position (3 Werte), Rotation (9 Werte), Step ≥ 1.

---

## Validierungsregeln (MVP1)

| Regel | Schweregrad |
|---|---|
| Leere Teileliste | High |
| Teileanzahl > MaxParts | High |
| Nicht unterstützter Teil | High |
| Nicht erlaubte Farbe | High |
| Step = 0 | High |
| Kollision (zwei Teile an gleicher Position) | High |
| Position fehlt / falsch | Medium |
| Schwebende Teile (kein Teil auf Y=0) | Medium |
| Schritte nicht monoton | Medium |
| Kein Teil in Step 1 | Medium |

---

## Exportformate (MVP0/MVP1)

| Datei | Format | Pflichtfeld |
|---|---|---|
| `model.mpd` | LDraw/MPD | Ja |
| `parts.csv` | CSV | Ja |
| `instructions.md` | Markdown | Ja |
| `report.md` | Markdown | Ja |
| `brickgraph.json` | JSON | Ja |
| `validation.json` | JSON | Ja |

Alle Exporte enthalten folgenden Disclaimer:
> Dieses Dokument wurde automatisch durch BrickForge erzeugt.
> Es handelt sich nicht um eine offizielle LEGO-Bauanleitung und nicht um ein von LEGO geprüftes Modell.

---

## Sicherheitsregeln

- Keine Ausführung von Prompt-Inhalt oder AI-Ausgaben
- Ausgabedateien nur unter `data/outputs/{jobId}/`
- Pfadtraversal wird blockiert (`../`, `..\`, absolute Pfade)
- Keine lokalen Maschinenpfade in user-facing Output
- Keine externen AI-APIs im Standard-Modus

---

## API-Endpunkte (MVP1)

```
POST   /api/generation-jobs                      Neuen Job erstellen
GET    /api/generation-jobs/{id}                 Job-Status abfragen
GET    /api/generation-jobs/{id}/files           Erzeugte Dateien auflisten
GET    /api/generation-jobs/{id}/validation      Validierungsergebnis abrufen
GET    /api/generation-jobs/{id}/download        Datei herunterladen
```

---

## Entwicklungsphasen (BF-MVP1-029)

| Phase | Bezeichnung | Status |
|---|---|---|
| 1 | Grundstruktur & Abhängigkeiten | ✅ Abgeschlossen |
| 2 | Datenhaltung (SQLite) | ✅ Abgeschlossen |
| 3 | API-Endpunkte (Jobs, Files, Validation, Download) | ✅ Abgeschlossen |
| 4 | Generierungspipeline (Prompt → BrickGraph → Export) | ✅ Abgeschlossen |
| 5 | Validierung (regelbasiert, Repair) | ✅ Abgeschlossen |
| 6 | Agenten-Architektur (PromptAnalysis, TemplateSelection) | ✅ Abgeschlossen |
| 7 | Konfiguration, Logging, Health Checks | ✅ Abgeschlossen |
| 8 | Stabilisierung: Tests, Golden Samples, Dokumentation | ✅ Abgeschlossen |

---

## Golden Samples (BF-MVP1-028 §28.3)

Die folgenden fünf Referenzprompts sind durch automatisierte Integrationstests abgedeckt
(`tests/BrickForge.Integration.Tests/FullGenerationPipelineTests.cs`):

1. **Kaffeemaschine** – kleine schwarze Kaffeemaschine mit silbernem Frontpanel
2. **Gartenhaus** – kleines rotes Gartenhaus
3. **Werkbank** – graue Werkbank
4. **Sportwagen** – kleiner blauer Sportwagen
5. **Verkaufsstand** – gelber Verkaufsstand

Der **Siebträger-Akzeptanztest** (§27.3) befindet sich in
`tests/BrickForge.Integration.Tests/Mvp1AcceptanceTests.cs`.

---

## Demo-Szenarien (BF-MVP1-029 §29 Phase 8)

### Szenario 1 – Standard-Generierung (Kaffeemaschine)

```bash
dotnet run --project src/BrickForge.Cli -- generate \
  --prompt "Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel."
```

Erwartetes Ergebnis: `data/outputs/{jobId}/` mit 6 Dateien, Validierung gültig.

### Szenario 2 – Ollama nicht verfügbar (Fallback)

Ollama beenden, dann:

```bash
dotnet run --project src/BrickForge.Cli -- generate \
  --prompt "Erstelle eine Werkbank."
```

Erwartetes Ergebnis: Fallback-Analyse greift, Pipeline läuft durch oder bricht sauber ab.

### Szenario 3 – API-Nutzung (MVP1)

```bash
curl -X POST http://localhost:5000/api/generation-jobs \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Erstelle einen kleinen Sportwagen."}'
# Response: {"id": "abc123", ...}

curl http://localhost:5000/api/generation-jobs/abc123
# Response: {"id": "abc123", "status": "Completed", ...}

curl http://localhost:5000/api/generation-jobs/abc123/download?file=model.mpd \
  --output model.mpd
```

### Szenario 4 – Health Check

```bash
dotnet run --project src/BrickForge.Cli -- health
# oder
curl http://localhost:5000/health
```

---

## Technische Entscheidungen

| Thema | Entscheidung | Begründung |
|---|---|---|
| KI-Backend | Ollama lokal | Lokal-first, keine Kosten, keine Cloud-Abhängigkeit |
| Datenhaltung | SQLite (MVP1) | Einfach, kein Docker für Entwicklung nötig |
| Export-Format | LDraw/MPD | Offenes Format, kompatibel mit BrickLink Studio / LPub3D |
| BrickGraph | Eigenes Domainmodell | Unabhängig von externen Bibliotheken |
| Validierung | Regelbasiert | Deterministisch, testbar, kein AI-Vertrauen |
| Tests | xUnit, kein live Ollama | CI-fähig, deterministisch |

---

## Qualitätsdefinition MVP 1 (BF-MVP1-033)

Ein generiertes Modell gilt als MVP-tauglich (`MvpQualityChecker.Check` gibt `IsAcceptable == true`), wenn:

| Kriterium | Code | Prüfung |
|---|---|---|
| Nur unterstützte Teile und Farben | `UNSUPPORTED_PARTS` | Keine `UNSUPPORTED_PART`- oder `UNSUPPORTED_COLOR`-Issues |
| Keine kritischen Validierungsfehler | `VALIDATION_FAILED` | `ValidationResult.Valid == true` (keine High-Severity-Issues) |
| LDraw/MPD-Export vorhanden | `MISSING_MPD` | `model.mpd` in erzeugten Dateien |
| Teileliste vorhanden | `MISSING_PARTS_CSV` | `parts.csv` in erzeugten Dateien |
| Bauanleitung vorhanden | `MISSING_INSTRUCTIONS` | `instructions.md` in erzeugten Dateien |
| BrickGraph-Datei vorhanden | `MISSING_BRICKGRAPH` | `brickgraph.json` in erzeugten Dateien |
| Keine externen AI-Kosten | (Konfigurationspolitik) | Lokales Ollama als Standard erzwungen |

Ein Modell muss im MVP **nicht**:
- optisch perfekt sein
- professionell gerendert sein
- vollständig physikalisch simuliert sein
- mit offiziellen LEGO-Anleitungen vergleichbar sein
- alle Details des Prompts exakt erfüllen

---

## Erweiterungspfad nach MVP 1 (BF-MVP1-034)

| MVP | Bezeichnung | Kernfunktion |
|---|---|---|
| **MVP 2** | Bestehendes Modell modifizieren | LDraw/MPD-Import, Baugruppen erkennen, Änderungen per Prompt, Teile-Differenzliste |
| **MVP 3** | Bild zu stilisiertem Gebäude | Bildanalyse, Gebäudefassade extrahieren, vereinfachtes Brick-Modell |
| **MVP 4** | 3D-Modell zu Brick-Modell | Mesh-Import, Voxelisierung, Brickification, Validierung |
| **MVP 5** | Premium-Anleitungen | LPub3D-Pipeline, Render je Schritt, Callouts, Submodelle, professionelle PDFs |

Architektonische Vorbereitung in MVP 1:
- `IGenerationPipelineService` ist erweiterbar für neue Eingabequellen
- `BrickGraphValidator` ist durch neue Regelklassen erweiterbar
- Export-Pipeline ist zustandslos und kann für neue Formate erweitert werden

---

## Offene Entscheidungen (BF-MVP1-035)

Die folgenden Entscheidungen wurden in MVP 1 getroffen:

| Frage | Entscheidung | Status |
|---|---|---|
| WPF oder Blazor als Frontend? | Blazor (optional) – MVP1 API-first, kein UI-Pflicht | ✅ Entschieden |
| C#-only oder Python-Service? | C#-only | ✅ Entschieden |
| SQLite oder PostgreSQL? | SQLite für MVP1 (PostgreSQL-Vorbereitung in Architektur) | ✅ Entschieden |
| PDF direkt oder Markdown/HTML? | Markdown als primäres Ausgabeformat; PDF optional | ✅ Entschieden |
| Welche 50–100 Teile initial? | ~50 LDraw-Grundteile in `supported-parts.json` | ✅ Entschieden |
| Welche lokalen AI-Modelle? | Konfigurierbar via `OllamaOptions.ModelName`; kein Standard-Modell fixiert | ✅ Entschieden |
| LPub3D nur empfohlen oder automatisiert? | Nur empfohlen; keine automatische Anbindung in MVP1 | ✅ Entschieden |
| Wie streng soll erste Validierung sein? | High-Severity-Issues blockieren Export; Medium erlaubt | ✅ Entschieden |

---

## Empfehlung zur Umsetzung (BF-MVP1-036)

Die empfohlene und in MVP 1 umgesetzte Stack-Konfiguration:

```
Frontend:      Blazor Web App lokal (optional; MVP1 läuft ohne UI)
Backend:       ASP.NET Core API (BrickForge.Api)
AI:            Ollama lokal – Windows, RTX 4090
Kern:          C# BrickGraph Engine (BrickForge.BrickGraph)
Daten:         SQLite + lokales Dateisystem (data/outputs/{jobId}/)
Teile:         Reduzierte lokale LDraw-basierte Supported-Parts-Liste
Export:        LDraw/MPD/LDR + CSV + Markdown
PDF:           Optional über HTML-Konvertierung oder später LPub3D
```

Diese Konfiguration erfüllt alle MVP-Grundsätze:
- ✅ Lokale AI (kein Cloud-Zwang)
- ✅ Keine laufenden Betriebskosten
- ✅ Geringe Lizenzabhängigkeit (offene Formate)
- ✅ Offene Exportformate (LDraw, CSV, Markdown)
- ✅ Kontrollierbare MVP-Komplexität
- ✅ Erweiterbar für Bild-/3D-/Modifikationsfunktionen (MVP 2–5)

---

## Technische Kette – Nachweis (BF-MVP1-038)

MVP 1 beweist, dass die zentrale technische Kette vollständig funktioniert.

```text
Textbeschreibung
  -> strukturierter Modellplan   (PromptAnalysisResult)
  -> BrickGraph                  (BrickGraph.Parts / BrickGraph.Steps)
  -> Validierung                 (ValidationResult)
  -> LDraw/MPD/LDR               (model.mpd)
  -> Teileliste                  (parts.csv)
  -> einfache Bauanleitung       (instructions.md)
```

Jeder Schritt ist durch `TechnicalChainVerificationTests` (in `BrickForge.Integration.Tests`)
einzeln und im Verbund automatisiert verifiziert.

| Schritt | Verantwortliche Klasse            | Testklasse                          |
|---------|-----------------------------------|-------------------------------------|
| 1       | `PromptAnalysisService`           | `TechnicalChainVerificationTests`   |
| 2       | `SmallMachineGenerator`           | `TechnicalChainVerificationTests`   |
| 3       | `BrickGraphValidator`             | `TechnicalChainVerificationTests`   |
| 4       | `LDrawExporter`                   | `TechnicalChainVerificationTests`   |
| 5       | `CsvPartsExporter`                | `TechnicalChainVerificationTests`   |
| 6       | `MarkdownInstructionsExporter`    | `TechnicalChainVerificationTests`   |

Damit entsteht die notwendige Basis für spätere, deutlich anspruchsvollere Funktionen
wie reale Objektanalyse, 3D-Scan-Verarbeitung, bestehende Modellmodifikation und
Premium-Bauanleitungen.

---

## Quellenhinweise (BF-MVP1-037)

Die vollständige Liste der verwendeten externen Quellen und Ressourcen befindet sich in:

**[`docs/references.md`](references.md)**

Kurzübersicht:

| Kategorie | Ressource | URL |
|-----------|-----------|-----|
| AI | Ollama Windows-Dokumentation | https://docs.ollama.com/windows |
| AI | Ollama GPU-Dokumentation | https://docs.ollama.com/gpu |
| Export | LDraw Legal Info | https://www.ldraw.org/legal-info |
| Export | LDraw Parts Library | https://library.ldraw.org/ |
| Optional | LPub3D Projektseite | https://trevorsandy.github.io/lpub3d/ |
| Optional | BrickLink Studio Licence | https://studiohelp.bricklink.com/hc/en-us/articles/6606313426711-Studio-Software-License-Agreement |
| Optional | BrickLink Studio Instruction Maker | https://studiohelp.bricklink.com/hc/en-us/articles/5626403887511-Introduction-to-instructions-maker |
