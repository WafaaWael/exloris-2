# Exloris — project overview

## Purpose and gameplay

Exloris is a Unity 2D educational game set around travel through ancient Egypt and a learning activity built around the Nilometer. The serialized build list contains a main menu, player selection, map, boat level, and Nilometer scene. The boat level is a three-lane forward journey with obstacles, coins, stars, an arrival sequence, and a completion panel. The Nilometer experience presents staged water levels, spoken dialogue, multiple-choice questions, hints, lesson content, and a completion display.

This description is based on the current scene names, project code, and serialized configuration. It is not a claim that every route has been play-tested.

## Verified project facts

| Item | Evidence / value |
| --- | --- |
| Unity editor version | `6000.6.0f1` from `ProjectSettings/ProjectVersion.txt`. |
| Render pipeline | Universal Render Pipeline package `17.6.0`; project includes a URP asset and 2D renderer assets. |
| Language | C# scripts under `Assets/scripts` and `Assets/Editor`. |
| Input | Input System package `1.20.0`; boat input directly uses `UnityEngine.InputSystem`. The project also contains legacy Input Manager axes. |
| Localization | Unity Localization `1.5.12`; English and Arabic locale/table assets are present. RTL TextMeshPro support is imported. |
| UI | UGUI, TextMeshPro, UnityEvents, and custom presentation scripts. |
| Build scene entries | 7 configured entries: 5 enabled and 2 disabled. The two disabled Exloris scene paths are missing in the current checkout. |
| Player save | `Application.persistentDataPath/player.save`, written by `Exloris.Saving.SaveSystem`. |
| Addressables | Addressable settings and localization groups exist. No Addressables runtime load call was found in the inspected project-owned C# scripts. |
| Target-related configuration | Android, iPhone, Standalone, and WebGL settings/defines are present. This does not establish a supported release matrix or successful builds. |

## Main route

The enabled scene order in `ProjectSettings/EditorBuildSettings.asset` is:

```text
main menu → player → Map → boat/BoatLevel → NiloMeter/NiloMeter
```

The scene order is confirmed. Actual button navigation is serialized in scenes and exposed through menu/scene-manager components; not every edge was verified by running the game. See [scene and transition reference](05-Scenes-and-Transitions.md).

## High-level architecture

The project currently combines two styles:

- The boat level has a newer namespaced feature structure (`BoatGame.GameplaySystems`, `BoatGame.Presentation`, `BoatGame.CoreData`). `BoatLevelFlow` owns level state and score; input and boat actors report interaction; a typed static `EventBus` informs UI, audio, and VFX.
- Menu, player selection, Nilometer, and older boat scenes use mostly global-namespace, scene-bound `MonoBehaviour`s and UnityEvents. Several old and new classes share names, so scene attachment and namespaces matter.

There is no single application bootstrap coordinator in the active build list. `Assets/Exloris/Scenes/Bootstrap.unity` and `FoundationHarness.unity` are disabled entries and do not exist at their configured paths. Unity starts from the first enabled build scene under the normal build convention, but actual launch behavior was not runtime-tested.

## Major systems

| System | Primary code |
| --- | --- |
| Scene navigation | `Assets/scripts/SceneManager.cs`, `ManagersDetector.cs`, scene UnityEvents. |
| Avatar/player | `Player/player.cs`, `PlayerAvatarSelector.cs`, `SaveManager.cs`, selection UI. |
| Boat gameplay | `GameplaySystems/BoatLevel/BoatLevelFlow.cs`, `BoatController.cs`, `BoatInput.cs`. |
| Boat presentation | `Presentation/BoatLevelUI/BoatLevelUI.cs`, dialogue, stars, VFX. |
| Nilometer | `Presentation/SharedUI/NilometerUI.cs`, `WaterOperator.cs`, `WaterAnimator.cs`, `Tail.cs`. |
| Audio | Event-driven `AudioService`; older key-based `SoundManager` also remains. |
| Persistence | `Exloris.Saving.SaveManager` and `SaveSystem`. |
| Localization | Unity Localization tables plus `LanguageSwitcher`, `LocalizationID`, and localized audio helpers. |

## Limitations and open issues

- The configured continuation name in `BoatLevelFlow` defaults to `BoatHistory`, which is not enabled in the build list. `BoatLevelUI.OnButtonClick_Next` currently does not call `LoadNextSection`.
- No game-over or explicit failure state was found in the boat state machine. Stars can reach zero, but no separate fail transition is implemented there.
- The repo has both a global `BoatController` for `Assets/Scenes/boat/Boat.unity` and a namespaced `BoatController` for `BoatLevel.unity`.
- Addressables are configured for localization content, but project-owned scripts inspected here do not call the Addressables API directly.
- A local save key is kept in ignored `SaveSecrets.cs`. Its value is deliberately excluded from these documents. Client-side key storage is not strong anti-tamper protection.
- This analysis is static. Scene runtime order, visual behavior, platform builds, and all serialized UnityEvent effects remain unverified unless stated otherwise.

## Navigation

- [Project structure](01-Project-Structure.md)
- [System architecture](02-System-Architecture.md)
- [Script reference](03-Script-Reference.md)
- [Gameplay lifecycle](04-Gameplay-Lifecycle.md)
- [Flowcharts](09-Flowcharts.md) and [UML diagrams](10-UML-Diagrams.md)
- [Glossary and limitations](11-Glossary-and-Limitations.md)
