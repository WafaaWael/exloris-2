using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class CompletionAdultReportEvidenceCapture : MonoBehaviour
    {
        private static readonly HashSet<string> Cases = new HashSet<string>(StringComparer.Ordinal)
        {
            "completion-start", "completion-reveal", "completion-settle", "completion-ready",
            "completion-performance", "completion-pause", "completion-app-pause", "completion-leave",
            "adult-0", "adult-25", "adult-50", "adult-75", "adult-100",
            "adult-release-2999", "adult-cancel", "adult-interruption",
            "adult-pointer-exit",
            "report-empty", "report-full", "report-close", "report-restart",
            "reset-cancel", "reset-confirm",
        };

        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;
        private bool _actionAccepted;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!TryOptions(Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            yield return null; yield return null;

            var deadline = Time.realtimeSinceStartup + 45f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Closing evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);

            if (options.Case == "report-empty")
            {
                if (!_host.PresentWelcomeAdultForDiagnostics())
                { Fail("Empty report evidence did not enter Adult Gate.", 3); yield break; }
            }
            else
            {
                var avatar = options.Child == "boy" ? AvatarChoice.Boy : AvatarChoice.Girl;
                if (!_host.EnterAvatarForDiagnostics(avatar)
                    || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                    || !_host.EnterJourneyForDiagnostics()
                    || !_host.EnterBoat1ForDiagnostics())
                { Fail("Closing evidence could not begin the journey.", 4); yield break; }
                yield return DriveJourneyToCompletion(deadline);
                if (_host.CurrentRoute != RouteId.Completion)
                { Fail("Closing evidence did not reach Completion.", 5); yield break; }
            }

            if (IsLiveAdultInputCase(options.Case))
            {
                yield return ApplyLiveAdultInputCase(options.Case);
                if (!_actionAccepted)
                { Fail("Closing evidence could not create live-input case " + options.Case + ".", 6); yield break; }
            }
            else if (!ApplyCase(options))
            { Fail("Closing evidence could not create case " + options.Case + ".", 6); yield break; }
            yield return null; yield return null;

            if (_host.CurrentRoute == RouteId.ParentReport && _host.ActiveParentReportView != null)
            {
                var scale = options.TextScale >= 1.9f ? 2f : 1f;
                var pseudo = options.PseudoPercent >= 35 ? 1.35f : 1f;
                if (!_host.ActiveParentReportView.TryApplyResponsiveLayout(
                        resolution.x, resolution.y, resolution.x, resolution.y, 326f, 88, scale, pseudo))
                { Fail("Parent Report accessibility layout is infeasible.", 7); yield break; }
            }

            var captureAt = Time.realtimeSinceStartupAsDouble + options.DelaySeconds;
            do
            {
                yield return null;
                _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f);
            } while (Time.realtimeSinceStartupAsDouble < captureAt);
            yield return new WaitForEndOfFrame();
            WriteCapture(options, resolution);
            Application.Quit(0);
        }

        private IEnumerator DriveJourneyToCompletion(float deadline)
        {
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics()) yield break;
            while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentStage1ControlForDiagnostics(Stage1StageView.PlayControlId)
                || !_host.AttachStage1MarkerForDiagnostics(false)
                || !_host.MoveStage1MarkerForDiagnostics(0.5d)
                || !_host.PresentStage1ControlForDiagnostics(Stage1StageView.ConfirmControlId)
                || !_host.CompleteStage1OutcomeForDiagnostics()
                || !_host.PresentStage1ControlForDiagnostics(Stage1StageView.ContinueControlId)) yield break;
            while ((!_host.Boat2InputReady || _host.CurrentRoute != RouteId.Boat2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat2ChoiceForDiagnostics(Boat2Choice.Dock)
                || !_host.PresentBoat2BoundaryForDiagnostics()
                || !_host.CompleteBoat2ArrivalForDiagnostics()) yield break;
            while ((!_host.Stage2InputReady || _host.CurrentRoute != RouteId.Stage2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentStage2PlayForDiagnostics()
                || !_host.PresentStage2RiverForDiagnostics(Stage2River.Middle)) yield break;
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
                if (!_host.PresentStage2HotspotForDiagnostics(hotspot)
                    || !_host.CompleteStage2ConsequenceForDiagnostics()) yield break;
            if (!_host.PresentStage2FinishForDiagnostics()) yield break;
            while ((!_host.Boat3InputReady || _host.CurrentRoute != RouteId.Boat3)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat3BoundaryForDiagnostics() || !_host.CompleteBoat3ArrivalForDiagnostics()) yield break;
            while ((!_host.Stage3InputReady || _host.CurrentRoute != RouteId.Stage3)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentStage3PlayForDiagnostics()
                || !_host.PresentStage3PlanForDiagnostics(Stage3Plan.Grain)
                || !_host.CompleteStage3OutcomeForDiagnostics()
                || !_host.PresentStage3FinishForDiagnostics()) yield break;
            while (_host.CurrentRoute != RouteId.Completion && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private bool ApplyCase(Options options)
        {
            switch (options.Case)
            {
                case "completion-start": return true;
                case "completion-reveal": return _host.AdvanceClosingCompletionForDiagnostics(0.6d);
                case "completion-settle": return _host.AdvanceClosingCompletionForDiagnostics(2.4d);
                case "completion-ready":
                case "completion-performance": return ReadyCompletion();
                case "completion-pause": return ReadyCompletion()
                    && Accept(CompletionStageView.PauseControlId);
                case "completion-app-pause": return ReadyCompletion()
                    && (_actionAccepted = _host.PresentClosingInterruptionForDiagnostics());
                case "completion-leave": return ReadyCompletion()
                    && Accept(CompletionStageView.HomeControlId);
                case "adult-0": return EnterAdult();
                case "adult-cancel": return EnterAdult()
                    && Accept(AdultGateView.CancelControlId);
                case "report-empty": return EnterReportFromCurrentAdult();
                case "report-full": return EnterAdult() && EnterReportFromCurrentAdult();
                case "report-close": return EnterAdult() && EnterReportFromCurrentAdult()
                    && Accept(ParentReportView.CloseControlId);
                case "report-restart": return EnterAdult() && EnterReportFromCurrentAdult()
                    && Accept(ParentReportView.RestartControlId);
                case "reset-cancel": return EnterAdult() && EnterReportFromCurrentAdult()
                    && Accept(ParentReportView.RestartControlId)
                    && Accept(AdultGateView.HoldControlId, 3d)
                    && Accept(AdultGateView.ConfirmControlId)
                    && Accept(AdultGateView.CancelControlId);
                case "reset-confirm": return EnterAdult() && EnterReportFromCurrentAdult()
                    && Accept(ParentReportView.RestartControlId)
                    && Accept(AdultGateView.HoldControlId, 3d)
                    && Accept(AdultGateView.ConfirmControlId)
                    && Accept(AdultGateView.ConfirmControlId);
                default: return false;
            }
        }

        private bool ReadyCompletion() => _host.ClosingCompletionSnapshot.State == CompletionState.Ready
            || _host.AdvanceClosingCompletionForDiagnostics(CompletionController.SemanticSeconds);
        private bool EnterAdult() => ReadyCompletion() && Accept(CompletionStageView.AdultControlId);
        private bool EnterReportFromCurrentAdult() =>
            Accept(AdultGateView.HoldControlId, AdultGateController.HoldSeconds)
            && Accept(AdultGateView.ConfirmControlId);
        private bool Accept(string id, double duration = 0d) =>
            (_actionAccepted = _host.PresentClosingControlForDiagnostics(id, duration));

        private static bool IsLiveAdultInputCase(string name) =>
            name == "adult-25" || name == "adult-50" || name == "adult-75"
            || name == "adult-100" || name == "adult-release-2999"
            || name == "adult-interruption" || name == "adult-pointer-exit";

        private IEnumerator ApplyLiveAdultInputCase(string name)
        {
            if (!EnterAdult()) yield break;
            var mouse = Mouse.current ?? InputSystem.AddDevice<Mouse>();
            var center = _host.ActiveAdultGateView.GetControlScreenBounds(
                AdultGateView.HoldControlId).center;
            var seconds = name == "adult-25" ? 0.75d
                : name == "adult-50" || name == "adult-interruption" ? 1.5d
                : name == "adult-75" ? 2.25d
                : name == "adult-release-2999" ? 2.999d
                : name == "adult-pointer-exit" ? 0.75d
                : 3d;
            var start = InputState.currentTime + 0.01d;
            InputSystem.QueueStateEvent(mouse,
                new MouseState { position = center }.WithButton(MouseButton.Left), start);
            yield return null;
            InputSystem.QueueStateEvent(mouse,
                new MouseState { position = center + Vector2.right }.WithButton(MouseButton.Left),
                start + seconds);
            yield return null;

            if (name == "adult-pointer-exit")
            {
                InputSystem.QueueStateEvent(mouse,
                    new MouseState { position = new Vector2(-100f, -100f) }
                        .WithButton(MouseButton.Left), start + seconds + 0.05d);
                yield return null;
                _actionAccepted = _host.ClosingAdultSnapshot.State == AdultGateState.Holding
                    && _host.ClosingAdultSnapshot.HoldProgress == 0d
                    && _host.ClosingAdultSnapshot.ActivePointerId == -1;
                yield break;
            }
            if (name == "adult-release-2999")
            {
                InputSystem.QueueStateEvent(mouse,
                    new MouseState { position = center + Vector2.right }, start + seconds);
                yield return null;
                _actionAccepted = _host.ClosingAdultSnapshot.State == AdultGateState.Holding
                    && _host.ClosingAdultSnapshot.HoldProgress == 0d;
                yield break;
            }
            if (name == "adult-interruption")
            {
                _actionAccepted = _host.PresentClosingInterruptionForDiagnostics()
                    && _host.ClosingAdultSnapshot.HoldProgress == 0d;
                yield break;
            }
            var expected = seconds / AdultGateController.HoldSeconds;
            _actionAccepted = Math.Abs(_host.ClosingAdultSnapshot.HoldProgress - expected) <= 0.002d;
        }

        private void WriteCapture(Options options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("ClosingEvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255);
            camera.cullingMask = ~0;
            var target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (!canvas.gameObject.activeInHierarchy) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
            }
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(resolution.x, resolution.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, resolution.x, resolution.y), 0, 0);
            texture.Apply(); File.WriteAllBytes(options.Path, texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release();
            Destroy(texture); Destroy(target); Destroy(cameraObject);

            var performance = FoundationPerformanceMetrics.Summarize(
                _frameMilliseconds, Profiler.GetTotalAllocatedMemoryLong(),
                Profiler.GetTotalReservedMemoryLong(), resolution.x, resolution.y);
            var completion = _host.ClosingCompletionSnapshot;
            var adult = _host.ClosingAdultSnapshot;
            var report = _host.ClosingParentReportSnapshot;
            var record = new EvidenceRecord
            {
                width = resolution.x, height = resolution.y,
                requested_ratio = options.Ratio, selected_child = options.Child,
                requested_case = options.Case, sound_enabled = options.SoundEnabled,
                captions_enabled = options.CaptionsEnabled, reduced_motion = _host.ReducedMotionEnabled,
                text_scale = options.TextScale, pseudo_percent = options.PseudoPercent,
                route = _host.CurrentRoute.ToString(), session_location = _host.SessionLocation.ToString(),
                completion_state = completion.State.ToString(), completion_elapsed_seconds = completion.ElapsedSeconds,
                completion_selected_child_count = _host.ActiveCompletionView == null ? 0 : _host.ActiveCompletionView.SelectedChildCount,
                completion_selected_child_opacity = _host.ActiveCompletionView == null ? 0f : _host.ActiveCompletionView.SelectedChildOpacity,
                completion_caption_visible = _host.ActiveCompletionView != null && _host.ActiveCompletionView.CaptionVisible,
                completion_visual_equivalent_visible = _host.ActiveCompletionView != null && _host.ActiveCompletionView.VisualEquivalentVisible,
                adult_state = adult.State.ToString(), adult_hold_progress = adult.HoldProgress,
                adult_purpose = adult.Request.Purpose.ToString(), adult_origin = adult.Request.Origin.ToString(),
                adult_confirm_focusable = _host.ActiveAdultGateView != null && _host.ActiveAdultGateView.ConfirmFocusable,
                adult_progress_aligned = _host.ActiveAdultGateView != null && _host.ActiveAdultGateView.ProgressGeometryAligned,
                report_state = report.State.ToString(), report_card_count = report.Cards == null ? 0 : report.Cards.Count,
                report_rendered_card_count = _host.ActiveParentReportView == null ? 0 : _host.ActiveParentReportView.RenderedCardCount,
                report_scroll_reachable = _host.ActiveParentReportView != null && _host.ActiveParentReportView.ScrollReachable,
                pause_state = _host.ClosingPauseState.ToString(),
                leave_visible = _host.ActiveLeaveConfirmationView != null && _host.ActiveLeaveConfirmationView.IsVisible,
                action_accepted = _actionAccepted, diagnostic_control_present = false,
                frame_samples = performance.SampleCount, p95_frame_ms = performance.P95FrameMilliseconds,
                p99_frame_ms = performance.P99FrameMilliseconds,
                maximum_frame_ms = performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms = performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib = performance.AllocatedMemoryMiB, reserved_mib = performance.ReservedMemoryMiB,
            };
            var jsonPath = Path.ChangeExtension(options.Path, ".json");
            File.WriteAllText(jsonPath, JsonUtility.ToJson(record, true));
            Debug.Log($"CLOSING_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private static bool TryOptions(string[] args, out Options options)
        {
            options = default;
            if (!Get(args, "-closingCapture", null, out var path)
                || !Get(args, "-closingRatio", "16x9", out var ratio)
                || !Get(args, "-closingChild", "girl", out var child)
                || !Get(args, "-closingSound", "off", out var sound)
                || !Get(args, "-closingCaptions", "on", out var captions)
                || !Get(args, "-closingMotion", "full", out var motion)
                || !Get(args, "-closingCase", "completion-ready", out var evidenceCase)
                || !Get(args, "-closingTextScale", "1", out var scaleText)
                || !Get(args, "-closingPseudoPercent", "0", out var pseudoText)
                || !Get(args, "-closingCaptureDelay", "0.1", out var delayText)) return false;
            if (string.IsNullOrWhiteSpace(path) || (ratio != "4x3" && ratio != "16x9" && ratio != "24x10")
                || (child != "boy" && child != "girl") || (sound != "on" && sound != "off")
                || (captions != "on" && captions != "off") || (motion != "full" && motion != "reduced")
                || !Cases.Contains(evidenceCase)
                || !float.TryParse(scaleText, NumberStyles.Float, CultureInfo.InvariantCulture, out var scale)
                || (scale != 1f && scale != 2f)
                || !int.TryParse(pseudoText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pseudo)
                || (pseudo != 0 && pseudo != 35)
                || !double.TryParse(delayText, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
                || delay < 0d || delay > 30d) return false;
            options = new Options(path, ratio, child, sound == "on", captions == "on",
                motion == "reduced", evidenceCase, scale, pseudo, delay);
            return true;
        }

        private static bool Get(string[] args, string name, string fallback, out string value)
        {
            value = fallback;
            var found = false;
            for (var i = 0; args != null && i < args.Length; i++)
            {
                if (!string.Equals(args[i], name, StringComparison.Ordinal)) continue;
                if (found || i + 1 >= args.Length || args[i + 1].StartsWith("-closing", StringComparison.Ordinal))
                    return false;
                value = args[i + 1]; found = true;
            }
            return found || fallback != null;
        }

        private static Vector2Int ResolutionFor(string ratio) => ratio == "4x3"
            ? new Vector2Int(1440, 1080) : ratio == "24x10"
                ? new Vector2Int(1920, 800) : new Vector2Int(1600, 900);
        private static void Fail(string message, int code) { Debug.LogError(message); Application.Quit(code); }

        private readonly struct Options
        {
            public Options(string path, string ratio, string child, bool sound, bool captions,
                bool reduced, string evidenceCase, float textScale, int pseudoPercent, double delay)
            { Path=path;Ratio=ratio;Child=child;SoundEnabled=sound;CaptionsEnabled=captions;
                ReducedMotion=reduced;Case=evidenceCase;TextScale=textScale;PseudoPercent=pseudoPercent;DelaySeconds=delay; }
            public string Path { get; } public string Ratio { get; } public string Child { get; }
            public bool SoundEnabled { get; } public bool CaptionsEnabled { get; } public bool ReducedMotion { get; }
            public string Case { get; } public float TextScale { get; } public int PseudoPercent { get; }
            public double DelaySeconds { get; }
        }

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width,height,pseudo_percent,completion_selected_child_count,report_card_count,
                report_rendered_card_count,frame_samples,maximum_consecutive_over_33_3_ms;
            public string requested_ratio,selected_child,requested_case,route,session_location,
                completion_state,adult_state,adult_purpose,adult_origin,report_state,pause_state;
            public bool sound_enabled,captions_enabled,reduced_motion,completion_caption_visible,
                completion_visual_equivalent_visible,adult_confirm_focusable,adult_progress_aligned,
                report_scroll_reachable,leave_visible,action_accepted,diagnostic_control_present;
            public double completion_elapsed_seconds,adult_hold_progress;
            public float text_scale,completion_selected_child_opacity,p95_frame_ms,p99_frame_ms,
                maximum_frame_ms,allocated_mib,reserved_mib;
        }
    }
}
