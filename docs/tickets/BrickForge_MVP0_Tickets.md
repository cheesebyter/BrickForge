# BrickForge – MVP 0 Tickets

**Projekt:** BrickForge  
**Version:** MVP 0  
**Grundlage:** BrickForge MVP 1 Pflichtenheft und MVP 1 Ticketstruktur  
**Ziel von MVP 0:** Technischer Durchstich für die kleinste lauffähige Kette  
**Zielplattform:** Windows Workstation, NVIDIA RTX 4090, Ollama, lokales Dateisystem  
**Stand:** 30.05.2026  

---

## 1. Zweck von MVP 0

MVP 0 ist kein vollständiger Produkt-MVP.  
MVP 0 dient als technischer Proof of Concept für die Kernfrage:

> Kann BrickForge lokal aus einer Textbeschreibung ein stark vereinfachtes Brick-Modell erzeugen und dieses als strukturierte Dateien exportieren?

MVP 0 soll bewusst klein gehalten werden. Ziel ist nicht Qualität, Vollständigkeit oder professionelle Bauanleitung, sondern ein belastbarer technischer Durchstich.

---

## 2. Abgrenzung zu MVP 1

### MVP 0 enthält

- lokale Entwicklungsumgebung
- Ollama-Anbindung
- ein minimales Promptanalyse-Ergebnis
- ein hart codiertes oder sehr einfaches Template
- ein minimales BrickGraph-Datenmodell
- eine einfache BrickGraph-Erzeugung
- einen stark vereinfachten LDraw/MPD-Export
- eine CSV-Teileliste
- eine einfache Markdown-Anleitung
- ein minimales CLI- oder Developer-UI-Interface
- ein vollständiger End-to-End-Test mit einem Beispielprompt

### MVP 0 enthält nicht

- vollständige Agentenregistrierung
- vollständiges Agenten-Kommunikationsprotokoll
- PostgreSQL-Persistenz als Pflicht
- produktionsreife API
- vollständige UI
- RepairAgent
- komplexe Validierungslogik
- mehrere Templates
- professionelle Bauanleitung
- PDF-Export
- Bild-/Scan-/3D-Verarbeitung
- Import existierender Modelle
- Benutzerverwaltung
- Cloud- oder externe AI-Integration

---

## 3. Zielkette MVP 0

```text
Prompt
  -> lokaler Ollama-Aufruf
  -> minimales Modellbriefing
  -> einfaches Template
  -> BrickGraph
  -> Minimalvalidierung
  -> LDraw/MPD-Datei
  -> CSV-Teileliste
  -> Markdown-Anleitung
```

---

## 4. Ticket-Konvention

### Prioritäten

| Priorität | Bedeutung |
|---|---|
| P0 | zwingend für MVP0-Durchstich |
| P1 | sinnvoll für belastbaren POC |
| P2 | Vorbereitung für MVP1 |
| P3 | optional |

### Definition of Done – MVP 0

Ein Ticket gilt als erledigt, wenn:

- die beschriebene Funktion lokal lauffähig ist
- keine externe AI-API erforderlich ist
- das Verhalten kurz dokumentiert ist
- relevante Fehlerfälle abgefangen werden
- mindestens ein manueller oder automatisierter Test möglich ist

---

# 5. Tickets

## BF-MVP0-001 – Repository- und Solution-Grundstruktur erstellen

**Priorität:** P0  
**Bereich:** Projektbasis  
**Ziel:** Minimale technische Struktur für den POC.

### Beschreibung

Es soll eine reduzierte Projektstruktur erstellt werden, die später ohne grosse Umbrüche in die MVP1-Struktur überführt werden kann.

### Zielstruktur

```text
BrickForge/
  src/
    BrickForge.Cli/
    BrickForge.Core/
    BrickForge.Ai/
    BrickForge.BrickGraph/
    BrickForge.Export/
  tests/
    BrickForge.Core.Tests/
    BrickForge.BrickGraph.Tests/
    BrickForge.Export.Tests/
  data/
    outputs/
    samples/
    parts/
  docs/
```

