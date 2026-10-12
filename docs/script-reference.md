# Script reference

This reference covers project-owned runtime scripts under `Assets/scripts`. Imported packages, third-party folders, and generated code are intentionally excluded. Namespaced boat code is the current feature architecture; several global-namespace scripts are older scene-bound components.

## Core data and messaging

| Script | Namespace | Responsibility | Used by |
| --- | --- | --- | --- |
| `CoreData/BoatLevelEvents.cs` | `BoatGame.CoreData` | Defines `LevelState`, `SfxId`, and immutable boat/audio event payloads. | Boat flow, UI, VFX, audio, Nilometer. |
| `CoreData/CharacterMood.cs` | `BoatGame.CoreData` | Defines character expression/mood values. | Dialogue and character presentation. |
| `CoreData/DialogueLine.cs` | `BoatGame.CoreData` | Serializable text/voice dialogue line contract. | Shared dialogue and Nilometer. |
| `CoreData/GameSettings.cs` | `BoatGame.CoreData` | PlayerPrefs-backed music, SFX, voice, VSync, and frame-cap settings. | Options UI and `AudioService`. |
| `CoreData/Messaging/EventBus.cs` | `BoatGame.CoreData.Messaging` | Synchronous, typed, allocation-conscious publish/subscribe bus. | Cross-layer boat and audio communication. |
| `CoreData/Messaging/Service.cs` | `BoatGame.CoreData.Messaging` | Lazily resolves a scene `Component` and caches it. | Boat UI’s `BoatLevelFlow` lookup. |

## Boat-level gameplay

| Script | Namespace | Responsibility | Key API / notes |
| --- | --- | --- | --- |
| `GameplaySystems/BoatLevel/BoatLevelFlow.cs` | `BoatGame.GameplaySystems.BoatLevel` | Authoritative boat session coordinator: state, stars, coins, docking, replay, and continuation. | UI calls `CompleteIntro`, `StartPlaying`, pause/settings, replay, and next-section methods. Publishes state/progress/collision/coin events. |
| `GameplaySystems/BoatLevel/BoatController.cs` | `BoatGame.GameplaySystems.BoatLevel` | Three-lane Rigidbody2D boat movement, collision response, speed ramp, and dock autopilot. | Emits obstacle/coin callbacks to `BoatLevelFlow`; has `StepLane`, `SetProgress`, and `StartAutoPilot`. |
| `GameplaySystems/BoatLevel/BoatInput.cs` | `BoatGame.GameplaySystems.BoatLevel` | Converts keyboard, swipe, and non-UI side taps into lane steps. | Emits `OnLaneStep(-1/+1)` only while `IsInputEnabled`. |
| `GameplaySystems/BoatLevel/Coin.cs` | `BoatGame.GameplaySystems.BoatLevel` | Collectible state and pickup animation. | `Collect()` is guarded by `IsCollected`. |
| `GameplaySystems/BoatLevel/Obstacle.cs` | `BoatGame.GameplaySystems.BoatLevel` | Obstacle behavior and collision feedback hook. | Boat controller determines first/side/forward hit behavior. |
| `GameplaySystems/BoatLevel/CameraFollow.cs` | `BoatGame.GameplaySystems.BoatLevel` | Camera follow, max-height limit, and camera shake. | `BoatLevelFlow` sets `MaxY`; VFX can call `Shake`. |
| `GameplaySystems/BoatLevel/BoatLevelVfx.cs` | `BoatGame.GameplaySystems.BoatLevel` | Subscribes to boat events for splash, impact, pickup, and completion effects. | Requires its particles/camera references in the Inspector. |
| `GameplaySystems/BoatLevel/GodRaysPulse.cs` | `BoatGame.GameplaySystems.BoatLevel` | Pulses visual god-ray feedback. | `Brighten()` is a scene/UI hook. |
| `GameplaySystems/BoatLevel/InfiniteTileStrip.cs` | `BoatGame.GameplaySystems.BoatLevel` | Recycles scrolling background tiles. | Configure tile collection and movement values per scene. |

## Presentation and UI

| Script | Namespace | Responsibility | Key API / notes |
| --- | --- | --- | --- |
| `Presentation/BoatLevelUI/BoatLevelUI.cs` | `BoatGame.Presentation.BoatLevelUI` | Drives boat intro/tutorial/HUD/help/settings/arrival/completion screens from typed events. | UI button handlers call the level flow; subscribes in `OnEnable`. The current **Next** handler intentionally does not call `LoadNextSection`. |
| `Presentation/BoatLevelUI/DialoguePanel.cs` | `BoatGame.Presentation.BoatLevelUI` | Boat-scene dialogue playback and advance control. | Separate class from shared dialogue panel; use qualified names where needed. |
| `Presentation/BoatLevelUI/StarsDisplay.cs` | `BoatGame.Presentation.BoatLevelUI` | Renders and optionally animates star count. | Shared by boat and Nilometer UI. |
| `Presentation/BoatLevelUI/UiMotes.cs` | `BoatGame.Presentation.BoatLevelUI` | Decorative UI mote particle behavior. | Presentation-only visual component. |
| `Presentation/BoatLevelUI/UiPulse.cs` | `BoatGame.Presentation.BoatLevelUI` | Pulses UI elements for feedback. | Presentation-only visual component. |
| `Presentation/SharedUI/DialoguePanel.cs` | `BoatGame.Presentation.SharedUI` | Reusable typed/narrated dialogue with line and finish callbacks. | Nilometer reacts to line/voice/finish callbacks. |
| `Presentation/SharedUI/NilometerUI.cs` | global | Coordinates Nilometer dialogue, water stages, answer buttons, stars, hints, lesson, and completion. | `SubmitAnswer(levelIndex, answerIndex)` validates through `WaterOperator`. |
| `Presentation/SharedUI/CharacterExpressions.cs` | `BoatGame.Presentation.SharedUI` | Controls character mood, talk frames, and hop reaction. | Receives voice activity and dialogue-driven moods. |
| `Presentation/SharedUI/DialogueMoodDriver.cs` | `BoatGame.Presentation.SharedUI` | Maps dialogue progress to character mood. | Configure dialogue and expression references. |
| `Presentation/SharedUI/OptionsPanel.cs` | `BoatGame.Presentation.SharedUI` | Opens/closes settings and persists player audio/display choices. | Publish audio/pause events after changes. |

