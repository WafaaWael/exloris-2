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
using Exloris.Runtime.Stage2;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum Boat3EvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum Boat3EvidenceChild { Boy, Girl }
    public enum Boat3EvidenceChoice { GrainStore, Irrigation, Ibis }
    public enum Boat3EvidenceBeat { Choice, Travel0, Travel2, Travel4, Arrival }
    public enum Boat3EvidenceOverlay
    {
        None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Loading, Recovery,
        AppBackgroundPause, AudioFocusPause, HelpAppBackgroundPause,
        HelpAudioFocusPause, LeaveAppBackgroundPause, LeaveAudioFocusPause,
        HelpAppPauseHelpBack,
    }
    public enum Boat3EvidenceFailure { None, EntryTimeout, EntryAsset, EntryLayout, Stage3Timeout, Stage3Asset, Stage3Layout }

    public readonly struct Boat3EvidenceCaptureOptions
    {
        public Boat3EvidenceCaptureOptions(
            string path, Boat3EvidenceRatio ratio, Boat3EvidenceChild child,
            Boat3EvidenceChoice choice, bool soundEnabled, bool captionsEnabled,
            bool reducedMotion, Boat3EvidenceBeat beat, Boat3EvidenceOverlay overlay,
            Boat3EvidenceFailure failure, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; Choice = choice;
            SoundEnabled = soundEnabled; CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion; Beat = beat; Overlay = overlay;
            Failure = failure; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public Boat3EvidenceRatio Ratio { get; }
        public Boat3EvidenceChild Child { get; }
        public Boat3EvidenceChoice Choice { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public Boat3EvidenceBeat Beat { get; }
        public Boat3EvidenceOverlay Overlay { get; }
        public Boat3EvidenceFailure Failure { get; }
        public double DelaySeconds { get; }
    }

    public static class Boat3EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-boat3Capture", "-boat3Ratio", "-boat3Child", "-boat3Choice", "-boat3Sound",
            "-boat3Captions", "-boat3Motion", "-boat3Beat", "-boat3Overlay", "-boat3Failure",
            "-boat3CaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Boat3EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !TryGetSingleValue(arguments, "-boat3Capture", out var path, out var hasPath)
                || !hasPath || string.IsNullOrWhiteSpace(path)
                || !TryGetOptional(arguments, "-boat3Ratio", "16x9", out var ratioValue)
                || !TryRatio(ratioValue, out var ratio)
                || !TryGetOptional(arguments, "-boat3Child", "girl", out var childValue)
                || !TryChild(childValue, out var child)
                || !TryGetOptional(arguments, "-boat3Choice", "grain-store", out var choiceValue)
                || !TryChoice(choiceValue, out var choice)
                || !TryGetOptional(arguments, "-boat3Sound", "on", out var soundValue)
                || !TryOnOff(soundValue, out var soundEnabled)
                || !TryGetOptional(arguments, "-boat3Captions", "on", out var captionsValue)
                || !TryOnOff(captionsValue, out var captionsEnabled)
                || !TryGetOptional(arguments, "-boat3Motion", "full", out var motionValue)
                || !TryMotion(motionValue, out var reducedMotion)
                || !TryGetOptional(arguments, "-boat3Beat", "choice", out var beatValue)
                || !TryBeat(beatValue, out var beat)
                || !TryGetOptional(arguments, "-boat3Overlay", "none", out var overlayValue)
                || !TryOverlay(overlayValue, out var overlay)
                || !TryGetOptional(arguments, "-boat3Failure", "none", out var failureValue)
                || !TryFailure(failureValue, out var failure)
                || !TryGetOptional(arguments, "-boat3CaptureDelay", "0", out var delayValue)
                || !double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var delaySeconds)
                || double.IsNaN(delaySeconds) || double.IsInfinity(delaySeconds)
                || delaySeconds < 0d || delaySeconds > 30d)
                return false;

            if ((failure == Boat3EvidenceFailure.None && overlay == Boat3EvidenceOverlay.Recovery)
                || (failure != Boat3EvidenceFailure.None && overlay != Boat3EvidenceOverlay.Recovery)
                || (overlay == Boat3EvidenceOverlay.Loading && beat != Boat3EvidenceBeat.Arrival))
                return false;

            options = new Boat3EvidenceCaptureOptions(
                path, ratio, child, choice, soundEnabled, captionsEnabled,
                reducedMotion, beat, overlay, failure, delaySeconds);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-boat3", StringComparison.Ordinal)) continue;
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
                    || arguments[index + 1].StartsWith("-boat3", StringComparison.Ordinal))
                    return false;
                value = arguments[index + 1]; present = true;
            }
            return true;
        }

        private static bool TryRatio(string value, out Boat3EvidenceRatio result)
        {
            switch (value)
            {
                case "4x3": result = Boat3EvidenceRatio.FourThree; return true;
                case "16x9": result = Boat3EvidenceRatio.SixteenNine; return true;
                case "24x10": result = Boat3EvidenceRatio.UltraWide; return true;
                default: result = default; return false;
            }
        }

        private static bool TryChild(string value, out Boat3EvidenceChild result)
        {
            if (value == "boy") { result = Boat3EvidenceChild.Boy; return true; }
            if (value == "girl") { result = Boat3EvidenceChild.Girl; return true; }
            result = default; return false;
        }

        private static bool TryChoice(string value, out Boat3EvidenceChoice result)
        {
            switch (value)
            {
                case "grain-store": result = Boat3EvidenceChoice.GrainStore; return true;
                case "irrigation": result = Boat3EvidenceChoice.Irrigation; return true;
                case "ibis": result = Boat3EvidenceChoice.Ibis; return true;
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

        private static bool TryBeat(string value, out Boat3EvidenceBeat result)
        {
            switch (value)
            {
                case "choice": result = Boat3EvidenceBeat.Choice; return true;
                case "travel-0": result = Boat3EvidenceBeat.Travel0; return true;
                case "travel-2": result = Boat3EvidenceBeat.Travel2; return true;
                case "travel-4": result = Boat3EvidenceBeat.Travel4; return true;
                case "arrival": result = Boat3EvidenceBeat.Arrival; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOverlay(string value, out Boat3EvidenceOverlay result)
        {
            switch (value)
            {
                case "none": result = Boat3EvidenceOverlay.None; return true;
                case "help": result = Boat3EvidenceOverlay.Help; return true;
                case "pause": result = Boat3EvidenceOverlay.Pause; return true;
                case "pause-help": result = Boat3EvidenceOverlay.PauseHelp; return true;
                case "direct-leave": result = Boat3EvidenceOverlay.DirectLeave; return true;
                case "nested-leave": result = Boat3EvidenceOverlay.NestedLeave; return true;
                case "loading": result = Boat3EvidenceOverlay.Loading; return true;
                case "recovery": result = Boat3EvidenceOverlay.Recovery; return true;
                case "app-pause": result = Boat3EvidenceOverlay.AppBackgroundPause; return true;
                case "audio-focus-pause": result = Boat3EvidenceOverlay.AudioFocusPause; return true;
                case "help-app-pause": result = Boat3EvidenceOverlay.HelpAppBackgroundPause; return true;
                case "help-audio-focus-pause": result = Boat3EvidenceOverlay.HelpAudioFocusPause; return true;
                case "leave-app-pause": result = Boat3EvidenceOverlay.LeaveAppBackgroundPause; return true;
                case "leave-audio-focus-pause": result = Boat3EvidenceOverlay.LeaveAudioFocusPause; return true;
                case "help-app-pause-help-back": result = Boat3EvidenceOverlay.HelpAppPauseHelpBack; return true;
                default: result = default; return false;
            }
        }

        private static bool TryFailure(string value, out Boat3EvidenceFailure result)
        {
            switch (value)
            {
                case "none": result = Boat3EvidenceFailure.None; return true;
                case "entry-timeout": result = Boat3EvidenceFailure.EntryTimeout; return true;
                case "entry-asset": result = Boat3EvidenceFailure.EntryAsset; return true;
                case "entry-layout": result = Boat3EvidenceFailure.EntryLayout; return true;
                case "stage3-timeout": result = Boat3EvidenceFailure.Stage3Timeout; return true;
                case "stage3-asset": result = Boat3EvidenceFailure.Stage3Asset; return true;
                case "stage3-layout": result = Boat3EvidenceFailure.Stage3Layout; return true;
                default: result = default; return false;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class Boat3EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Boat3EvidenceOptions.TryGetCapture(
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
            if (!_host.WelcomeInputReady) { Fail("Boat 3 evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == Boat3EvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar)
                || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics())
            { Fail("Boat 3 evidence did not reach Journey Overview.", 3); yield break; }
            yield return null;
            while (!_host.JourneyInputReady && Time.realtimeSinceStartup < deadline) yield return null;

            if (!_host.EnterBoat1ForDiagnostics())
            { Fail("Boat 3 evidence did not reach Boat 1.", 4); yield break; }
            yield return null;
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics())
            { Fail("Boat 3 evidence did not reach Stage 1 entry.", 5); yield break; }
            while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage1InputReady || !BeginBoat2Entry())
            { Fail("Boat 3 evidence did not start the Boat 2 entry boundary.", 6); yield break; }
            while ((!_host.Boat2InputReady || _host.CurrentRoute != RouteId.Boat2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Boat2InputReady
                || !_host.PresentBoat2ChoiceForDiagnostics(Boat2Choice.Dock)
                || !_host.PresentBoat2BoundaryForDiagnostics()
                || !_host.CompleteBoat2ArrivalForDiagnostics())
            { Fail("Boat 3 evidence did not start the Stage 2 entry boundary.", 7); yield break; }
            while ((!_host.Stage2InputReady || _host.CurrentRoute != RouteId.Stage2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage2InputReady || !BeginBoat3Entry())
            { Fail("Boat 3 evidence did not start the Boat 3 entry boundary.", 8); yield break; }

            if (IsEntryFailure(options.Failure)) ApplyEntryFailure(options.Failure);
            else
            {
                while ((!_host.Boat3InputReady || _host.CurrentRoute != RouteId.Boat3)
                       && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_host.Boat3InputReady)
                { Fail("Boat 3 evidence did not enter Boat 3.", 9); yield break; }

                if (IsStage3Failure(options.Failure))
                {
                    _host.PresentBoat3BoundaryForDiagnostics();
                    while (!_host.Boat3BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (!_host.Boat3BoundaryLoading)
                    { Fail("Boat 3 evidence did not reach the Stage 3 boundary.", 10); yield break; }
                    ApplyStage3Failure(options.Failure);
                }
                else
                {
                    if (options.Beat != Boat3EvidenceBeat.Choice)
                        _host.PresentBoat3ChoiceForDiagnostics(ToChoice(options.Choice));
                    var target = TargetElapsed(options.Beat);
                    while (_host.Boat3Snapshot.TravelElapsedSeconds < target
                           && !_host.Boat3BoundaryLoading && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    ApplyOverlay(options.Overlay);
                    if (options.Beat == Boat3EvidenceBeat.Arrival
                        && options.Overlay == Boat3EvidenceOverlay.Loading)
                        while (!_host.Boat3BoundaryLoading && Time.realtimeSinceStartup < deadline)
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

        private void ApplyEntryFailure(Boat3EvidenceFailure failure)
        {
            if (failure == Boat3EvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Boat3EvidenceFailure.EntryAsset) _host.ReportStage2BoundaryAssetFailure();
            else _host.ReportBoat3EntryLayoutFailureForDiagnostics();
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

        private bool BeginBoat3Entry()
        {
            if (!_host.PresentStage2PlayForDiagnostics()
                || !_host.PresentStage2RiverForDiagnostics(Stage2River.High)) return false;
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            {
                if (!_host.PresentStage2HotspotForDiagnostics(hotspot)
                    || !_host.CompleteStage2ConsequenceForDiagnostics()) return false;
            }
            return _host.PresentStage2FinishForDiagnostics();
        }

        private void ApplyStage3Failure(Boat3EvidenceFailure failure)
        {
            if (failure == Boat3EvidenceFailure.Stage3Timeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Boat3EvidenceFailure.Stage3Asset) _host.ReportBoat3BoundaryAssetFailure();
            else _host.TryApplyBoat3Layout(360f, 182f, 360f, 182f, 326f, 88);
        }

        private void ApplyOverlay(Boat3EvidenceOverlay overlay)
        {
            if (overlay == Boat3EvidenceOverlay.Help) _host.PresentBoat3HelpForDiagnostics();
            else if (overlay == Boat3EvidenceOverlay.Pause) _host.PresentBoat3PauseForDiagnostics();
            else if (overlay == Boat3EvidenceOverlay.PauseHelp) _host.PresentBoat3PauseHelpForDiagnostics();
            else if (overlay == Boat3EvidenceOverlay.DirectLeave) _host.PresentBoat3LeaveForDiagnostics(false);
            else if (overlay == Boat3EvidenceOverlay.NestedLeave) _host.PresentBoat3LeaveForDiagnostics(true);
            else if (overlay == Boat3EvidenceOverlay.Loading)
            {
                if (_host.Boat3Snapshot.State != Boat3State.Arriving)
                    _host.PresentBoat3BoundaryForDiagnostics();
                _host.CompleteBoat3ArrivalForDiagnostics();
            }
            else if (overlay == Boat3EvidenceOverlay.AppBackgroundPause)
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AppBackground);
            else if (overlay == Boat3EvidenceOverlay.AudioFocusPause)
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AudioFocusLoss);
            else if (overlay == Boat3EvidenceOverlay.HelpAppBackgroundPause)
            {
                _host.PresentBoat3HelpForDiagnostics();
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AppBackground);
            }
            else if (overlay == Boat3EvidenceOverlay.HelpAudioFocusPause)
            {
                _host.PresentBoat3HelpForDiagnostics();
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AudioFocusLoss);
            }
            else if (overlay == Boat3EvidenceOverlay.LeaveAppBackgroundPause)
            {
                _host.PresentBoat3LeaveForDiagnostics(false);
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AppBackground);
            }
            else if (overlay == Boat3EvidenceOverlay.LeaveAudioFocusPause)
            {
                _host.PresentBoat3LeaveForDiagnostics(false);
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AudioFocusLoss);
            }
            else if (overlay == Boat3EvidenceOverlay.HelpAppPauseHelpBack)
            {
                _host.PresentBoat3HelpForDiagnostics();
                _host.PresentBoat3InterruptionForDiagnostics(Boat3PauseReason.AppBackground);
                _host.PresentBoat3PauseHelpBackForDiagnostics();
            }
        }

        private static bool IsEntryFailure(Boat3EvidenceFailure failure) =>
            failure == Boat3EvidenceFailure.EntryTimeout || failure == Boat3EvidenceFailure.EntryAsset
            || failure == Boat3EvidenceFailure.EntryLayout;

        private static bool IsStage3Failure(Boat3EvidenceFailure failure) =>
            failure == Boat3EvidenceFailure.Stage3Timeout || failure == Boat3EvidenceFailure.Stage3Asset
            || failure == Boat3EvidenceFailure.Stage3Layout;

        private static Boat3Choice ToChoice(Boat3EvidenceChoice choice) =>
            choice == Boat3EvidenceChoice.GrainStore ? Boat3Choice.GrainStore
            : choice == Boat3EvidenceChoice.Irrigation ? Boat3Choice.Irrigation : Boat3Choice.FollowIbis;

        private static double TargetElapsed(Boat3EvidenceBeat beat)
        {
            if (beat == Boat3EvidenceBeat.Travel2) return 2d;
            if (beat == Boat3EvidenceBeat.Travel4) return 4d;
            if (beat == Boat3EvidenceBeat.Arrival) return 6d;
            return 0d;
        }

        private static Vector2Int ResolutionFor(Boat3EvidenceRatio ratio)
        {
            if (ratio == Boat3EvidenceRatio.FourThree) return new Vector2Int(1440, 1080);
            if (ratio == Boat3EvidenceRatio.UltraWide) return new Vector2Int(1920, 800);
            return new Vector2Int(1600, 900);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            var safeWidth = Mathf.Max(1f, resolution.x - 64f);
            var safeHeight = Mathf.Max(1f, resolution.y - 64f);
            if (_host.ActiveBoat3View != null)
                _host.TryApplyBoat3Layout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
            else if (_host.ActiveJourneyOverviewView != null)
                _host.TryApplyJourneyLayout(
                    resolution.x, resolution.y, safeWidth, safeHeight, 326f, 88);
        }

        private void WriteCapture(Boat3EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Boat3EvidenceCamera");
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
                boat_state = _host.Boat3Snapshot.State.ToString(), choice = _host.Boat3Snapshot.Choice.ToString(),
                travel_elapsed_seconds = _host.Boat3Snapshot.TravelElapsedSeconds,
                arrival_committed = _host.Boat3Snapshot.ArrivalCommitted,
                input_ready = _host.Boat3InputReady, boundary_loading = _host.Boat3BoundaryLoading,
                arrival_sfx_playing = _host.Boat3ArrivalSfxPlaying,
                ibis_sfx_playing = _host.Boat3IbisSfxPlaying,
                navigation_transaction_id = _host.ActiveNavigationId?.ToString() ?? string.Empty,
                navigation_caller = _host.ActiveNavigationCaller?.ToString() ?? string.Empty,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                stage3_type_present = Type.GetType("Exloris.Runtime.Stage3.Stage3StageHost, Exloris.Runtime") != null,
                stage3_entry_completed = _host.CurrentRoute == RouteId.Stage3,
                stale_completion_accepted = _host.TryCompleteNavigation(Guid.NewGuid()),
                pause_state = _host.Boat3PauseState.ToString(),
                pause_reason = _host.Boat3LastPauseReason?.ToString() ?? string.Empty,
                pause_caller_state = _host.Boat3PauseCallerState.ToString(),
                leave_caller_state = _host.Boat3DirectLeaveCallerState.ToString(),
                help_visible = _host.ActiveBoat3HelpView != null && _host.ActiveBoat3HelpView.IsVisible,
                leave_visible = _host.ActiveLeaveConfirmationView != null && _host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state = EffectiveRecovery().ToString(),
                semantic_control_count = _host.ActiveBoat3View == null ? 0 : _host.ActiveBoat3View.SemanticControlCount,
                display_semantic_control_count = _host.ActiveBoat3View == null ? 0 : _host.ActiveBoat3View.DisplaySemanticControlCount,
                accepted_command_count = _host.Boat3AcceptedCommandCount,
                frame_samples = performance.SampleCount, p95_frame_ms = performance.P95FrameMilliseconds,
                p99_frame_ms = performance.P99FrameMilliseconds,
                maximum_frame_ms = performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = performance.AllocatedMemoryMiB, reserved_mib = performance.ReservedMemoryMiB,
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"BOAT3_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery()
        {
            return _host.Boat3RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Boat3RecoveryState : _host.Stage2RecoveryState;
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
            public bool stage3_type_present, stage3_entry_completed, stale_completion_accepted;
            public double travel_elapsed_seconds;
            public float p95_frame_ms, p99_frame_ms, maximum_frame_ms, allocated_mib, reserved_mib;
        }
    }
}
