# Script reference

This inventory includes 55 runtime/support scripts under `Assets/scripts` and one project editor script under `Assets/Editor`. Scene attachment statements are based on `.meta` GUID matches in serialized scenes. “No scene attachment found” does not mean unused: a script may be referenced by a prefab, editor workflow, UnityEvent, or runtime instantiation path.

## Core data and messaging

| File / type | Kind and role | Main API / dependencies | Scene evidence |
| --- | --- | --- | --- |
| `CoreData/BoatLevelEvents.cs` — `LevelState`, `SfxId`, event structs | Enums and immutable `IEvent` payloads. | Carries state, collisions, coins, progress, SFX, voice, voice activity, pause/settings messages. | Indirect; consumed by active boat/Nilometer components. |
| `CoreData/CharacterMood.cs` — `CharacterMood` | Enum. | Expression states used by dialogue and characters. | Used by Nilometer/character scripts. |
| `CoreData/DialogueLine.cs` — `DialogueLine` | Serializable struct. | Text and optional voice clip data used by dialogue panels. | Serialized in boat/Nilometer scenes. |
| `CoreData/GameSettings.cs` — `GameSettings` | Static class. | PlayerPrefs properties for music, sounds, voice, VSync, frame cap; `Save()`. | Read by audio/options systems. |
| `CoreData/Messaging/EventBus.cs` — `IEvent`, `EventBus` | Interface and static generic event bus. | `Subscribe<T>`, `Unsubscribe<T>`, `Publish<T>` for structs. | Indirect runtime dependency. |
| `CoreData/Messaging/Service.cs` — `Service<T>` | Generic struct resolver. | Lazily locates/caches a scene `Component`; used for `BoatLevelFlow`. | Through `BoatLevelUI` in BoatLevel scene. |

## Boat level gameplay

| File / type | Kind, ownership, and flow | Important behavior/dependencies | Scene evidence |
| --- | --- | --- | --- |
| `GameplaySystems/BoatLevel/BoatLevelFlow.cs` — `BoatLevelFlow` | `MonoBehaviour`; owns level state, 3 starting stars, coin count, progress, docking, and outcome navigation. | `CompleteIntro`, `StartPlaying`, `OpenHelp`, `Opensettings`, `CloseSettings`, `CompleteArrival`, `ReplayLevel`, `LoadNextSection`. Registers direct boat/input callbacks. In `Update`, calculates clamped progress and starts autopilot within configured dock distance. | Attached to `Assets/Scenes/boat/BoatLevel.unity`. |
| `GameplaySystems/BoatLevel/BoatController.cs` — `BoatGame.GameplaySystems.BoatLevel.BoatController` | `MonoBehaviour`; owns 3-lane motion and boat collision response. | `StepLane`, `SetProgress`, `StartAutoPilot`, `HasReachedAutoPilotStop`. Uses Rigidbody2D `MovePosition`, speed interpolation, lane smoothing, bump/flicker, trigger detection. Publishes lane/blocked/oar events; calls obstacle/coin delegates. | BoatLevel scene. |
| `GameplaySystems/BoatLevel/BoatInput.cs` — `BoatInput` | `MonoBehaviour`; translates device input into lane steps. | Keyboard A/D/arrows; pointer/touch swipe or screen-half tap; EventSystem raycasts prevent UI taps from steering. `IsInputEnabled` gates input; raises `OnLaneStep`. | BoatLevel scene. |
| `GameplaySystems/BoatLevel/Coin.cs` — `Coin` | `MonoBehaviour`; collectible visual/state. | `IsCollected`, `Collect()` and per-frame glow/collection animation. Flow guards duplicate collection. | No scene GUID match; inspect prefabs/instantiation for use. |
| `GameplaySystems/BoatLevel/Obstacle.cs` — `Obstacle` | `MonoBehaviour`; obstacle visual/collision feedback. | `PlayBump`; boat controller classifies lane impact and handles response. | No scene GUID match; inspect prefab instances for use. |
| `GameplaySystems/BoatLevel/CameraFollow.cs` — `CameraFollow` | `MonoBehaviour`; camera follow and shake. | `MaxY`, `Shake`, `SnapToTarget`; flow sets dock limit. | BoatLevel scene. |
| `GameplaySystems/BoatLevel/BoatLevelVfx.cs` — `BoatLevelVfx` | `MonoBehaviour`; event subscriber for scene VFX. | Handles coin, obstacle, and level state events; uses serialized particle/camera/celebration references. | BoatLevel scene. |
| `GameplaySystems/BoatLevel/GodRaysPulse.cs` — `GodRaysPulse` | `MonoBehaviour`; animated ray intensity. | `Brighten`; update pulse driven by serialized values. | BoatLevel scene. |
| `GameplaySystems/BoatLevel/InfiniteTileStrip.cs` — `InfiniteTileStrip` | `MonoBehaviour`; background tile recycling. | Serialized tile/scroll configuration and transform updates. | BoatLevel scene. |