## Nilometer, player, and common scene systems

| Script | Namespace | Responsibility | Key API / notes |
| --- | --- | --- | --- |
| `WaterOperator.cs` | global | Owns serialized water levels, correct answers, and water-level events. | `MoveToLevel`, `IsCorrectAnswer`, and `WaterLevelReached` drive Nilometer progression. |
| `WaterAnimator.cs` | global | Animates water height. | `WaterOperator` owns level sequencing; do not put quiz logic here. |
| `Tail.cs` | global | Question-panel glow/tick animation controller. | Uses `BrunoMikoski.AnimationSequencer`; this requires the `DOTWEEN_ENABLED` scripting define. |
| `Player/PlayerManager.cs` | global | Persistent player manager singleton. | Scene-level player state bridge. |
| `Player/PlayerAvatarSelector.cs` | global | Applies and reports the selected avatar/gender. | Nilometer uses it to select traveler presentation. |
| `Player/PlayerData.cs` | global | Serializable saved player data. | Used by save system/manager. |
| `Player/player.cs` | global | Scene-facing player level/step/gender component. | Legacy lower-case class name; preserve the exact serialized type name. |
| `PlayerSelector.cs` / `PlayerUISelector.cs` | global | Avatar selection UI buttons. | File/class names are intentionally crossed; verify scene binding before renaming. |
| `SceneManager.cs` | global | Persistent scene loader with optional CanvasGroup fade. | Uses DOTween and Unity scene management. |
| `SceneOrgniser.cs` | global | Scene organization helper. | Legacy spelling is part of the type/file name. |
| `ManagersDetector.cs` | global | Inspector/button-friendly façade for scene/audio manager calls. | Use only where an existing scene expects it. |
| `ScoreSystem.cs` / `StarManager.cs` | global | Older score/star scene components. | Kept for legacy scene bindings. |
| `HitDetector.cs`, `spawning.cs` | global | Older collision and pooled spawning support. | Verify their exact scene use before editing. |
| `Boat/BoatController.cs`, `Boat/BoatBackground.cs` | global | Older boat/control/background implementation. | Not the namespaced `BoatLevel` system; avoid mixing the two in one scene. |

## Audio, localization, persistence, and interaction helpers

| Script | Namespace | Responsibility | Key API / notes |
| --- | --- | --- | --- |
| `CoreGameSystems/Audio/AudioService.cs` | `BoatGame.CoreGameSystems.Audio` | Event-driven music, ambience, SFX, voice, volume, snapshots, and voice ducking. | Subscribe/publish typed audio events rather than directly calling sources from gameplay. |
| `audio/SoundManager.cs` | global | Older key-based sound manager. | Separate from `AudioService`; retain only for legacy scene bindings. |
| `Save system/SaveManager.cs` | `Exloris.Saving` | Persistent save coordinator and active-player owner. | Creates, loads, applies, saves, and deletes `PlayerData`. |
| `Save system/SaveSystem.cs` | `Exloris.Saving` | JSON serialization, AES-CBC encryption, HMAC validation, and file I/O. | Writes `Application.persistentDataPath/player.save`. |
| `Save system/SaveSecrets.cs` | `Exloris.Saving` | Local encryption-key provider. | Ignored from Git; never commit or document its real key. |
| `Localization/LanguageSwitcher.cs` | global | English/Arabic locale selection UI. | Use Unity Localization tables for new visible content. |
| `Localization/LocalizationID.cs` | global | Exposes string/audio localization entry IDs. | Used for ID-based scene/content wiring. |
| `Localization/LocalizedInstructionPlayer.cs` | global | Plays localised instructions. | Provide aligned locale/audio data. |
| `Localization/LocalizationManager.cs` | global | Container for localized instruction references. | Currently minimal; validate desired behavior before expanding. |
| `Hover/UIHover.cs`, `Hover/GameObjHover.cs`, `Hover/HoverTest.cs` | global | UI/world hover feedback helpers. | Scene presentation utilities. |

## Script naming and safety notes

- `BoatController` and `DialoguePanel` each exist in multiple namespaces/locations. Use fully qualified names in new C# files when ambiguity is possible.
- Several legacy class and file names use non-standard casing or spelling. Renaming a `MonoBehaviour` can break serialized script references; migrate carefully in Unity rather than changing names blindly.
- Public UI handlers are serialized scene contracts. Do not rename/remove them without rebinding the relevant Button/UnityEvent in the Inspector.