### Akzeptanzkriterien

- Solution ist lokal buildbar.
- CLI-Projekt kann gestartet werden.
- Core-, AI-, BrickGraph- und Export-Projekte sind getrennt.
- Testprojekte sind angelegt.
- README enthält Zweck von MVP0 und Startbefehl.

---

## BF-MVP0-002 – Lokale Konfiguration für MVP0 anlegen

**Priorität:** P0  
**Bereich:** Konfiguration  
**Ziel:** Konfigurierbare lokale Ausführung ohne Cloud-Abhängigkeit.

### Anforderungen

Konfiguration enthält:

```json
{
  "Generation": {
    "MaxParts": 80,
    "DefaultTargetParts": 50,
    "OutputRoot": "data/outputs"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3.1:8b",
    "TimeoutSeconds": 120,
    "Temperature": 0.2
  },
  "Export": {
    "GenerateMpd": true,
    "GenerateCsv": true,
    "GenerateMarkdown": true
  }
}
```

### Akzeptanzkriterien

- Konfiguration wird beim Start geladen.
- Fehlende Werte erhalten Defaultwerte.
- OutputRoot ist konfigurierbar.
- Externe AI ist nicht konfiguriert und nicht notwendig.

---

## BF-MVP0-003 – Ollama-Verfügbarkeitsprüfung implementieren

**Priorität:** P0  
**Bereich:** AI  
**Ziel:** Prüfen, ob lokales Ollama erreichbar ist.

### Beschreibung

Vor einer Generierung soll geprüft werden, ob Ollama lokal erreichbar ist.

### Akzeptanzkriterien

- CLI kann `ollama health` oder äquivalenten Check ausführen.
- Bei erreichbarem Ollama wird Status `available` ausgegeben.
- Bei nicht erreichbarem Ollama erscheint eine klare Fehlermeldung.
- Kein unhandled exception bei fehlendem Ollama.

---

## BF-MVP0-004 – Minimalen Ollama-Client implementieren

**Priorität:** P0  
**Bereich:** AI  
**Ziel:** Lokale LLM-Anfragen an Ollama senden.

### Beschreibung

Es wird ein einfacher HTTP-Client erstellt, der einen Prompt an ein lokales Ollama-Modell sendet und Text zurückgibt.

### Akzeptanzkriterien

- Modellname ist konfigurierbar.
- Timeout wird berücksichtigt.
- Temperatur ist konfigurierbar.
- Antworttext wird zurückgegeben.
- Fehler bei Timeout oder Nichterreichbarkeit werden sauber gemeldet.

---

## BF-MVP0-005 – Einfaches Promptanalyse-JSON erzeugen

**Priorität:** P0  
**Bereich:** AI  
**Ziel:** Aus einer Textbeschreibung ein minimales strukturiertes Modellbriefing erzeugen.

### Beschreibung

MVP0 benötigt noch keinen vollständigen PromptAnalysisAgent. Es genügt eine einfache Analyse über Ollama mit JSON-Ausgabe.

### Minimaler Output

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

### Akzeptanzkriterien

- Prompt wird an Ollama gesendet.
- Antwort wird als JSON geparst.
- Bei ungültigem JSON wird ein einfacher Fallback verwendet.
- `target_parts` wird auf maximal 80 begrenzt.
- `feasible = false` bricht Generierung verständlich ab.

---

## BF-MVP0-006 – Fallback-Promptanalyse ohne AI implementieren

**Priorität:** P1  
**Bereich:** AI  
**Ziel:** MVP0 soll auch bei instabiler LLM-Ausgabe testbar bleiben.

### Beschreibung

Falls Ollama ungültiges JSON liefert, soll eine einfache regelbasierte Fallbackanalyse greifen.

