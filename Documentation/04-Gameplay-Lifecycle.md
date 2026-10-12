# Gameplay lifecycle

## Evidence labels

- **Serialized/configured:** supported by build settings or scene serialization.
- **Code path:** supported by project code, but not runtime-tested.
- **Inferred:** a likely sequence from code/configuration whose actual runtime order or callback binding was not confirmed.

## 1. Application startup and entry scene

The build settings list `main menu.unity` as the first enabled scene after two disabled entries. Under ordinary Unity build behavior, the first enabled scene is the startup scene. This was not run to verify. The main menu scene serializes a `SceneManager`, `SceneOrgniser`, `SaveManager`, legacy `SoundManager`, `ManagersDetector`, and `AudioService`. Their `Awake`/`Start` relative order is not proven because no explicit execution order or bootstrap coordinator was verified.

`SaveManager.Awake` establishes its singleton/persistent behavior and loads/coordinates player data according to its code. `SceneOrgniser.Start` invokes its serialized start-scene event. Whether this event changes the menu view or scene depends on its scene binding.

## 2. Menu, player selection, and map route

The enabled scene sequence is menu → player → map → BoatLevel → Nilometer. The menu's serialized button events and `ManagersDetector`/`SceneManager` provide navigation entry points. The player scene contains selection controls, avatar selector, save manager, and localized instruction components. Selection handlers set gender/create or register a player through `SaveManager`; `PlayerAvatarSelector` applies the stored selection. The map scene contains manager and locale/hover components.

The route is supported by enabled scene order and serialized components, but each button-to-scene edge should be verified from its serialized UnityEvent target and by play-testing. A scene being next in build order does not itself transition to it.

## 3. Boat level initialization

`BoatLevelFlow.Awake` reads the boat start position and sets the camera's maximum Y from the dock reference. `OnEnable` registers callbacks from boat and input; `Start` disables motion/input and publishes `LevelState.Intro`. `BoatLevelUI` subscribes to state changes and uses `Service<BoatLevelFlow>` for button actions. On an Intro event it shows and starts the intro dialogue. The dialogue's finish callback calls `CompleteIntro`, moving the flow to Tutorial.

These are code paths. Whether all required serialized references and listeners are enabled at the relevant moment must be checked in Unity.

## 4. Starting and steering

The tutorial tap handler publishes a UI tap SFX and calls `StartPlaying`. The flow checks that the state is Tutorial or Paused, restores `Time.timeScale`, enables boat movement and input, and changes state to Playing.

`BoatInput.Update` runs only while enabled. A/D or arrow key presses emit a lane step. Pointer input begins only outside UI; a horizontal displacement beyond the configured inch threshold is a swipe, otherwise release chooses direction based on the press half of the screen. `BoatLevelFlow` forwards the step to `BoatController.StepLane`. The controller clamps lanes to three positions and publishes lane-move audio.

## 5. Main boat loop and collisions

While Playing or Arriving, `BoatLevelFlow.Update` computes normalized vertical progress between boat start and dock. It passes progress to the boat speed ramp and publishes progress in increments (or at completion). `BoatController.FixedUpdate` moves the Rigidbody2D forward, smooths lane position, handles pushback/autopilot, and updates oar sound. Trigger overlaps dispatch coin or obstacle callbacks.

- **Coin:** flow checks `IsCollected`, calls `Collect`, increments coins, and publishes coin SFX plus `CoinCollectedEvent`. HUD updates count; VFX reacts at the event position.
- **Obstacle:** controller classifies first hit and side/forward collision response, then invokes flow callback. Flow publishes bump SFX, removes at most one star on first eligible hit, may publish star-loss SFX, and publishes contact position/stars. UI and VFX respond. A forward collision can block the lane; changing lane clears the block. No separate game-over transition was found when stars reach zero.

## 6. Pause, help, and settings

Help is modeled as `Paused`; it sets `Time.timeScale = 0` and disables input. Continue calls `StartPlaying`. Settings also pauses time/input and `CloseSettings` restores both. `BoatLevelFlow.OnDisable` restores time scale. The separate shared `OptionsPanel` publishes `GamePausedEvent` for dialogue/audio synchronization. Whether every scene's settings overlay uses this exact flow is scene-dependent.

## 7. Arrival and boat completion

When the boat reaches the configured autopilot distance to the dock, the flow disables input, starts boat autopilot toward the stop point, changes to Arriving, and waits until the controller reaches its stop. After a delay it changes to Arrived. UI then plays arrival dialogue. The dialogue finish callback calls `CompleteArrival`, publishes win SFX, and sets Complete. UI shows completion stars and coins.

Replay resets time scale and reloads the active scene by build index. The continuation method loads `_nextSceneName`, defaulting to `BoatHistory`; that destination is not enabled in current build settings. Further, `BoatLevelUI.OnButtonClick_Next` currently does not call `LoadNextSection`, so the completion Next button does not establish the transition in the inspected code.

## 8. Nilometer learning activity

On Start, `NilometerUI` waits one frame before resolving the selected traveler, then initializes stars/panels and begins intro dialogue. The one-frame wait is a deliberate ordering workaround for avatar selection, but actual order remains scene-dependent.

After intro, the UI begins level 0. It plays a water-rise prompt; when voice starts, a delay can precede `WaterOperator.MoveToLevel`. If there is no prompt text, water moves immediately. `WaterOperator` animates via `WaterAnimator`, then signals the reached level. The UI opens that question and accepts answers.

- A correct answer disables answers, emits correct SFX, changes traveler/elder mood, animates/highlights the selected answer, then advances dialogue/level logic.
- An incorrect answer increments per-level attempts, subtracts one star, emits wrong SFX, and disables that wrong button. On the first miss, “almost” dialogue enables remaining answers again. On the second miss, “sorry” dialogue moves to the next water level or lesson.
- A hint temporarily disables answers, plays the next hint line, then resumes the question.
- After the final level, lesson and cubit dialogue/cards lead to completion display and remaining stars.

Serialized `WaterLevel` data, answer button indices, dialogue arrays, optional mood arrays, and callback hookups must be kept aligned. No separate Nilometer failure/game-over state was found.

## 9. Audio, UI, and visual feedback

`AudioService` subscribes to typed events and drives sources/mixer/snapshots. It fades music and ducks music/ambience during voice playback using unscaled time. It publishes `VoiceActivityEvent` for character talking poses. `BoatLevelVfx` handles coin, impact, and state effects. The Nilometer and legacy scenes also contain `SoundManager`; confirm which audio component a particular scene uses before changing clips.

## 10. Persistence, restart, and exit

Player selection and manager methods use `SaveManager`; `SaveSystem` serializes `PlayerData` to JSON, encrypts/authenticates an envelope, and writes `player.save` under `Application.persistentDataPath`. Load validates the header and HMAC before decrypting and deserializing; exceptions are logged and yield a null result. Explicit save/load call points are documented in [data and dependencies](06-Assets-and-Dependencies.md).

The inspected code provides replay and scene loading. A full return-to-menu, app quit, or scene-unload cleanup route was not proven across all scenes. Unity lifecycle methods unsubscribe listeners and restore time scale in several systems, but this analysis did not run the game.