## Boat presentation and shared presentation

| File / type | Kind and responsibility | Main behavior/dependencies | Scene evidence |
| --- | --- | --- | --- |
| `Presentation/BoatLevelUI/BoatLevelUI.cs` — `BoatGame.Presentation.BoatLevelUI.BoatLevelUI` | `MonoBehaviour`; switches intro, tutorial/help, settings, HUD, arrival, and completion views. | Button handlers call flow through `Service<BoatLevelFlow>` and publish UI SFX. Subscribes to state/coin/hit/progress/blocked events. Next handler currently only plays tap feedback; flow call is commented. | BoatLevel scene. |
| `Presentation/BoatLevelUI/DialoguePanel.cs` — namespaced `DialoguePanel` | `MonoBehaviour`; boat intro/arrival dialogue. | Advances serialized dialogue lines, publishes `PlayVoiceEvent`, emits finish callback. | BoatLevel scene. |
| `Presentation/BoatLevelUI/StarsDisplay.cs` — `StarsDisplay` | `MonoBehaviour`; star display and animation. | `CountStars`, `ShowStars`; used by boat UI and Nilometer completion. | BoatLevel and Nilometer scenes. |
| `Presentation/BoatLevelUI/UiMotes.cs` — `UiMotes` | `MonoBehaviour`; decorative mote particle simulation. | Initializes and updates small visual motes. | BoatLevel scene. |
| `Presentation/BoatLevelUI/UiPulse.cs` — `UiPulse` | `MonoBehaviour`; pulsing UI animation. | Lifecycle starts/stops pulse and updates presentation. | Main menu, player, map, BoatLevel. |
| `Presentation/SharedUI/DialoguePanel.cs` — `BoatGame.Presentation.SharedUI.DialoguePanel` | `MonoBehaviour`; reusable timed/voice dialogue. | `PlayLines`, `Play`, `Hide`, `PreloadTimelines`, advance handler; publishes voice/tap events; responds to `GamePausedEvent`. | Nilometer scene; separate from boat panel. |
| `Presentation/SharedUI/NilometerUI.cs` — global `NilometerUI` | `MonoBehaviour`; coordinates Nilometer story, quiz, hints, stars, lesson, and completion. | `SubmitAnswer` validates with `WaterOperator`; two wrong attempts advance; dialogue callbacks gate water/question progression; selects traveler from avatar. Emits SFX and serialized UnityEvents. | Nilometer scene. |
| `Presentation/SharedUI/CharacterExpressions.cs` — namespaced `CharacterExpressions` | `MonoBehaviour`; mood/sprite and talking pose driver. | Mood and hop methods; listens to `VoiceActivityEvent`. | BoatLevel and Nilometer scenes. |
| `Presentation/SharedUI/DialogueMoodDriver.cs` — `DialogueMoodDriver` | `MonoBehaviour`; maps dialogue lines to character moods. | Hooks dialogue lifecycle and changes character expression. | BoatLevel scene. |
| `Presentation/SharedUI/OptionsPanel.cs` — `OptionsPanel` | `MonoBehaviour`; options UI. | Opens/closes settings, applies PlayerPrefs-backed audio/display choices, publishes audio settings and pause events. | Menu, player, map, Nilometer scenes. |

