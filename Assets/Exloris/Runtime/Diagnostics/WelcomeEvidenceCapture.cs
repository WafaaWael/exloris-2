using System;
using System.Collections;
using System.IO;
using System.Globalization;
using Exloris.Core.Navigation;
using Exloris.Runtime.UI;
using UnityEngine;

namespace Exloris.Runtime.Diagnostics
{
    public enum WelcomeEvidenceScenario
    {
        Welcome,
        Interruption,
        SessionChoice
    }

    public readonly struct WelcomeEvidenceCaptureOptions
    {
        public WelcomeEvidenceCaptureOptions(
            string path,
            WelcomeEvidenceScenario scenario,
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
        public WelcomeEvidenceScenario Scenario { get; }
        public bool Muted { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public double DelaySeconds { get; }
    }

    public static class WelcomeEvidenceOptions
    {
        public static bool TryGetCapture(string[] arguments, out WelcomeEvidenceCaptureOptions options)
        {
            options = default;
            if (!TryGetValue(arguments, "-welcomeCapture", out var path) || string.IsNullOrWhiteSpace(path))
                return false;

            var scenario = WelcomeEvidenceScenario.Welcome;
            if (TryGetValue(arguments, "-welcomeScenario", out var scenarioValue) &&
                !Enum.TryParse(scenarioValue, true, out scenario))
                return false;

            var delaySeconds = 0d;
            if (TryGetValue(arguments, "-welcomeCaptureDelay", out var delayValue) &&
                (!double.TryParse(delayValue, NumberStyles.Float, CultureInfo.InvariantCulture, out delaySeconds) ||
                 delaySeconds < 0d || delaySeconds > 30d))
                return false;

            options = new WelcomeEvidenceCaptureOptions(
                path,
                scenario,
                Contains(arguments, "-welcomeMuted"),
                Contains(arguments, "-welcomeCaptions"),
                Contains(arguments, "-welcomeReducedMotion"),
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
            {
                if (string.Equals(value, option, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }

    [DisallowMultipleComponent]
    public sealed class WelcomeEvidenceCapture : MonoBehaviour
    {
        private ScreenHost _host;

        private void Awake()
        {
            _host = GetComponent<ScreenHost>();
        }

        private IEnumerator Start()
        {
            if (!WelcomeEvidenceOptions.TryGetCapture(Environment.GetCommandLineArgs(), out var options))
                yield break;

            var deadline = Time.realtimeSinceStartup + 20f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while ((!_host.WelcomeInputReady || _host.ActiveWelcomeView == null) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!_host.WelcomeInputReady || _host.ActiveWelcomeView == null)
            {
                Debug.LogError("Welcome evidence capture did not reach usable Welcome before timeout.");
                Application.Quit(2);
                yield break;
            }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                !options.Muted, options.CaptionsEnabled, options.ReducedMotion);
            if (options.Scenario == WelcomeEvidenceScenario.Interruption)
                _host.HandleWelcomeInterruptionForTests();
            else if (options.Scenario == WelcomeEvidenceScenario.SessionChoice &&
                     !_host.PresentSessionChoiceForDiagnostics())
            {
                Debug.LogError("Welcome evidence capture could not present Session Choice.");
                Application.Quit(3);
                yield break;
            }

            if (options.DelaySeconds > 0d)
                yield return new WaitForSecondsRealtime((float)options.DelaySeconds);
            yield return null;
            yield return new WaitForEndOfFrame();
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Screen.width, Screen.height), 0, 0);
            texture.Apply();
            File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            Destroy(texture);

            var view = _host.ActiveWelcomeView;
            var record = new EvidenceRecord
            {
                width = Screen.width,
                height = Screen.height,
                route = _host.CurrentRoute.ToString(),
                background_variant = view.BackgroundVariant.ToString(),
                scenario = options.Scenario.ToString(),
                muted = options.Muted,
                captions_enabled = _host.WelcomeCaptionsEnabled,
                reduced_motion = _host.ReducedMotionEnabled,
                inputs_active = _host.InputsActive,
                normal_controls_enabled = view.NormalControlsEnabled,
                technical_overlay = view.TechnicalOverlayState.ToString(),
                control_ids = string.Join(",", view.ControlIds),
                technical_control_ids = string.Join(",", view.TechnicalControlIds),
                session_choice_control_ids = _host.ActiveSessionChoiceView == null
                    ? string.Empty
                    : string.Join(",", _host.ActiveSessionChoiceView.ControlIds),
                last_accepted_control = _host.LastAcceptedControlId ?? string.Empty,
                navigation_destination = _host.ActiveNavigationDestination?.ToString() ?? string.Empty,
                welcome_input_ready = _host.WelcomeInputReady,
                session_choice_visible = _host.ActiveSessionChoiceView != null && _host.ActiveSessionChoiceView.IsVisible
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"WELCOME_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
            Application.Quit(0);
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width;
            public int height;
            public string route;
            public string background_variant;
            public string scenario;
            public bool muted;
            public bool captions_enabled;
            public bool reduced_motion;
            public bool inputs_active;
            public bool normal_controls_enabled;
            public string technical_overlay;
            public string control_ids;
            public string technical_control_ids;
            public string session_choice_control_ids;
            public string last_accepted_control;
            public string navigation_destination;
            public bool welcome_input_ready;
            public bool session_choice_visible;
        }
    }
}
