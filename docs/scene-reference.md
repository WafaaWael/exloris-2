# Scene and feature reference

## Enabled build scenes

Unity loads these scenes in the enabled order from `ProjectSettings/EditorBuildSettings.asset`.

| Index | Scene | Role |
| ---: | --- | --- |
| 0 | `Assets/Scenes/main menu.unity` | Launch/menu entry point. |
| 1 | `Assets/Scenes/player.unity` | Player avatar selection. |
| 2 | `Assets/Scenes/Map.unity` | Map/navigation stage. |
| 3 | `Assets/Scenes/boat/BoatLevel.unity` | Namespaced three-lane boat level. |
| 4 | `Assets/Scenes/NiloMeter/NiloMeter.unity` | Water-level, quiz, and lesson experience. |

Other scenes are prototypes, sample content, or third-party demos. They are not player-navigation targets unless deliberately enabled.

## Feature ownership

| Feature | Primary components | Notes |
| --- | --- | --- |
| Scene transition | `SceneManager` | Persistent singleton; direct or CanvasGroup-faded loads via DOTween. |
| Avatar selection | `PlayerManager`, `PlayerAvatarSelector`, `PlayerUISelector`, `PlayerData` | Provides traveler selection to Nilometer presentation. |
| Boat session | `BoatLevelFlow`, `BoatController`, `BoatInput` | Flow owns state/score/arrival; controller owns movement/collisions. |
| Boat UI | `BoatLevelUI`, `StarsDisplay`, boat `DialoguePanel` | Intro, tutorial, HUD, arrival, completion, settings. |
| Boat feedback | `BoatLevelVfx`, `CameraFollow`, `GodRaysPulse`, `UiMotes`, `UiPulse` | Reacts to gameplay events. |
| Audio | `AudioService`, `GameSettings`, `OptionsPanel` | Mixer-backed settings and event consumers. |
| Nilometer | `NilometerUI`, `WaterOperator`, `WaterAnimator`, `Tail` | Dialogue, water, questions, stars, lesson cards. |
| Persistence | `SaveManager`, `SaveSystem`, `PlayerData` | Player data under Unity persistent storage. |
| Localization | `LocalizationManager`, `LanguageSwitcher`, Unity tables | Content tables under `Assets/Exloris/Content/Localization`. |

## Boat scene wiring checklist

1. `BoatLevelFlow` references controller, input, camera, dock stop, and next-scene name.
2. Boat has a `Rigidbody2D`, trigger-compatible colliders, visual transform, and renderer; obstacles/coins have trigger colliders and components.
3. `BoatLevelUI` can resolve its flow and all screens, dialogue, HUD, and button references are populated.
4. `AudioService` has its sources, mixer parameters, snapshots, clips, and entries for used `SfxId` values.
5. `BoatLevelVfx` has particle systems, camera follow, and celebration point.
6. An `EventSystem` exists for UI-aware pointer input.

## Nilometer scene wiring checklist

1. `WaterOperator` has a `WaterAnimator` and one or more `WaterLevel` entries.
2. `NilometerUI` has dialogue, avatar presentation, question `Tail` panels, star displays, and completion presentation.
3. Each question callback passes the level and answer indices expected by `NilometerUI.SubmitAnswer`.
4. Dialogue lines and optional mood arrays are aligned by index; voice clips are import-compatible with timeline preload when synchronization is wanted.
5. The scene includes audio/event consumers for feedback SFX and voice.

## Navigation contract

The boat flow reloads its active scene for replay and loads serialized `_nextSceneName` for continuation. After renaming a scene, update both the Build Profiles/Build Settings list and all serialized string references.

At the time of this documentation pass, `BoatLevelFlow` defaults `_nextSceneName` to `BoatHistory`, while `BoatHistory` is not among the enabled build scenes listed above. Confirm the intended continuation destination and align the serialized value/build list before a release.
