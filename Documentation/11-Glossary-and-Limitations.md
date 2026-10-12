# Glossary and technical notes

## Glossary

| Term | Meaning in this project |
| --- | --- |
| `BoatLevelFlow` | Namespaced coordinator for boat state, score, progress, docking, and outcome actions. |
| `BoatController` | Ambiguous class name: namespaced boat level controller or older global controller in the separate Boat scene. |
| `EventBus` | Static synchronous publish/subscribe for `IEvent` structs in the newer boat/audio architecture. |
| `LevelState` | Boat flow state enum: Intro, Tutorial, Playing, Paused, Settings, Arriving, Arrived, Complete, Lesson. |
| `Nilometer` | Learning activity coordinated by `NilometerUI` and water-stage data from `WaterOperator`. |
| `Tail` | Question panel visual component with tick/glow animation sequences. |
| `WaterLevel` | Serialized Nilometer stage definition with target height, correct answer, and associated dialogue index. |
| `Addressables` | Unity asset delivery/loading system; configuration and localization groups exist, but project-owned scripts inspected do not call its loading API. |
| `confirmed` | Directly supported by code or serialized project data inspected. Does not mean play-tested. |
| `inferred` | A likely behavior derived from static source/configuration whose runtime execution was not observed. |

## Important technical notes

- There are two classes named `BoatController` and two named `DialoguePanel`. Use fully qualified names when code namespace resolution could be ambiguous.
- `PlayerSelector.cs` declares class `PlayerUISelector`; `PlayerUISelector.cs` declares class `PlayerSelector`. Serialized component references rely on Unity script GUID/type metadata; renaming files/classes carelessly can break bindings.
- The `LevelState.Lesson` value is defined but not used by the current boat flow. Nilometer lesson state is internal to `NilometerUI`, not that boat enum.
- The boat completion Next UI callback currently does not call the flow's next-scene method. `BoatHistory` is a default string but is not an enabled build scene.
- The save file uses encryption and an HMAC, but the game is client-side and the secret key is part of the application environment. Do not treat this as secure authority for valuable or networked data.
- UnityEvent and inspector bindings are runtime contracts not fully established from C# alone. Scene YAML can reveal serialized object references, but static analysis does not prove the target is active or successfully invoked.

## Scope and limitations

- This package is based on static file inspection of the checked-out project, scene YAML, metadata GUIDs, package manifests, and settings. Unity Editor was not opened, the game was not executed, and no build was made.
- There are 56 project-owned C# files in `Assets/scripts` plus `Assets/Editor` (55 runtime/support files and one editor helper). Third-party package source is not fully inventoried.
- Nine checked-in scenes were inventoried. Seven entries exist in Editor Build Settings; two disabled Exloris scene paths are missing. No separate Build Profile assets were found.
- Scene script GUID matches show serialized attachment, not enabled state, successful references, build inclusion, or runtime invocation.
- Prefab files were inventoried by path and selected serialized references. A full nested-prefab and override audit was not performed in Unity.
- Addressables configuration exists; no direct project-owned Addressables load API was found. Package-internal localization behavior was not traced.
- Dynamic asset paths, external build automation, platform-specific runtime behavior, and actual navigation callbacks remain to be verified in Unity.

## Suggested follow-up verification in Unity

1. Resolve the missing disabled Bootstrap/FoundationHarness paths or remove them through the project owner's normal workflow.
2. Verify main menu button callbacks and scene transitions against the five enabled scenes.
3. Decide and wire the BoatLevel completion Next behavior and target scene.
4. Inspect Nilometer answer UnityEvents and verify stage/question indexes.
5. Check scene-specific AudioService/SoundManager ownership and persistent singleton behavior.
6. Play through English and Arabic at target aspect ratios and validate save/load on intended platforms.
