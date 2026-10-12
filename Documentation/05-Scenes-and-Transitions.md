# Scenes and transitions

## Build scene list

Evidence: `ProjectSettings/EditorBuildSettings.asset`. There are no separate Build Profile files in the inspected project tree.

| Configured position | Scene path | Enabled | Notes |
| ---: | --- | --- | --- |
| 0 | `Assets/Exloris/Scenes/Bootstrap.unity` | No | Scene file is missing at this path. |
| 1 | `Assets/Exloris/Scenes/FoundationHarness.unity` | No | Scene file is missing at this path. |
| 2 | `Assets/Scenes/main menu.unity` | Yes | First enabled scene. |
| 3 | `Assets/Scenes/player.unity` | Yes | Avatar/player selection. |
| 4 | `Assets/Scenes/Map.unity` | Yes | Map navigation. |
| 5 | `Assets/Scenes/boat/BoatLevel.unity` | Yes | Namespaced three-lane boat challenge. |
| 6 | `Assets/Scenes/NiloMeter/NiloMeter.unity` | Yes | Nilometer learning scene. |

No profile-specific scene differences were found because no standalone Build Profile asset was found. Project settings contain platform-specific configuration for Android, iPhone, Standalone, and WebGL; that does not prove equivalent build outputs.

## All checked-in scene files

| Scene path | Build inclusion | Serialized project-script evidence and role |
| --- | --- | --- |
| `Assets/Scenes/main menu.unity` | Enabled | Scene organization/navigation, save manager, legacy sound manager, manager façade, event audio, and menu presentation components. |
| `Assets/Scenes/player.unity` | Enabled | Player component, avatar and selection UI, save manager, localization/instruction helpers, options UI. |
| `Assets/Scenes/Map.unity` | Enabled | Scene/manager façade, language switcher, hover components, map UI. |
| `Assets/Scenes/boat/BoatLevel.unity` | Enabled | Namespaced BoatLevel flow/input/controller/camera/VFX, boat UI/dialogue/stars, character expression. |
| `Assets/Scenes/NiloMeter/NiloMeter.unity` | Enabled | Nilometer UI, water operator/animator, Tail question components, shared dialogue, avatar/expression, stars, save/audio/options helpers. |
| `Assets/Scenes/boat/Boat.unity` | Not listed | Legacy global BoatController, background movement, hit detector, score/spawning path. |
| `Assets/Scenes/boat/BoatHistory.unity` | Not listed | Title/canvas/camera content; default continuation string points here but scene is not enabled. |
| `Assets/Scenes/SampleScene.unity` | Not listed | Camera and 2D light sample. |
| `Assets/Scenes/mouseHover.unity` | Not listed | Hover UI/world interaction sample. |

Scene YAML GUID matches prove that script components are serialized in these files. They do not establish active state, valid references, runtime instantiation, successful entry, or callback execution. Prefab nesting and all UnityEvent target methods require Inspector/runtime verification.

## Navigation mechanisms

`Assets/scripts/SceneManager.cs` wraps Unity scene loads by name or build index and offers CanvasGroup fade overloads using async loads. It is a persistent singleton. `ManagersDetector` forwards serialized/UI calls to this manager. Menu/player/map navigation also depends on scene button UnityEvents and string/index targets.

`BoatLevelFlow.ReplayLevel` reloads the active build index. `LoadNextSection` loads `_nextSceneName` by name. The boat UI's Next handler currently only sends tap SFX; its flow call is commented. The default `BoatHistory` target is not listed as enabled, so this path needs configuration before it can be relied on.

## Scene-local setup references

- **BoatLevel:** assign `BoatLevelFlow` references for boat, input, camera, and dock. `BoatLevelUI` requires its screens, buttons, dialogue, HUD, progress, and stars. Boat collisions require Rigidbody2D/collider configuration. `BoatInput` requires an EventSystem for UI exclusion.
- **Nilometer:** `WaterOperator` needs a `WaterAnimator` and level entries. `NilometerUI` needs dialogue, question panels, button/event wiring, star displays, and avatar character references. Level/answer indexes must match.
- **Audio:** `AudioService` needs sources, mixer exposed parameters, snapshots, clips and an entry for each used SFX ID. Scene-specific legacy `SoundManager` may coexist.
- **Scene transitions:** scene names must match enabled Build Settings entries. Replay depends on active scene build index.

## Profile and runtime limitations

`EditorBuildSettings.asset` includes Addressables and Input System configuration object references. No distinct profile files were located. Static project data cannot confirm the selected Editor platform, active profile in an external build pipeline, or whether all scene destinations are valid in a produced build.