## Player, persistence, and scene management

| File / type | Kind and responsibility | Main behavior/dependencies | Scene evidence |
| --- | --- | --- | --- |
| `Player/player.cs` — global `player`, `PlayerGender` | `MonoBehaviour` plus enum; scene-facing player level/step/gender. | `Start` registers with `SaveManager`; properties and `SetGender` expose data. | Player scene. |
| `Player/PlayerData.cs` — `PlayerData` | Serializable data class. | Stores level, step, gender and fields consumed by save/avatar systems. | Runtime data; not a component attachment. |
| `Player/PlayerManager.cs` — `PlayerManager` | `MonoBehaviour`; older singleton/persistent manager. | `Awake` singleton and `DontDestroyOnLoad`; source contains minimal Start/Update. | No active scene GUID match found. |
| `Player/PlayerAvatarSelector.cs` — `PlayerAvatarSelector` | `MonoBehaviour`; applies saved gender to avatar presentation and reports selection. | `Start` reads `SaveManager.CurrentData`; `ApplyGender`, `GetCurrentGender`. | Player and Nilometer scenes. |
| `PlayerSelector.cs` — class `PlayerUISelector` | `MonoBehaviour`; legacy selection buttons. | Selects gender through `SaveManager`; has commented scene-loading call. | No direct scene GUID match. |
| `PlayerUISelector.cs` — class `PlayerSelector` | `MonoBehaviour`; selection UI path. | Calls `SaveManager.SetGender` and creates selected player. | Player scene. |
| `Save system/SaveManager.cs` — `Exloris.Saving.SaveManager` | Persistent singleton `MonoBehaviour`; owns active and loaded player data. | Registers player, loads/saves/deletes, applies gender, creates player; invokes serialized `OnLoadedData`. | Main menu, player, Nilometer scenes. |
| `Save system/SaveSystem.cs` — `SaveSystem` | Static persistence service. | JSON serialization, AES-CBC encryption, HMAC-SHA256 verification, Base64 envelope, file I/O. | Called by `SaveManager`; not attached. |
| `Save system/SaveSecrets.cs` — `SaveSecrets` | Internal static key source. | Supplies cryptographic key to save service. Value intentionally omitted. | Compile-time dependency only; do not expose key. |
| `SceneManager.cs` — global `SceneManager` | Persistent singleton `MonoBehaviour`; scene load façade. | Name/index loads; optional CanvasGroup fade through async loading; reload and next build index. | Main menu, map, Nilometer scenes. |
| `ManagersDetector.cs` — `ManagersDetector` | `MonoBehaviour`; button-friendly façade. | Forwards scene and sound actions to manager singletons. | Main menu, player, map, BoatLevel, Nilometer. |
| `SceneOrgniser.cs` — `SceneOrgniser` | `MonoBehaviour`; invokes serialized start-scene event. | `Start` is the scene entry hook. | Main menu scene. |

## Nilometer, audio, localization, and legacy scripts