### Beispielregeln

- enthält „Kaffeemaschine“ -> `small_machine`
- enthält „Haus“ oder „Gebäude“ -> `small_building`
- enthält „Auto“ -> `small_vehicle`
- keine Kategorie erkannt -> `display_object`

### Akzeptanzkriterien

- Fallbackanalyse ist implementiert.
- Fallback wird im Bericht dokumentiert.
- Generierung kann auch ohne valides LLM-JSON fortgesetzt werden.
- Tests für mindestens drei Keywords existieren.

---

## BF-MVP0-007 – Minimales BrickGraph-Datenmodell erstellen

**Priorität:** P0  
**Bereich:** BrickGraph  
**Ziel:** Interne Modellstruktur für den technischen Durchstich.

### Datenmodell

```json
{
  "model": {
    "id": "model_001",
    "name": "Simple Coffee Machine",
    "target_parts": 50,
    "actual_parts": 0
  },
  "parts": [],
  "steps": []
}
```

### Teilstruktur

```json
{
  "instance_id": "part_001",
  "part_number": "3001",
  "part_name": "Brick 2 x 4",
  "color": "black",
  "position": [0, 0, 0],
  "rotation": [1, 0, 0, 0, 1, 0, 0, 0, 1],
  "step": 1
}
```

### Akzeptanzkriterien

- BrickGraph kann erstellt werden.
- Parts können hinzugefügt werden.
- Steps können hinzugefügt werden.
- BrickGraph kann als JSON gespeichert werden.
- Unit Test für Serialisierung existiert.

---

## BF-MVP0-008 – Reduzierte MVP0-Teileliste definieren

**Priorität:** P0  
**Bereich:** BrickGraph  
**Ziel:** Sehr kleine kontrollierte Teilebasis für erste Modelle.

### Unterstützte Teile MVP0

```text
3005 - Brick 1 x 1
3004 - Brick 1 x 2
3622 - Brick 1 x 3
3010 - Brick 1 x 4
3003 - Brick 2 x 2
3002 - Brick 2 x 3
3001 - Brick 2 x 4
3024 - Plate 1 x 1
3023 - Plate 1 x 2
3710 - Plate 1 x 4
3022 - Plate 2 x 2
3020 - Plate 2 x 4
3069b - Tile 1 x 2
2431 - Tile 1 x 4
```

### Unterstützte Farben MVP0

```text
black
white
red
blue
yellow
light_bluish_gray
dark_bluish_gray
transparent_clear
```

### Akzeptanzkriterien

- Teileliste liegt als JSON-Datei vor.
- Farbenliste liegt als JSON-Datei vor.
- Teil kann anhand Teilenummer gefunden werden.
- Nicht unterstützte Teile werden abgelehnt.

---

## BF-MVP0-009 – Einfaches `small_machine`-Template erstellen

**Priorität:** P0  
**Bereich:** Templates  
**Ziel:** Ein einziges Template für den ersten Durchstich.

### Beschreibung

Das Template erzeugt eine einfache, rechteckige kleine Maschine, z. B. Kaffeemaschine, Werkbankmaschine oder Displaygerät.

### Standardstruktur

```text
base
main_body
front_panel
top
simple_detail
```

### Akzeptanzkriterien

- Template ist als JSON oder Code-Konstante vorhanden.
- Template definiert Breite, Tiefe, Höhe.
- Template definiert Standardfarben.
- Template definiert einfache Baugruppen.
- Template kann vom Generator verwendet werden.

---

## BF-MVP0-010 – BrickGraph aus `small_machine`-Template generieren

**Priorität:** P0  
**Bereich:** BrickGraph  
**Ziel:** Aus dem Template konkrete Teile erzeugen.

### Beschreibung

Der Generator erzeugt eine einfache Maschine aus Standardteilen.

### Mindestmodell

