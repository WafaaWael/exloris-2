# External packages and integrations

Package versions below are from `Packages/manifest.json`; transitive resolved versions belong to `Packages/packages-lock.json` and may differ. Imported source folders are also present under `Assets/`.

## Relevant Unity packages

| Package | Declared version | Project use / evidence |
| --- | --- | --- |
| `com.unity.render-pipelines.universal` | `17.6.0` | URP asset, 2D renderer/settings assets. |
| `com.unity.inputsystem` | `1.20.0` | Namespaced boat input directly uses Input System keyboard/pointer APIs; action assets are present. |
| `com.unity.localization` | `1.5.12` | Locale, localized string/audio tables, locale switcher components. |
| `com.unity.ugui` | `2.6.0` | Canvas/Button/EventSystem-based screens and controls. |
| `com.unity.test-framework` | `1.8.0` | Package installed; project-owned test source inventory was not established in this pass. |
| `com.unity.recorder` | `5.1.7` | Editor recording tool; no runtime game dependency found. |
| `com.unity.ide.visualstudio` | `2.0.28` | Editor integration. |
| `com.unity.2d.sprite` | `1.0.0` | Sprite asset/editor support. |
| `com.anklebreaker.unity-mcp` | Git URL | Unity MCP package; tool/editor integration, not game runtime behavior. |
| `com.kitwright.unity.mcp` | Git URL | Unity MCP package; tool/editor integration. |
| `com.coffee.ui-particle` | Git URL | UI particle package; project has UI particle-related content/settings. Exact active instances depend on serialized scene/prefab wiring. |

Unity engine modules in the manifest include audio, physics2D, UI, animation, JSON serialization, video, and accessibility support.

## Imported plugins and source content

- **DOTween:** imported under `Assets/Plugins/Demigiant/DOTween`; used by `WaterAnimator`, scene fade animation, and animation components. `DOTweenSettings.asset` is in Resources.
- **Animation Sequencer:** imported under `Assets/AnimationSequencer`; `Tail.cs` references `BrunoMikoski.AnimationSequencer.AnimationSequencer`. Its runtime type is conditionally compiled by `DOTWEEN_ENABLED`.
- **RTLTMPro:** imported under `Assets/RTLTMPro`; supplies RTL TextMeshPro support and Arabic font assets.
- **Unity Events 2.0 for Unity:** imported under `Assets/Events 2.0 for Unity`; custom `Button2` and `UnityEvent2` types appear in the Nilometer code.
- **Cainos art/water content:** imported under `Assets/Cainos`; includes 2D art and Interactive Pixel Water code. Scene/runtime use must be verified per asset.
- **Rounded corners and other UI/plugin assets:** folders are present; usage is scene-specific.

## Addressables and localization integration

Addressable settings and localization groups are serialized in `Assets/AddressableAssetsData/`. `EditorBuildSettings.asset` references the Addressables settings object and Input System settings/actions object. Localization tables and locale assets are present. No direct project code calls the Addressables API, so the confirmed integration is configuration/package-backed localization rather than a custom asset-loading service.

## Platforms and build integration

Project settings contain platform-specific sections for Android, iPhone, Standalone, and WebGL and platform scripting defines. `Assets/Editor/WebGLBuildCli.cs` is an editor build helper. There are no separate Build Profile assets in the inspected files. This documentation does not assert that builds for all configured targets succeed.

## No evidence found

No project-owned network client, multiplayer/networking stack, external analytics SDK, or runtime AI system was identified in the inspected code and manifest. Absence of a direct script reference is not proof that imported package samples or future/uninspected content never uses such systems.
