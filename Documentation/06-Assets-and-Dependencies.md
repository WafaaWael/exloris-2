# Data and asset dependencies

## Data contracts and persistence

`PlayerData` is the serializable player model; `SaveManager` owns the active player and delegates disk operations to `SaveSystem`. The save file path is `Application.persistentDataPath/player.save`.

The envelope is Base64 text containing:

```text
EX2S format header | HMAC-SHA256 | AES-CBC IV | AES-CBC ciphertext
```

Loading checks minimum length and header, verifies HMAC using fixed-time comparison, decrypts, then deserializes JSON. A failure is caught/logged and returns null. Encryption and integrity checks reduce casual modification but do not make an embedded client key a secure trust boundary. `SaveSecrets.cs` is ignored/local and its secret value is not reproduced here.

`GameSettings` stores music/SFX/voice volumes, VSync, and frame cap through PlayerPrefs. `LanguageSwitcher` stores language selection separately. These settings are not the same storage mechanism as `player.save`.

## Localization and audio content

`Assets/Exloris/Content/Localization/` contains English and Arabic locale, string table, audio table, shared-data, and localization settings assets. Addressables groups include localization asset/string tables and locale assets. Project scripts use Unity Localization components and locale switching. Direct project-owned `Addressables.LoadAssetAsync` or equivalent calls were not found in the inspected scripts; localization package internals may load addressable content.

`Assets/Audio/` is organized into Ambience, Music, SFX, and Voice folders. `AudioService` serializes clips/sources/mixer/snapshots and a lookup array keyed by `SfxId`. Legacy `SoundManager` uses string keys and clip registrations. Scene serialization and prefabs determine which clips are actually assigned.

## Rendering, animation, and UI assets

- `Assets/Settings/` contains URP pipeline/renderer/global settings and post-processing profiles.
- `Assets/Animation/Avatar/` contains boy/girl sprite animation content.
- `Assets/ART/`, `Assets/UI/`, `Assets/Atlases/`, and imported Cainos content contain art, fonts, and UI materials. File presence does not prove use by active scenes.
- `Assets/Resources/DOTweenSettings.asset` configures DOTween. `WaterAnimator` calls DOTween. `Tail` references Animation Sequencer runtime types and requires the `DOTWEEN_ENABLED` scripting define for that package's conditional compilation.
- TextMeshPro and RTLTMPro assets support localized typography. Verify actual Arabic direction and layout in the target scenes.

## Prefabs

26 prefab files were found in `Assets/prefabs/`, with names indicating audio, UI/settings, coin/obstacle, island, avatar selector, and manager content. A name is not proof of a runtime role. `spawning` contains a dynamic `Instantiate` path, but prefab argument assignment/runtime use was not exhaustively proven. Scene GUID attachments are cataloged in [project structure](01-Project-Structure.md).

## Runtime asset loading findings

Search of project-owned runtime/editor scripts found:

- Unity `SceneManager.LoadScene` and `LoadSceneAsync` calls in the global scene manager; boat replay/continuation also call Unity scene APIs.
- `Instantiate` in the legacy spawner.
- No project-owned `Resources.Load`, `Resources.LoadAsync`, AssetBundle API, or Addressables load method call.
- One intro video under `Assets/StreamingAssets/Exloris/Intro/`; a script-level use path was not verified.

This does not rule out package-internal loading, Unity serialized references, UnityEvents, VideoPlayer clip assignment, or prefab-instantiated object dependencies.

## Input and physics

`BoatInput` uses the Input System's `Keyboard` and `Pointer` and an EventSystem raycast to ignore UI. The project contains `Assets/InputSystem_Actions.inputactions`, `Assets/BoatControls.inputactions`, an Input System package, an EditorBuildSettings config object reference, and legacy `ProjectSettings/InputManager.asset`. `BoatController` uses a dynamic, zero-gravity Rigidbody2D with trigger-compatible colliders and `MovePosition`. Validate target scene colliders and EventSystem module settings in Unity.