- Grundfläche aus Plates
- Hauptkörper aus Bricks
- Frontpanel aus Tiles
- einfacher Top-Abschluss
- optional kleines Detailteil

### Akzeptanzkriterien

- Generator erzeugt mindestens 20 Teile.
- Generator erzeugt maximal 80 Teile.
- Jedes Teil hat Position, Rotation, Farbe und Step.
- `actual_parts` wird korrekt gesetzt.
- Modell ist als BrickGraph JSON speicherbar.

---

## BF-MVP0-011 – Bauschritte minimal erzeugen

**Priorität:** P0  
**Bereich:** BrickGraph  
**Ziel:** Einfache bottom-up Schrittfolge erzeugen.

### Beschreibung

MVP0 benötigt keine perfekte Bauanleitung, aber jedes Teil muss einem Schritt zugeordnet sein.

### Akzeptanzkriterien

- Es werden mindestens 5 Schritte erzeugt.
- Kein Teil hat Step 0 oder null.
- Schritte sind aufsteigend.
- Pro Schritt werden neue Teile referenziert.
- Schritte erscheinen später in `instructions.md`.

---

## BF-MVP0-012 – Minimalvalidierung implementieren

**Priorität:** P0  
**Bereich:** Validierung  
**Ziel:** Offensichtliche Fehler vor Export erkennen.

### Prüfungen MVP0

- Teileanzahl <= 80
- alle Teile sind in Supported-Parts-Liste
- alle Farben sind erlaubt
- jedes Teil besitzt einen Step
- Positionen sind gesetzt
- keine leere Parts-Liste

### Akzeptanzkriterien

- Validierung liefert `valid`, `score`, `issues`.
- High-Severity-Fehler führen zu Abbruch.
- Validierungsergebnis wird als JSON gespeichert.
- Mindestens drei Unit Tests existieren.

---

## BF-MVP0-013 – Einfachen LDraw/MPD-Exporter implementieren

**Priorität:** P0  
**Bereich:** Export  
**Ziel:** Erstes digitales Modellformat erzeugen.

### Beschreibung

Der Exporter schreibt eine einfache `.mpd`-Datei aus dem BrickGraph.

### Mindestinhalt

```text
0 FILE model.mpd
0 BrickForge generated MVP0 model
0 Name: model.mpd
0 Author: BrickForge MVP0
0 !HISTORY Generated at {timestamp}
0 STEP
1 0 0 0 0 1 0 0 0 1 0 0 0 3001.dat
```

### Akzeptanzkriterien

- `model.mpd` wird erzeugt.
- Datei enthält Header.
- Datei enthält mindestens eine Teilezeile.
- STEP-Kommandos werden geschrieben.
- Export schlägt bei leerem BrickGraph kontrolliert fehl.

---

## BF-MVP0-014 – CSV-Teileliste erzeugen

**Priorität:** P0  
**Bereich:** Export  
**Ziel:** Aggregierte Teileliste exportieren.

### Spalten

```text
quantity
part_number
part_name
color
```

### Akzeptanzkriterien

- `parts.csv` wird erzeugt.
- Gleiche Teile mit gleicher Farbe werden aggregiert.
- CSV ist UTF-8-kodiert.
- Kopfzeile ist vorhanden.
- Unit Test prüft Aggregation.

---

## BF-MVP0-015 – Einfache Markdown-Bauanleitung erzeugen

**Priorität:** P0  
**Bereich:** Export  
**Ziel:** Lesbare Schritt-für-Schritt-Anleitung aus BrickGraph erzeugen.

### Inhalt

- Titel
- kurzer Hinweis „AI-generiertes BrickForge MVP0-Modell“
- Teileliste
- Bauschritte
- pro Schritt neue Teile

### Akzeptanzkriterien

- `instructions.md` wird erzeugt.
- Alle Steps erscheinen in der Anleitung.
- Pro Step werden Teile aufgelistet.
- Datei ist als Markdown sauber lesbar.
- Keine Behauptung, dass es eine offizielle LEGO-Anleitung ist.

