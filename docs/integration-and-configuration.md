# Integration and configuration guide

Use this guide when extending Exloris 2. Make changes in the target scene, keep serialized references valid, then play the complete affected route.

## Add or change a build scene

1. Create or update the scene under `Assets/Scenes`.
2. Add it to **File → Build Profiles/Build Settings** in the intended order.
3. Update all serialized scene names and any `SceneManager`/`BoatLevelFlow` continuation references.
4. Update [scene-reference.md](scene-reference.md) and the player-route diagram.
5. Play navigation from the main menu and test replay/back behavior.

The enabled configuration is currently:

```text
main menu → player → Map → boat/BoatLevel → NiloMeter/NiloMeter
```

`BoatLevelFlow` defaults `_nextSceneName` to `BoatHistory`, which is not enabled in the current build list. Decide whether that is intentional before enabling the boat completion **Next** path.

## Configure the boat level

### Required scene objects

- **Boat:** namespaced `BoatGame.GameplaySystems.BoatLevel.BoatController`, `Rigidbody2D`, collider(s), visual transform, and sprite renderer.
- **Input:** `BoatInput` and an EventSystem so pointer input can ignore UI.
- **Flow:** `BoatLevelFlow` with boat, input, camera-follow, dock stop point, and intended next-scene name assigned.
- **Actors:** `Coin` and `Obstacle` instances with trigger-compatible colliders.
- **Presentation:** `BoatLevelUI`, `StarsDisplay`, intro/arrival dialogue, screens, buttons, HUD and progress fill references.
- **Feedback:** `BoatLevelVfx`, `AudioService`, sources/mixer/snapshots, and an `SfxEntry` for each `SfxId` used by the scene.

### Runtime rules to preserve

- Input is enabled only after the tutorial begins `Playing`.
- The boat moves on three lanes (`0..2`) and ignores invalid lane moves.
- A first eligible obstacle impact costs at most one star; a forward impact blocks the lane until the player moves to a new lane.
- Near the dock, flow disables input and starts autopilot. Arrival dialogue advances the state to completion.
- `Time.timeScale` is zero in help/settings. Animations or audio that must continue need unscaled/DSP timing.

## Configure a Nilometer level

1. Add a `WaterOperator.WaterLevel` entry with a unique ID, target water height, duration, correct answer index, and correct-dialogue line index.
2. Create/assign the matching `Tail` question panel and its answer buttons.
3. Bind each answer button to `NilometerUI.SubmitAnswer(levelIndex, answerIndex)` with matching zero-based values.
4. Ensure `NilometerUI` has dialogue lines, optional mood arrays, question panels, star displays, and its `WaterOperator` reference.
5. Test one correct answer, one first incorrect answer/retry, and two incorrect answers through lesson/completion.

`NilometerUI` allows two incorrect answers per water level. Each wrong answer removes a star; the first wrong answer disables only that answer, while the second moves to the next level or lesson.

## Add audio or feedback

1. For the event-driven boat path, add an audio clip entry to `AudioService` for the relevant `SfxId`.
2. Publish `PlaySfxEvent` at the authoritative system, rather than from every visual listener.
3. Use `PlayVoiceEvent` for narrated dialogue. `AudioService` publishes `VoiceActivityEvent` for speaking animations and ducks music/ambience.
4. Keep audio settings writes inside `GameSettings`; persist with `GameSettings.Save()` when needed.

Do not mix the newer `AudioService` pathway into legacy `SoundManager` usage without deliberately migrating the target scene.

## Add localized content

1. Add string/audio entries to the Unity Localization tables under `Assets/Exloris/Content/Localization`.
2. Reference the table entries from scene components rather than duplicating language-specific UI objects.
3. Test English and Arabic in the actual target aspect ratio.
4. Verify RTL TextMeshPro layout, button direction, and text overflow.

## Extend saving safely

1. Add serializable fields to `PlayerData` with backward-compatible defaults.
2. Load older/no-save cases safely in `SaveManager`.
3. Keep serialization/encryption in `SaveSystem`; keep player/session coordination in `SaveManager`.
4. Never commit `SaveSecrets.cs` or an encryption key.
5. Test new save data, missing file, corrupt file, and previous-format behavior.

The on-disk format is Base64 text containing an `EX2S` header, HMAC-SHA256, AES-CBC IV, and AES-CBC ciphertext. HMAC validation occurs before decryption.

## Add a typed event

1. Define a small `readonly struct` that implements `IEvent` in `CoreData`.
2. Publish from the system that owns the state transition.
3. Subscribe/unsubscribe in `OnEnable`/`OnDisable`.
4. Use events for transient notifications, not durable state. Components that enable later should query their coordinator’s current state.
5. Update [events-and-data.md](events-and-data.md), [script-reference.md](script-reference.md), and a diagram if player-visible behavior changes.

## Animation Sequencer / DOTween requirement

`Tail.cs` imports `BrunoMikoski.AnimationSequencer`. The package compiles its runtime `AnimationSequencer` class only when the `DOTWEEN_ENABLED` scripting define is set. Keep that symbol enabled for every build target that compiles the Nilometer scene, and restart Unity after changing scripting defines so assemblies recompile.
