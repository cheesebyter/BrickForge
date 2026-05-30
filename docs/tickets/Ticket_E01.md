# Pflichtenheft – MVP 1: AI-gestütztes System zur Erstellung baubarer Brick-Bauanleitungen aus Textbeschreibungen

**Projektname:** BrickForge – MVP 1  
**Dokumenttyp:** Pflichtenheft  
**Version:** 1.0  
**Stand:** 30.05.2026  
**Zielplattform:** Windows Workstation mit NVIDIA RTX 4090, Ollama und lokalen AI-Modellen  
**Primärziel:** Textbeschreibung → kleines baubares Brick-Modell → digitale Modell-Datei → einfache Bauanleitung

---

## 1. Zweck des Dokuments

Dieses Pflichtenheft beschreibt die funktionalen, technischen, qualitativen, rechtlichen und betrieblichen Anforderungen für die Entwicklung eines ersten MVP-Systems, das aus einer textuellen Benutzerbeschreibung ein kleines baubares Brick-Modell erzeugt.

Der MVP fokussiert sich bewusst auf **Text-zu-Modell-Generierung**. Die Verarbeitung realer Objekte aus Fotos, Videos, 3D-Scans oder bestehenden LEGO-/Brick-Modellen ist nicht Bestandteil von MVP 1, wird aber architektonisch vorbereitet.

---

## 2. Projektvision

Das langfristige Ziel ist ein System, mit dem Benutzer beliebige Modelle als Brick-Bauanleitung erstellen lassen können. Dazu gehören:

- Modelle aus freier Beschreibung
- Modelle aus realen Objekten, z. B. Gebäude, Maschinen, Fahrzeuge
- Modifikationen existierender Brick-Modelle
- Erstellung von Bauanleitungen, Teilelisten und digitalen Modelldateien

MVP 1 bildet die technische Grundlage dafür. Er soll beweisen, dass ein AI-gestütztes System aus einer textuellen Beschreibung ein kleines, vereinfachtes, aber grundsätzlich baubares Brick-Modell erzeugen kann.

Der Projektname **BrickForge** steht für das gezielte Formen und Schmieden baubarer Brick-Modelle aus Beschreibungen. Der Name ist bewusst neutral gehalten und vermeidet eine direkte Markenabhängigkeit zu LEGO.

---

## 3. Grundsätze

Für MVP 1 gelten folgende verbindliche Grundsätze:

1. **Local-first AI**
   - AI-Modelle sollen so weit wie möglich lokal betrieben werden.
   - Zielumgebung ist Windows mit RTX 4090 und Ollama.
   - Externe AI-APIs sind nur optional und dürfen nicht für den Kernbetrieb erforderlich sein.

2. **Minimale laufende Betriebskosten**
   - Keine zwingende Abhängigkeit von kostenpflichtigen Cloud-AI-APIs.
   - Keine zwingende Abhängigkeit von kostenpflichtigen SaaS-Diensten.
   - Rechenintensive Verarbeitung erfolgt lokal.

3. **Lizenzabhängigkeiten minimieren**
   - Bevorzugt werden offene Formate, offene Bibliotheken und lokal ausführbare Tools.
   - Proprietäre Tools dürfen höchstens optional eingebunden werden.
   - Das Kernsystem muss ohne BrickLink Studio oder andere proprietäre Modellierungssoftware lauffähig sein.

4. **Baubarkeit vor Optik**
   - Das erzeugte Modell muss technisch nachvollziehbar und baubar sein.
   - Optische Detailtreue ist im MVP nachrangig.
   - Der MVP darf stilisierte und vereinfachte Modelle erzeugen.

5. **Deterministische Validierung**
   - AI darf planen, entwerfen und Vorschläge erzeugen.
   - Die finale Baubarkeit wird durch regelbasierte Validierungslogik geprüft.

6. **Exportierbarkeit**
   - Das System muss offene digitale Modellformate erzeugen können.
   - LDraw/MPD/LDR ist das bevorzugte Zielformat.
   - PDF-Anleitungen können im MVP vereinfacht sein.

---

## 4. MVP-Abgrenzung

### 4.1 Bestandteil von MVP 1

MVP 1 umfasst:

- Eingabe einer Textbeschreibung durch den Benutzer
- Analyse der Beschreibung durch lokale AI
- Ableitung eines technischen Modellbriefings
- Erzeugung eines einfachen BrickGraph-Modells
- Umwandlung des BrickGraph in LDraw/MPD oder LDR
- Erzeugung einer einfachen Schrittstruktur
- Erzeugung einer Teileliste
- einfache Validierung auf Baubarkeit
- Ausgabe als:
  - LDraw/MPD/LDR-Datei
  - Teileliste als CSV oder JSON
  - einfache Bauanleitung als Markdown, HTML oder PDF
  - technischer Generierungsbericht

### 4.2 Nicht Bestandteil von MVP 1

Nicht Bestandteil von MVP 1:

- Foto-zu-Modell
- Video-zu-Modell
- 3D-Scan-zu-Modell
- Import und Modifikation existierender LEGO-Sets
- vollständige physikalische Stabilitätssimulation
- perfekte LEGO-ähnliche Premium-Bauanleitung
- automatische Bestellung von Teilen
- Nutzerverwaltung
- Zahlungsfunktionen
- Cloud-Rendering
- Training eigener Foundation Models
- kommerzielle Veröffentlichung generierter Bauanleitungen direkt aus dem System

---

## 5. Zielmodell für MVP 1

Der MVP soll kleine Modelle aus Text erzeugen können.

### 5.1 Unterstützte Modelltypen

MVP 1 unterstützt folgende einfache Modellkategorien:

- kleine Gebäude
- Möbel
- kleine Maschinen
- einfache Fahrzeuge
- Verkaufsstände
- einfache Diorama-Elemente
- einfache Alltagsobjekte

Beispiele:

- kleine Kaffeemaschine
- Gartenhaus
- Werkbank
- kleiner Sportwagen
- Schweizer Werkhalle im Miniaturstil
- mittelalterlicher Marktstand
- einfache CNC-Maschine als Displaymodell

### 5.2 Nicht unterstützte Modelltypen

MVP 1 unterstützt nicht oder nur sehr eingeschränkt:

- komplexe Technic-Mechanismen
- grosse Gebäude mit Innenraum
- organische Figuren
- Tiere mit komplexen Rundungen
- lizenzierte Fahrzeuge mit hoher Detailtreue
- motorisierte Modelle
- Modelle mit Gummibändern, Pneumatik oder Elektronik
- Modelle mit sehr vielen Schräg- und SNOT-Techniken

---

## 6. Zielgrösse der generierten Modelle

MVP 1 arbeitet mit bewusst begrenzten Modellgrössen.

### 6.1 Standardgrenzen

| Kriterium | Zielwert |
|---|---:|
| Maximale Teileanzahl | 300 Teile |
| Empfohlene Teileanzahl | 80–200 Teile |
| Maximale Grundfläche | 32 x 32 Studs |
| Maximale Höhe | 25 Bricks |
| Maximale Bauschritte | 60 Schritte |
| Anzahl Farben | maximal 8 |
| Unterstützte Teilefamilien | Standard-Bricks, Plates, Tiles, Slopes, einfache transparente Teile |

### 6.2 Gründe für die Begrenzung

Die Begrenzung ist fachlich notwendig, weil:

- kleine Modelle einfacher zu validieren sind
- Bauanleitungen überschaubar bleiben
- lokale AI-Modelle weniger fehleranfällig planen
- Teileauswahl kontrollierbar bleibt
- der MVP schneller entwickelbar ist
- die Kosten tief bleiben

---

## 7. Nutzerrollen

### 7.1 Endbenutzer

Der Endbenutzer beschreibt ein Modell in natürlicher Sprache und lädt die generierten Dateien herunter.

Typische Ziele:

- Idee als baubares Brick-Modell visualisieren
- einfache Bauanleitung erhalten
- Teileliste erhalten
- Modell in LDraw-kompatiblen Tools weiterbearbeiten

### 7.2 Entwickler / Administrator

Der Entwickler betreibt das lokale System, verwaltet Modelle, Teilebibliotheken und AI-Konfigurationen.

Typische Aufgaben:

- lokale AI-Modelle konfigurieren
- LDraw-Parts-Library aktualisieren
- Systemlogs auswerten
- Validierungsregeln erweitern
- Modelltemplates pflegen

---

## 8. Hauptanwendungsfall

### 8.1 Use Case: Textbeschreibung zu Bauanleitung

**Akteur:** Endbenutzer  
**Ziel:** Aus einer Beschreibung ein kleines baubares Modell generieren

Ablauf:

1. Benutzer gibt eine Beschreibung ein.
2. System analysiert die Beschreibung.
3. System erzeugt ein strukturiertes Modellbriefing.
4. System wählt ein geeignetes Modelltemplate.
5. System erzeugt ein BrickGraph-Modell.
6. System validiert das Modell.
7. System korrigiert einfache Validierungsfehler automatisch.
8. System erzeugt LDraw/MPD/LDR-Datei.
9. System erzeugt eine Teileliste.
10. System erzeugt eine einfache Bauanleitung.
11. Benutzer erhält die Dateien als Download.

Beispielprompt:

> Erstelle eine kleine moderne Siebträgermaschine als Brick-Modell. Sie soll schwarz und silber sein, ca. 180 Teile haben, mit Siebträger, Tasse, Dampflanze und Wassertank. Das Modell soll stabil und für Anfänger baubar sein.

Erwartete Ausgaben:

- `espresso_machine.mpd`
- `espresso_machine_parts.csv`
- `espresso_machine_instructions.pdf`
- `espresso_machine_report.md`

---

## 9. Systemübersicht

### 9.1 Logische Architektur

```text
Frontend
  |
  v
Backend API
  |
  v
Generation Orchestrator
  |
  |-- Local LLM Adapter
  |-- Prompt Analysis Agent
  |-- Template Selection Agent
  |-- BrickGraph Generator
  |-- Validation Engine
  |-- Repair Engine
  |-- LDraw Exporter
  |-- Instruction Generator
  |
  v
Output Storage
```

### 9.2 Lokale Zielumgebung

Die Zielumgebung für MVP 1:

```text
Betriebssystem: Windows 11
GPU: NVIDIA RTX 4090, 24 GB VRAM
Lokale AI: Ollama
Backend: .NET oder Python/FastAPI
Speicher: lokales Dateisystem
Datenbank: optional SQLite oder PostgreSQL
Exportformat: LDraw/MPD/LDR
```

Ollama kann unter Windows nativ betrieben werden und stellt standardmässig eine lokale API unter `http://localhost:11434` bereit. Die offizielle Dokumentation nennt Windows-Unterstützung und GPU-Unterstützung, unter anderem für NVIDIA-GPUs.  
Quelle: https://docs.ollama.com/windows

---

## 10. Empfohlene technische Architektur

### 10.1 Frontend

Für MVP 1 sind zwei Frontend-Varianten geeignet.

#### Variante A: WPF Desktop POC

Vorteile:

- passt gut zu Windows-local-first
- keine Webhosting-Kosten
- einfache lokale Dateiverwaltung
- gute Integration in bestehende .NET-Erfahrung

Nachteile:

- weniger geeignet für spätere Webplattform
- UI-Distribution schwieriger als Web

#### Variante B: Blazor Web App lokal

Vorteile:

- gute Grundlage für spätere Web-/SaaS-Version
- kann lokal betrieben werden
- moderne UI möglich
- Backend und UI in .NET integrierbar

Nachteile:

- etwas mehr Setup als WPF
- Browser-/Server-Trennung beachten

Empfehlung für MVP 1:

> Blazor Web App lokal oder WPF POC. Bei Fokus auf schnelles lokales Demo-System ist WPF geeignet. Bei Fokus auf spätere Produktisierung ist Blazor sinnvoller.

### 10.2 Backend

Empfohlener Backend-Stack:

```text
ASP.NET Core API
C#
lokale Dateiablage
SQLite für MVP
Ollama HTTP API
eigene BrickGraph Engine
```

Alternative:

```text
Python FastAPI
Pydantic
lokale Dateiablage
SQLite
Ollama HTTP API
```

Empfehlung:

> Für den Nutzerkontext und spätere Business-Anwendung ist ASP.NET Core als Backend naheliegend. Für 3D-/AI-Prototyping kann ein separater Python-Service ergänzend verwendet werden.

### 10.3 AI-Layer

MVP 1 verwendet lokale LLMs über Ollama.

Aufgaben des LLM:

- Prompt verstehen
- Modellbriefing erzeugen
- Modelltemplate auswählen
- Baugruppen vorschlagen
- Farben und Details vorschlagen
- Reparaturvorschläge formulieren
- Generierungsbericht schreiben

Nicht Aufgabe des LLM:

- finale Kollisionsprüfung
- finale Verbindungsgültigkeit
- deterministische Teileplatzierung ohne Prüfung
- Lizenzentscheidungen
- Teileverfügbarkeit garantieren

### 10.4 BrickGraph Engine

Die BrickGraph Engine ist der technische Kern.

Sie verwaltet:

- Teileinstanzen
- Positionen
- Rotationen
- Farben
- Verbindungen
- Baugruppen
- Bauschritte
- Validierungsstatus

Sie darf nicht vollständig vom LLM abhängen.

### 10.5 Export Layer

Der Export Layer erzeugt:

- LDraw/MPD/LDR
- CSV-Teileliste
- JSON-Teileliste
- einfache Anleitung als Markdown/HTML/PDF
- Generierungsbericht

LDraw ist als textbasiertes Format und Teilebibliothek für digitale Brick-Modelle etabliert. Die LDraw Parts Library steht gemäss LDraw Legal Info unter Creative Commons Attribution 2.0, wobei einzelne Teile abweichend markiert sein können.  
Quelle: https://www.ldraw.org/legal-info

---

## 11. AI-Agenten im MVP

### 11.1 PromptAnalysisAgent

Aufgabe:

- Benutzerbeschreibung analysieren
- technische Anforderungen extrahieren
- fehlende Angaben mit Defaultwerten ergänzen
- Komplexität bewerten

Input:

```text
Erstelle eine kleine moderne Kaffeemaschine als Brick-Modell.
```

Output:

```json
{
  "object_type": "appliance",
  "model_category": "small_machine",
  "style": "modern",
  "target_parts": 180,
  "colors": ["black", "light_bluish_gray", "transparent_clear"],
  "features": ["portafilter", "cup", "steam_wand", "water_tank"],
  "difficulty": "beginner",
  "stability_priority": "high"
}
```

### 11.2 TemplateSelectionAgent

Aufgabe:

- passendes Template wählen
- Modellkategorie zuordnen
- erlaubte Teilefamilien bestimmen

Beispiel:

```text
object_type = appliance
template = rectangular_machine_template
```

### 11.3 BrickPlanAgent

Aufgabe:

- Baugruppen planen
- grobe Abmessungen vorschlagen
- sichtbare Details definieren
- Teilbudget verteilen

Beispiel:

```json
{
  "subassemblies": [
    {"name": "base", "parts_budget": 30},
    {"name": "main_body", "parts_budget": 80},
    {"name": "front_panel", "parts_budget": 25},
    {"name": "details", "parts_budget": 30},
    {"name": "cup", "parts_budget": 15}
  ]
}
```

### 11.4 BrickGraphGenerator

Aufgabe:

- konkretes Modell generieren
- Teileinstanzen anlegen
- Rasterpositionen setzen
- erste Bauschritte erzeugen

Dieser Agent ist nicht nur LLM-basiert. Er kombiniert:

- Templates
- regelbasierte Generierung
- Teilebibliothek
- Heuristiken

### 11.5 ValidationAgent

Aufgabe:

- Modell validieren
- Fehler klassifizieren
- Score erzeugen

Prüfungen:

- überlappende Teile
- Teile ohne Verbindung
- nicht unterstützte Teile
- ungültige Farben
- zu viele Teile
- zu viele Farben
- zu hohe Komplexität
- nicht erzeugbare Bauschrittfolge

### 11.6 RepairAgent

Aufgabe:

- einfache Fehler automatisch beheben
- Validierungsfehler an den BrickGraphGenerator zurückgeben

Beispiele:

- schwebendes Teil entfernen
- Verbindungsteil hinzufügen
- Wand verstärken
- unzulässige Farbe durch erlaubte Farbe ersetzen
- Teilbudget reduzieren

### 11.7 InstructionAgent

Aufgabe:

- Bauschritte aus BrickGraph ableiten
- Teile pro Schritt gruppieren
- einfache Bauanleitung erzeugen
- Exportdaten vorbereiten

---

## 12. BrickGraph-Spezifikation

### 12.1 Zweck

BrickGraph ist die interne kanonische Modellstruktur.

Sie entkoppelt:

```text
AI-Planung
  von
digitalem Modellformat
  von
Bauanleitung
```

### 12.2 Grundstruktur

```json
{
  "model": {
    "id": "model_001",
    "name": "Modern Espresso Machine",
    "unit": "ldraw",
    "target_parts": 180
  },
  "parts": [
    {
      "instance_id": "part_001",
      "part_number": "3001",
      "part_name": "Brick 2 x 4",
      "color": "black",
      "position": [0, 0, 0],
      "rotation": [1, 0, 0, 0, 1, 0, 0, 0, 1],
      "step": 1,
      "subassembly": "base"
    }
  ],
  "connections": [
    {
      "from": "part_001",
      "to": "part_002",
      "type": "stud_tube",
      "confidence": 1.0
    }
  ]
}
```