---

## BF-MVP0-016 – Technischen Generierungsbericht erzeugen

**Priorität:** P1  
**Bereich:** Export  
**Ziel:** Nachvollziehbarkeit des POC-Laufs.

### Inhalt

- Originalprompt
- verwendetes Modell
- AI-Analyse oder Fallbackanalyse
- Zielteilezahl
- tatsächliche Teilezahl
- Validierungsergebnis
- erzeugte Dateien
- bekannte Einschränkungen

### Akzeptanzkriterien

- `report.md` wird erzeugt.
- Bericht nennt MVP0-Einschränkungen.
- Bericht nennt, ob AI oder Fallback verwendet wurde.
- Bericht verweist auf keine offizielle LEGO-Anleitung.

---

## BF-MVP0-017 – CLI-Befehl `generate` implementieren

**Priorität:** P0  
**Bereich:** CLI  
**Ziel:** End-to-End-Generierung über Kommandozeile starten.

### Beispiel

```bash
brickforge generate "Erstelle eine kleine schwarze Kaffeemaschine mit Tasse"
```

### Akzeptanzkriterien

- CLI nimmt Prompt entgegen.
- CLI startet vollständige Pipeline.
- Output-Verzeichnis wird ausgegeben.
- Fehler werden verständlich angezeigt.
- Exit Code 0 bei Erfolg, ungleich 0 bei Fehler.

---

## BF-MVP0-018 – Beispielprompt für MVP0 definieren

**Priorität:** P0  
**Bereich:** Tests  
**Ziel:** Reproduzierbarer Testfall für den Durchstich.

### Beispielprompt

```text
Erstelle eine kleine schwarze Kaffeemaschine mit silbernem Frontpanel und einer Tasse. Das Modell soll einfach und stabil sein.
```

### Erwartetes Ergebnis

- Kategorie: small_machine
- Teileanzahl: 20–80
- Dateien:
  - `brickgraph.json`
  - `model.mpd`
  - `parts.csv`
  - `instructions.md`
  - `validation.json`
  - optional `report.md`

### Akzeptanzkriterien

- Prompt liegt unter `data/samples/`.
- Erwartete Eigenschaften sind dokumentiert.
- Prompt kann manuell über CLI ausgeführt werden.

---

## BF-MVP0-019 – End-to-End-Test für MVP0 erstellen

**Priorität:** P0  
**Bereich:** Tests  
**Ziel:** Gesamtkette automatisiert absichern.

### Ablauf

```text
Sample Prompt
  -> Promptanalyse
  -> Template
  -> BrickGraph
  -> Validierung
  -> Export
```

### Akzeptanzkriterien

- Test läuft lokal.
- Test kann optional Ollama mocken.
- Alle Pflichtdateien werden erzeugt.
- Validierung ist erfolgreich.
- Teileanzahl liegt im MVP0-Rahmen.

---

## BF-MVP0-020 – Mock-Ollama-Modus für Tests implementieren

**Priorität:** P1  
**Bereich:** Tests / AI  
**Ziel:** Tests sollen nicht zwingend von laufendem Ollama abhängen.

### Beschreibung

Für automatisierte Tests soll ein Mock-Modus existieren, der eine feste Promptanalyse zurückliefert.

### Akzeptanzkriterien

- Mock-Modus ist per Konfiguration aktivierbar.
- Mock gibt valides Analyse-JSON zurück.
- E2E-Test kann ohne Ollama laufen.
- Produktiver Modus nutzt weiterhin echtes Ollama.

---

## BF-MVP0-021 – Minimaler Health Check implementieren

**Priorität:** P1  
**Bereich:** Diagnose  
**Ziel:** Lokale Umgebung schnell prüfen.

### Prüfung

