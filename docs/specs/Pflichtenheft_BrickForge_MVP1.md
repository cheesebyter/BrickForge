# Pflichtenheft – MVP 1: AI-Agentenbasiertes System zur Erstellung baubarer Brick-Bauanleitungen

## Dokumentinformationen

| Feld | Wert |
| --- | --- |
| Projektname | BrickForge – MVP 1 (Agent-Optimiert) |
| Dokumenttyp | Pflichtenheft |
| Version | 2.0 (AI-Agent Edition) |
| Stand | 30.05.2026 |
| Zielplattform | Windows Workstation mit NVIDIA RTX 4090, Ollama, PostgreSQL (Docker) |
| Primärziel | Textbeschreibung → kleines baubares Brick-Modell → digitale Modell-Datei → einfache Bauanleitung |


## 1 Agenten-Architektur im Überblick

```text
┌─────────────────────────────────────────────────────────────┐
│                    Orchestrator Agent                        │
│  (Koordiniert Workflow, Zustandsmaschine, Fehlerhandling)   │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Prompt      │    │  Template    │    │  BrickPlan   │
│  Analysis    │───▶│  Selection   │───▶│  Agent       │
│  Agent       │    │  Agent       │    │              │
└──────────────┘    └──────────────┘    └──────────────┘
                                                  │
                                                  ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Validation  │◀───│  BrickGraph  │    │  Repair      │
│  Agent       │    │  Generator   │    │  Agent       │
│              │    │  Agent       │    │              │
└──────────────┘    └──────────────┘    └──────────────┘
        │                   │                    │
        └───────────────────┼────────────────────┘
                            ▼
                ┌──────────────────────┐
                │  Export Agent Suite   │
                │  - LDraw Export       │
                │  - CSV Export         │
                │  - Instructions       │
                └──────────────────────┘
```

## 2 Agenten-Kommunikationsprotokoll

2.1 Standardisiertes Nachrichtenformat
Alle Agenten kommunizieren über ein einheitliches Nachrichtenformat:

```json
{
  "message_id": "uuid-v4",
  "timestamp": "2026-05-30T10:00:00Z",
  "from_agent": "PromptAnalysisAgent",
  "to_agent": "Orchestrator",
  "workflow_id": "job_123",
  "message_type": "request|response|error|heartbeat",
  "correlation_id": "previous_message_id",
  "payload": { ... },
  "metadata": {
    "retry_count": 0,
    "priority": "high|normal|low",
    "timeout_seconds": 30
  }
}
2.2 Agenten-Lebenszyklus
Jeder Agent implementiert folgende Zustände:

text
[INITIALIZED] → [READY] → [PROCESSING] → [COMPLETED]
                    ↓              ↓
                 [ERROR]      [TIMEOUT]
                    ↓              ↓
                 [RETRY] ←─────────┘
                    ↓
                 [FAILED]
2.3 Agenten-Registrierung
json
{
  "agent_id": "PromptAnalysisAgent_v1",
  "capabilities": ["text_analysis", "json_extraction", "schema_validation"],
  "input_schema": "prompt_analysis_input.json",
  "output_schema": "prompt_analysis_output.json",
  "required_models": ["qwen2.5-coder:14b"],
  "timeout_seconds": 45,
  "max_retries": 3,
  "health_check_endpoint": "/health/prompt-analysis"
}
```

## 3 Agent 1: PromptAnalysisAgent

3.1 Aufgaben & Verantwortlichkeiten
Extrahiert strukturierte Informationen aus natürlichem Text

Erkennt Modellkategorie, Farben, Features, Komplexität

Ergänzt fehlende Informationen mit Defaultwerten

Bewertet Machbarkeit (innerhalb MVP-Grenzen)

