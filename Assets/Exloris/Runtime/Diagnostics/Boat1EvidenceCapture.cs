using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum BoatEvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum BoatEvidenceChild { Boy, Girl }
    public enum BoatEvidenceChoice { Reeds, Village, Ibis }
    public enum BoatEvidenceBeat { Choice, Travel0, Travel2, Travel4, Arrival }
    public enum BoatEvidenceOverlay
    {
        None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Loading, Recovery,
        AppBackgroundPause, AudioFocusPause, HelpAppBackgroundPause,
        HelpAudioFocusPause, LeaveAppBackgroundPause, LeaveAudioFocusPause,
        HelpAppPauseHelpBack,
    }
    public enum BoatEvidenceFailure { None, EntryTimeout, EntryAsset, EntryLayout, Stage1Timeout, Stage1Asset, Stage1Layout }

    public readonly struct Boat1EvidenceCaptureOptions
    {
        public Boat1EvidenceCaptureOptions(
            string path, BoatEvidenceRatio ratio, BoatEvidenceChild child,
            BoatEvidenceChoice choice, bool soundEnabled, bool captionsEnabled,
            bool reducedMotion, BoatEvidenceBeat beat, BoatEvidenceOverlay overlay,
            BoatEvidenceFailure failure, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; Choice = choice;
            SoundEnabled = soundEnabled; CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion; Beat = beat; Overlay = overlay;
            Failure = failure; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public BoatEvidenceRatio Ratio { get; }
        public BoatEvidenceChild Child { get; }
        public BoatEvidenceChoice Choice { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public BoatEvidenceBeat Beat { get; }
        public BoatEvidenceOverlay Overlay { get; }
        public BoatEvidenceFailure Failure { get; }
        public double DelaySeconds { get; }
    }

    public static class Boat1EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-boatCapture", "-boatRatio", "-boatChild", "-boatChoice", "-boatSound",
            "-boatCaptions", "-boatMotion", "-boatBeat", "-boatOverlay", "-boatFailure",
            "-boatCaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Boat1EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !TryGetSingleValue(arguments, "-boatCapture", out var path, out var hasPath)
                || !hasPath || string.IsNullOrWhiteSpace(path)
                || !TryGetOptional(arguments, "-boatRatio", "16x9", out var ratioValue)
                || !TryRatio(ratioValue, out var ratio)
                || !TryGetOptional(arguments, "-boatChild", "girl", out var childValue)
                || !TryChild(childValue, out var child)
                || !TryGetOptional(arguments, "-boatChoice", "reeds", out var choiceValue)
                || !TryChoice(choiceValue, out var choice)
                || !TryGetOptional(arguments, "-boatSound", "on", out var soundValue)
                || !TryOnOff(soundValue, out var soundEnabled)
                || !TryGetOptional(arguments, "-boatCaptions", "on", out var captionsValue)
                || !TryOnOff(captionsValue, out var captionsEnabled)
                || !TryGetOptional(arguments, "-boatMotion", "full", out var motionValue)
                || !TryMotion(motionValue, out var reducedMotion)
                || !TryGetOptional(arguments, "-boatBeat", "choice", out var beatValue)
                || !TryBeat(beatValue, out var beat)
                || !TryGetOptional(arguments, "-boatOverlay", "none", out var overlayValue)
                || !TryOverlay(overlayValue, out var overlay)
                || !TryGetOptional(arguments, "-boatFailure", "none", out var failureValue)
                || !TryFailure(failureValue, out var failure)
                || !TryGetOptional(arguments, "-boatCaptureDelay", "0", out var delayValue)
                || !double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var delaySeconds)
                || double.IsNaN(delaySeconds) || double.IsInfinity(delaySeconds)
                || delaySeconds < 0d || delaySeconds > 30d)
                return false;

            if ((failure == BoatEvidenceFailure.None && overlay == BoatEvidenceOverlay.Recovery)
                || (failure != BoatEvidenceFailure.None && overlay != BoatEvidenceOverlay.Recovery)
                || (overlay == BoatEvidenceOverlay.Loading && beat != BoatEvidenceBeat.Arrival))
                return false;

            options = new Boat1EvidenceCaptureOptions(
                path, ratio, child, choice, soundEnabled, captionsEnabled,
                reducedMotion, beat, overlay, failure, delaySeconds);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-boat", StringComparison.Ordinal)) continue;
                var known = false;
                foreach (var option in KnownOptions)
                    if (string.Equals(argument, option, StringComparison.Ordinal)) known = true;
                if (!known) return false;
            }
            return true;
        }

        private static bool TryGetOptional(string[] arguments, string option, string fallback, out string value)
        {
            if (!TryGetSingleValue(arguments, option, out value, out var present)) return false;
            if (!present) value = fallback;
            return true;
        }

        private static bool TryGetSingleValue(
            string[] arguments, string option, out string value, out bool present)
        {
            value = null; present = false;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                if (present || index + 1 >= arguments.Length
                    || arguments[index + 1].StartsWith("-boat", StringComparison.Ordinal))
                    return false;
                value = arguments[index + 1]; present = true;
            }
            return true;
        }

        private static bool TryRatio(string value, out BoatEvidenceRatio result)
        {
            switch (value)
            {
                case "4x3": result = BoatEvidenceRatio.FourThree; return true;
                case "16x9": result = BoatEvidenceRatio.SixteenNine; return true;
                case "24x10": result = BoatEvidenceRatio.UltraWide; return true;
                default: result = default; return false;
            }
        }

        private static bool TryChild(string value, out BoatEvidenceChild result)
        {
            if (value == "boy") { result = BoatEvidenceChild.Boy; return true; }
            if (value == "girl") { result = BoatEvidenceChild.Girl; return true; }
            result = default; return false;
        }

        private static bool TryChoice(string value, out BoatEvidenceChoice result)
        {
            switch (value)
            {
                case "reeds": result = BoatEvidenceChoice.Reeds; return true;
                case "village": result = BoatEvidenceChoice.Village; return true;
                case "ibis": result = BoatEvidenceChoice.Ibis; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOnOff(string value, out bool result)
        {
            if (value == "on") { result = true; return true; }
            if (value == "off") { result = false; return true; }
            result = false; return false;
        }

        private static bool TryMotion(string value, out bool reduced)
        {
            if (value == "full") { reduced = false; return true; }
            if (value == "reduced") { reduced = true; return true; }
            reduced = false; return false;
        }

        private static bool TryBeat(string value, out BoatEvidenceBeat result)
        {
            switch (value)
            {
                case "choice": result = BoatEvidenceBeat.Choice; return true;
                case "travel-0": result = BoatEvidenceBeat.Travel0; return true;
                case "travel-2": result = BoatEvidenceBeat.Travel2; return true;
                case "travel-4": result = BoatEvidenceBeat.Travel4; return true;
                case "arrival": result = BoatEvidenceBeat.Arrival; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOverlay(string value, out BoatEvidenceOverlay result)
        {
            switch (value)
            {
                case "none": result = BoatEvidenceOverlay.None; return true;
                case "help": result = BoatEvidenceOverlay.Help; return true;
                case "pause": result = BoatEvidenceOverlay.Pause; return true;
                case "pause-help": result = BoatEvidenceOverlay.PauseHelp; return true;
                case "direct-leave": result = BoatEvidenceOverlay.DirectLeave; return true;
                case "nested-leave": result = BoatEvidenceOverlay.NestedLeave; return true;
                case "loading": result = BoatEvidenceOverlay.Loading; return true;
                case "recovery": result = BoatEvidenceOverlay.Recovery; return true;
                case "app-pause": result = BoatEvidenceOverlay.AppBackgroundPause; return true;
                case "audio-focus-pause": result = BoatEvidenceOverlay.AudioFocusPause; return true;
                case "help-app-pause": result = BoatEvidenceOverlay.HelpAppBackgroundPause; return true;
                case "help-audio-focus-pause": result = BoatEvidenceOverlay.HelpAudioFocusPause; return true;
                case "leave-app-pause": result = BoatEvidenceOverlay.LeaveAppBackgroundPause; return true;
                case "leave-audio-focus-pause": result = BoatEvidenceOverlay.LeaveAudioFocusPause; return true;
                case "help-app-pause-help-back": result = BoatEvidenceOverlay.HelpAppPauseHelpBack; return true;
                default: result = default; return false;
            }
        }

        private static bool TryFailure(string value, out BoatEvidenceFailure result)
        {
            switch (value)
            {
                case "none": result = BoatEvidenceFailure.None; return true;
                case "entry-timeout": result = BoatEvidenceFailure.EntryTimeout; return true;
                case "entry-asset": result = BoatEvidenceFailure.EntryAsset; return true;
                case "entry-layout": result = BoatEvidenceFailure.EntryLayout; return true;
                case "stage1-timeout": result = BoatEvidenceFailure.Stage1Timeout; return true;
                case "stage1-asset": result = BoatEvidenceFailure.Stage1Asset; return true;
                case "stage1-layout": result = BoatEvidenceFailure.Stage1Layout; return true;
                default: result = default; return false;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class Boat1EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Boat1EvidenceOptions.TryGetCapture(
                    Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            yield return null;
            yield return null;
            var deadline = Time.realtimeSinceStartup + 30f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Boat evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == BoatEvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar)
                || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics())
            { Fail("Boat evidence did not reach Journey Overview.", 3); yield break; }
            yield return null;
            while (!_host.JourneyInputReady && Time.realtimeSinceStartup < deadline) yield return null;

            if (IsEntryFailure(options.Failure))
            {
                _host.DeferNextJourneyBoundaryForTests();
                _host.PresentJourneyBoundaryForDiagnostics();
                ApplyEntryFailure(options.Failure);
            }
            else
            {
                if (!_host.EnterBoat1ForDiagnostics())
                { Fail("Boat evidence did not enter Boat 1.", 4); yield break; }
                yield return null;
                while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;

                if (IsStage1Failure(options.Failure))
                {
                    _host.PresentBoat1BoundaryForDiagnostics();
                    while (!_host.Boat1BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (!_host.Boat1BoundaryLoading)
                    { Fail("Boat evidence did not reach the Stage 1 boundary.", 5); yield break; }
                    ApplyStage1Failure(options.Failure);
                }
                else
                {
                    if (options.Beat != BoatEvidenceBeat.Choice)
                        _host.PresentBoat1ChoiceForDiagnostics(ToChoice(options.Choice));
                    var target = TargetElapsed(options.Beat);
                    while (_host.Boat1Snapshot.TravelElapsedSeconds < target
                           && !_host.Boat1BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    ApplyOverlay(options.Overlay);
                    if (options.Beat == BoatEvidenceBeat.Arrival
                        && options.Overlay == BoatEvidenceOverlay.Loading)
                        while (!_host.Boat1BoundaryLoading && Time.realtimeSinceStartup < deadline)
                            yield return null;
                }
            }

            var captureAt = Time.realtimeSinceStartupAsDouble + options.DelaySeconds;
            do
            {
                yield return null;
                _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
            }
            while (Time.realtimeSinceStartupAsDouble < captureAt);
            yield return new WaitForEndOfFrame();
            ApplyCaptureLayout(resolution);
            yield return null;
            yield return new WaitForEndOfFrame();
            WriteCapture(options, resolution);
            Application.Quit(0);
        }

        private void ApplyEntryFailure(BoatEvidenceFailure failure)
        {
            if (failure == BoatEvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == BoatEvidenceFailure.EntryAsset) _host.ReportJourneyBoundaryAssetFailure();
            else _host.TryApplyJourneyLayout(360f, 182f, 360f, 182f, 326f, 88);
        }

        private void ApplyStage1Failure(BoatEvidenceFailure failure)
        {
            if (failure == BoatEvidenceFailure.Stage1Timeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == BoatEvidenceFailure.Stage1Asset) _host.ReportBoat1BoundaryAssetFailure();
            else _host.TryApplyBoat1Layout(360f, 182f, 360f, 182f, 326f, 88);
        }

        private void ApplyOverlay(BoatEvidenceOverlay overlay)
        {
            if (overlay == BoatEvidenceOverlay.Help) _host.PresentBoat1HelpForDiagnostics();
            else if (overlay == BoatEvidenceOverlay.Pause) _host.PresentBoat1PauseForDiagnostics();
            else if (overlay == BoatEvidenceOverlay.PauseHelp) _host.PresentBoat1PauseHelpForDiagnostics();
            else if (overlay == BoatEvidenceOverlay.DirectLeave) _host.PresentBoat1LeaveForDiagnostics(false);
            else if (overlay == BoatEvidenceOverlay.NestedLeave) _host.PresentBoat1LeaveForDiagnostics(true);
            else if (overlay == BoatEvidenceOverlay.Loading) _host.PresentBoat1BoundaryForDiagnostics();
            else if (overlay == BoatEvidenceOverlay.AppBackgroundPause)
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AppBackground);
            else if (overlay == BoatEvidenceOverlay.AudioFocusPause)
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AudioFocusLoss);
            else if (overlay == BoatEvidenceOverlay.HelpAppBackgroundPause)
            {
                _host.PresentBoat1HelpForDiagnostics();
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AppBackground);
            }
            else if (overlay == BoatEvidenceOverlay.HelpAudioFocusPause)
            {
                _host.PresentBoat1HelpForDiagnostics();
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AudioFocusLoss);
            }
            else if (overlay == BoatEvidenceOverlay.LeaveAppBackgroundPause)
            {
                _host.PresentBoat1LeaveForDiagnostics(false);
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AppBackground);
            }
            else if (overlay == BoatEvidenceOverlay.LeaveAudioFocusPause)
            {
                _host.PresentBoat1LeaveForDiagnostics(false);
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AudioFocusLoss);
            }
            else if (overlay == BoatEvidenceOverlay.HelpAppPauseHelpBack)
            {
                _host.PresentBoat1HelpForDiagnostics();
                _host.PresentBoat1InterruptionForDiagnostics(Boat1PauseReason.AppBackground);
                _host.PresentBoat1PauseHelpBackForDiagnostics();
            }
        }

        private static bool IsEntryFailure(BoatEvidenceFailure failure) =>
            failure == BoatEvidenceFailure.EntryTimeout || failure == BoatEvidenceFailure.EntryAsset
            || failure == BoatEvidenceFailure.EntryLayout;

        private static bool IsStage1Failure(BoatEvidenceFailure failure) =>
            failure == BoatEvidenceFailure.Stage1Timeout || failure == BoatEvidenceFailure.Stage1Asset
            || failure == BoatEvidenceFailure.Stage1Layout;

        private static Boat1Choice ToChoice(BoatEvidenceChoice choice) =>
            choice == BoatEvidenceChoice.Reeds ? Boat1Choice.Reeds
            : choice == BoatEvidenceChoice.Village ? Boat1Choice.Village : Boat1Choice.FollowIbis;

        private static double TargetElapsed(BoatEvidenceBeat beat)
        {
            if (beat == BoatEvidenceBeat.Travel2) return 2d;
            if (beat == BoatEvidenceBeat.Travel4) return 4d;
            if (beat == BoatEvidenceBeat.Arrival) return 6d;
            return 0d;
        }

        private static Vector2Int ResolutionFor(BoatEvidenceRatio ratio)
        {
            if (ratio == BoatEvidenceRatio.FourThree) return new Vector2Int(1440, 1080);
            if (ratio == BoatEvidenceRatio.UltraWide) return new Vector2Int(1920, 800);
            return new Vector2Int(1600, 900);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            var safeWidth = Mathf.Max(1f, resolution.x - 64f);
            var safeHeight = Mathf.Max(1f, resolution.y - 64f);
            if (_host.ActiveBoat1View != null)
                _host.TryApplyBoat1Layout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
            else if (_host.ActiveJourneyOverviewView != null)
                _host.TryApplyJourneyLayout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
        }

        private void WriteCapture(Boat1EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Boat1EvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255);
            camera.cullingMask = ~0;
            var renderTexture = new RenderTexture(
                resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(
                resolution.x, resolution.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, resolution.x, resolution.y), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            camera.targetTexture = null;
            renderTexture.Release();
            Destroy(texture); Destroy(renderTexture); Destroy(cameraObject);
            var performance = FoundationPerformanceMetrics.Summarize(
                _frameMilliseconds, Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(), resolution.x, resolution.y);
            var record = new EvidenceRecord
            {
                width = resolution.x, height = resolution.y, requested_ratio = options.Ratio.ToString(),
                selected_child = options.Child.ToString(), requested_choice = options.Choice.ToString(),
                sound_enabled = options.SoundEnabled, captions_enabled = options.CaptionsEnabled,
                reduced_motion = _host.ReducedMotionEnabled, requested_beat = options.Beat.ToString(),
                requested_overlay = options.Overlay.ToString(), failure = options.Failure.ToString(),
                route = _host.CurrentRoute.ToString(), session_location = _host.SessionLocation.ToString(),
                boat_state = _host.Boat1Snapshot.State.ToString(), choice = _host.Boat1Snapshot.Choice.ToString(),
                travel_elapsed_seconds = _host.Boat1Snapshot.TravelElapsedSeconds,
                arrival_committed = _host.Boat1Snapshot.ArrivalCommitted,
                input_ready = _host.Boat1InputReady, boundary_loading = _host.Boat1BoundaryLoading,
                arrival_sfx_playing = _host.Boat1ArrivalSfxPlaying,
                ibis_sfx_playing = _host.Boat1IbisSfxPlaying,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                pause_state = _host.Boat1PauseState.ToString(),
                pause_reason = _host.Boat1LastPauseReason?.ToString() ?? string.Empty,
                pause_caller_state = _host.Boat1PauseCallerState.ToString(),
                leave_caller_state = _host.Boat1DirectLeaveCallerState.ToString(),
                help_visible = _host.ActiveBoat1HelpView != null && _host.ActiveBoat1HelpView.IsVisible,
                leave_visible = _host.ActiveLeaveConfirmationView != null && _host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state = EffectiveRecovery().ToString(),
                semantic_control_count = _host.ActiveBoat1View == null ? 0 : _host.ActiveBoat1View.SemanticControlCount,
                display_semantic_control_count = _host.ActiveBoat1View == null ? 0 : _host.ActiveBoat1View.DisplaySemanticControlCount,
                accepted_command_count = _host.Boat1AcceptedCommandCount,
                frame_samples = performance.SampleCount, p95_frame_ms = performance.P95FrameMilliseconds,
                p99_frame_ms = performance.P99FrameMilliseconds,
                maximum_frame_ms = performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = performance.AllocatedMemoryMiB, reserved_mib = performance.ReservedMemoryMiB,
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"BOAT1_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery()
        {
            return _host.Boat1RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Boat1RecoveryState : _host.JourneyRecoveryState;
        }

        private static void Fail(string message, int exitCode)
        {
            Debug.LogError(message); Application.Quit(exitCode);
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width, height, semantic_control_count, display_semantic_control_count;
            public int accepted_command_count, frame_samples, maximum_consecutive_over_33_3_ms;
            public string requested_ratio, selected_child, requested_choice, requested_beat, requested_overlay;
            public string failure, route, session_location, boat_state, choice, navigation_destination;
            public string pause_state, pause_reason, pause_caller_state, leave_caller_state, recovery_state;
            public bool sound_enabled, captions_enabled, reduced_motion, arrival_committed;
            public bool input_ready, boundary_loading, help_visible, leave_visible;
            public bool arrival_sfx_playing, ibis_sfx_playing;
            public double travel_elapsed_seconds;
            public float p95_frame_ms, p99_frame_ms, maximum_frame_ms, allocated_mib, reserved_mib;
        }
    }
}