### 12.3 Pflichtfelder je Teil

| Feld | Pflicht | Beschreibung |
|---|---|---|
| instance_id | ja | eindeutige Instanz-ID |
| part_number | ja | LDraw-/interne Teilenummer |
| color | ja | definierte Farbe |
| position | ja | Position im Raster |
| rotation | ja | Rotation |
| step | ja | Bauschritt |
| subassembly | nein | Baugruppe |

### 12.4 Interne Einheiten

Das System muss intern eine konsistente Koordinateneinheit verwenden.

Empfehlung:

- Intern: Stud-/Plate-Raster
- Export: Umrechnung nach LDraw-Koordinaten

---

## 13. Unterstützte Teile im MVP

MVP 1 verwendet eine reduzierte Teilebibliothek.

### 13.1 Pflichtteile

Mindestens unterstützt:

- Brick 1x1
- Brick 1x2
- Brick 1x3
- Brick 1x4
- Brick 2x2
- Brick 2x3
- Brick 2x4
- Plate 1x1
- Plate 1x2
- Plate 1x3
- Plate 1x4
- Plate 2x2
- Plate 2x4
- Tile 1x1
- Tile 1x2
- Tile 1x4
- Slope 1x2
- Slope 2x2
- transparente 1x2 Bricks oder Panels
- einfache runde 1x1 Teile

### 13.2 Optionale Teile

Optional:

- Wheels
- simple Axle/Technic Pin
- Brackets
- Clips/Bars
- curved slopes
- wedge plates

### 13.3 Bewusst ausgeschlossene Teile

Für MVP 1 ausgeschlossen:

- komplexe Technic-Gearbox-Teile
- Pneumatik
- Motoren
- flexible Schläuche
- Gummibänder
- Stoffteile
- Sticker
- bedruckte Spezialteile als Pflichtbestandteil

---

## 14. Template-System

MVP 1 soll nicht jedes Modell vollständig frei generieren. Stattdessen werden Templates verwendet.

### 14.1 Pflichttemplates

Mindestens folgende Templates sind zu implementieren:

1. `small_building_template`
2. `small_vehicle_template`
3. `small_machine_template`
4. `furniture_template`
5. `display_object_template`

### 14.2 Template-Aufbau

Ein Template definiert:

- Grundfläche
- typische Baugruppen
- erlaubte Teile
- Standard-Proportionen
- Validierungsregeln
- Standard-Bauschrittlogik

Beispiel `small_machine_template`:

```json
{
  "name": "small_machine_template",
  "max_parts": 220,
  "subassemblies": [
    "base",
    "main_body",
    "front_panel",
    "side_details",
    "top_details"
  ],
  "allowed_parts": [
    "brick",
    "plate",
    "tile",
    "slope",
    "round_1x1"
  ],
  "default_dimensions": {
    "width": 10,
    "depth": 8,
    "height": 10
  }
}
```

---

## 15. Validierungsanforderungen

### 15.1 Muss-Prüfungen

Das System muss prüfen:

1. Teileanzahl <= definierte Maximalteile
2. alle Teile besitzen gültige Teilenummern
3. alle Farben sind erlaubt
4. keine Teilinstanzen teilen denselben Raum unzulässig
5. keine sichtbaren Hauptteile schweben frei
6. jedes Teil ist einem Bauschritt zugeordnet
7. Bauschritte sind monoton und nachvollziehbar
8. Modell hat mindestens eine zusammenhängende Hauptstruktur
9. Exportdatei ist syntaktisch gültig
10. Generierung erzeugt keine offiziell geschützte Anleitung als Kopie

### 15.2 Soll-Prüfungen

Das System sollte prüfen:

1. Wandstabilität
2. Basisstabilität
3. Farbkonsistenz
4. Symmetrie bei Fahrzeugen und Gebäuden
5. übermässige Fragilität
6. sinnvolle Teileökonomie
7. Vermeidung unnötig vieler Einzelteile
8. Möglichkeit der späteren LPub3D-Verarbeitung

### 15.3 Validierungsergebnis

Das Validierungsergebnis muss maschinenlesbar sein.

```json
{
  "valid": true,
  "score": 0.82,
  "issues": [
    {
      "severity": "medium",
      "type": "weak_structure",
      "message": "Left wall uses too many single-stud connections.",
      "suggested_fix": "Add 1x4 plate reinforcement."
    }
  ]
}
```

---

## 16. Bauanleitungsanforderungen

### 16.1 Mindestanforderungen

Die Bauanleitung muss enthalten:

- Titel
- kurze Beschreibung
- Teileliste
- Bauschritte
- pro Schritt hinzugefügte Teile
- einfache Draufsicht oder textuelle Beschreibung
- Exporthinweis zur LDraw-Datei

### 16.2 Ausgabeformate

Pflicht:

- Markdown
- HTML oder PDF
- CSV-Teileliste
- LDraw/MPD/LDR

Optional:

- PNG-Render je Schritt
- LPub3D-kompatibler Export
- ZIP-Paket mit allen Dateien

LPub3D ist ein Open-Source-WYSIWYG-Tool für LEGO-ähnliche digitale Bauanleitungen und kann LDraw-basierte Modelle zu Anleitungsdokumenten, Seiten und Teilelisten verarbeiten.  
Quelle: https://trevorsandy.github.io/lpub3d/

### 16.3 Qualität der Anleitung

MVP 1 muss keine perfekte LEGO-ähnliche Anleitung erzeugen. Erforderlich ist eine sachlich verständliche Anleitung.

Akzeptabel:

```text
Schritt 1: Baue die Grundplatte aus zwei 2x4 Plates und einer 1x4 Plate.
Schritt 2: Setze vier 1x2 Bricks an die Rückseite.
```

Nicht erforderlich in MVP 1:

- professionelle Layoutseiten
- fotorealistische Renderings
- automatische Pfeile
- Explosionsansichten
- komplexe Callouts

---

## 17. Ausgabe-Paket

Jede erfolgreiche Generierung erzeugt ein Output-Paket.

### 17.1 Pflichtdateien

```text
/output/{jobId}/
  model.mpd
  parts.csv
  instructions.md
  report.md
  generation.json
```

### 17.2 Optionale Dateien

```text
/output/{jobId}/
  instructions.pdf
  instructions.html
  preview.png
  validation.json
  brickgraph.json
```

### 17.3 Generierungsbericht

Der Bericht muss enthalten:

- Originalprompt
- gewähltes Template
- Modellkategorie
- Teileanzahl
- Farbliste
- Validierungsergebnis
- bekannte Einschränkungen
- Hinweis auf stilisierte Interpretation
- Hinweis auf keine offizielle LEGO-Anleitung

---

## 18. Rechtliche Anforderungen

### 18.1 Marken- und Urheberrecht

Das System darf nicht als offizielles LEGO-System dargestellt werden.

Verboten:

- „offizielle LEGO-Bauanleitung“
- Verwendung offizieller LEGO-Set-Anleitungen als automatisierte Kopiervorlage
- automatisches Nachbauen lizenzierter Sets ohne Nutzerimport und Rechteklärung
- irreführende Markenverwendung

Erlaubter neutraler Sprachgebrauch:

- Brick-Modell
- Klemmbaustein-kompatibel
- MOC
- digitale Bauanleitung
- fanbasierte Modellbeschreibung

### 18.2 LDraw-Nutzung

Die Nutzung der LDraw Parts Library muss die jeweilige Lizenz beachten. Gemäss LDraw Legal Info ist die Parts Library unter Creative Commons Attribution 2.0 lizenziert, wobei abweichende Teilemarkierungen zu beachten sind.

Pflicht:

- Attribution in Dokumentation und About-Dialog
- lokale Kopie der Lizenzinformationen
- keine Entfernung von Copyright-/Lizenzhinweisen
- Prüfung, ob einzelne Teile abweichende Lizenzinformationen enthalten

### 18.3 LPub3D-Nutzung

LPub3D darf für den MVP als optionaler lokaler Export-/Layout-Schritt betrachtet werden. Da LPub3D Open Source ist, sind Lizenzbedingungen einzuhalten. Bei Integration oder Distribution ist die konkrete Lizenzprüfung Teil der technischen Umsetzung.

### 18.4 Proprietäre Tools

BrickLink Studio darf nicht als zwingende Komponente des MVP-Kerns vorausgesetzt werden. Es kann optional für manuelle Nachbearbeitung erwähnt oder unterstützt werden. BrickLink Studio unterliegt eigenen Lizenz- und Nutzungsbedingungen.  
Quelle: https://studiohelp.bricklink.com/hc/en-us/articles/6606313426711-Studio-Software-License-Agreement

---

## 19. Nichtfunktionale Anforderungen

### 19.1 Performance

Zielwerte auf Windows + RTX 4090:

| Vorgang | Zielwert |
|---|---:|
| Promptanalyse | < 30 Sekunden |
| Modellplanung | < 60 Sekunden |
| BrickGraph-Erzeugung | < 30 Sekunden |
| Validierung | < 10 Sekunden |
| Export | < 10 Sekunden |
| Gesamtlauf MVP-Modell | < 3 Minuten |

### 19.2 Betriebskosten

Der MVP muss im Standardbetrieb ohne laufende API-Kosten funktionieren.

Pflicht:

- lokales LLM via Ollama
- lokale Validierung
- lokale Dateierzeugung
- keine Pflicht-Cloud

Optional:

- externer LLM-Fallback
- externe Bildgenerierung
- externe 3D-Generierung

Externe Dienste dürfen im MVP nur als deaktivierbare Zusatzoption vorgesehen werden.

### 19.3 Datenschutz

MVP 1 verarbeitet primär Textprompts. Trotzdem gelten folgende Anforderungen:

- Prompts werden lokal gespeichert oder gar nicht gespeichert
- keine automatische Übertragung an Drittanbieter
- externe AI-Nutzung nur nach expliziter Konfiguration
- Logs dürfen keine sensiblen API-Keys enthalten
- lokale Ausgabedateien liegen in einem konfigurierbaren Verzeichnis

### 19.4 Wartbarkeit

Das System muss modular aufgebaut sein:

- AI-Adapter austauschbar
- Templates erweiterbar
- Teilebibliothek aktualisierbar
- Validierungsregeln separat pflegbar
- Exporter austauschbar

### 19.5 Robustheit

Das System muss mit fehlerhaften Prompts umgehen können.

Beispiele:

- zu grosses Modell gewünscht
- unklare Beschreibung
- widersprüchliche Farbangaben
- nicht unterstützte Funktion
- ungültige Teilefamilie

Das System soll keine unkontrollierten Exceptions an den Benutzer weitergeben, sondern sachliche Fehlermeldungen erzeugen.

---

## 20. Lokale AI-Anforderungen

### 20.1 Ollama-Integration

Das System muss eine Ollama-kompatible HTTP-Integration besitzen.

Konfiguration:

```json
{
  "ollama": {
    "baseUrl": "http://localhost:11434",
    "planningModel": "qwen2.5-coder:14b",
    "fallbackModel": "llama3.1:8b",
    "temperature": 0.2,
    "timeoutSeconds": 120
  }
}
```

Die konkreten Modelle sind austauschbar zu halten.

### 20.2 Empfohlene Modelltypen

Für MVP 1 sind folgende Modellkategorien sinnvoll:

- kleines schnelles Modell für Klassifikation und Promptanalyse
- stärkeres lokales Modell für Planungsaufgaben
- optional Code-/JSON-starkes Modell für strukturierte Ausgaben

### 20.3 Strukturierte AI-Ausgaben

AI-Ausgaben müssen bevorzugt als JSON erzeugt werden.

Pflicht:

- JSON-Schema definieren
- Antwort validieren
- ungültige AI-Ausgabe automatisch reparieren oder neu anfordern
- kein ungeprüftes Freitext-Parsing für Kernlogik

### 20.4 Externe AI-Fallbacks

Externe AI-Fallbacks sind erlaubt, aber nicht Pflicht.

Anforderungen:

- standardmässig deaktiviert
- separate Konfiguration
- Kostenlimit möglich
- Protokollierung, wann externe API genutzt wurde
- keine automatische Nutzung ohne bewusste Aktivierung

---

## 21. Datenhaltung

### 21.1 MVP-Speicher

Für MVP 1 ausreichend:

- lokales Dateisystem
- SQLite für Jobs und Metadaten

### 21.2 Tabellen / Datenstrukturen

Minimal:

```sql
GenerationJob
- Id
- CreatedAt
- Prompt
- Status
- TemplateName
- TargetParts
- ActualParts
- OutputPath
- ValidationScore

GeneratedFile
- Id
- JobId
- FileType
- FilePath
- CreatedAt

ModelTemplate
- Id
- Name
- Version
- Description
- JsonDefinitionPath

PartDefinition
- Id
- PartNumber
- Name
- Category
- Supported
- DefaultDimensions
```

---

## 22. API-Anforderungen

### 22.1 Pflichtendpunkte

```http
POST /api/generation-jobs
GET  /api/generation-jobs/{id}
GET  /api/generation-jobs/{id}/files
GET  /api/generation-jobs/{id}/validation
GET  /api/generation-jobs/{id}/download
```

### 22.2 Beispiel: Job erstellen

Request:

```json
{
  "prompt": "Erstelle eine kleine moderne Kaffeemaschine als Brick-Modell.",
  "targetParts": 180,
  "difficulty": "beginner",
  "outputFormats": ["mpd", "csv", "md", "pdf"]
}
```

Response:

```json
{
  "jobId": "job_20260530_001",
  "status": "queued"
}
```

### 22.3 Jobstatus

Mögliche Statuswerte:

```text
queued
analyzing_prompt
planning_model
generating_brickgraph
validating
repairing
exporting
completed
failed
completed_with_warnings
```

---

## 23. UI-Anforderungen

### 23.1 Hauptmaske

Die Hauptmaske muss enthalten:

- Texteingabe für Modellbeschreibung
- Auswahl Modellgrösse
- Auswahl Zielschwierigkeit
- Teilelimit
- bevorzugte Farben
- Checkbox „nur lokale AI verwenden“
- Startbutton
- Fortschrittsanzeige
- Ergebnisbereich mit Downloads

### 23.2 Eingabefelder

Pflichtfelder:

- Beschreibung

Optionale Felder:

- Modelltyp
- Teilelimit
- Hauptfarben
- Zielschwierigkeit
- Stil
- Ausgabeformate

### 23.3 Ergebnisanzeige

Die Ergebnisanzeige muss anzeigen:

- Status
- Modellname
- Teileanzahl
- Farben
- Validierungsscore
- Warnungen
- Downloadlinks

---

## 24. Fehlerbehandlung

### 24.1 Typische Fehlerfälle

| Fehler | Verhalten |
|---|---|
| Ollama nicht erreichbar | verständliche Fehlermeldung, Retry anbieten |
| AI-Ausgabe ungültig | automatische Reparatur oder Neuversuch |
| Modell überschreitet Teilelimit | Modell reduzieren |
| keine passende Vorlage | generisches Display-Template verwenden |
| Validierung fehlgeschlagen | Reparaturversuch, danach Warnbericht |
| Export fehlgeschlagen | BrickGraph und Bericht trotzdem speichern |

### 24.2 Fehlermeldungen

Fehlermeldungen müssen professionell, kurz und technisch nachvollziehbar sein.

Beispiel:

```text
Das Modell konnte nicht vollständig validiert werden. Die generierte Datei wurde gespeichert, enthält aber 3 strukturelle Warnungen. Bitte prüfen Sie den Bericht.
```

---

## 25. Sicherheit

### 25.1 Prompt-Sicherheit

Auch bei lokaler AI sind Schutzmassnahmen nötig:

- keine Ausführung von AI-generiertem Code
- keine Shell-Befehle aus Prompts ausführen
- keine Dateipfade aus Prompts ungeprüft verwenden
- maximale Promptlänge definieren
- JSON-Ausgaben validieren

### 25.2 Dateisicherheit

- Ausgabepfade normalisieren
- keine Pfadmanipulation zulassen
- Downloads nur aus Output-Verzeichnis
- keine beliebigen Dateien exponieren

### 25.3 API-Sicherheit

Für lokalen MVP:

- API nur lokal erreichbar
- CORS einschränken
- keine offenen Netzwerkbindungen ohne Konfiguration
- optional lokale Authentifizierung für spätere Version

---

## 26. Logging und Diagnose

### 26.1 Pflichtlogs

Das System muss loggen:

- Jobstart
- Promptanalyse gestartet/beendet
- verwendetes AI-Modell
- Templateauswahl
- Validierungsergebnis
- Exportergebnis
- Fehler mit Stacktrace im Entwicklerlog

### 26.2 Keine sensiblen Logs

Nicht loggen:

- API-Keys
- komplette lokale Dateisystemstruktur
- personenbezogene Informationen, sofern später eingeführt

### 26.3 Diagnosebericht

Pro Job wird ein `report.md` erzeugt.

Inhalt:

- Eingabeprompt
- gewählte Annahmen
- Template
- Teileanzahl
- Validierungsscore
- bekannte Schwächen
- Exportdateien

---

## 27. Akzeptanzkriterien

### 27.1 Funktionale Akzeptanzkriterien

Der MVP gilt fachlich als erfolgreich, wenn folgende Kriterien erfüllt sind:

1. Ein Benutzer kann eine Textbeschreibung eingeben.
2. Das System erzeugt daraus ein strukturiertes Modellbriefing.
3. Das System wählt ein Template.
4. Das System erzeugt einen BrickGraph.
5. Das System erzeugt eine LDraw/MPD/LDR-Datei.
6. Das System erzeugt eine Teileliste.
7. Das System erzeugt eine einfache Bauanleitung.
8. Das System validiert das Modell regelbasiert.
9. Das System erstellt einen Generierungsbericht.
10. Das System läuft ohne externe AI-API.

### 27.2 Technische Akzeptanzkriterien

1. Das System läuft lokal auf Windows.
2. Ollama wird lokal angesprochen.
3. Bei fehlender Ollama-Verbindung wird sauber abgebrochen.
4. Keine Cloud-Komponente ist für die Standardgenerierung erforderlich.
5. Teilebibliothek ist lokal verfügbar.
6. Exportdateien werden reproduzierbar abgelegt.
7. AI-Ausgaben werden schema-validiert.
8. Ein Job kann vollständig nachvollzogen werden.
9. Das System erzeugt keine offiziellen LEGO-Kopien.
10. Die Kernlogik ist modular testbar.

### 27.3 Beispiel-Akzeptanztest

Input:

```text
Erstelle eine kleine moderne Siebträgermaschine als Brick-Modell.
Sie soll schwarz und silber sein, ca. 180 Teile haben, mit Siebträger, Tasse, Dampflanze und Wassertank.
Das Modell soll stabil und einfach baubar sein.
```

Erwartetes Ergebnis:

- Modell wird generiert
- Teileanzahl zwischen 100 und 220
- mindestens 3 Baugruppen
- mindestens 10 Bauschritte
- LDraw/MPD/LDR-Datei vorhanden
- CSV-Teileliste vorhanden
- Markdown-Anleitung vorhanden
- Validierungsscore >= 0.70
- keine High-Severity-Validierungsfehler

---

## 28. Testanforderungen

### 28.1 Unit Tests

Zu testen:

- Prompt-Schema-Validierung
- Templateauswahl
- Teilebibliothekszugriff
- BrickGraph-Erzeugung
- Kollisionsprüfung
- Teilezählung
- Export nach LDraw
- CSV-Export
- Fehlerbehandlung

### 28.2 Integrationstests

Zu testen:

- vollständiger Generierungsjob
- Ollama nicht erreichbar
- ungültige AI-Antwort
- Teilelimit überschritten
- Exportpfad nicht beschreibbar
- Validierungsfehler mit Reparaturversuch

### 28.3 Golden Sample Tests

Es müssen mindestens fünf stabile Beispielprompts gepflegt werden:

1. Kaffeemaschine
2. kleines Gartenhaus
3. Werkbank
4. kleiner Sportwagen
5. Verkaufsstand

Für jeden Beispielprompt werden erwartete Eigenschaften definiert.

---

## 29. Entwicklungsphasen

### Phase 1: Technische Basis

- Projektstruktur erstellen
- Konfiguration
- lokaler Ollama-Adapter
- Jobmodell
- Dateiausgabe
- Logging

### Phase 2: Promptanalyse

- PromptAnalysisAgent
- JSON-Schema
- Validierung
- Fehlerbehandlung

### Phase 3: Template-System

- Templateformat
- erste Templates
- Templateauswahl
- Defaultwerte

### Phase 4: BrickGraph Engine

- Datenmodell
- Teileinstanzen
- Baugruppen
- Bauschritte
- Grundvalidierung

### Phase 5: Export

- LDraw/MPD/LDR-Export
- CSV-Teileliste
- Markdown-Anleitung
- Generierungsbericht

### Phase 6: Validierung und Reparatur

- Kollisions-/Überschneidungsprüfung
- einfache Verbindungskontrolle
- Teilelimitkontrolle
- Repair-Regeln

### Phase 7: UI

- Eingabemaske
- Fortschrittsanzeige
- Ergebnisanzeige
- Downloadfunktionen

### Phase 8: Stabilisierung

- Tests
- Golden Samples
- Dokumentation
- Demo-Szenarien

---

## 30. Empfohlene Ordnerstruktur

```text
BrickForge/
  src/
    BrickForge.App/
    BrickForge.Api/
    BrickForge.Core/
    BrickForge.Ai/
    BrickForge.BrickGraph/
    BrickForge.Validation/
    BrickForge.Export/
    BrickForge.Templates/
    BrickForge.Infrastructure/
  tests/
    BrickForge.Core.Tests/
    BrickForge.BrickGraph.Tests/
    BrickForge.Validation.Tests/
    BrickForge.Export.Tests/
    BrickForge.IntegrationTests/
  data/
    parts/
    templates/
    samples/
    outputs/
  docs/
    architecture.md
    ai-models.md
    ldraw-export.md
    validation-rules.md
    legal-notes.md
  tools/
    ldraw/
    lpub3d/
  config/
    appsettings.Development.json
    appsettings.LocalAi.json
```

---

## 31. Konfigurationsbeispiel

```json
{
  "Generation": {
    "MaxParts": 300,
    "DefaultTargetParts": 180,
    "MaxColors": 8,
    "AllowExternalAi": false,
    "OutputRoot": "data/outputs"
  },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "PlanningModel": "qwen2.5-coder:14b",
    "FastModel": "llama3.1:8b",
    "TimeoutSeconds": 120
  },
  "PartsLibrary": {
    "Provider": "LDraw",
    "RootPath": "data/parts/ldraw",
    "SupportedPartsFile": "data/parts/supported-parts.json"
  },
  "Export": {
    "GenerateMpd": true,
    "GenerateCsv": true,
    "GenerateMarkdown": true,
    "GeneratePdf": false
  }
}
```