- Konfiguration lesbar
- OutputRoot beschreibbar
- Ollama erreichbar
- Supported-Parts-Datei vorhanden

### Akzeptanzkriterien

- CLI-Befehl `brickforge health` existiert.
- Health Check zeigt verständliche Statusausgabe.
- Fehler werden mit konkreter Ursache angezeigt.
- Health Check erzeugt keinen Generierungsjob.

---

## BF-MVP0-022 – Logging für MVP0 einrichten

**Priorität:** P1  
**Bereich:** Diagnose  
**Ziel:** Technische Probleme nachvollziehbar machen.

### Anforderungen

- Start und Ende der Generierung
- Ollama-Aufruf
- Fallback-Verwendung
- BrickGraph-Erzeugung
- Validierung
- Exportdateien
- Fehler

### Akzeptanzkriterien

- Logs enthalten Zeitstempel.
- Logs enthalten JobId oder RunId.
- Keine API-Keys oder sensiblen Daten werden geloggt.
- Fehler sind nachvollziehbar.

---

## BF-MVP0-023 – Rechtlichen Hinweis in Outputs integrieren

**Priorität:** P1  
**Bereich:** Recht / Dokumentation  
**Ziel:** MVP0 vermeidet irreführende Marken- oder Herkunftsaussagen.

### Hinweistext

```text
Dieses Dokument wurde automatisch durch BrickForge erzeugt. Es handelt sich nicht um eine offizielle LEGO-Bauanleitung und nicht um ein von LEGO geprüftes Modell.
```

### Akzeptanzkriterien

- Hinweis erscheint in `instructions.md`.
- Hinweis erscheint in `report.md`.
- README enthält denselben Hinweis.
- Keine Output-Datei nennt sich „official LEGO instruction“.

---

## BF-MVP0-024 – Entwicklerdokumentation für MVP0 erstellen

**Priorität:** P1  
**Bereich:** Dokumentation  
**Ziel:** Lokales Setup und Ausführung dokumentieren.

### Inhalte

- Voraussetzungen
- Ollama starten
- Modell installieren
- Projekt bauen
- Health Check ausführen
- Beispielgenerierung starten
- Output-Dateien erklären
- bekannte Grenzen

### Akzeptanzkriterien

- `docs/mvp0-setup.md` existiert.
- Ein Entwickler kann MVP0 anhand der Anleitung lokal starten.
- Troubleshooting für „Ollama nicht erreichbar“ ist enthalten.

---

## BF-MVP0-025 – MVP0-Abnahme durchführen

**Priorität:** P0  
**Bereich:** Abschluss  
**Ziel:** Prüfen, ob der technische Durchstich erfüllt ist.

### Abnahmekriterien

- CLI läuft lokal.
- Ollama wird lokal verwendet oder im Testmodus gemockt.
- Beispielprompt wird verarbeitet.
- BrickGraph wird erzeugt.
- Minimalvalidierung läuft.
- `model.mpd` wird erzeugt.
- `parts.csv` wird erzeugt.
- `instructions.md` wird erzeugt.
- Keine externe AI-API wird benötigt.
- Einschränkungen sind dokumentiert.

### Akzeptanzkriterien

- Abnahmeprotokoll liegt unter `docs/mvp0-acceptance.md`.
- Alle P0-Tickets sind abgeschlossen.
- Bekannte Abweichungen sind dokumentiert.
- Entscheidung für Übergang zu MVP1 ist möglich.

---

# 6. Empfohlene Umsetzungsreihenfolge

## Schritt 1 – Lokaler technischer Rahmen

1. BF-MVP0-001 – Repository- und Solution-Grundstruktur erstellen  
2. BF-MVP0-002 – Lokale Konfiguration für MVP0 anlegen  
3. BF-MVP0-003 – Ollama-Verfügbarkeitsprüfung implementieren  
4. BF-MVP0-004 – Minimalen Ollama-Client implementieren  

