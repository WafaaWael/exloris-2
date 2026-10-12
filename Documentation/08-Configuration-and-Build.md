# Setup, build, and configuration

## Open the project

1. Install Unity `6000.6.0f1` through Unity Hub.
2. Open the verified project root: `exloris-2`.
3. Allow Package Manager to resolve manifest dependencies. Git-based packages require Git and network access in the Unity environment.
4. Start with `Assets/Scenes/main menu.unity`, the first enabled scene in the current Editor Build Settings list.

No build or package restore was run during static analysis.

## Build scene configuration

`ProjectSettings/EditorBuildSettings.asset` contains two disabled, missing scene paths followed by five enabled scenes: main menu, player, Map, BoatLevel, and NiloMeter. No separate Build Profile file was found. Confirm the selected target, scene list, and destination scenes in the Unity Editor before building.

The boat flow's serialized/default continuation name is `BoatHistory`; that scene is not in the current enabled list. The BoatLevel UI Next button's method does not currently invoke flow continuation. Resolve intended continuation before treating the route as complete.

## Input configuration

- Input System package: `1.20.0`; project input handling includes `BoatInput` polling `Keyboard.current` and `Pointer.current`.
- Input action assets: `Assets/InputSystem_Actions.inputactions` and `Assets/BoatControls.inputactions`.
- Legacy axes also remain in `ProjectSettings/InputManager.asset`.
- `ProjectSettings/ProjectSettings.asset` indicates active input handling is Input System. Validate EventSystem/UI input module setup in each scene.

## Rendering and platform settings

URP `17.6.0` and 2D renderer assets are configured under `Assets/Settings`. Player settings contain a 1920×1080 default display size and Android/iPhone/Standalone/WebGL-specific configuration. Platform-specific scripting define strings include DOTween/Animation Sequencer support. Treat these settings as configuration evidence, not release validation.

## Required scene wiring checks

### BoatLevel

- Assign all `BoatLevelFlow` serialized dependencies: controller, input, camera, dock stop, and next scene.
- Confirm Rigidbody2D, trigger colliders, lane geometry, coin/obstacle components, and camera target.
- Assign `BoatLevelUI` screens, dialogue panels, buttons, HUD, progress image, and star displays.
- Configure `AudioService` sources/mixer/snapshots and each used SFX entry; configure `BoatLevelVfx` references.
- Ensure EventSystem exists so UI taps are filtered by `BoatInput`.

### Nilometer

- Assign `WaterOperator`, `WaterAnimator`, ordered water level entries, and question panels.
- Match zero-based answer indexes to each `WaterLevel.CorrectAnswerIndex` and bind buttons to `NilometerUI.SubmitAnswer(levelIndex, answerIndex)`.
- Align dialogue lines and optional mood arrays. Assign avatar, elder/traveler expressions, star displays, lesson and completion objects.
- Keep Animation Sequencer runtime define active for supported targets that compile `Tail.cs`.

## Build and verification checklist

1. Inspect Unity Console for compile errors and missing serialized references.
2. Confirm the active build target and intended five enabled scenes.
3. Test menu → selection → map → BoatLevel → Nilometer navigation.
4. Test boat tutorial, keyboard and pointer steering, coin, obstacle, help/settings, arrival, completion, replay, and intended Next behavior.
5. Test Nilometer correct answer, first wrong answer/retry, second wrong answer/advance, hints, lesson, and completion.
6. Test Arabic/English UI and voice, save creation/load/corruption handling, and target-device input/layout.
7. Build on the chosen target and test the produced artifact. None of these runtime checks are claimed as completed here.

`Assets/Editor/WebGLBuildCli.cs` exists for WebGL editor builds; inspect its source arguments and usage before invoking. This analysis did not run it.
