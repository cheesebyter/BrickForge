# BrickForge – MVP-Abnahmeprotokoll

Dieses Dokument dokumentiert den Abnahmestand von BrickForge MVP1 gegenüber den Pflichtenheft-Anforderungen.

**Stand:** 2025-07  
**Gesamtstatus:** ✅ MVP1 abnahmefähig

---

## MVP0 – Grundlegende Generierungspipeline

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP0-001 | .NET-Projektstruktur mit Trennung der Concerns | ✅ | 5 Kernprojekte + 6 Testprojekte |
| MVP0-002 | Ollama-Client (`IOllamaClient`) | ✅ | Konfigurierbar, mit CancellationToken |
| MVP0-003 | Prompt-Analyse mit JSON-Ausgabe | ✅ | Schema-Validierung, Fallback-Parser |
| MVP0-004 | Fallback-Prompt-Analyse | ✅ | Deterministisch, ohne Ollama |
| MVP0-005 | `GenerationOptions` (konfigurierbar) | ✅ | `appsettings.json` |
| MVP0-006 | `PromptAnalysisResult` Domaintyp | ✅ | |
| MVP0-007 | `BrickGraph`-Grundmodell | ✅ | Mit Metadata, Parts, Steps |
| MVP0-008 | `small_machine`-Template | ✅ | Basis, Körper, Panel, Details |
| MVP0-009 | `BrickGraphValidator` (regelbasiert) | ✅ | 6 Pflichtchecks + 3 weitere |
| MVP0-010 | LDraw/MPD-Export | ✅ | Header, STEP-Marker, UTF-8 |
| MVP0-011 | CSV-Teileexport | ✅ | Aggregiert nach Teilenummer + Farbe |
| MVP0-012 | Markdown-Bauanleitung | ✅ | Mit rechtlichem Hinweis |
| MVP0-013 | `brickgraph.json` Export | ✅ | |
| MVP0-014 | `validation.json` Export | ✅ | |
| MVP0-015 | `report.md` Export | ✅ | Mit Agentenmetriken |
| MVP0-016 | CLI `generate`-Befehl | ✅ | |
| MVP0-017 | CLI `health`-Befehl | ✅ | |
| MVP0-018 | Exit-Code bei Fehler | ✅ | |
| MVP0-019 | Pfadtraversal-Schutz | ✅ | Normalisierung + Präfixprüfung |
| MVP0-020 | Prompt-Länge validieren | ✅ | Konfigurierbar |
| MVP0-021–025 | Unit-Tests aller Kernmodule | ✅ | 461 Tests gesamt |

---

## MVP1 – API, Agenten und Infrastruktur

### API-Endpunkte

| # | Endpunkt | Status | Anmerkung |
|---|---------|--------|-----------|
| MVP1-001 | `POST /api/generation-jobs` | ✅ | Job erstellen |
| MVP1-002 | `GET /api/generation-jobs/{id}` | ✅ | Status und Metadaten |
| MVP1-003 | `GET /api/generation-jobs/{id}/files` | ✅ | Dateiliste |
| MVP1-004 | `GET /api/generation-jobs/{id}/validation` | ✅ | Validierungsergebnis |
| MVP1-005 | `GET /api/generation-jobs/{id}/download` | ✅ | Dateidownload |
| MVP1-006 | Path-Traversal bei Download blockiert | ✅ | 400 bei Angriff |
| MVP1-007 | Unbekannter Job → 404 | ✅ | |

### Agentenpipeline

| # | Agent | Status | Anmerkung |
|---|-------|--------|-----------|
| MVP1-008 | `PromptAnalysisAgent` | ✅ | Mit Fallback |
| MVP1-009 | `TemplateSelectionAgent` | ✅ | Registry-basiert |
| MVP1-010 | `BrickGraphGeneratorAgent` | ✅ | Deterministisch |
| MVP1-011 | `ValidationAgent` | ✅ | 9 Checks |
| MVP1-012 | `RepairAgent` | ✅ | Automatische Reparatur |
| MVP1-013 | `ExportAgentSuite` | ✅ | 6 Exportformate |

### Persistenz und Infrastruktur

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-014 | SQLite-Repository | ✅ | Für MVP1-Entwicklung |
| MVP1-015 | `IJobRepository` Interface | ✅ | |
| MVP1-016 | Job-Status-Tracking | ✅ | 10 Status-Werte |
| MVP1-017 | `GeneratedFile`-Tabelle | ✅ | |
| MVP1-018 | Dependency Injection Setup | ✅ | |