## Schritt 2 – Minimalanalyse und Datenmodell

1. BF-MVP0-005 – Einfaches Promptanalyse-JSON erzeugen  
2. BF-MVP0-006 – Fallback-Promptanalyse ohne AI implementieren  
3. BF-MVP0-007 – Minimales BrickGraph-Datenmodell erstellen  
4. BF-MVP0-008 – Reduzierte MVP0-Teileliste definieren  
5. BF-MVP0-009 – Einfaches `small_machine`-Template erstellen  

## Schritt 3 – Generierung und Validierung

1. BF-MVP0-010 – BrickGraph aus `small_machine`-Template generieren  
2. BF-MVP0-011 – Bauschritte minimal erzeugen  
3. BF-MVP0-012 – Minimalvalidierung implementieren  

## Schritt 4 – Export und CLI

1. BF-MVP0-013 – Einfachen LDraw/MPD-Exporter implementieren  
2. BF-MVP0-014 – CSV-Teileliste erzeugen  
3. BF-MVP0-015 – Einfache Markdown-Bauanleitung erzeugen  
4. BF-MVP0-016 – Technischen Generierungsbericht erzeugen  
5. BF-MVP0-017 – CLI-Befehl `generate` implementieren  

## Schritt 5 – Tests, Diagnose und Abnahme

1. BF-MVP0-018 – Beispielprompt für MVP0 definieren  
2. BF-MVP0-019 – End-to-End-Test für MVP0 erstellen  
3. BF-MVP0-020 – Mock-Ollama-Modus für Tests implementieren  
4. BF-MVP0-021 – Minimaler Health Check implementieren  
5. BF-MVP0-022 – Logging für MVP0 einrichten  
6. BF-MVP0-023 – Rechtlichen Hinweis in Outputs integrieren  
7. BF-MVP0-024 – Entwicklerdokumentation für MVP0 erstellen  
8. BF-MVP0-025 – MVP0-Abnahme durchführen  

---

# 7. P0-Mindestumfang für MVP0

Der kleinste sinnvolle MVP0 besteht aus folgenden Tickets:

```text
BF-MVP0-001
BF-MVP0-002
BF-MVP0-003
BF-MVP0-004
BF-MVP0-005
BF-MVP0-007
BF-MVP0-008
BF-MVP0-009
BF-MVP0-010
BF-MVP0-011
BF-MVP0-012
BF-MVP0-013
BF-MVP0-014
BF-MVP0-015
BF-MVP0-017
BF-MVP0-018
BF-MVP0-019
BF-MVP0-025
```

Damit wird folgende Kette nachgewiesen:

```text
Prompt
  -> lokale AI oder Fallback
  -> minimales Template
  -> BrickGraph
  -> Minimalvalidierung
  -> LDraw/MPD
  -> Teileliste
  -> Markdown-Anleitung
```

---

# 8. Übergang von MVP0 zu MVP1

MVP0 bereitet MVP1 vor, indem folgende Komponenten wiederverwendbar entstehen:

| MVP0-Komponente | Verwendung in MVP1 |
|---|---|
| Ollama-Client | Local LLM Adapter |
| Promptanalyse | PromptAnalysisAgent |
| BrickGraph | BrickGraph Engine |
| Teileliste | Supported Parts Library |
| small_machine Template | Template-System |
| Minimalvalidierung | ValidationAgent-Grundlage |
| MPD-Exporter | LDrawExporter |
| CSV-Exporter | CSVExporter |
| Markdown-Anleitung | InstructionExporter |
| CLI | Entwickler-/Testwerkzeug |

MVP1 erweitert diese Basis um:

- Orchestrator Agent
- Agentenregistrierung
- standardisiertes Agentenprotokoll
- PostgreSQL-Persistenz
- mehrere Templates
- BrickPlanAgent
- RepairAgent
- API
- UI
- Monitoring und Health Checks
- umfangreichere Tests