3.2 Eingabe-Schema (JSON Schema)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "required": ["raw_prompt", "workflow_id"],
  "properties": {
    "raw_prompt": {
      "type": "string",
      "minLength": 10,
      "maxLength": 2000,
      "description": "Benutzereingabe in natürlicher Sprache"
    },
    "workflow_id": {
      "type": "string",
      "pattern": "^job_[0-9]{14}_[a-z0-9]{6}$"
    },
    "hints": {
      "type": "object",
      "properties": {
        "target_parts": { "type": "integer", "minimum": 50, "maximum": 300 },
        "colors": { "type": "array", "items": { "type": "string" }, "maxItems": 5 },
        "difficulty": { "type": "string", "enum": ["beginner", "intermediate"] }
      }
    }
  }
}
3.3 Ausgabe-Schema (JSON Schema)
json
{
  "type": "object",
  "required": ["object_type", "model_category", "target_parts", "colors", "confidence"],
  "properties": {
    "object_type": {
      "type": "string",
      "enum": ["appliance", "building", "vehicle", "furniture", "display", "unknown"]
    },
    "model_category": {
      "type": "string",
      "enum": ["small_machine", "small_building", "small_vehicle", "furniture", "display_object"]
    },
    "target_parts": { "type": "integer", "minimum": 50, "maximum": 300 },
    "colors": {
      "type": "array",
      "items": { "type": "string", "enum": ["black", "white", "red", "blue", "yellow", "green", "dark_bluish_gray", "light_bluish_gray", "brown", "transparent_clear"] },
      "maxItems": 8
    },
    "features": {
      "type": "array",
      "items": { "type": "string" },
      "maxItems": 10
    },
    "difficulty": { "type": "string", "enum": ["beginner", "intermediate"] },
    "stability_priority": { "type": "string", "enum": ["high", "medium", "low"], "default": "medium" },
    "estimated_complexity": { "type": "integer", "minimum": 1, "maximum": 10 },
    "feasible": { "type": "boolean" },
    "confidence": { "type": "number", "minimum": 0, "maximum": 1 },
    "warnings": {
      "type": "array",
      "items": { "type": "string" }
    }
  }
}
3.4 LLM-Prompt-Template
text
System: Du bist ein Analyse-Agent für Brick-Modelle. Extrahiere aus der Benutzereingabe folgende Informationen als JSON.
Antworte NUR mit gültigem JSON, kein erklärender Text.

Analyse-Regeln:
- object_type: appliance, building, vehicle, furniture, display, unknown
- model_category: small_machine, small_building, small_vehicle, furniture, display_object
- target_parts: Wenn nicht genannt → 180
- colors: Erkenne genannte Farben, max 8
- features: Liste erkannter Merkmale (z.B. "portafilter", "cup", "steam_wand")
- difficulty: beginner (Standard) wenn "einfach", "anfänger", sonst intermediate
- stability_priority: high wenn "stabil" genannt
- feasible: false wenn Modell zu komplex (Technic, >300 Teile, organische Formen)
- confidence: 0.0-1.0 basierend auf Klarheit der Beschreibung

Benutzer-Eingabe: {{prompt}}

Ausgabe-Schema: {{output_schema}}
3.5 Fehlerbehandlung
Fehler	Aktion
LLM antwortet nicht in JSON	Retry mit niedrigerer Temperatur (0.1)
Schema-Validierung fehlschlägt	Verwende Defaultwerte + logge Warnung
Extraktion zu unsicher (confidence < 0.4)	Frage Benutzer nach Klarstellung (UI)
feasible = false	Breche ab mit spezifischer Fehlermeldung
```

## 4 Agent 2: TemplateSelectionAgent

4.1 Aufgaben
Wählt basierend auf model_category das passende Template

Berücksichtigt verfügbare Teilebibliothek

Passt Template-Parameter an spezifische Anforderungen an

4.2 Eingabe-Schema
```json
{
  "type": "object",
  "required": ["model_category", "target_parts", "features"],
  "properties": {
    "model_category": { "type": "string" },
    "target_parts": { "type": "integer" },
    "features": { "type": "array" },
    "colors": { "type": "array" },
    "stability_priority": { "type": "string" }
  }
}
4.3 Ausgabe-Schema
json
{
  "type": "object",
  "required": ["template_name", "template_version", "adapted_parameters", "matching_score"],
  "properties": {
    "template_name": {
      "type": "string",
      "enum": ["small_machine_template", "small_building_template", "small_vehicle_template", "furniture_template", "display_object_template"]
    },
    "template_version": { "type": "string", "pattern": "^\\d+\\.\\d+$" },
    "adapted_parameters": {
      "type": "object",
      "properties": {
        "width": { "type": "integer", "minimum": 4, "maximum": 32 },
        "depth": { "type": "integer", "minimum": 4, "maximum": 32 },
        "height": { "type": "integer", "minimum": 3, "maximum": 25 },
        "subassembly_budgets": { "type": "object" }
      }
    },
    "matching_score": { "type": "number", "minimum": 0, "maximum": 1 },
    "selected_fallback": { "type": "boolean", "default": false }
  }
}
4.4 Template-Matching-Logik (Regelbasiert)
python
def select_template(model_category, features, target_parts):
    templates = load_all_templates()
    
    # Regel-basiertes Matching (kein LLM im MVP für diese Aufgabe)
    if model_category == "small_machine":
        template = templates["small_machine_template"]
        # Passe Dimensionen an Features an
        if "portafilter" in features:
            template.width = max(template.width, 8)
    elif model_category == "small_building":
        template = templates["small_building_template"]
        # Gebäude brauchen solide Basis
        template.subassembly_budgets["base"] *= 1.2
    else:
        template = templates["display_object_template"]
        template.selected_fallback = True
    
    # Skaliere Template basierend auf target_parts
    scale_factor = target_parts / template.default_parts
    template.scale_dimensions(scale_factor)
    
    return template