| File / type | Kind and responsibility | Main behavior/dependencies | Scene evidence |
| --- | --- | --- | --- |
| `WaterOperator.cs` — global `WaterOperator` | `MonoBehaviour`; owns water stage data and sequencing. | `MoveToLevel`, `MoveToNextLevel`, `IsCorrectAnswer`, event on reaching target; uses `WaterAnimator`. | Nilometer scene. |
| `WaterAnimator.cs` — global `WaterAnimator` | `MonoBehaviour`; tween-based water-height animation. | `AnimateYSize` and return variants; uses DOTween; kills active tween on destruction. | Nilometer scene. |
| `Tail.cs` — global `Tail` | `MonoBehaviour`; question tick/glow sequence. | `SetGlow` overloads and serialized `AnimationSequencer` list; requires Animation Sequencer runtime symbol. | Nilometer scene. |
| `CoreGameSystems/Audio/AudioService.cs` — namespaced `AudioService` | `MonoBehaviour`; music, ambience, SFX, voice, snapshots, mixer settings, voice ducking. | Subscribes to audio/state/pause events in enable; builds `SfxId` lookup; fades with unscaled time; publishes voice activity. | Main menu scene attachment found; verify lifetime across route. |
| `audio/SoundManager.cs` — `SoundManager`, `SoundData` | Legacy persistent audio manager. | Key-based clip registry and play/stop/loop/mute/localized playback. | Main menu and Nilometer scenes. |
| `Localization/LanguageSwitcher.cs` — `LanguageSwitcher` | `MonoBehaviour`; changes active locale and stores language choice. | English/Arabic selection and locale code path. | Player and map scenes. |
| `Localization/LocalizationID.cs` — `LocalizationID` | `MonoBehaviour`; binds localized text/audio references and responds to table value changes. | Exposes string/audio entry IDs; subscriptions in enable/disable. | Player scene. |
| `Localization/LocalizedInstructionPlayer.cs` — same class | `MonoBehaviour`; selects and plays localized instruction clips. | Instruction index, clip/volume selection, playback methods; lifecycle event subscription. | Player scene. |
| `Localization/LocalizationManager.cs` — `LocalizationManager` | `MonoBehaviour`; currently contains an instruction list and empty Start/Update methods. | Scene attachment alone does not imply active localization behavior. | Player scene. |
| `Boat/BoatController.cs` — global `BoatController` | Legacy boat `MonoBehaviour`, distinct from namespaced class. | Uses older input/controller flow and movement methods. | `Assets/Scenes/boat/Boat.unity`. |
| `Boat/BoatBackground.cs` — `BoatBackground` | Legacy background component. | Moves background by distance/step method. | Legacy Boat scene. |
| `HitDetector.cs` — `HitDetector` | Legacy collision component. | Invokes serialized hit/coin UnityEvents. | Legacy Boat scene. |
| `spawning.cs` — `spawning` | Legacy pooled spawner. | Instantiates configured objects and manages active pool entries/return. | Legacy Boat scene. |
| `ScoreSystem.cs` — `ScoreSystem` | Legacy score `MonoBehaviour`. | Add/subtract/reset score operations. | Legacy Boat scene. |
| `StarManager.cs` — `StarManager` | Legacy star `MonoBehaviour`. | Star-count/presentation support. | No scene GUID match found. |
| `Hover/GameObjHover.cs` — `GameObjHover` | World-object hover component. | Invoke serialized pointer-enter/exit behavior and restore state. | Map and mouseHover scenes. |
| `Hover/UIHover.cs` — `UIHover` | UGUI pointer handler. | Saves/restores image/text/button visual state on pointer events. | Map scene. |
| `Hover/HoverTest.cs` — `HoverTest` | Input/hover test helper. | Runtime pointer/input probing plus serialized UnityEvents. | No scene GUID match found. |

## Editor script

| File / type | Kind and responsibility | Evidence / limit |
| --- | --- | --- |
| `Assets/Editor/WebGLBuildCli.cs` — static `WebGLBuildCli` | Editor-only build command helper. | Relevant to editor/build workflow, not a runtime `MonoBehaviour`; inspect source and its command entry points before using. |

## Script usage caveats

- Scene GUID matching confirms serialization in a scene file, not GameObject active state, component enabled state, valid references, callback execution, or inclusion in a particular build after profile processing.
- Prefab references and dynamic use require following prefab YAML and all factory paths; absence from scene YAML is not evidence of dead code.
- Third-party script inventory is excluded from the count. The project imports DOTween, Animation Sequencer, RTLTMPro, UnityEvents 2.0, Cainos assets, and other packages/content.
