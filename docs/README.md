# Exloris 2 documentation

This folder is the technical reference for Exloris 2. It describes the Unity project as it is currently structured, including the newer namespaced boat feature and the older scene-bound menu and Nilometer components.

## Start here

| Document | Use it when you need to… |
| --- | --- |
| [Architecture](architecture.md) | Understand runtime layers, module boundaries, and migration constraints. |
| [Scene and feature reference](scene-reference.md) | Open the correct scene, find feature ownership, or repair Inspector wiring. |
| [Gameplay flows](gameplay-flows.md) | Understand the player journey, boat state machine, Nilometer quiz, or runtime pipelines. |
| [Script reference](script-reference.md) | Find the responsibility, namespace, and dependencies of a project-owned C# script. |
| [Events and data](events-and-data.md) | Publish/consume typed events or work with settings, save data, and localization. |
| [Integration and configuration](integration-and-configuration.md) | Add a level, question, scene, audio entry, localized content, or UI binding safely. |
| [Development guide](development.md) | Set up Unity, validate features, test, build, and maintain the documentation. |

## Diagram sources

The [diagrams](diagrams/) directory contains standalone Mermaid (`.mmd`) source scripts. GitHub renders the matching `mermaid` blocks in the Markdown documents; the standalone files can be pasted into [Mermaid Live Editor](https://mermaid.live/) or imported by other documentation tooling.

| Source script | Diagram |
| --- | --- |
| [system-overview.mmd](diagrams/system-overview.mmd) | Runtime layers and integration points. |
| [player-route.mmd](diagrams/player-route.mmd) | Enabled build-scene route. |
| [boat-level-state.mmd](diagrams/boat-level-state.mmd) | Boat-level state machine. |
| [boat-gameplay-loop.mmd](diagrams/boat-gameplay-loop.mmd) | Input, collision, feedback, and retry loop. |
| [nilometer-session.mmd](diagrams/nilometer-session.mmd) | Water-rise quiz and lesson sequence. |
| [event-pipeline.mmd](diagrams/event-pipeline.mmd) | Typed `EventBus` publishers and consumers. |
| [save-load-flow.mmd](diagrams/save-load-flow.mmd) | Encrypted save and verified load process. |
| [scene-navigation.mmd](diagrams/scene-navigation.mmd) | Scene transitions and navigation checks. |

## Documentation rules

1. Treat code and serialized scene references as the source of truth; update these documents when they change.
2. Update both an embedded Mermaid block and its matching `.mmd` file when a documented flow changes.
3. Label assumptions or unverified scene bindings clearly instead of presenting them as confirmed behavior.
4. Do not include secret values, credentials, or `SaveSecrets.cs` contents in documentation.