```

## 5 Agent 3: BrickPlanAgent

5.1 Aufgaben
Plant die Baugruppen und verteilt das Teilbudget

Definiert grobe Abmessungen

Schlägt Detail-Elemente vor

5.2 Eingabe-Schema
```json
{
  "type": "object",
  "required": ["template", "features", "colors", "target_parts"],
  "properties": {
    "template": { "type": "object" },
    "features": { "type": "array" },
    "colors": { "type": "array" },
    "target_parts": { "type": "integer" }
  }
}
5.3 Ausgabe-Schema
json
{
  "type": "object",
  "required": ["subassemblies", "dimensions", "color_assignments", "part_budget_distribution"],
  "properties": {
    "subassemblies": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["name", "parts_budget", "position", "priority"],
        "properties": {
          "name": { "type": "string" },
          "parts_budget": { "type": "integer", "minimum": 5 },
          "position": { "type": "string", "enum": ["bottom", "middle", "top", "front", "back", "sides"] },
          "priority": { "type": "integer", "minimum": 1, "maximum": 5 }
        }
      }
    },
    "dimensions": {
      "type": "object",
      "required": ["width", "depth", "height"],
      "properties": {
        "width": { "type": "integer", "minimum": 4, "maximum": 32 },
        "depth": { "type": "integer", "minimum": 4, "maximum": 32 },
        "height": { "type": "integer", "minimum": 3, "maximum": 25 }
      }
    },
    "color_assignments": {
      "type": "object",
      "patternProperties": {
        ".*": { "type": "string" }
      }
    },
    "part_budget_distribution": {
      "type": "object",
      "properties": {
        "bricks": { "type": "number", "minimum": 0, "maximum": 1 },
        "plates": { "type": "number", "minimum": 0, "maximum": 1 },
        "tiles": { "type": "number", "minimum": 0, "maximum": 1 },
        "slopes": { "type": "number", "minimum": 0, "maximum": 1 },
        "details": { "type": "number", "minimum": 0, "maximum": 1 }
      }
    }
  }
}
5.4 Budget-Verteilungs-Algorithmus
python
def distribute_part_budget(target_parts, subassemblies, template):
    budget_remaining = target_parts
    
    # Priorisiere Subassemblies basierend auf priority
    sorted_assemblies = sorted(subassemblies, key=lambda x: x["priority"], reverse=True)
    
    for assembly in sorted_assemblies:
        # Standard-Budget aus Template
        default_budget = template.subassembly_budgets.get(assembly["name"], 20)
        
        # Passe an basierend auf target_parts
        allocated = min(default_budget, budget_remaining - (len(sorted_assemblies) * 5))
        assembly["parts_budget"] = max(allocated, 5)
        budget_remaining -= allocated
    
    # Verbleibendes Budget auf Hauptbaugruppe (main_body)
    if budget_remaining > 0:
        main_assembly = next(a for a in subassemblies if a["name"] == "main_body")
        main_assembly["parts_budget"] += budget_remaining
    
    return subassemblies
