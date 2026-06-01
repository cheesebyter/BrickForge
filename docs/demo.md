# BrickForge – Demo-Szenario

Dieses Dokument beschreibt das primäre Demo-Szenario für BrickForge MVP1.

---

## Demo-Prompt: Siebträgermaschine

```
Erstelle eine kleine schwarze Siebträger-Kaffeemaschine mit silbernem Frontpanel,
einem Siebträger-Griff und einer Auffangschale. Das Modell soll kompakt, stabil
und mit etwa 60 Klemmbausteinen baubar sein.
```

### Alternativ-Prompt (ursprüngliches MVP0-Golden-Sample)

```
Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse.
Das Modell soll einfach und stabil sein.
```

---

## Erwartete Ausgaben

Nach einer erfolgreichen Generierung entstehen folgende Dateien unter `data/outputs/{jobId}/`:

| Datei | Inhalt |
|-------|--------|
| `brickgraph.json` | Internes BrickGraph-Modell (JSON) |
| `validation.json` | Validierungsergebnis mit Score und Issues |
| `model.mpd` | LDraw MPD-Datei (öffenbar in LDView, LPub3D) |
| `parts.csv` | CSV-Teileliste (Teilenummer, Name, Farbe, Anzahl) |
| `instructions.md` | Markdown-Bauanleitung mit Schritt-Übersicht |
| `instructions.html` | HTML-Bauanleitung |
| `report.md` | Technischer Bericht inkl. Agentenmetriken |
| `generation.json` | Maschinenlesbare Generierungszusammenfassung |

---

## Voraussetzungen

- .NET 9 SDK installiert
- Ollama läuft lokal (`ollama serve`)
- Modell geladen: `ollama pull llama3`
- BrickForge API gestartet: `dotnet run` in `src/BrickForge.Api`

> **Hinweis:** Ohne Ollama läuft BrickForge im Fallback-Modus mit deterministischer Analyse. Alle Ausgaben werden trotzdem erzeugt.

---

## Demo-Ablauf (API)

### 1. Job erstellen

```http
POST /api/generation-jobs
Content-Type: application/json

{
  "prompt": "Erstelle eine kleine schwarze Siebträger-Kaffeemaschine mit silbernem Frontpanel, einem Siebträger-Griff und einer Auffangschale. Das Modell soll kompakt, stabil und mit etwa 60 Klemmbausteinen baubar sein."
}
```

**Antwort:**
```json
{
  "id": "abc123",
  "status": "Queued",
  "createdAt": "2025-01-01T12:00:00Z"
}
```

### 2. Status abfragen

```http
GET /api/generation-jobs/abc123
```

**Antwort (abgeschlossen):**
```json
{
  "id": "abc123",
  "status": "Completed",
  "templateName": "small_machine",
  "actualParts": 58,
  "validationScore": 1.0,
  "mainColor": "black",
  "accentColor": "light_bluish_gray"
}
```

### 3. Dateien auflisten

```http
GET /api/generation-jobs/abc123/files
```

### 4. Datei herunterladen

```http
GET /api/generation-jobs/abc123/download?file=model.mpd
```

### 5. Validierungsergebnis

```http
GET /api/generation-jobs/abc123/validation
```

---

## Erwartete Validierungs-Kennzahlen

| Kennzahl | Erwarteter Wert |
|---------|----------------|
| Validierungsscore | ≥ 0.85 |
| Teileanzahl | 40–80 |
| High-Severity-Issues | 0 |
| Status | `Completed` oder `CompletedWithWarnings` |

---

## Ohne Ollama (Fallback-Modus)

Wenn Ollama nicht verfügbar ist:
- `used_fallback: true` im Analyseergebnis
- Modellname: `Unbekanntes Modell`
- Kategorie: `small_machine`
- Teileanzahl: Standardwert (50)
- Farben: `black` / `light_bluish_gray`

Die Generierung läuft trotzdem vollständig durch. Alle Ausgabedateien werden erzeugt.

---

## Swagger UI

Alle Endpunkte können über die Swagger UI getestet werden:

```
https://localhost:7000/swagger
```

---

## Hinweise

- Keine externen AI-APIs werden verwendet.
- Prompts werden nicht gespeichert oder weitergeleitet.
- Generierte Dateien bleiben lokal unter `data/outputs/{jobId}/`.
- Die Ausgabe ist keine offizielle LEGO-Bauanleitung.
