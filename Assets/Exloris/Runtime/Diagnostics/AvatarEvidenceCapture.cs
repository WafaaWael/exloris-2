using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum AvatarEvidenceScenario
    {
        SelectBoyLeft,
        SelectGirlLeft,
        PreviewBoy,
        PreviewGirl,
        LeaveSelect,
        LeavePreview,
        Interruption,
        RecoverySelect,
        SessionResumePreview
    }

    public readonly struct AvatarEvidenceCaptureOptions
    {
        public AvatarEvidenceCaptureOptions(
            string path,
            AvatarEvidenceScenario scenario,
            bool muted,
            bool captionsEnabled,
            bool reducedMotion,
            double delaySeconds)
        {
            Path = path;
            Scenario = scenario;
            Muted = muted;
            CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion;
            DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public AvatarEvidenceScenario Scenario { get; }
        public bool Muted { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public double DelaySeconds { get; }
    }

    public static class AvatarEvidenceOptions
    {
        public static bool TryGetCapture(string[] arguments, out AvatarEvidenceCaptureOptions options)
        {
            options = default;
            if (!TryGetValue(arguments, "-avatarCapture", out var path) ||
                string.IsNullOrWhiteSpace(path))
                return false;

            var scenario = AvatarEvidenceScenario.SelectBoyLeft;
            if (TryGetValue(arguments, "-avatarScenario", out var scenarioValue) &&
                !Enum.TryParse(scenarioValue, true, out scenario))
                return false;

            var delaySeconds = 0d;
            if (TryGetValue(arguments, "-avatarCaptureDelay", out var delayValue) &&
                (!double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture,
                    out delaySeconds) || delaySeconds < 0d || delaySeconds > 30d))
                return false;

            options = new AvatarEvidenceCaptureOptions(
                path,
                scenario,
                Contains(arguments, "-avatarMuted"),
                Contains(arguments, "-avatarCaptions"),
                Contains(arguments, "-avatarReducedMotion"),
                delaySeconds);
            return true;
        }

        private static bool TryGetValue(string[] arguments, string option, out string value)
        {
            value = null;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                value = arguments[index + 1];
                return true;
            }
            return false;
        }

        private static bool Contains(string[] arguments, string option)
        {
            if (arguments == null) return false;
            foreach (var value in arguments)
                if (string.Equals(value, option, StringComparison.Ordinal)) return true;
            return false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class AvatarEvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;

        private void Awake()
        {
            _host = GetComponent<ScreenHost>();
        }

        private IEnumerator Start()
        {
            if (!AvatarEvidenceOptions.TryGetCapture(
                Environment.GetCommandLineArgs(), out var options)) yield break;

            var deadline = Time.realtimeSinceStartup + 20f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (!_host.WelcomeInputReady)
            {
                Fail("Avatar evidence capture did not reach usable Welcome before timeout.", 2);
                yield break;
            }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                !options.Muted, options.CaptionsEnabled, options.ReducedMotion);
            var initialLeft = options.Scenario == AvatarEvidenceScenario.SelectGirlLeft
                ? AvatarChoice.Girl
                : AvatarChoice.Boy;
            var onsetStartedAt = Time.realtimeSinceStartupAsDouble;
            if (!_host.EnterAvatarForDiagnostics(initialLeft))
            {
                Fail("Avatar evidence capture could not enter Avatar Select.", 3);
                yield break;
            }
            yield return null;
            var onsetSeconds = Time.realtimeSinceStartupAsDouble - onsetStartedAt;

            if (options.Scenario == AvatarEvidenceScenario.PreviewBoy &&
                !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Boy) ||
                options.Scenario == AvatarEvidenceScenario.PreviewGirl &&
                !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Girl) ||
                options.Scenario == AvatarEvidenceScenario.LeavePreview &&
                !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Girl) ||
                options.Scenario == AvatarEvidenceScenario.Interruption &&
                !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Girl) ||
                options.Scenario == AvatarEvidenceScenario.SessionResumePreview &&
                !_host.PresentAvatarPreviewForDiagnostics(AvatarChoice.Girl))
            {
                Fail("Avatar evidence capture could not present the requested preview.", 4);
                yield break;
            }
            if (options.Scenario == AvatarEvidenceScenario.PreviewBoy ||
                options.Scenario == AvatarEvidenceScenario.PreviewGirl ||
                options.Scenario == AvatarEvidenceScenario.LeavePreview ||
                options.Scenario == AvatarEvidenceScenario.Interruption ||
                options.Scenario == AvatarEvidenceScenario.SessionResumePreview)
                yield return null;

            if ((options.Scenario == AvatarEvidenceScenario.LeaveSelect ||
                 options.Scenario == AvatarEvidenceScenario.LeavePreview) &&
                !_host.PresentAvatarLeaveForDiagnostics())
            {
                Fail("Avatar evidence capture could not present Leave.", 5);
                yield break;
            }
            if (options.Scenario == AvatarEvidenceScenario.Interruption &&
                !_host.HandleAvatarInterruptionForTests())
            {
                Fail("Avatar evidence capture could not present interruption.", 6);
                yield break;
            }
            if (options.Scenario == AvatarEvidenceScenario.RecoverySelect &&
                !_host.PresentAvatarRecoveryForDiagnostics())
            {
                Fail("Avatar evidence capture could not present recovery.", 7);
                yield break;
            }
            if (options.Scenario == AvatarEvidenceScenario.SessionResumePreview &&
                !_host.PresentAvatarSessionResumeForDiagnostics())
            {
                Fail("Avatar evidence capture could not resume the Preview session.", 8);
                yield break;
            }

            var captureAt = Time.realtimeSinceStartupAsDouble + options.DelaySeconds;
            do
            {
                yield return null;
                _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
            }
            while (Time.realtimeSinceStartupAsDouble < captureAt);
            yield return new WaitForEndOfFrame();

            WriteCapture(options, onsetSeconds);
            Application.Quit(0);
        }

        private void WriteCapture(AvatarEvidenceCaptureOptions options, double onsetSeconds)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            Destroy(texture);

            var report = FoundationPerformanceMetrics.Summarize(
                _frameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(),
                Screen.width,
                Screen.height);
            var select = _host.ActiveAvatarSelectView;
            var preview = _host.ActiveAvatarPreviewView;
            var leave = _host.ActiveLeaveConfirmationView;
            var record = new EvidenceRecord
            {
                width = Screen.width,
                height = Screen.height,
                route = _host.CurrentRoute.ToString(),
                scenario = options.Scenario.ToString(),
                left_choice = select == null ? string.Empty : select.LeftChoice.ToString(),
                preview_choice = _host.AvatarPreviewChoice?.ToString() ?? string.Empty,
                muted = options.Muted,
                captions_enabled = _host.AvatarCaptionsEnabled,
                voice_playing = _host.AvatarVoicePlaying,
                caption_visible = select != null && select.CaptionVisible ||
                                  preview != null && preview.CaptionVisible ||
                                  leave != null && leave.CaptionVisible,
                visual_equivalent_visible = select != null && select.VisualEquivalentVisible ||
                                            preview != null && preview.VisualEquivalentVisible ||
                                            leave != null && leave.VisualEquivalentVisible,
                reduced_motion = _host.ReducedMotionEnabled,
                input_ready = _host.AvatarInputReady,
                select_controls_enabled = select != null && select.ControlsEnabled,
                preview_controls_enabled = preview != null && preview.ControlsEnabled,
                leave_visible = leave != null && leave.IsVisible,
                technical_overlay = leave == null ? string.Empty : leave.TechnicalOverlayState.ToString(),
                current_preview_token = _host.CurrentAvatarPreviewToken,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                session_location = _host.SessionLocation.ToString(),
                onset_seconds = onsetSeconds,
                frame_samples = report.SampleCount,
                p95_frame_ms = report.P95FrameMilliseconds,
                p99_frame_ms = report.P99FrameMilliseconds,
                maximum_frame_ms = report.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = report.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = report.AllocatedMemoryMiB,
                reserved_mib = report.ReservedMemoryMiB
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"AVATAR_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
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
            public string route;
            public string scenario;
            public string left_choice;
            public string preview_choice;
            public bool muted;
            public bool captions_enabled;
            public bool voice_playing;
            public bool caption_visible;
            public bool visual_equivalent_visible;
            public bool reduced_motion;
            public bool input_ready;
            public bool select_controls_enabled;
            public bool preview_controls_enabled;
            public bool leave_visible;
            public string technical_overlay;
            public long current_preview_token;
            public string navigation_destination;
            public string session_location;
            public double onset_seconds;
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