```

## 6 Agent 4: BrickGraphGeneratorAgent

6.1 Aufgaben
Generiert konkrete Teil-Instanzen

Platziert Teile im 3D-Raster

Erzeugt Verbindungen

Erstellt erste Bauschritte

6.2 Eingabe-Schema
```json
{
  "type": "object",
  "required": ["plan", "template", "supported_parts"],
  "properties": {
    "plan": { "type": "object" },
    "template": { "type": "object" },
    "supported_parts": {
      "type": "object",
      "properties": {
        "bricks": { "type": "array" },
        "plates": { "type": "array" },
        "tiles": { "type": "array" },
        "slopes": { "type": "array" }
      }
    }
  }
}
6.3 Ausgabe-Schema (BrickGraph)
json
{
  "type": "object",
  "required": ["model", "parts", "connections", "steps", "metadata"],
  "properties": {
    "model": {
      "type": "object",
      "required": ["id", "name", "unit", "target_parts", "actual_parts"],
      "properties": {
        "id": { "type": "string" },
        "name": { "type": "string" },
        "unit": { "type": "string", "enum": ["ldraw", "stud"] },
        "target_parts": { "type": "integer" },
        "actual_parts": { "type": "integer" }
      }
    },
    "parts": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["instance_id", "part_number", "color", "position", "rotation", "step"],
        "properties": {
          "instance_id": { "type": "string", "pattern": "^part_[0-9]{3}$" },
          "part_number": { "type": "string", "pattern": "^[0-9]{4,5}$" },
          "color": { "type": "string" },
          "position": { "type": "array", "minItems": 3, "maxItems": 3, "items": { "type": "integer" } },
          "rotation": { "type": "array", "minItems": 9, "maxItems": 9, "items": { "type": "number" } },
          "step": { "type": "integer", "minimum": 1, "maximum": 60 },
          "subassembly": { "type": "string" }
        }
      }
    },
    "connections": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["from", "to", "type"],
        "properties": {
          "from": { "type": "string" },
          "to": { "type": "string" },
          "type": { "type": "string", "enum": ["stud_tube", "plate_stud", "technic_pin", "clip_bar"] },
          "confidence": { "type": "number", "minimum": 0, "maximum": 1 }
        }
      }
    },
    "steps": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["step_number", "part_ids", "description"],
        "properties": {
          "step_number": { "type": "integer" },
          "part_ids": { "type": "array", "items": { "type": "string" } },
          "description": { "type": "string" }
        }
      }
    },
    "metadata": {
      "type": "object",
      "properties": {
        "generation_time_ms": { "type": "integer" },
        "template_used": { "type": "string" },
        "llm_calls": { "type": "integer" }
      }
    }
  }
}
6.4 Generierungs-Logik (Pseudo-code)
python
def generate_brickgraph(plan, template, supported_parts):
    brickgraph = BrickGraph()
    
    # 1. Basisplatte (immer)
    base_plate = create_base_plate(plan.dimensions.width, plan.dimensions.depth)
    brickgraph.add_parts(base_plate)
    
    # 2. Baugruppen in der richtigen Reihenfolge
    for subassembly in plan.subassemblies:
        if subassembly.position == "bottom":
            generate_bottom_layer(brickgraph, subassembly, plan)
        elif subassembly.position == "middle":
            generate_middle_layers(brickgraph, subassembly, plan)
        elif subassembly.position == "top":
            generate_top_layer(brickgraph, subassembly, plan)
        elif subassembly.position == "front":
            generate_front_details(brickgraph, subassembly, plan)
    
    # 3. Details & Features
    for feature in plan.features:
        add_feature(brickgraph, feature, plan.color_assignments)
    
    # 4. Bauschritte generieren (bottom-up)
    brickgraph.generate_steps()
    
    # 5. Verbindungen berechnen
    brickgraph.calculate_connections()
    
    return brickgraph
