using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum JourneyEvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum JourneyEvidenceBeat { Measure, Read, Help, Ready }
    public enum JourneyEvidenceOverlay { None, Pause, Help, Leave, Recovery }
    public enum JourneyEvidenceFailure { None, EntryTimeout, Boat1Timeout, AssetLoad, Layout }
    public enum JourneyEvidenceProbe { None, HelpPictograms, LockedNotYet }

    public readonly struct JourneyOverviewEvidenceCaptureOptions
    {
        public JourneyOverviewEvidenceCaptureOptions(
            string path,
            JourneyEvidenceRatio ratio,
            bool soundEnabled,
            bool captionsEnabled,
            bool reducedMotion,
            JourneyEvidenceBeat beat,
            JourneyEvidenceOverlay overlay,
            JourneyEvidenceFailure failure,
            JourneyEvidenceProbe probe,
            double delaySeconds)
        {
            Path = path;
            Ratio = ratio;
            SoundEnabled = soundEnabled;
            CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion;
            Beat = beat;
            Overlay = overlay;
            Failure = failure;
            Probe = probe;
            DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public JourneyEvidenceRatio Ratio { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public JourneyEvidenceBeat Beat { get; }
        public JourneyEvidenceOverlay Overlay { get; }
        public JourneyEvidenceFailure Failure { get; }
        public JourneyEvidenceProbe Probe { get; }
        public double DelaySeconds { get; }
    }

    public static class JourneyOverviewEvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-journeyCapture", "-journeyRatio", "-journeySound", "-journeyCaptions",
            "-journeyMotion", "-journeyBeat", "-journeyOverlay", "-journeyFailure",
            "-journeyProbe", "-journeyCaptureDelay",
        };

        public static bool TryGetCapture(
            string[] arguments,
            out JourneyOverviewEvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownJourneyOptions(arguments)
                || !TryGetSingleValue(arguments, "-journeyCapture", out var path, out var hasPath)
                || !hasPath || string.IsNullOrWhiteSpace(path)
                || !TryGetOptional(arguments, "-journeyRatio", "16x9", out var ratioValue)
                || !TryRatio(ratioValue, out var ratio)
                || !TryGetOptional(arguments, "-journeySound", "on", out var soundValue)
                || !TryOnOff(soundValue, out var soundEnabled)
                || !TryGetOptional(arguments, "-journeyCaptions", "on", out var captionsValue)
                || !TryOnOff(captionsValue, out var captionsEnabled)
                || !TryGetOptional(arguments, "-journeyMotion", "full", out var motionValue)
                || !TryMotion(motionValue, out var reducedMotion)
                || !TryGetOptional(arguments, "-journeyBeat", "measure", out var beatValue)
                || !TryBeat(beatValue, out var beat)
                || !TryGetOptional(arguments, "-journeyOverlay", "none", out var overlayValue)
                || !TryOverlay(overlayValue, out var overlay)
                || !TryGetOptional(arguments, "-journeyFailure", "none", out var failureValue)
                || !TryFailure(failureValue, out var failure)
                || !TryGetOptional(arguments, "-journeyProbe", "none", out var probeValue)
                || !TryProbe(probeValue, out var probe)
                || !TryGetOptional(arguments, "-journeyCaptureDelay", "0", out var delayValue)
                || !double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var delaySeconds)
                || double.IsNaN(delaySeconds) || double.IsInfinity(delaySeconds)
                || delaySeconds < 0d || delaySeconds > 30d)
                return false;

            if (probe == JourneyEvidenceProbe.HelpPictograms
                && overlay != JourneyEvidenceOverlay.Help)
                return false;
            if (probe == JourneyEvidenceProbe.LockedNotYet
                && (beat != JourneyEvidenceBeat.Measure
                    || overlay != JourneyEvidenceOverlay.None
                    || failure != JourneyEvidenceFailure.None))
                return false;
            if (failure == JourneyEvidenceFailure.None
                && overlay == JourneyEvidenceOverlay.Recovery
                || failure != JourneyEvidenceFailure.None
                && (overlay != JourneyEvidenceOverlay.Recovery
                    || probe != JourneyEvidenceProbe.None))
                return false;

            options = new JourneyOverviewEvidenceCaptureOptions(
                path, ratio, soundEnabled, captionsEnabled, reducedMotion,
                beat, overlay, failure, probe, delaySeconds);
            return true;
        }

        private static bool HasOnlyKnownJourneyOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-journey", StringComparison.Ordinal))
                    continue;
                var known = false;
                foreach (var option in KnownOptions)
                    if (string.Equals(argument, option, StringComparison.Ordinal)) known = true;
                if (!known) return false;
            }
            return true;
        }

        private static bool TryGetOptional(
            string[] arguments,
            string option,
            string fallback,
            out string value)
        {
            if (!TryGetSingleValue(arguments, option, out value, out var present)) return false;
            if (!present) value = fallback;
            return true;
        }

        private static bool TryGetSingleValue(
            string[] arguments,
            string option,
            out string value,
            out bool present)
        {
            value = null;
            present = false;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                if (present || index + 1 >= arguments.Length
                    || arguments[index + 1].StartsWith("-journey", StringComparison.Ordinal))
                    return false;
                value = arguments[index + 1];
                present = true;
            }
            return true;
        }

        private static bool TryRatio(string value, out JourneyEvidenceRatio result)
        {
            switch (value)
            {
                case "4x3": result = JourneyEvidenceRatio.FourThree; return true;
                case "16x9": result = JourneyEvidenceRatio.SixteenNine; return true;
                case "24x10": result = JourneyEvidenceRatio.UltraWide; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOnOff(string value, out bool result)
        {
            if (value == "on") { result = true; return true; }
            if (value == "off") { result = false; return true; }
            result = false;
            return false;
        }

        private static bool TryMotion(string value, out bool reduced)
        {
            if (value == "full") { reduced = false; return true; }
            if (value == "reduced") { reduced = true; return true; }
            reduced = false;
            return false;
        }

        private static bool TryBeat(string value, out JourneyEvidenceBeat result)
        {
            switch (value)
            {
                case "measure": result = JourneyEvidenceBeat.Measure; return true;
                case "read": result = JourneyEvidenceBeat.Read; return true;
                case "help": result = JourneyEvidenceBeat.Help; return true;
                case "ready": result = JourneyEvidenceBeat.Ready; return true;
                default: result = default; return false;
            }
        }

        private static bool TryOverlay(string value, out JourneyEvidenceOverlay result)
        {
            switch (value)
            {
                case "none": result = JourneyEvidenceOverlay.None; return true;
                case "pause": result = JourneyEvidenceOverlay.Pause; return true;
                case "help": result = JourneyEvidenceOverlay.Help; return true;
                case "leave": result = JourneyEvidenceOverlay.Leave; return true;
                case "recovery": result = JourneyEvidenceOverlay.Recovery; return true;
                default: result = default; return false;
            }
        }

        private static bool TryFailure(string value, out JourneyEvidenceFailure result)
        {
            switch (value)
            {
                case "none": result = JourneyEvidenceFailure.None; return true;
                case "entry-timeout": result = JourneyEvidenceFailure.EntryTimeout; return true;
                case "boat1-timeout": result = JourneyEvidenceFailure.Boat1Timeout; return true;
                case "asset-load": result = JourneyEvidenceFailure.AssetLoad; return true;
                case "layout": result = JourneyEvidenceFailure.Layout; return true;
                default: result = default; return false;
            }
        }

        private static bool TryProbe(string value, out JourneyEvidenceProbe result)
        {
            switch (value)
            {
                case "none": result = JourneyEvidenceProbe.None; return true;
                case "help-pictograms": result = JourneyEvidenceProbe.HelpPictograms; return true;
                case "locked-not-yet": result = JourneyEvidenceProbe.LockedNotYet; return true;
                default: result = default; return false;
            }
        }
    }

    [DisallowMultipleComponent]
    public sealed class JourneyOverviewEvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;

        private void Awake()
        {
            _host = GetComponent<ScreenHost>();
        }

        private IEnumerator Start()
        {
            if (!JourneyOverviewEvidenceOptions.TryGetCapture(
                    Environment.GetCommandLineArgs(), out var options))
                yield break;

            var deadline = Time.realtimeSinceStartup + 20f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!_host.WelcomeInputReady)
            {
                Fail("Journey evidence did not reach Welcome.", 2);
                yield break;
            }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            if (!_host.EnterAvatarForDiagnostics(AvatarChoice.Girl)
                || !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Girl))
            {
                Fail("Journey evidence did not reach Avatar Preview.", 3);
                yield break;
            }

            if (options.Failure == JourneyEvidenceFailure.EntryTimeout)
            {
                _host.DeferNextAvatarTransitionForTests();
                _host.EnterJourneyForDiagnostics();
                _host.ForceNavigationTimeoutForTests();
            }
            else
            {
                if (!_host.EnterJourneyForDiagnostics())
                {
                    Fail("Journey evidence did not enter Journey Overview.", 4);
                    yield break;
                }
                yield return null;
                while (!_host.JourneyInputReady && Time.realtimeSinceStartup < deadline)
                    yield return null;

                if (options.Failure != JourneyEvidenceFailure.None)
                {
                    _host.PresentJourneyBoundaryForDiagnostics();
                    if (options.Failure == JourneyEvidenceFailure.Boat1Timeout)
                        _host.ForceNavigationTimeoutForTests();
                    else if (options.Failure == JourneyEvidenceFailure.AssetLoad)
                        _host.ReportJourneyBoundaryAssetFailure();
                    else
                        _host.TryApplyJourneyLayout(360f, 182f, 360f, 182f, 326f, 88);
                }
                else
                {
                    var target = TargetElapsed(options);
                    while (_host.JourneySnapshot.ElapsedSeconds < target
                           && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (options.Overlay == JourneyEvidenceOverlay.Pause)
                        _host.PresentJourneyPauseForDiagnostics();
                    else if (options.Overlay == JourneyEvidenceOverlay.Help)
                        _host.PresentJourneyHelpForDiagnostics();
                    else if (options.Overlay == JourneyEvidenceOverlay.Leave)
                        _host.PresentJourneyLeaveForDiagnostics();
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
            WriteCapture(options);
            Application.Quit(0);
        }

        private static double TargetElapsed(JourneyOverviewEvidenceCaptureOptions options)
        {
            if (options.ReducedMotion) return 0d;
            switch (options.Beat)
            {
                case JourneyEvidenceBeat.Read: return 3.5d;
                case JourneyEvidenceBeat.Help: return 6.4d;
                case JourneyEvidenceBeat.Ready: return 8.2d;
                default: return 0.4d;
            }
        }

        private void WriteCapture(JourneyOverviewEvidenceCaptureOptions options)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            Destroy(texture);

            var performance = FoundationPerformanceMetrics.Summarize(
                _frameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(),
                Screen.width,
                Screen.height);
            var view = _host.ActiveJourneyOverviewView;
            var help = _host.ActiveJourneyPauseHelpView;
            var avatarRecovery = _host.AvatarRecoveryState;
            var journeyRecovery = _host.JourneyRecoveryState;
            var effectiveRecovery = journeyRecovery != AvatarTechnicalOverlayState.Hidden
                ? journeyRecovery : avatarRecovery;
            var record = new EvidenceRecord
            {
                width = Screen.width,
                height = Screen.height,
                requested_ratio = options.Ratio.ToString(),
                route = _host.CurrentRoute.ToString(),
                session_location = _host.SessionLocation.ToString(),
                sound_enabled = options.SoundEnabled,
                captions_enabled = options.CaptionsEnabled,
                reduced_motion = _host.ReducedMotionEnabled,
                requested_beat = options.Beat.ToString(),
                reveal_state = _host.JourneySnapshot.State.ToString(),
                reveal_token = _host.CurrentJourneyOverviewToken.ToString(),
                elapsed_seconds = _host.JourneySnapshot.ElapsedSeconds,
                revealed_beat_count = _host.JourneySnapshot.RevealedBeatCount,
                input_ready = _host.JourneyInputReady,
                boundary_loading = _host.JourneyBoundaryLoading,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                requested_overlay = options.Overlay.ToString(),
                pause_state = _host.JourneyPauseState.ToString(),
                help_visible = help != null && help.IsVisible,
                leave_visible = _host.ActiveLeaveConfirmationView != null
                    && _host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state = effectiveRecovery.ToString(),
                avatar_recovery_state = avatarRecovery.ToString(),
                journey_recovery_state = journeyRecovery.ToString(),
                timeline_semantic_controls = view == null ? 0 : view.TimelineSemanticControlCount,
                timeline_has_collider = view != null && view.TimelineHasCollider,
                help_display_count = help == null ? 0 : help.DisplayPictogramCount,
                help_display_semantic_count = help == null ? 0 : help.DisplaySemanticControlCount,
                help_control_ids = help == null ? string.Empty : string.Join(",", help.ControlIds),
                accepted_command_count = _host.JourneyAcceptedCommandCount,
                failure = options.Failure.ToString(),
                probe = options.Probe.ToString(),
                frame_samples = performance.SampleCount,
                p95_frame_ms = performance.P95FrameMilliseconds,
                p99_frame_ms = performance.P99FrameMilliseconds,
                maximum_frame_ms = performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = performance.AllocatedMemoryMiB,
                reserved_mib = performance.ReservedMemoryMiB,
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"JOURNEY_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private static void Fail(string message, int exitCode)
        {
            Debug.LogError(message);
            Application.Quit(exitCode);
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width;
            public int height;
            public string requested_ratio;
            public string route;
            public string session_location;
            public bool sound_enabled;
            public bool captions_enabled;
            public bool reduced_motion;
            public string requested_beat;
            public string reveal_state;
            public string reveal_token;
            public double elapsed_seconds;
            public int revealed_beat_count;
            public bool input_ready;
            public bool boundary_loading;
            public string navigation_destination;
            public string requested_overlay;
            public string pause_state;
            public bool help_visible;
            public bool leave_visible;
            public string recovery_state;
            public string avatar_recovery_state;
            public string journey_recovery_state;
            public int timeline_semantic_controls;
            public bool timeline_has_collider;
            public int help_display_count;
            public int help_display_semantic_count;
            public string help_control_ids;
            public int accepted_command_count;
            public string failure;
            public string probe;
            public int frame_samples;
            public float p95_frame_ms;
            public float p99_frame_ms;
            public float maximum_frame_ms;
            public int maximum_consecutive_over_33_3_ms;
            public float allocated_mib;
            public float reserved_mib;
        }
    }
}
