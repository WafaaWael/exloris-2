using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum Stage1EvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum Stage1EvidenceChild { Boy, Girl }
    public enum Stage1EvidenceBeat { Intro, AttachCue, Playing, OutcomePlaying, OutcomeReady, Boat2Loading }
    public enum Stage1EvidenceAttach { None, Tap, Drag }
    public enum Stage1EvidencePosition { Minimum, LowBoundary, Middle, HighBoundary, Maximum }
    public enum Stage1EvidenceOverlay { None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Recovery, AppPause, AudioFocusPause }
    public enum Stage1EvidenceFailure { None, EntryTimeout, EntryAsset, EntryLayout, Boat2Timeout, Boat2Asset, Boat2Layout }
    public enum Stage1EvidenceRecoveryAction { None, Retry, Home }

    public readonly struct Stage1EvidenceCaptureOptions
    {
        public Stage1EvidenceCaptureOptions(
            string path, Stage1EvidenceRatio ratio, Stage1EvidenceChild child,
            bool soundEnabled, bool captionsEnabled, bool reducedMotion,
            Stage1EvidenceBeat beat, Stage1EvidenceAttach attach,
            Stage1EvidencePosition position, int supportTier,
            Stage1EvidenceOverlay overlay, Stage1EvidenceFailure failure,
            Stage1EvidenceRecoveryAction recoveryAction, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; SoundEnabled = soundEnabled;
            CaptionsEnabled = captionsEnabled; ReducedMotion = reducedMotion; Beat = beat;
            Attach = attach; Position = position; SupportTier = supportTier; Overlay = overlay;
            Failure = failure; RecoveryAction = recoveryAction; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public Stage1EvidenceRatio Ratio { get; }
        public Stage1EvidenceChild Child { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public Stage1EvidenceBeat Beat { get; }
        public Stage1EvidenceAttach Attach { get; }
        public Stage1EvidencePosition Position { get; }
        public int SupportTier { get; }
        public Stage1EvidenceOverlay Overlay { get; }
        public Stage1EvidenceFailure Failure { get; }
        public Stage1EvidenceRecoveryAction RecoveryAction { get; }
        public double DelaySeconds { get; }
    }

    public static class Stage1EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-stage1Capture", "-stage1Ratio", "-stage1Child", "-stage1Sound",
            "-stage1Captions", "-stage1Motion", "-stage1Beat", "-stage1Attach",
            "-stage1Position", "-stage1Support", "-stage1Overlay", "-stage1Failure",
            "-stage1Recovery", "-stage1CaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Stage1EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !Get(arguments, "-stage1Capture", null, true, out var path)
                || string.IsNullOrWhiteSpace(path)
                || !Get(arguments, "-stage1Ratio", "16x9", false, out var ratioText)
                || !TryRatio(ratioText, out var ratio)
                || !Get(arguments, "-stage1Child", "girl", false, out var childText)
                || !TryChild(childText, out var child)
                || !Get(arguments, "-stage1Sound", "on", false, out var soundText)
                || !TryOnOff(soundText, out var sound)
                || !Get(arguments, "-stage1Captions", "on", false, out var captionText)
                || !TryOnOff(captionText, out var captions)
                || !Get(arguments, "-stage1Motion", "full", false, out var motionText)
                || !TryMotion(motionText, out var reduced)
                || !Get(arguments, "-stage1Beat", "intro", false, out var beatText)
                || !TryBeat(beatText, out var beat)
                || !Get(arguments, "-stage1Attach", "tap", false, out var attachText)
                || !TryAttach(attachText, out var attach)
                || !Get(arguments, "-stage1Position", "middle", false, out var positionText)
                || !TryPosition(positionText, out var position)
                || !Get(arguments, "-stage1Support", "0", false, out var supportText)
                || !int.TryParse(supportText, NumberStyles.None, CultureInfo.InvariantCulture, out var support)
                || support < 0 || support > 3
                || !Get(arguments, "-stage1Overlay", "none", false, out var overlayText)
                || !TryOverlay(overlayText, out var overlay)
                || !Get(arguments, "-stage1Failure", "none", false, out var failureText)
                || !TryFailure(failureText, out var failure)
                || !Get(arguments, "-stage1Recovery", "none", false, out var recoveryText)
                || !TryRecovery(recoveryText, out var recovery)
                || !Get(arguments, "-stage1CaptureDelay", "0", false, out var delayText)
                || !double.TryParse(delayText, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
                || double.IsNaN(delay) || double.IsInfinity(delay) || delay < 0d || delay > 30d)
                return false;

            if ((failure == Stage1EvidenceFailure.None && overlay == Stage1EvidenceOverlay.Recovery)
                || (failure != Stage1EvidenceFailure.None && overlay != Stage1EvidenceOverlay.Recovery)
                || (failure == Stage1EvidenceFailure.None && recovery != Stage1EvidenceRecoveryAction.None)
                || (support > 0 && (beat != Stage1EvidenceBeat.Playing
                    || (position != Stage1EvidencePosition.Minimum && position != Stage1EvidencePosition.Maximum)))
                || ((beat == Stage1EvidenceBeat.Intro || beat == Stage1EvidenceBeat.AttachCue) && support != 0)
                || (beat == Stage1EvidenceBeat.Boat2Loading
                    && (position == Stage1EvidencePosition.Minimum || position == Stage1EvidencePosition.Maximum)))
                return false;

            options = new Stage1EvidenceCaptureOptions(
                path, ratio, child, sound, captions, reduced, beat, attach,
                position, support, overlay, failure, recovery, delay);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-stage1", StringComparison.Ordinal)) continue;
                var known = false;
                foreach (var option in KnownOptions)
                    if (string.Equals(argument, option, StringComparison.Ordinal)) known = true;
                if (!known) return false;
            }
            return true;
        }

        private static bool Get(string[] arguments, string option, string fallback, bool required, out string value)
        {
            value = fallback; var found = false;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                if (found || index + 1 >= arguments.Length
                    || arguments[index + 1].StartsWith("-stage1", StringComparison.Ordinal)) return false;
                value = arguments[index + 1]; found = true;
            }
            return found || !required;
        }

        private static bool TryRatio(string value, out Stage1EvidenceRatio result)
        { if (value == "4x3") { result=Stage1EvidenceRatio.FourThree;return true; } if(value=="16x9"){result=Stage1EvidenceRatio.SixteenNine;return true;} if(value=="24x10"){result=Stage1EvidenceRatio.UltraWide;return true;} result=default;return false; }
        private static bool TryChild(string value, out Stage1EvidenceChild result)
        { if(value=="boy"){result=Stage1EvidenceChild.Boy;return true;}if(value=="girl"){result=Stage1EvidenceChild.Girl;return true;}result=default;return false; }
        private static bool TryOnOff(string value, out bool result)
        { if(value=="on"){result=true;return true;}if(value=="off"){result=false;return true;}result=false;return false; }
        private static bool TryMotion(string value, out bool result)
        { if(value=="full"){result=false;return true;}if(value=="reduced"){result=true;return true;}result=false;return false; }
        private static bool TryBeat(string value, out Stage1EvidenceBeat result)
        { switch(value){case "intro":result=Stage1EvidenceBeat.Intro;return true;case "attach-cue":result=Stage1EvidenceBeat.AttachCue;return true;case "playing":result=Stage1EvidenceBeat.Playing;return true;case "outcome-playing":result=Stage1EvidenceBeat.OutcomePlaying;return true;case "outcome-ready":result=Stage1EvidenceBeat.OutcomeReady;return true;case "boat2-loading":result=Stage1EvidenceBeat.Boat2Loading;return true;default:result=default;return false;} }
        private static bool TryAttach(string value, out Stage1EvidenceAttach result)
        { if(value=="none"){result=Stage1EvidenceAttach.None;return true;}if(value=="tap"){result=Stage1EvidenceAttach.Tap;return true;}if(value=="drag"){result=Stage1EvidenceAttach.Drag;return true;}result=default;return false; }
        private static bool TryPosition(string value, out Stage1EvidencePosition result)
        { switch(value){case "minimum":result=Stage1EvidencePosition.Minimum;return true;case "low-boundary":result=Stage1EvidencePosition.LowBoundary;return true;case "middle":result=Stage1EvidencePosition.Middle;return true;case "high-boundary":result=Stage1EvidencePosition.HighBoundary;return true;case "maximum":result=Stage1EvidencePosition.Maximum;return true;default:result=default;return false;} }
        private static bool TryOverlay(string value, out Stage1EvidenceOverlay result)
        { switch(value){case "none":result=Stage1EvidenceOverlay.None;return true;case "help":result=Stage1EvidenceOverlay.Help;return true;case "pause":result=Stage1EvidenceOverlay.Pause;return true;case "pause-help":result=Stage1EvidenceOverlay.PauseHelp;return true;case "direct-leave":result=Stage1EvidenceOverlay.DirectLeave;return true;case "nested-leave":result=Stage1EvidenceOverlay.NestedLeave;return true;case "recovery":result=Stage1EvidenceOverlay.Recovery;return true;case "app-pause":result=Stage1EvidenceOverlay.AppPause;return true;case "audio-focus-pause":result=Stage1EvidenceOverlay.AudioFocusPause;return true;default:result=default;return false;} }
        private static bool TryFailure(string value, out Stage1EvidenceFailure result)
        { switch(value){case "none":result=Stage1EvidenceFailure.None;return true;case "entry-timeout":result=Stage1EvidenceFailure.EntryTimeout;return true;case "entry-asset":result=Stage1EvidenceFailure.EntryAsset;return true;case "entry-layout":result=Stage1EvidenceFailure.EntryLayout;return true;case "boat2-timeout":result=Stage1EvidenceFailure.Boat2Timeout;return true;case "boat2-asset":result=Stage1EvidenceFailure.Boat2Asset;return true;case "boat2-layout":result=Stage1EvidenceFailure.Boat2Layout;return true;default:result=default;return false;} }
        private static bool TryRecovery(string value, out Stage1EvidenceRecoveryAction result)
        { if(value=="none"){result=Stage1EvidenceRecoveryAction.None;return true;}if(value=="retry"){result=Stage1EvidenceRecoveryAction.Retry;return true;}if(value=="home"){result=Stage1EvidenceRecoveryAction.Home;return true;}result=default;return false; }
    }

    [DisallowMultipleComponent]
    public sealed class Stage1EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;
        private bool _staleCompletionAccepted;
        private bool _recoveryActionAccepted;
        private Guid? _failedTransactionId;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Stage1EvidenceOptions.TryGetCapture(Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            yield return null; yield return null;
            var deadline = Time.realtimeSinceStartup + 30f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline) yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Stage 1 evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == Stage1EvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar) || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics() || !_host.EnterBoat1ForDiagnostics())
            { Fail("Stage 1 evidence did not reach Boat 1.", 3); yield break; }
            yield return null;
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics())
            { Fail("Stage 1 evidence did not reach its entry boundary.", 4); yield break; }

            if (IsEntryFailure(options.Failure))
            {
                _failedTransactionId = _host.ActiveNavigationId;
                ApplyEntryFailure(options.Failure);
                yield return null;
                ApplyRecoveryAction(options.RecoveryAction);
                yield return null;
            }
            else
            {
                while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                       && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_host.Stage1InputReady) { Fail("Stage 1 evidence did not enter Stage 1.", 5); yield break; }
                if (!DriveStage(options)) { Fail("Stage 1 evidence could not create the requested beat.", 6); yield break; }
                if (IsBoat2Failure(options.Failure))
                {
                    _failedTransactionId = _host.ActiveNavigationId;
                    ApplyBoat2Failure(options.Failure);
                    yield return null;
                    ApplyRecoveryAction(options.RecoveryAction);
                    yield return null;
                }
                else ApplyOverlay(options.Overlay);
            }

            var captureAt = Time.realtimeSinceStartupAsDouble + options.DelaySeconds;
            do { yield return null; _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000f); }
            while (Time.realtimeSinceStartupAsDouble < captureAt);
            yield return new WaitForEndOfFrame();
            ApplyCaptureLayout(resolution); yield return null; yield return new WaitForEndOfFrame();
            WriteCapture(options, resolution);
            Application.Quit(0);
        }

        private bool DriveStage(Stage1EvidenceCaptureOptions options)
        {
            if (options.Beat == Stage1EvidenceBeat.Intro) return true;
            if (!_host.PresentStage1ControlForDiagnostics(Stage1StageView.PlayControlId)) return false;
            if (options.Beat == Stage1EvidenceBeat.AttachCue)
                return _host.PresentStage1ControlForDiagnostics(Stage1StageView.UpControlId);
            if (options.Attach == Stage1EvidenceAttach.None
                || !_host.AttachStage1MarkerForDiagnostics(options.Attach == Stage1EvidenceAttach.Drag)
                || !_host.MoveStage1MarkerForDiagnostics(PositionValue(options.Position))) return false;

            for (var tier = 0; tier < options.SupportTier; tier++)
            {
                if (!_host.PresentStage1ControlForDiagnostics(Stage1StageView.ConfirmControlId)
                    || !_host.CompleteStage1OutcomeForDiagnostics()
                    || !_host.PresentStage1ControlForDiagnostics(Stage1StageView.TryAgainControlId)) return false;
            }
            if (options.Beat == Stage1EvidenceBeat.Playing) return true;
            if (!_host.PresentStage1ControlForDiagnostics(Stage1StageView.ConfirmControlId)) return false;
            if (options.Beat == Stage1EvidenceBeat.OutcomePlaying) return true;
            if (!_host.CompleteStage1OutcomeForDiagnostics()) return false;
            if (options.Beat == Stage1EvidenceBeat.OutcomeReady) return true;
            return _host.PresentStage1ControlForDiagnostics(Stage1StageView.ContinueControlId)
                && _host.Stage1BoundaryLoading;
        }

        private void ApplyOverlay(Stage1EvidenceOverlay overlay)
        {
            if (overlay == Stage1EvidenceOverlay.Help)
                _host.PresentStage1ControlForDiagnostics(Stage1StageView.HelpControlId);
            else if (overlay == Stage1EvidenceOverlay.Pause)
                _host.PresentStage1ControlForDiagnostics(Stage1StageView.PauseControlId);
            else if (overlay == Stage1EvidenceOverlay.PauseHelp)
            {
                _host.PresentStage1ControlForDiagnostics(Stage1StageView.PauseControlId);
                _host.PresentStage1ControlForDiagnostics(PauseOverlayView.HelpControlId);
            }
            else if (overlay == Stage1EvidenceOverlay.DirectLeave)
                _host.PresentStage1ControlForDiagnostics(Stage1StageView.HomeControlId);
            else if (overlay == Stage1EvidenceOverlay.NestedLeave)
            {
                _host.PresentStage1ControlForDiagnostics(Stage1StageView.PauseControlId);
                _host.PresentStage1ControlForDiagnostics(PauseOverlayView.HomeControlId);
            }
            else if (overlay == Stage1EvidenceOverlay.AppPause)
                _host.PresentStage1InterruptionForDiagnostics(Stage1PauseReason.AppBackground);
            else if (overlay == Stage1EvidenceOverlay.AudioFocusPause)
                _host.PresentStage1InterruptionForDiagnostics(Stage1PauseReason.AudioFocusLoss);
        }

        private void ApplyEntryFailure(Stage1EvidenceFailure failure)
        {
            if (failure == Stage1EvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage1EvidenceFailure.EntryAsset) _host.ReportBoat1BoundaryAssetFailure();
            else _host.ReportStage1EntryLayoutFailureForDiagnostics();
        }

        private void ApplyBoat2Failure(Stage1EvidenceFailure failure)
        {
            if (failure == Stage1EvidenceFailure.Boat2Timeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage1EvidenceFailure.Boat2Asset) _host.ReportStage1BoundaryAssetFailure();
            else _host.TryApplyStage1Layout(360, 182, 0, 0, 360, 182, 326, 88);
        }

        private void ApplyRecoveryAction(Stage1EvidenceRecoveryAction action)
        {
            if (action == Stage1EvidenceRecoveryAction.None) return;
            var control = action == Stage1EvidenceRecoveryAction.Retry
                ? LeaveConfirmationView.RetryControlId : LeaveConfirmationView.HomeControlId;
            _recoveryActionAccepted = _host.HandleStage1RecoveryActionForDiagnostics(control);
            if (_recoveryActionAccepted && _failedTransactionId.HasValue)
                _staleCompletionAccepted = _host.TryCompleteNavigation(_failedTransactionId.Value);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            if (_host.ActiveStage1View == null) return;
            _host.TryApplyStage1Layout(
                resolution.x, resolution.y, 32, 32,
                resolution.x - 64, resolution.y - 64,
                Screen.dpi > 0f ? Screen.dpi : 72f, 88);
        }

        private void WriteCapture(Stage1EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Stage1EvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255); camera.cullingMask = ~0;
            var target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            { if (!canvas.gameObject.activeInHierarchy) continue; canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f; }
            Canvas.ForceUpdateCanvases(); camera.Render(); var previous=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);texture.Apply();File.WriteAllBytes(options.Path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(texture);Destroy(target);Destroy(cameraObject);

            var performance=FoundationPerformanceMetrics.Summarize(_frameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),Profiler.GetTotalReservedMemoryLong(),resolution.x,resolution.y);
            var snapshot=_host.Stage1Snapshot;var cue=_host.ActiveStage1View==null?default:_host.ActiveStage1View.LastMediaCue;
            var outcomeFrame=_host.ActiveStage1View==null?default:_host.ActiveStage1View.LastOutcomeFrame;
            var record=new EvidenceRecord
            {
                width=resolution.x,height=resolution.y,requested_ratio=options.Ratio.ToString(),selected_child=options.Child.ToString(),
                sound_enabled=options.SoundEnabled,captions_enabled=options.CaptionsEnabled,reduced_motion=_host.ReducedMotionEnabled,
                requested_beat=options.Beat.ToString(),requested_attach=options.Attach.ToString(),requested_position=options.Position.ToString(),
                requested_support_tier=options.SupportTier,requested_overlay=options.Overlay.ToString(),failure=options.Failure.ToString(),
                recovery_action=options.RecoveryAction.ToString(),route=_host.CurrentRoute.ToString(),session_location=_host.SessionLocation.ToString(),
                stage_state=snapshot.State.ToString(),marker_attached=snapshot.MarkerAttached,marker_position=snapshot.MarkerPosition,
                support_tier=snapshot.SupportTier,option_count=snapshot.OptionCount,outcome=snapshot.Outcome.ToString(),
                outcome_elapsed_seconds=snapshot.OutcomeElapsedSeconds,outcome_fact_recorded=snapshot.OutcomeFactRecorded,
                completion_recorded=snapshot.CompletionRecorded,input_ready=_host.Stage1InputReady,boundary_loading=_host.Stage1BoundaryLoading,
                navigation_destination=_host.ActiveNavigationDestination?.ToString()??string.Empty,pause_state=_host.Stage1PauseState.ToString(),
                pause_reason=_host.Stage1LastPauseReason?.ToString()??string.Empty,help_visible=_host.ActiveStage1HelpView!=null&&_host.ActiveStage1HelpView.IsVisible,
                leave_visible=_host.ActiveLeaveConfirmationView!=null&&_host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state=EffectiveRecovery().ToString(),media_mode=cue.Mode.ToString(),caption_id=cue.CaptionId??string.Empty,
                caption_visible=cue.CaptionVisible,recovery_action_accepted=_recoveryActionAccepted,
                stale_completion_accepted=_staleCompletionAccepted,
                voice_playing=_host.Stage1VoicePlaying,loops_playing=_host.Stage1LoopsPlaying,
                attach_sfx_playing=_host.Stage1AttachSfxPlaying,step_sfx_playing=_host.Stage1StepSfxPlaying,
                outcome_phase=outcomeFrame.Phase.ToString(),outcome_reduced_motion=outcomeFrame.ReducedMotion,
                semantic_control_count=_host.ActiveStage1View==null?0:_host.ActiveStage1View.SemanticControlCount,
                display_semantic_control_count=_host.ActiveStage1View==null?0:_host.ActiveStage1View.DisplaySemanticControlCount,
                accepted_command_count=_host.Stage1AcceptedCommandCount,frame_samples=performance.SampleCount,p95_frame_ms=performance.P95FrameMilliseconds,
                p99_frame_ms=performance.P99FrameMilliseconds,maximum_frame_ms=performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms=performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib=performance.AllocatedMemoryMiB,reserved_mib=performance.ReservedMemoryMiB,
            };
            var jsonPath=Path.ChangeExtension(options.Path,".json");File.WriteAllText(jsonPath,JsonUtility.ToJson(record,true));
            Debug.Log($"STAGE1_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery() =>
            _host.Stage1RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Stage1RecoveryState : _host.Boat1RecoveryState;
        private static bool IsEntryFailure(Stage1EvidenceFailure value) => value==Stage1EvidenceFailure.EntryTimeout||value==Stage1EvidenceFailure.EntryAsset||value==Stage1EvidenceFailure.EntryLayout;
        private static bool IsBoat2Failure(Stage1EvidenceFailure value) => value==Stage1EvidenceFailure.Boat2Timeout||value==Stage1EvidenceFailure.Boat2Asset||value==Stage1EvidenceFailure.Boat2Layout;
        private static double PositionValue(Stage1EvidencePosition value) => value==Stage1EvidencePosition.Minimum?0d:value==Stage1EvidencePosition.LowBoundary?Stage1Controller.InclusiveLow:value==Stage1EvidencePosition.HighBoundary?Stage1Controller.InclusiveHigh:value==Stage1EvidencePosition.Maximum?1d:0.5d;
        private static Vector2Int ResolutionFor(Stage1EvidenceRatio ratio) => ratio==Stage1EvidenceRatio.FourThree?new Vector2Int(1440,1080):ratio==Stage1EvidenceRatio.UltraWide?new Vector2Int(1920,800):new Vector2Int(1600,900);
        private static void Fail(string message,int exitCode){Debug.LogError(message);Application.Quit(exitCode);}

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width,height,requested_support_tier,support_tier,option_count,semantic_control_count,display_semantic_control_count,accepted_command_count,frame_samples,maximum_consecutive_over_33_3_ms;
            public string requested_ratio,selected_child,requested_beat,requested_attach,requested_position,requested_overlay,failure,recovery_action,route,session_location,stage_state,outcome,navigation_destination,pause_state,pause_reason,recovery_state,media_mode,caption_id,outcome_phase;
            public bool sound_enabled,captions_enabled,reduced_motion,marker_attached,outcome_fact_recorded,completion_recorded,input_ready,boundary_loading,help_visible,leave_visible,caption_visible,recovery_action_accepted,stale_completion_accepted,voice_playing,loops_playing,attach_sfx_playing,step_sfx_playing,outcome_reduced_motion;
            public double marker_position,outcome_elapsed_seconds;
            public float p95_frame_ms,p99_frame_ms,maximum_frame_ms,allocated_mib,reserved_mib;
        }
    }
}
