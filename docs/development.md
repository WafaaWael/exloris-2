# Development guide

## Requirements

- Unity Hub and Unity Editor **6000.6.0f1**.
- Git: `Packages/manifest.json` includes Git-based dependencies.
- A platform module supported by the installed Unity editor.

The manifest includes URP, Input System, Localization, Recorder, UGUI, Unity Test Framework, and two Unity MCP packages. Let Package Manager resolve them before diagnosing missing APIs.

## Daily workflow

1. Inspect `git status` before working and preserve unrelated local changes.
2. Open the project in the pinned Unity version; wait for imports and compilation to settle.
3. Work in the relevant enabled scene rather than an imported sample scene.
4. Make the smallest cohesive change; inspect console errors and missing references.
5. Play the affected route, including pause/resume, dialogue, and navigation as applicable.
6. Run suitable tests and review changed assets plus their `.meta` files before committing.

## Validation

### Boat level

- Start at `main menu`, reach `BoatLevel`, finish intro/tutorial, and test keyboard plus pointer controls.
- Collect a coin and hit an obstacle; verify stars, blocked hint, VFX, SFX, and camera response.
- Open/close help and settings; verify time resumes and controls reactivate.
- Finish docking; verify arrival dialogue, completion stars/coins, replay, and next-scene navigation.

### Nilometer

- Test the selected avatar path.
- Validate a correct answer, a first incorrect retry, and two incorrect answers.
- Check water timing, dialogue timing, lesson/cubit cards, completion, and star displays.

### Localization/accessibility

- Test English and Arabic at target aspect ratios.
- Confirm RTL TextMeshPro does not overlap controls.
- Test pointer/touch as well as keyboard after UI changes.

## Tests

Unity Test Framework is installed. This pass found third-party/package tests and placeholder `Assets/Exloris/Tests/EditMode` and `PlayMode` folders, but no project-owned test source files.

Add focused tests for non-trivial changes:

- **EditMode:** state transitions, event payloads, save-envelope boundaries, data validation.
- **PlayMode:** scene wiring, input-to-lane behavior, dialogue progression, and water/quiz integration.

Run them with **Window → General → Test Runner**. Manual checks are still required for serialized references, rendering, audio mixer configuration, and full navigation.

## Build and release

1. Verify the five enabled scenes and their order.
2. Set target platform and player settings.
3. Make a development build and test the full route on target hardware/browser.
4. Before release, check console warnings, missing references, localization, save behavior, and input.

No repository-owned command-line build pipeline or CI configuration was found. A future automation should pin Unity, restore packages, run both test suites, and archive the build artifact.

## Documentation maintenance

Update the matching document when changing build scenes/navigation, architecture boundaries, typed event/persistence/localization contracts, or player-visible flows. Update both [gameplay flows](gameplay-flows.md) and its matching `.mmd` script for a flow change.
