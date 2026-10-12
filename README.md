# Exloris 2

Exloris 2 is a Unity educational game about ancient Egypt. Its enabled build configuration contains a menu, avatar selection, map, three-lane boat journey, and Nilometer learning activity.

## At a glance

| Item | Value |
| --- | --- |
| Engine | Unity `6000.6.0f1` |
| Render pipeline | URP 17.6.0 |
| Language | C# |
| Input | Unity Input System |
| Localization | Unity Localization; English and Arabic assets |
| Primary launch scene | `Assets/Scenes/main menu.unity` |

## Documentation

- [Documentation index](docs/README.md) — complete navigation for the project reference.
- [Architecture](docs/architecture.md) — runtime layers, services, assets, and current boundaries.
- [Scene and feature reference](docs/scene-reference.md) — build order, scene responsibilities, and script ownership.
- [Gameplay flows](docs/gameplay-flows.md) — rendered Mermaid diagrams and player journeys.
- [Script reference](docs/script-reference.md) — project-owned runtime scripts, responsibilities, and integration notes.
- [Integration and configuration](docs/integration-and-configuration.md) — scene setup, Nilometer levels, audio, localization, persistence, and typed events.
- [Flowchart sources](docs/diagrams/) — standalone Mermaid (`.mmd`) scripts.
- [Event and data reference](docs/events-and-data.md) — typed events, persistence, localization, and contracts.
- [Development guide](docs/development.md) — setup, validation, testing, builds, and conventions.

## Open and run

1. Install **Unity 6000.6.0f1** using Unity Hub.
2. Clone this repository and open its root folder in Unity Hub.
3. Allow Package Manager to resolve packages. Some packages use Git URLs, so the initial import needs network access and Git available to Unity.
4. Confirm the enabled scene order described in [the scene reference](docs/scene-reference.md#enabled-build-scenes).
5. Open `Assets/Scenes/main menu.unity` and press Play.

Do not commit generated Unity directories (`Library`, `Temp`, `Logs`, `Builds`, and similar); the root `.gitignore` covers them.

## Boat-level controls

- **Keyboard:** `A` / Left Arrow moves left; `D` / Right Arrow moves right.
- **Pointer or touch:** swipe horizontally, or tap the left/right half of a non-UI area.

Controls only become active once the boat-level tutorial starts the run.

## Repository orientation

```text
Assets/
  Scenes/                    Enabled game scenes and older prototypes
  scripts/                   Current gameplay, UI, data, and legacy components
    CoreData/                Event contracts, settings, dialogue data, service lookup
    CoreGameSystems/Audio/   Event-driven audio service
    GameplaySystems/         Boat gameplay
    Presentation/            Boat UI, dialogue, Nilometer UI, expressions, options
  Exloris/                   Target-oriented content/runtime structure; mostly content and placeholders today
  Plugins/                   Third-party plugins, including DOTween
  RTLTMPro/                  Right-to-left TextMeshPro support
Packages/                    Unity package manifest and lock file
ProjectSettings/             Editor, render, input, and build configuration
docs/                        Project documentation and Mermaid source diagrams
```

## Working conventions

- Prefer namespaced `BoatGame.*` modules for new boat gameplay work.
- Use `EventBus` for boat/audio/UI cross-layer notifications instead of adding broad singleton dependencies.
- Keep scene references serialized and validate them in the target scene before committing.
- `Assets/scripts/Save system/SaveSecrets.cs` is a local secret and intentionally ignored; never add it to Git.
- Update the matching document and Mermaid source when a player flow, event contract, build scene, or system boundary changes.

## Current implementation note

The repository contains both newer namespaced systems (notably the boat level) and older scene-specific components. A few class names are duplicated across the two areas, including two `BoatController` implementations and two dialogue panels. Use fully qualified names or the established scene binding when touching those features. See [architecture notes](docs/architecture.md#current-architecture-notes) before reorganizing them.