---

## 32. Risiken

### 32.1 Technische Risiken

| Risiko | Bewertung | Massnahme |
|---|---|---|
| AI erzeugt unbrauchbare Strukturen | hoch | Templates und Validator erzwingen |
| LDraw-Export fehlerhaft | mittel | Golden Sample Tests |
| lokale Modelle liefern instabiles JSON | hoch | Schema-Validierung und Retry |
| Baubarkeit schwer zu prüfen | hoch | MVP auf einfache Modelle begrenzen |
| Teilebibliothek zu komplex | mittel | reduzierte Supported-Parts-Liste |
| PDF-Anleitung zu aufwendig | mittel | zunächst Markdown/HTML, PDF optional |

### 32.2 Produkt-/Rechtsrisiken

| Risiko | Bewertung | Massnahme |
|---|---|---|
| Markenrechtsprobleme | mittel | neutrales Branding |
| Kopie offizieller Sets | hoch | MVP nur generische Textmodelle |
| Lizenzverletzung bei Teilen | mittel | LDraw Attribution und Lizenzprüfung |
| falsche Erwartung perfekter Modelle | hoch | klare MVP-Kommunikation |

---

## 33. Qualitätsdefinition MVP 1

Ein generiertes Modell gilt als MVP-tauglich, wenn:

- es aus unterstützten Teilen besteht
- es eine erkennbare Interpretation des Prompts ist
- es keine kritischen Validierungsfehler enthält
- es als LDraw/MPD/LDR exportierbar ist
- es eine nachvollziehbare Teileliste besitzt
- es eine einfache Schritt-für-Schritt-Anleitung besitzt
- es keine externen AI-Kosten verursacht

Ein Modell muss im MVP nicht:

- optisch perfekt sein
- professionell gerendert sein
- vollständig physikalisch simuliert sein
- mit offiziellen LEGO-Anleitungen vergleichbar sein
- alle Details des Prompts exakt erfüllen

---

## 34. Erweiterungspfad nach MVP 1

Nach erfolgreichem MVP 1 können folgende Erweiterungen umgesetzt werden:

### MVP 2: Bestehendes Modell modifizieren

- Import LDraw/MPD
- Baugruppen erkennen
- einfache Änderungen per Prompt
- Teile-Differenzliste

### MVP 3: Bild zu stilisiertem Gebäude

- Bildanalyse
- Gebäudefassade extrahieren
- vereinfachtes Brick-Modell erzeugen

### MVP 4: 3D-Modell zu Brick-Modell

- Mesh-Import
- Voxelisierung
- Brickification
- Validierung

### MVP 5: Premium-Anleitungen

- LPub3D-Pipeline
- Render je Schritt
- Callouts
- Submodelle
- professionelle PDFs

---

## 35. Offene Entscheidungen

Vor Entwicklungsstart zu entscheiden:

1. WPF oder Blazor als MVP-Frontend?
2. C#-only oder zusätzlicher Python-Service?
3. SQLite oder PostgreSQL für MVP?
4. PDF direkt im MVP oder erst Markdown/HTML?
5. Welche 50–100 Teile werden initial unterstützt?
6. Welche lokalen AI-Modelle werden als Standard ausgeliefert?
7. Wird LPub3D nur empfohlen oder automatisiert angebunden?
8. Wie streng soll die erste Validierung sein?

---

## 36. Empfehlung zur Umsetzung

Für einen wirtschaftlichen und kontrollierbaren MVP wird folgende Variante empfohlen:

```text
Frontend:
Blazor Web App lokal

Backend:
ASP.NET Core API

AI:
Ollama lokal auf Windows mit RTX 4090

Kern:
C# BrickGraph Engine

Daten:
SQLite + lokales Dateisystem

Teile:
reduzierte lokale LDraw-basierte Supported-Parts-Liste

Export:
LDraw/MPD/LDR + CSV + Markdown
PDF optional über HTML-Konvertierung oder später LPub3D
```

Diese Variante erfüllt die Grundsätze:

- lokale AI
- tiefe Betriebskosten
- geringe Lizenzabhängigkeit
- offene Exportformate
- kontrollierbare MVP-Komplexität
- gute Grundlage für spätere Bild-/3D-/Modifikationsfunktionen

---

## 37. Quellenhinweise

- Ollama Windows-Dokumentation: https://docs.ollama.com/windows
- Ollama GPU-Dokumentation: https://docs.ollama.com/gpu
- LDraw Legal Info: https://www.ldraw.org/legal-info
- LDraw Library: https://library.ldraw.org/
- LPub3D Projektseite: https://trevorsandy.github.io/lpub3d/
- BrickLink Studio Software License Agreement: https://studiohelp.bricklink.com/hc/en-us/articles/6606313426711-Studio-Software-License-Agreement
- BrickLink Studio Instruction Maker: https://studiohelp.bricklink.com/hc/en-us/articles/5626403887511-Introduction-to-instructions-maker

---

## 38. Schlussbemerkung

MVP 1 soll nicht beweisen, dass beliebige reale Objekte sofort perfekt als Brick-Bauanleitung erzeugt werden können. Er soll beweisen, dass die zentrale technische Kette funktioniert:

```text
Textbeschreibung
  -> strukturierter Modellplan
  -> BrickGraph
  -> Validierung
  -> LDraw/MPD/LDR
  -> Teileliste
  -> einfache Bauanleitung
```

Damit entsteht die notwendige Basis für spätere, deutlich anspruchsvollere Funktionen wie reale Objektanalyse, 3D-Scan-Verarbeitung, bestehende Modellmodifikation und Premium-Bauanleitungen.