```

## 7 Agent 5: ValidationAgent

7.1 Aufgaben
Prüft Modell auf Baubarkeit

Klassifiziert Fehler (High/Medium/Low)

Berechnet Validierungs-Score

7.2 Eingabe-Schema
```json
{
  "type": "object",
  "required": ["brickgraph", "validation_rules"],
  "properties": {
    "brickgraph": { "type": "object" },
    "validation_rules": {
      "type": "array",
      "items": { "type": "string" }
    },
    "strict_mode": { "type": "boolean", "default": true }
  }
}
7.3 Ausgabe-Schema
json
{
  "type": "object",
  "required": ["valid", "score", "issues", "statistics"],
  "properties": {
    "valid": { "type": "boolean" },
    "score": { "type": "number", "minimum": 0, "maximum": 1 },
    "issues": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["id", "severity", "type", "message", "affected_parts"],
        "properties": {
          "id": { "type": "string" },
          "severity": { "type": "string", "enum": ["high", "medium", "low"] },
          "type": { "type": "string" },
          "message": { "type": "string" },
          "affected_parts": { "type": "array", "items": { "type": "string" } },
          "suggested_fix": { "type": "string" }
        }
      }
    },
    "statistics": {
      "type": "object",
      "properties": {
        "total_parts": { "type": "integer" },
        "unique_parts": { "type": "integer" },
        "high_severity_count": { "type": "integer" },
        "medium_severity_count": { "type": "integer" },
        "low_severity_count": { "type": "integer" },
        "connection_density": { "type": "number" },
        "color_harmony_score": { "type": "number" }
      }
    }
  }
}
7.4 Validierungs-Regeln (JSON)
json
{
  "rules": [
    {
      "id": "V001",
      "name": "MaxPartsCheck",
      "severity": "high",
      "condition": "brickgraph.parts.count > config.max_parts",
      "message": "Teileanzahl überschreitet Limit von {max_parts}",
      "auto_fixable": true,
      "fix_action": "reduce_parts"
    },
    {
      "id": "V002",
      "name": "CollisionCheck",
      "severity": "high",
      "condition": "any part overlaps another",
      "message": "Teil {part_id} überschneidet sich mit Teil {other_id}",
      "auto_fixable": false,
      "fix_action": null
    },
    {
      "id": "V003",
      "name": "FloatingPartCheck",
      "severity": "high",
      "condition": "part has no connection and not on ground",
      "message": "Teil {part_id} schwebt ohne Verbindung",
      "auto_fixable": true,
      "fix_action": "remove_or_connect"
    }
  ]
}
```

## 8 Agent 6: RepairAgent

8.1 Aufgaben
Behebt automatisch validierte Fehler

Implementiert fix-Actions aus ValidationAgent

Dokumentiert Reparaturen

8.2 Eingabe-Schema
```json
{
  "type": "object",
  "required": ["brickgraph", "validation_result", "fix_strategies"],
  "properties": {
    "brickgraph": { "type": "object" },
    "validation_result": { "type": "object" },
    "fix_strategies": {
      "type": "array",
      "items": { "type": "string" }
    },
    "max_repair_iterations": { "type": "integer", "default": 3 }
  }
}
8.3 Ausgabe-Schema
json
{
  "type": "object",
  "required": ["repaired_brickgraph", "repairs_applied", "still_unresolved", "repair_log"],
  "properties": {
    "repaired_brickgraph": { "type": "object" },
    "repairs_applied": {
      "type": "array",
      "items": {
        "type": "object",
        "required": ["issue_id", "action", "success", "details"],
        "properties": {
          "issue_id": { "type": "string" },
          "action": { "type": "string" },
          "success": { "type": "boolean" },
          "details": { "type": "string" }
        }
      }
    },
    "still_unresolved": {
      "type": "array",
      "items": { "type": "string" }
    },
    "repair_log": { "type": "string" }
  }
}
8.4 Fix-Strategien
python
FIX_STRATEGIES = {
    "reduce_parts": lambda bg: remove_low_priority_subassemblies(bg),
    "remove_or_connect": lambda bg, part_id: find_connection_point(bg, part_id) or remove_part(bg, part_id),
    "replace_color": lambda bg, part_id, old_color, new_color: change_part_color(bg, part_id, new_color),
    "reinforce_wall": lambda bg, wall_parts: add_plate_reinforcement(bg, wall_parts),
    "redistribute_budget": lambda bg, exceeded_by: scale_down_model(bg, exceeded_by)
}
```

## 9 Agent 7: ExportAgentSuite

9.1 Sub-Agenten
| Agent | Aufgabe | Ausgabeformat |
| --- | --- | --- |
| LDrawExporter | Konvertiert BrickGraph nach LDraw | .mpd, .ldr |
| CSVExporter | Erzeugt Teileliste | .csv, .json |
| InstructionExporter | Erzeugt Bauanleitung | .md, .html, .pdf |
| ReportExporter | Erzeugt Generierungsbericht | .md |

9.2 LDrawExporter - Spezifikation
Eingabe: BrickGraph, LDraw-Mappings
Ausgabe: LDraw-kompatible Datei

```json
{
  "type": "object",
  "required": ["brickgraph", "part_mappings", "ldraw_header"],
  "properties": {
    "brickgraph": { "type": "object" },
    "part_mappings": {
      "type": "object",
      "description": "Map von internen Teilenummern zu LDraw-Nummern"
    },
    "ldraw_header": {
      "type": "object",
      "properties": {
        "author": { "type": "string", "default": "BrickForge MVP 1.0" },
        "license": { "type": "string", "default": "CC BY 2.0" },
        "generated_by": { "type": "string" }
      }
    }
  }
}
LDraw-Header-Template:

ldr
0 FILE model.mpd
0 BrickForge generated model
0 Name: model.mpd
0 Author: BrickForge MVP 1.0 (AI-generated)
0 !LICENSE Redistributable under CC BY 2.0
0 !HISTORY Generated at {{timestamp}}
0 
9.3 InstructionExporter - Schritt-für-Schritt
python
def generate_instructions(brickgraph, format="markdown"):
    instructions = []
    
    # Titel
    instructions.append(f"# Bauanleitung: {brickgraph.model.name}\n")
    instructions.append(f"**Teileanzahl:** {brickgraph.model.actual_parts}\n")
    
    # Teileliste
    instructions.append("## Teileliste\n")
    for part in brickgraph.get_unique_parts():
        instructions.append(f"- {part.quantity}x {part.name} ({part.color})")
    
    # Bauschritte
    instructions.append("\n## Bauschritte\n")
    for step in brickgraph.steps:
        instructions.append(f"\n### Schritt {step.step_number}\n")
        instructions.append(f"**Neue Teile:** {', '.join(step.part_names)}\n")
        instructions.append(f"{step.description}\n")
    
    if format == "html":
        return convert_markdown_to_html(instructions)
    elif format == "pdf":
        return convert_html_to_pdf(instructions)
    
    return "\n".join(instructions)
```

## 10 Orchestrator Agent (Zentraler Workflow)

10.1 Zustandsmaschine
```python
class Orchestrator:
    def __init__(self):
        self.workflow_states = {
            "initialized": self.handle_initialized,
            "analyzing_prompt": self.handle_analyzing_prompt,
            "selecting_template": self.handle_selecting_template,
            "planning_brick": self.handle_planning_brick,
            "generating_brickgraph": self.handle_generating_brickgraph,
            "validating": self.handle_validating,
            "repairing": self.handle_repairing,
            "exporting": self.handle_exporting,
            "completed": self.handle_completed,
            "failed": self.handle_failed
        }
    
    async def run_workflow(self, job_id, prompt):
        state = "initialized"
        context = {"job_id": job_id, "prompt": prompt}
        
        while state not in ["completed", "failed"]:
            handler = self.workflow_states.get(state)
            result = await handler(context)
            
            if result["success"]:
                state = result["next_state"]
                context.update(result["context"])
            else:
                if context.get("retry_count", 0) < 3:
                    context["retry_count"] = context.get("retry_count", 0) + 1
                    # Verbleibe im gleichen State
                else:
                    state = "failed"
                    context["error"] = result["error"]
            
            # Persistiere Zustand in PostgreSQL
            await self.persist_state(job_id, state, context)
        
        return context
