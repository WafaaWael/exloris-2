# Project structure

This page describes the checked-out repository layout at a useful system level. It does not inventory every imported art file or third-party example asset.

## Root layout

| Path | Purpose / observed contents |
| --- | --- |
| `Assets/Scenes/` | 9 Unity scenes: 5 enabled build scenes and 4 other scenes/prototypes. |
| `Assets/scripts/` | 55 project-owned runtime C# files, including gameplay, UI, persistence, localization, and legacy helpers. |
| `Assets/Editor/` | Project-owned editor/build helper `WebGLBuildCli.cs`. |
| `Assets/Exloris/` | Newer target-oriented content/runtime/editor folder layout. It contains localization assets and many runtime/core folder placeholders; the active gameplay C# inventory remains primarily under `Assets/scripts`. |
| `Assets/prefabs/` | 26 project prefabs found at the time of inventory, including audio, UI, coin, obstacle, island, avatar, and manager-related names. Actual use varies by scene. |
| `Assets/AddressableAssetsData/` | Addressable settings, groups, schemas, and builders; localization-related groups are present. |
| `Assets/Exloris/Content/Localization/` | English/Arabic locale, string table, audio table, and localization settings assets. |
| `Assets/Animation/` | Avatar sprite animation content. |
| `Assets/Audio/` | Ambience, music, SFX, and voice content. |
| `Assets/Resources/` | `DOTweenSettings.asset`; this is configuration, not evidence of a project runtime `Resources.Load` call. |
| `Assets/StreamingAssets/Exloris/Intro/` | `intro-16x9.mp4` is present. Runtime use was not proven from inspected project-owned scripts. |
| `Assets/Settings/` | URP, renderer, and post-processing settings assets. |
| `Assets/Plugins/`, `Assets/RTLTMPro/`, `Assets/AnimationSequencer/`, `Assets/Events 2.0 for Unity/`, `Assets/Cainos/` | Third-party plugins, samples, and imported art/tool content. |
| `Packages/` | `manifest.json` and lock file declare Unity packages and Git package dependencies. |
| `ProjectSettings/` | Unity editor, player, graphics, quality, input, and build scene configuration. |
| `Builds/`, `Logs/`, `Recordings/`, `UserSettings/` | Local build/log/recording/editor data; not treated as source architecture. |

## Project-owned code organization

```text
Assets/scripts/
  Boat/                         Legacy boat scripts
  CoreData/                     Boat events, settings, dialogue, service/event bus
  CoreGameSystems/Audio/         Event-driven audio service
  GameplaySystems/BoatLevel/     Namespaced boat gameplay
  Hover/                        UI/world pointer feedback
  Localization/                 Locale and instruction helpers
  Player/                       Player data and avatar components
  Presentation/BoatLevelUI/      Boat UI and feedback
  Presentation/SharedUI/         Shared dialogue, options, Nilometer and expressions
  Save system/                  Save coordinator, serializer, local key provider
  audio/                        Legacy SoundManager
```

## Scene files

| Scene | Build state | Observed role / attached project components |
| --- | --- | --- |
| `Assets/Scenes/main menu.unity` | Enabled | Menu/entry scene. Serialized components include scene organization, scene manager, save manager, legacy sound manager, manager façade, and event-driven audio service. |
| `Assets/Scenes/player.unity` | Enabled | Player selection and locale/instruction UI. Includes player/avatar selection, save, localization, and presentation components. |
| `Assets/Scenes/Map.unity` | Enabled | Map UI with scene/manager and localization/hover helpers. |
| `Assets/Scenes/boat/BoatLevel.unity` | Enabled | Namespaced boat gameplay flow, controller, input, camera, VFX, and boat presentation. |
| `Assets/Scenes/NiloMeter/NiloMeter.unity` | Enabled | Water quiz/lesson, dialogue, avatar expressions, stars, save/avatar components, and legacy audio. |
| `Assets/Scenes/boat/Boat.unity` | Not in current build list | Older boat implementation with global `BoatController`, `BoatBackground`, spawner, score, and hit detector scripts. |
| `Assets/Scenes/boat/BoatHistory.unity` | Not in current build list | Title/canvas/camera content. Its name is referenced as the boat flow's default next scene, but it is not an enabled build scene. |
| `Assets/Scenes/SampleScene.unity` | Not in current build list | Minimal camera and 2D light scene. |
| `Assets/Scenes/mouseHover.unity` | Not in current build list | Hover interaction sample/prototype. |

Scene attachment claims above come from matching serialized `m_Script` GUIDs to project script `.meta` files. They do not prove a component is enabled, its references are valid, or its callbacks execute in a built game.

## Generated, imported, and authored content

Unity folders such as `Library`, `Temp`, and cache outputs are generated and are not used to infer game architecture. Imported plugin code and samples are distinguished from project-owned scripts. The authored asset catalog and imported content under `Assets/Exloris/Content` indicate an asset organization effort; a directory name alone does not prove runtime use.

## Inspection limits

Prefab inventory was performed by file path and serialized script-reference search, not by opening the Unity Inspector. Nested prefab behavior and overrides were not executed. Addressables groups were inventoried, but no Addressables runtime call was found in inspected project scripts.
