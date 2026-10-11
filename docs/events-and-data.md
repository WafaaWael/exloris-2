# Events and data contracts

## Typed events

These `readonly struct` values implement `IEvent` and are published synchronously through `EventBus`.

| Event | Publisher | Consumers | Payload / meaning |
| --- | --- | --- | --- |
| `LevelStateChangedEvent` | `BoatLevelFlow` | Boat UI, audio, VFX | Previous and new `LevelState`. |
| `BoatHitObstacleEvent` | `BoatLevelFlow` | Boat UI, VFX | Stars left, star-loss flag, contact position. |
| `CoinCollectedEvent` | `BoatLevelFlow` | Boat UI, VFX | Running coin count and pickup position. |
| `BoatBlockedEvent` | `BoatController` | Boat UI | Whether the player must change lanes. |
| `LevelProgressEvent` | `BoatLevelFlow` | Boat UI | Progress from 0 to 1. |
| `PlaySfxEvent` | Flow, controller, UI, Nilometer | `AudioService` | A `SfxId` enum value. |
| `PlayVoiceEvent` | Dialogue, options UI | `AudioService` | A clip to play; `null` stops voice. |
| `VoiceActivityEvent` | `AudioService` | Character expressions | Whole-line speaking status. |
| `AudioSettingsChangedEvent` | Options UI | `AudioService` | PlayerPrefs-backed volume values changed. |
| `GamePausedEvent` | Options UI | Audio and shared dialogue | Pause transition for audio/dialogue synchronization. |

### Adding an event

1. Define a small immutable `readonly struct` in `CoreData` that implements `IEvent`.
2. Publish it at the source of truth, not at a visual side effect.
3. Subscribe/unsubscribe in matching Unity lifecycle methods.
4. Do not use events as durable state; read the coordinator's current state when UI enables.
5. Update this table and the relevant Mermaid flow when the contract affects a player journey.

## Boat-level state model

`LevelState` includes `Intro`, `Tutorial`, `Playing`, `Paused`, `Settings`, `Arriving`, `Arrived`, `Complete`, and `Lesson`.

The current `BoatLevelFlow` path is Intro → Tutorial → Playing → Arriving → Arrived → Complete; `Paused` and `Settings` are entered from Playing. `Lesson` exists in the enum but is not entered by the present flow implementation.

## Player settings

`GameSettings` stores music, sound, voice, VSync, and frame-cap values in `PlayerPrefs`. Write through this class and call `GameSettings.Save()` when persistence timing matters. Audio UI publishes `AudioSettingsChangedEvent` after volume changes.

## Save data

`Exloris.Saving.SaveSystem` serializes `PlayerData` to JSON, then writes this Base64-encoded envelope:

```text
EX2S header | HMAC-SHA256 | AES-CBC IV | AES-CBC ciphertext
```

The save path is `Application.persistentDataPath/player.save`. `SaveManager` is a persistent singleton coordinating active player and loaded data.

The encryption key is in ignored `SaveSecrets.cs`. Never commit that file or a production key. Encryption here deters casual inspection, but is not complete client-side anti-tamper protection when the key ships with the game.

## Localization and content

Unity Localization is installed. English and Arabic tables, including audio tables, live under `Assets/Exloris/Content/Localization`. `LocalizationManager`, `LanguageSwitcher`, `LocalizationID`, and `LocalizedInstructionPlayer` provide scene-facing behavior.

When adding visible text or audio:

1. Add it to the relevant localization table rather than hard-coding a per-language duplicate.
2. Test English and Arabic layout, including right-to-left TextMeshPro behavior.
3. Keep dialogue text, voice references, and mood arrays aligned by line index.