10.2 Agenten-Aufruf mit Retry-Logik
python
async def call_agent_with_retry(agent_name, input_data, max_retries=3):
    for attempt in range(max_retries):
        try:
            # Timeout nach 60 Sekunden
            response = await asyncio.wait_for(
                call_agent(agent_name, input_data),
                timeout=60
            )
            
            # Schema-Validierung
            if validate_against_schema(response, get_output_schema(agent_name)):
                return response
            else:
                logger.warning(f"Schema validation failed for {agent_name}, attempt {attempt+1}")
                
        except asyncio.TimeoutError:
            logger.error(f"Agent {agent_name} timeout, attempt {attempt+1}")
        except Exception as e:
            logger.error(f"Agent {agent_name} error: {e}, attempt {attempt+1}")
        
        # Exponentieller Backoff
        await asyncio.sleep(2 ** attempt)
    
    raise Exception(f"Agent {agent_name} failed after {max_retries} retries")
```

## 11 Agenten-Monitoring & Metriken

11.1 Metrik-Schema
```json
{
  "workflow_id": "job_20260530_001",
  "agent_metrics": [
    {
      "agent_name": "PromptAnalysisAgent",
      "start_time": "2026-05-30T10:00:00Z",
      "end_time": "2026-05-30T10:00:25Z",
      "duration_ms": 25123,
      "llm_calls": 1,
      "tokens_used": 450,
      "retries": 0,
      "success": true,
      "confidence": 0.85
    }
  ],
  "total_duration_ms": 172000,
  "total_llm_calls": 3,
  "total_tokens": 1250,
  "repair_iterations": 1,
  "final_score": 0.82
}
11.2 Health Checks
python
@app.get("/health/agents")
async def health_check():
    agents_status = {}
    
    for agent in registered_agents:
        try:
            response = await call_agent_health(agent.endpoint)
            agents_status[agent.name] = {
                "status": "healthy" if response.status == 200 else "unhealthy",
                "latency_ms": response.latency,
                "last_heartbeat": response.timestamp
            }
        except Exception as e:
            agents_status[agent.name] = {"status": "down", "error": str(e)}
    
    return agents_status
```

## 12 Agenten-Konfiguration (appsettings.json)

```json
{
  "Agents": {
    "PromptAnalysisAgent": {
      "enabled": true,
      "model": "qwen2.5-coder:14b",
      "temperature": 0.2,
      "timeout_seconds": 45,
      "max_retries": 3,
      "input_schema": "schemas/prompt_analysis_input.json",
      "output_schema": "schemas/prompt_analysis_output.json"
    },
    "TemplateSelectionAgent": {
      "enabled": true,
      "mode": "rule_based",
      "fallback_to_llm": false,
      "templates_path": "data/templates/"
    },
    "BrickPlanAgent": {
      "enabled": true,
      "model": "llama3.1:8b",
      "temperature": 0.3,
      "timeout_seconds": 60
    },
    "BrickGraphGeneratorAgent": {
      "enabled": true,
      "mode": "hybrid",
      "max_parts": 300,
      "generation_strategy": "template_based"
    },
    "ValidationAgent": {
      "enabled": true,
      "rules_file": "config/validation_rules.json",
      "strict_mode": true,
      "min_score_threshold": 0.6
    },
    "RepairAgent": {
      "enabled": true,
      "max_iterations": 3,
      "auto_fix_strategies": ["reduce_parts", "remove_or_connect", "replace_color"]
    }
  },
  "Orchestrator": {
    "max_parallel_jobs": 1,
    "job_timeout_minutes": 10,
    "state_persistence": "postgresql",
    "checkpoint_interval_seconds": 5
  }
}
```

## 13 Agenten-Kommunikation (Beispiel)

13.1 Sequence Diagram als Text
```text
Benutzer -> Orchestrator: POST /api/generation-jobs
Orchestrator -> PostgreSQL: INSERT Job (status=queued)
Orchestrator -> PromptAnalysisAgent: Request (raw_prompt)
PromptAnalysisAgent -> Ollama: LLM Call (qwen2.5-coder)
Ollama --> PromptAnalysisAgent: JSON Response
PromptAnalysisAgent --> Orchestrator: Structured Briefing
Orchestrator -> PostgreSQL: UPDATE status=selecting_template

Orchestrator -> TemplateSelectionAgent: Request (model_category)
TemplateSelectionAgent --> Orchestrator: Template Name
Orchestrator -> PostgreSQL: UPDATE status=planning_brick