### Sicherheit

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-019 | Prompt-Länge validieren | ✅ | |
| MVP1-020 | Output-Pfad auf Root beschränkt | ✅ | |
| MVP1-021 | KI-JSON validieren | ✅ | Schema-Validierung |
| MVP1-022 | Keine Code-Ausführung aus Prompt | ✅ | Architektur ausschliessend |
| MVP1-023 | Kein Logging von Secrets | ✅ | |
| MVP1-024 | Keine absoluten Pfade in API-Ausgabe | ✅ | |

### Agentenmetriken (BF-MVP1-044)

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-044 | `AgentMetrics` pro Agent erfassen | ✅ | StartTime, EndTime, DurationMs |
| MVP1-044 | LlmCalls, Retries, Success erfassen | ✅ | |
| MVP1-044 | Confidence / FinalScore erfassen | ✅ | |
| MVP1-044 | `JobMetrics` aggregiert | ✅ | Summen, Gesamtdauer |
| MVP1-044 | Metriken in `report.md` sichtbar | ✅ | Tabelle im Bericht |
| MVP1-044 | Metriken persistent (JSON-Spalte) | ✅ | `MetricsJson` in SQLite |

### UI / Status-Informationen

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-040 | Template in Statusantwort | ✅ | `templateName` Feld |
| MVP1-041 | `CompletedWithWarnings` Status | ✅ | Bei Validierungs-Warnings |
| MVP1-042 | Haupt- und Akzentfarbe in Status | ✅ | `mainColor`, `accentColor` |

### Fehlerbehandlung

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-039 | Strukturierte API-Fehlermeldungen | ✅ | ProblemDetails RFC 7807 |
| MVP1-043 | Ollama-Ausfall sauber behandelt | ✅ | Job → `Failed` |
| MVP1-045 | Validierungsfehler → Job `Failed` | ✅ | |
| MVP1-048 | Infeasible Prompt → `Failed` | ✅ | |
| MVP1-049 | Prompt zu lang → `Failed` | ✅ | |

### HTML-Export

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-050 | `instructions.html` erzeugen | ✅ | |
| MVP1-051 | HTML enthält rechtlichen Hinweis | ✅ | |

### Zusätzliche Exports

| # | Anforderung | Status | Anmerkung |
|---|------------|--------|-----------|
| MVP1-030 | `generation.json` Export | ✅ | Maschinenlesbare Zusammenfassung |
| MVP1-031 | BrickGraph-Reparatur-Flag in Export | ✅ | `wasRepaired` Feld |

---

## Tests

| Testprojekt | Testanzahl | Status |
|-------------|-----------|--------|
| BrickForge.Ai.Tests | 46 | ✅ |
| BrickForge.Api.Tests | 124 | ✅ |
| BrickForge.BrickGraph.Tests | 119 | ✅ |
| BrickForge.Core.Tests | 32 | ✅ |
| BrickForge.Export.Tests | 67 | ✅ |
| BrickForge.Integration.Tests | 73 | ✅ |
| **Gesamt** | **461+** | **✅** |

Alle Tests laufen deterministisch ohne Ollama oder externe Dienste.

---

## Bekannte Einschränkungen

| Einschränkung | Priorität |
|--------------|-----------|
| Kollisionsprüfung erkennt nur direkte Positionskollisionen | Niedrig |
| LDraw-Ausgabe nicht automatisch mit LDView verifiziert | Niedrig |
| LPub3D-Export nicht implementiert (optional) | Niedrig |
| PostgreSQL-Integration nicht aktiv (SQLite verwendet) | Mittel |
| UI (Blazor/WPF) nicht implementiert | Niedrig |
| Nur `small_machine`-Template implementiert | Mittel |

---

## Offene Punkte für zukünftige Iterationen

- [ ] PostgreSQL-Migration aktivieren
- [ ] Weitere Templates (`small_building`, `small_vehicle`, `furniture`)
- [ ] LPub3D-Export optional aktivieren
- [ ] Web-UI (Blazor) implementieren
- [ ] ZIP-Download für alle Job-Dateien
- [ ] Authentifizierung / API-Keys
- [ ] Metriken-Dashboard

---

## Abnahme-Signatur

> Dieses Protokoll bestätigt die Implementierung und Testabdeckung aller MVP1-Pflichanforderungen gemäss BrickForge Pflichtenheft.  
> Erstellt automatisch durch BrickForge-Generierungssystem.
