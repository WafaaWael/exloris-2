using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum Boat2EvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum Boat2EvidenceChild { Boy, Girl }
    public enum Boat2EvidenceChoice { FieldBank, Dock, Ibis }
    public enum Boat2EvidenceBeat { Choice, Travel0, Travel2, Travel4, Arrival }
    public enum Boat2EvidenceOverlay
    {
        None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Loading, Recovery,
        AppBackgroundPause, AudioFocusPause, HelpAppBackgroundPause,
        HelpAudioFocusPause, LeaveAppBackgroundPause, LeaveAudioFocusPause,
        HelpAppPauseHelpBack,
    }
    public enum Boat2EvidenceFailure { None, EntryTimeout, EntryAsset, EntryLayout, Stage2Timeout, Stage2Asset, Stage2Layout }

    public readonly struct Boat2EvidenceCaptureOptions
    {
        public Boat2EvidenceCaptureOptions(
            string path, Boat2EvidenceRatio ratio, Boat2EvidenceChild child,
            Boat2EvidenceChoice choice, bool soundEnabled, bool captionsEnabled,
            bool reducedMotion, Boat2EvidenceBeat beat, Boat2EvidenceOverlay overlay,
            Boat2EvidenceFailure failure, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; Choice = choice;
            SoundEnabled = soundEnabled; CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion; Beat = beat; Overlay = overlay;
            Failure = failure; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public Boat2EvidenceRatio Ratio { get; }
        public Boat2EvidenceChild Child { get; }
        public Boat2EvidenceChoice Choice { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public Boat2EvidenceBeat Beat { get; }
        public Boat2EvidenceOverlay Overlay { get; }
        public Boat2EvidenceFailure Failure { get; }
        public double DelaySeconds { get; }
    }

    public static class Boat2EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-boat2Capture", "-boat2Ratio", "-boat2Child", "-boat2Choice", "-boat2Sound",
            "-boat2Captions", "-boat2Motion", "-boat2Beat", "-boat2Overlay", "-boat2Failure",
            "-boat2CaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Boat2EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !TryGetSingleValue(arguments, "-boat2Capture", out var path, out var hasPath)
                || !hasPath || string.IsNullOrWhiteSpace(path)
                || !TryGetOptional(arguments, "-boat2Ratio", "16x9", out var ratioValue)
                || !TryRatio(ratioValue, out var ratio)
                || !TryGetOptional(arguments, "-boat2Child", "girl", out var childValue)
                || !TryChild(childValue, out var child)
                || !TryGetOptional(arguments, "-boat2Choice", "field-bank", out var choiceValue)
                || !TryChoice(choiceValue, out var choice)
                || !TryGetOptional(arguments, "-boat2Sound", "on", out var soundValue)
                || !TryOnOff(soundValue, out var soundEnabled)
                || !TryGetOptional(arguments, "-boat2Captions", "on", out var captionsValue)
                || !TryOnOff(captionsValue, out var captionsEnabled)
                || !TryGetOptional(arguments, "-boat2Motion", "full", out var motionValue)
                || !TryMotion(motionValue, out var reducedMotion)
                || !TryGetOptional(arguments, "-boat2Beat", "choice", out var beatValue)
                || !TryBeat(beatValue, out var beat)
                || !TryGetOptional(arguments, "-boat2Overlay", "none", out var overlayValue)
                || !TryOverlay(overlayValue, out var overlay)
                || !TryGetOptional(arguments, "-boat2Failure", "none", out var failureValue)
                || !TryFailure(failureValue, out var failure)
                || !TryGetOptional(arguments, "-boat2CaptureDelay", "0", out var delayValue)
                || !double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var delaySeconds)
                || double.IsNaN(delaySeconds) || double.IsInfinity(delaySeconds)
                || delaySeconds < 0d || delaySeconds > 30d)
                return false;

            if ((failure == Boat2EvidenceFailure.None && overlay == Boat2EvidenceOverlay.Recovery)
                || (failure != Boat2EvidenceFailure.None && overlay != Boat2EvidenceOverlay.Recovery)
                || (overlay == Boat2EvidenceOverlay.Loading && beat != Boat2EvidenceBeat.Arrival))
                return false;

            options = new Boat2EvidenceCaptureOptions(
                path, ratio, child, choice, soundEnabled, captionsEnabled,
                reducedMotion, beat, overlay, failure, delaySeconds);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-boat2", StringComparison.Ordinal)) continue;
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
                    || arguments[index + 1].StartsWith("-boat2", StringComparison.Ordinal))
                    return false;
                value = arguments[index + 1]; present = true;
            }
            return true;
        }

        private static bool TryRatio(string value, out Boat2EvidenceRatio result)
        {
            switch (value)
            {
                case "4x3": result = Boat2EvidenceRatio.FourThree; return true;
                case "16x9": result = Boat2EvidenceRatio.SixteenNine; return true;
                case "24x10": result = Boat2EvidenceRatio.UltraWide; return true;
                default: result = default; return false;
            }
        }

        private static bool TryChild(string value, out Boat2EvidenceChild result)
        {
            if (value == "boy") { result = Boat2EvidenceChild.Boy; return true; }
            if (value == "girl") { result = Boat2EvidenceChild.Girl; return true; }
            result = default; return false;
        }

        private static bool TryChoice(string value, out Boat2EvidenceChoice result)
        {
            switch (value)
            {
                case "field-bank": result = Boat2EvidenceChoice.FieldBank; return true;
                case "dock": result = Boat2EvidenceChoice.Dock; return true;
                case "ibis": result = Boat2EvidenceChoice.Ibis; return true;
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

        private static bool TryBeat(string value, out Boat2EvidenceBeat result)
        {
            switch (value)
            {
                case "choice": result = Boat2EvidenceBeat.Choice; return true;
                case "travel-0": result = Boat2EvidenceBeat.Travel0; return true;
                case "travel-2": result = Boat2EvidenceBeat.Travel2; return true;
                case "travel-4": result = Boat2EvidenceBeat.Travel4; return true;
                case "arrival": result = Boat2EvidenceBeat.Arrival; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOverlay(string value, out Boat2EvidenceOverlay result)
        {
            switch (value)
            {
                case "none": result = Boat2EvidenceOverlay.None; return true;
                case "help": result = Boat2EvidenceOverlay.Help; return true;
                case "pause": result = Boat2EvidenceOverlay.Pause; return true;
                case "pause-help": result = Boat2EvidenceOverlay.PauseHelp; return true;
                case "direct-leave": result = Boat2EvidenceOverlay.DirectLeave; return true;
                case "nested-leave": result = Boat2EvidenceOverlay.NestedLeave; return true;
                case "loading": result = Boat2EvidenceOverlay.Loading; return true;
                case "recovery": result = Boat2EvidenceOverlay.Recovery; return true;
                case "app-pause": result = Boat2EvidenceOverlay.AppBackgroundPause; return true;
                case "audio-focus-pause": result = Boat2EvidenceOverlay.AudioFocusPause; return true;
                case "help-app-pause": result = Boat2EvidenceOverlay.HelpAppBackgroundPause; return true;
                case "help-audio-focus-pause": result = Boat2EvidenceOverlay.HelpAudioFocusPause; return true;
                case "leave-app-pause": result = Boat2EvidenceOverlay.LeaveAppBackgroundPause; return true;
                case "leave-audio-focus-pause": result = Boat2EvidenceOverlay.LeaveAudioFocusPause; return true;
                case "help-app-pause-help-back": result = Boat2EvidenceOverlay.HelpAppPauseHelpBack; return true;
                default: result = default; return false;
            }
        }

        private static bool TryFailure(string value, out Boat2EvidenceFailure result)
        {
            switch (value)
            {
                case "none": result = Boat2EvidenceFailure.None; return true;
                case "entry-timeout": result = Boat2EvidenceFailure.EntryTimeout; return true;
                case "entry-asset": result = Boat2EvidenceFailure.EntryAsset; return true;
                case "entry-layout": result = Boat2EvidenceFailure.EntryLayout; return true;
                case "stage2-timeout": result = Boat2EvidenceFailure.Stage2Timeout; return true;
                case "stage2-asset": result = Boat2EvidenceFailure.Stage2Asset; return true;
                case "stage2-layout": result = Boat2EvidenceFailure.Stage2Layout; return true;
                default: result = default; return false;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class Boat2EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Boat2EvidenceOptions.TryGetCapture(
                    Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            yield return null;
            yield return null;
            var deadline = Time.realtimeSinceStartup + 30f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Boat 2 evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == Boat2EvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar)
                || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics())
            { Fail("Boat 2 evidence did not reach Journey Overview.", 3); yield break; }
            yield return null;
            while (!_host.JourneyInputReady && Time.realtimeSinceStartup < deadline) yield return null;

            if (!_host.EnterBoat1ForDiagnostics())
            { Fail("Boat 2 evidence did not reach Boat 1.", 4); yield break; }
            yield return null;
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics())
            { Fail("Boat 2 evidence did not reach Stage 1 entry.", 5); yield break; }
            while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage1InputReady || !BeginBoat2Entry())
            { Fail("Boat 2 evidence did not start the Boat 2 entry boundary.", 6); yield break; }

            if (IsEntryFailure(options.Failure)) ApplyEntryFailure(options.Failure);
            else
            {
                while ((!_host.Boat2InputReady || _host.CurrentRoute != RouteId.Boat2)
                       && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_host.Boat2InputReady)
                { Fail("Boat 2 evidence did not enter Boat 2.", 7); yield break; }

                if (IsStage2Failure(options.Failure))
                {
                    _host.PresentBoat2BoundaryForDiagnostics();
                    while (!_host.Boat2BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (!_host.Boat2BoundaryLoading)
                    { Fail("Boat 2 evidence did not reach the Stage 2 boundary.", 8); yield break; }
                    ApplyStage2Failure(options.Failure);
                }
                else
                {
                    if (options.Beat != Boat2EvidenceBeat.Choice)
                        _host.PresentBoat2ChoiceForDiagnostics(ToChoice(options.Choice));
                    var target = TargetElapsed(options.Beat);
                    while (_host.Boat2Snapshot.TravelElapsedSeconds < target
                           && !_host.Boat2BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    ApplyOverlay(options.Overlay);
                    if (options.Beat == Boat2EvidenceBeat.Arrival
                        && options.Overlay == Boat2EvidenceOverlay.Loading)
                        while (!_host.Boat2BoundaryLoading && Time.realtimeSinceStartup < deadline)
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

        private void ApplyEntryFailure(Boat2EvidenceFailure failure)
        {
            if (failure == Boat2EvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Boat2EvidenceFailure.EntryAsset) _host.ReportStage1BoundaryAssetFailure();
            else _host.ReportBoat2EntryLayoutFailureForDiagnostics();
        }

        private bool BeginBoat2Entry()
        {
            return _host.PresentStage1ControlForDiagnostics(Stage1StageView.PlayControlId)
                && _host.AttachStage1MarkerForDiagnostics(false)
                && _host.MoveStage1MarkerForDiagnostics(0.5d)
                && _host.PresentStage1ControlForDiagnostics(Stage1StageView.ConfirmControlId)
                && _host.CompleteStage1OutcomeForDiagnostics()
                && _host.PresentStage1ControlForDiagnostics(Stage1StageView.ContinueControlId);
        }

        private void ApplyStage2Failure(Boat2EvidenceFailure failure)
        {
            if (failure == Boat2EvidenceFailure.Stage2Timeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Boat2EvidenceFailure.Stage2Asset) _host.ReportBoat2BoundaryAssetFailure();
            else _host.TryApplyBoat2Layout(360f, 182f, 360f, 182f, 326f, 88);
        }

        private void ApplyOverlay(Boat2EvidenceOverlay overlay)
        {
            if (overlay == Boat2EvidenceOverlay.Help) _host.PresentBoat2HelpForDiagnostics();
            else if (overlay == Boat2EvidenceOverlay.Pause) _host.PresentBoat2PauseForDiagnostics();
            else if (overlay == Boat2EvidenceOverlay.PauseHelp) _host.PresentBoat2PauseHelpForDiagnostics();
            else if (overlay == Boat2EvidenceOverlay.DirectLeave) _host.PresentBoat2LeaveForDiagnostics(false);
            else if (overlay == Boat2EvidenceOverlay.NestedLeave) _host.PresentBoat2LeaveForDiagnostics(true);
            else if (overlay == Boat2EvidenceOverlay.Loading)
            {
                if (_host.Boat2Snapshot.State != Boat2State.Arriving)
                    _host.PresentBoat2BoundaryForDiagnostics();
                _host.CompleteBoat2ArrivalForDiagnostics();
            }
            else if (overlay == Boat2EvidenceOverlay.AppBackgroundPause)
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AppBackground);
            else if (overlay == Boat2EvidenceOverlay.AudioFocusPause)
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AudioFocusLoss);
            else if (overlay == Boat2EvidenceOverlay.HelpAppBackgroundPause)
            {
                _host.PresentBoat2HelpForDiagnostics();
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AppBackground);
            }
            else if (overlay == Boat2EvidenceOverlay.HelpAudioFocusPause)
            {
                _host.PresentBoat2HelpForDiagnostics();
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AudioFocusLoss);
            }
            else if (overlay == Boat2EvidenceOverlay.LeaveAppBackgroundPause)
            {
                _host.PresentBoat2LeaveForDiagnostics(false);
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AppBackground);
            }
            else if (overlay == Boat2EvidenceOverlay.LeaveAudioFocusPause)
            {
                _host.PresentBoat2LeaveForDiagnostics(false);
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AudioFocusLoss);
            }
            else if (overlay == Boat2EvidenceOverlay.HelpAppPauseHelpBack)
            {
                _host.PresentBoat2HelpForDiagnostics();
                _host.PresentBoat2InterruptionForDiagnostics(Boat2PauseReason.AppBackground);
                _host.PresentBoat2PauseHelpBackForDiagnostics();
            }
        }

        private static bool IsEntryFailure(Boat2EvidenceFailure failure) =>
            failure == Boat2EvidenceFailure.EntryTimeout || failure == Boat2EvidenceFailure.EntryAsset
            || failure == Boat2EvidenceFailure.EntryLayout;

        private static bool IsStage2Failure(Boat2EvidenceFailure failure) =>
            failure == Boat2EvidenceFailure.Stage2Timeout || failure == Boat2EvidenceFailure.Stage2Asset
            || failure == Boat2EvidenceFailure.Stage2Layout;

        private static Boat2Choice ToChoice(Boat2EvidenceChoice choice) =>
            choice == Boat2EvidenceChoice.FieldBank ? Boat2Choice.FieldBank
            : choice == Boat2EvidenceChoice.Dock ? Boat2Choice.Dock : Boat2Choice.FollowIbis;

        private static double TargetElapsed(Boat2EvidenceBeat beat)
        {
            if (beat == Boat2EvidenceBeat.Travel2) return 2d;
            if (beat == Boat2EvidenceBeat.Travel4) return 4d;
            if (beat == Boat2EvidenceBeat.Arrival) return 6d;
            return 0d;
        }

        private static Vector2Int ResolutionFor(Boat2EvidenceRatio ratio)
        {
            if (ratio == Boat2EvidenceRatio.FourThree) return new Vector2Int(1440, 1080);
            if (ratio == Boat2EvidenceRatio.UltraWide) return new Vector2Int(1920, 800);
            return new Vector2Int(1600, 900);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            var safeWidth = Mathf.Max(1f, resolution.x - 64f);
            var safeHeight = Mathf.Max(1f, resolution.y - 64f);
            if (_host.ActiveBoat2View != null)
                _host.TryApplyBoat2Layout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
            else if (_host.ActiveJourneyOverviewView != null)
                _host.TryApplyJourneyLayout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
        }

        private void WriteCapture(Boat2EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Boat2EvidenceCamera");
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
                boat_state = _host.Boat2Snapshot.State.ToString(), choice = _host.Boat2Snapshot.Choice.ToString(),
                travel_elapsed_seconds = _host.Boat2Snapshot.TravelElapsedSeconds,
                arrival_committed = _host.Boat2Snapshot.ArrivalCommitted,
                input_ready = _host.Boat2InputReady, boundary_loading = _host.Boat2BoundaryLoading,
                arrival_sfx_playing = _host.Boat2ArrivalSfxPlaying,
                ibis_sfx_playing = _host.Boat2IbisSfxPlaying,
                navigation_transaction_id = _host.ActiveNavigationId?.ToString() ?? string.Empty,
                navigation_caller = _host.ActiveNavigationCaller?.ToString() ?? string.Empty,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                stage2_type_present = Type.GetType("Exloris.Runtime.Stage2.Stage2StageHost, Exloris.Runtime") != null,
                stage2_entry_completed = _host.CurrentRoute == RouteId.Stage2,
                stale_completion_accepted = _host.TryCompleteNavigation(Guid.NewGuid()),
                pause_state = _host.Boat2PauseState.ToString(),
                pause_reason = _host.Boat2LastPauseReason?.ToString() ?? string.Empty,
                pause_caller_state = _host.Boat2PauseCallerState.ToString(),
                leave_caller_state = _host.Boat2DirectLeaveCallerState.ToString(),
                help_visible = _host.ActiveBoat2HelpView != null && _host.ActiveBoat2HelpView.IsVisible,
                leave_visible = _host.ActiveLeaveConfirmationView != null && _host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state = EffectiveRecovery().ToString(),
                semantic_control_count = _host.ActiveBoat2View == null ? 0 : _host.ActiveBoat2View.SemanticControlCount,
                display_semantic_control_count = _host.ActiveBoat2View == null ? 0 : _host.ActiveBoat2View.DisplaySemanticControlCount,
                accepted_command_count = _host.Boat2AcceptedCommandCount,
                frame_samples = performance.SampleCount, p95_frame_ms = performance.P95FrameMilliseconds,
                p99_frame_ms = performance.P99FrameMilliseconds,
                maximum_frame_ms = performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = performance.AllocatedMemoryMiB, reserved_mib = performance.ReservedMemoryMiB,
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"BOAT2_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery()
        {
            return _host.Boat2RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Boat2RecoveryState : _host.Stage1RecoveryState;
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
            public string failure, route, session_location, boat_state, choice;
            public string navigation_transaction_id, navigation_caller, navigation_destination;
            public string pause_state, pause_reason, pause_caller_state, leave_caller_state, recovery_state;
            public bool sound_enabled, captions_enabled, reduced_motion, arrival_committed;
            public bool input_ready, boundary_loading, help_visible, leave_visible;
            public bool arrival_sfx_playing, ibis_sfx_playing;
            public bool stage2_type_present, stage2_entry_completed, stale_completion_accepted;
            public double travel_elapsed_seconds;
            public float p95_frame_ms, p99_frame_ms, maximum_frame_ms, allocated_mib, reserved_mib;
        }
    }
}