Orchestrator -> BrickPlanAgent: Request (template, features)
BrickPlanAgent -> Ollama: LLM Call (llama3.1)
Ollama --> BrickPlanAgent: Budget Distribution
BrickPlanAgent --> Orchestrator: Subassembly Plan
Orchestrator -> PostgreSQL: UPDATE status=generating_brickgraph

Orchestrator -> BrickGraphGeneratorAgent: Request (plan)
BrickGraphGeneratorAgent --> Orchestrator: BrickGraph
Orchestrator -> PostgreSQL: UPDATE status=validating

Orchestrator -> ValidationAgent: Request (brickgraph)
ValidationAgent --> Orchestrator: ValidationResult (score=0.75)
Orchestrator -> PostgreSQL: UPDATE status=repairing

Orchestrator -> RepairAgent: Request (brickgraph, issues)
RepairAgent --> Orchestrator: Repaired BrickGraph (1 fix applied)
Orchestrator -> PostgreSQL: UPDATE status=exporting

Orchestrator -> ExportAgentSuite: Request (repaired_brickgraph)
ExportAgentSuite --> Orchestrator: Files (.mpd, .csv, .md)
Orchestrator -> PostgreSQL: UPDATE status=completed

Orchestrator --> Benutzer: 200 OK (download links)
```

## 14 Fehlerbehandlungs-Matrix

| Fehler | Erkennung | Agent | Aktion | Benutzermeldung |
| --- | --- | --- | --- | --- |
| Ollama nicht erreichbar | Health Check | Alle | Retry 3x, dann Fallback | "KI-Server nicht erreichbar. Bitte starten Sie Ollama." |
| LLM liefert ungültiges JSON | Schema-Validierung | Prompt/BrickPlan | Retry mit Temp=0.1 | Keine (automatisch) |
| Validierungsscore < 0.6 | Validation | Repair | Max 3 Reparaturen | "Modell enthält Warnungen. Details im Bericht." |
| Template nicht gefunden | TemplateSelection | Template | Fallback zu display | "Generisches Template verwendet." |
| Export fehlschlägt | Export | Export | BrickGraph speichern | "Export teilweise fehlgeschlagen. BrickGraph verfügbar." |
| Job-Timeout (10min) | Orchestrator | Alle | Job abbrechen, Status failed | "Generierung dauerte zu lange. Bitte versuchen Sie ein kleineres Modell." |

## 15 Entwicklungs-Priorität für Agenten

| Priorität | Agent | Abhängigkeiten | Geschätzte Tage |
| --- | --- | --- | --- |
| P0 | Orchestrator | Keine | 3 |
| P0 | PromptAnalysisAgent | Ollama | 2 |
| P1 | TemplateSelectionAgent | PromptAnalysis | 1 |
| P1 | BrickGraphGeneratorAgent | TemplateSelection | 4 |
| P2 | ValidationAgent | BrickGraph | 2 |
| P2 | RepairAgent | Validation | 2 |
| P3 | ExportAgentSuite | BrickGraph | 3 |
| P4 | BrickPlanAgent | TemplateSelection | 2 (optional) |

## 16 Agenten-Test-Suite

16.1 Unit Test Beispiel
```python
async def test_prompt_analysis_agent():
    agent = PromptAnalysisAgent()
    input_data = {
        "raw_prompt": "Erstelle eine kleine rote Kaffeemaschine mit 150 Teilen",
        "workflow_id": "test_001"
    }
    
    result = await agent.process(input_data)
    
    assert result["object_type"] == "appliance"
    assert result["model_category"] == "small_machine"
    assert "red" in result["colors"]
    assert result["target_parts"] == 150
    assert result["confidence"] > 0.8
16.2 Integration Test Beispiel
python
async def test_full_workflow():
    orchestrator = Orchestrator()
    
    result = await orchestrator.run_workflow(
        job_id="test_full_001",
        prompt="Erstelle einen kleinen blauen Sportwagen"
    )
    
    assert result["final_status"] == "completed"
    assert result["brickgraph"].model.actual_parts <= 300
    assert result["validation_result"]["score"] >= 0.7
    
    # Prüfe Export-Dateien
    output_dir = Path(f"data/outputs/{result['job_id']}")
    assert (output_dir / "model.mpd").exists()
    assert (output_dir / "parts.csv").exists()
    assert (output_dir / "instructions.md").exists()
```


