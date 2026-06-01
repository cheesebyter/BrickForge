# BrickForge – Entwickler-Setup

Dieses Dokument beschreibt die lokale Entwicklungsumgebung für BrickForge.

---

## Voraussetzungen

| Tool | Version | Zweck |
|------|---------|-------|
| .NET SDK | 9.0+ | Build und Tests |
| Docker Desktop | Aktuell | PostgreSQL (MVP1) |
| Ollama | Aktuell | Lokale KI-Analyse |
| Git | Aktuell | Versionierung |

> **Hinweis:** PostgreSQL und Ollama sind nur für MVP1 erforderlich. MVP0 läuft vollständig ohne externe Dienste (SQLite + Fallback-Analyse).

---

## Repository klonen

```bash
git clone https://github.com/your-org/BrickForge.git
cd BrickForge
```

---

## Projektstruktur

```
BrickForge/
├── src/
│   ├── BrickForge.Api            # ASP.NET Core API (MVP1)
│   ├── BrickForge.Ai             # Ollama-Client und Prompt-Analyse
│   ├── BrickForge.BrickGraph     # BrickGraph-Modell, Generierung, Validierung
│   ├── BrickForge.Core           # Domaintypen, Interfaces, Optionen
│   └── BrickForge.Export         # LDraw, CSV, Markdown, HTML, JSON-Export
├── tests/
│   ├── BrickForge.Ai.Tests
│   ├── BrickForge.Api.Tests
│   ├── BrickForge.BrickGraph.Tests
│   ├── BrickForge.Core.Tests
│   ├── BrickForge.Export.Tests
│   └── BrickForge.Integration.Tests
├── data/
│   └── outputs/                  # Generierte Ausgaben (pro Job-ID)
├── docs/
└── BrickForge.slnx
```

---

## Build

```bash
dotnet build
```

---

## Tests ausführen

```bash
dotnet test
```

Für Tests ist **kein** laufender Ollama-Dienst erforderlich. Alle KI-abhängigen Tests verwenden Fakes/Mocks.

Integration-Tests können mit:

```bash
dotnet test --filter Category=Integration
```

separat ausgeführt werden.

---

## Konfiguration

### appsettings.json (BrickForge.Api)

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "ModelName": "llama3",
    "TimeoutSeconds": 120,
    "Temperature": 0.2
  },
  "Generation": {
    "OutputRoot": "data/outputs",
    "MaxPromptLength": 2000,
    "MaxParts": 80
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/brickforge.db"
  }
}
```

### Ollama lokal starten (Windows)

1. Ollama von [https://ollama.com](https://ollama.com) herunterladen und installieren.
2. Modell herunterladen:
   ```bash
   ollama pull llama3
   ```
3. Ollama läuft standardmässig unter `http://localhost:11434`.

### Health-Check (Ollama)

```bash
curl http://localhost:11434/api/health
```

---

## API lokal starten (MVP1)

```bash
cd src/BrickForge.Api
dotnet run
```

Die API ist unter `https://localhost:7000` erreichbar (Port gemäss `launchSettings.json`).

### Swagger UI

```
https://localhost:7000/swagger
```

---

## PostgreSQL via Docker (MVP1 – optional)

Für persistente Datenhaltung ohne SQLite:

```bash
docker compose up -d
```

Setzt voraus, dass `docker-compose.yml` im Repository-Root vorhanden ist (MVP1-Erweiterung).

---

## Output-Verzeichnis

Generierte Dateien werden unter `data/outputs/{jobId}/` abgelegt.

Dieses Verzeichnis ist in `.gitignore` eingetragen.

---

## Troubleshooting

| Problem | Lösung |
|---------|--------|
| Ollama nicht erreichbar | `ollama serve` im Terminal starten |
| Build schlägt fehl | `dotnet restore` ausführen |
| SQLite-Datei gesperrt | Alle laufenden Prozesse beenden |
| Tests schlagen fehl | `dotnet clean` und `dotnet build` erneut ausführen |
| Port bereits belegt | `launchSettings.json` anpassen |

---

## Weiterführende Dokumentation

- [Architektur](architecture.md)
- [Agenten-Übersicht](agents.md)
- [Rechtliche Hinweise](legal-notes.md)
- [Demo-Szenario](demo.md)
