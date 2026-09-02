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
using Exloris.Runtime.Stage3;
using Exloris.Runtime.UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace Exloris.Runtime.Diagnostics
{
    public enum Stage3EvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum Stage3EvidenceChild { Boy, Girl }
    public enum Stage3EvidenceRiver { Low, Middle, High }
    public enum Stage3EvidencePlan { Grain, Channel, Boat }
    public enum Stage3EvidenceBeat
    {
        Intro, ChoosingPlan, OutcomeStart, OutcomeReveal, OutcomeBenefit, OutcomeNeed,
        OutcomeReady, WatchAgain, AnotherPlan, ChangeRiver, RiverBack, CompletionLoading,
    }
    public enum Stage3EvidenceOverlay
    {
        None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Recovery,
        AppBackgroundPause, AudioFocusPause, HelpAppBackgroundPause,
        HelpAudioFocusPause, LeaveAppBackgroundPause, LeaveAudioFocusPause,
    }
    public enum Stage3EvidenceFailure
    {
        None, EntryTimeout, EntryAsset, EntryLayout,
        CompletionTimeout, CompletionAsset, CompletionLayout,
    }
    public enum Stage3EvidenceRecoveryAction { None, Retry, Home }

    public readonly struct Stage3EvidenceCaptureOptions
    {
        public Stage3EvidenceCaptureOptions(string path, Stage3EvidenceRatio ratio,
            Stage3EvidenceChild child, Stage3EvidenceRiver river, Stage3EvidencePlan plan,
            bool soundEnabled, bool captionsEnabled, bool reducedMotion, Stage3EvidenceBeat beat,
            Stage3EvidenceOverlay overlay, Stage3EvidenceFailure failure,
            Stage3EvidenceRecoveryAction recoveryAction, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; River = river; Plan = plan;
            SoundEnabled = soundEnabled; CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion; Beat = beat; Overlay = overlay;
            Failure = failure; RecoveryAction = recoveryAction; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public Stage3EvidenceRatio Ratio { get; }
        public Stage3EvidenceChild Child { get; }
        public Stage3EvidenceRiver River { get; }
        public Stage3EvidencePlan Plan { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public Stage3EvidenceBeat Beat { get; }
        public Stage3EvidenceOverlay Overlay { get; }
        public Stage3EvidenceFailure Failure { get; }
        public Stage3EvidenceRecoveryAction RecoveryAction { get; }
        public double DelaySeconds { get; }
    }

    public static class Stage3EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-stage3Capture", "-stage3Ratio", "-stage3Child", "-stage3River",
            "-stage3Plan", "-stage3Sound", "-stage3Captions", "-stage3Motion",
            "-stage3Beat", "-stage3Overlay", "-stage3Failure", "-stage3Recovery",
            "-stage3CaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Stage3EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !Get(arguments, "-stage3Capture", null, true, out var path)
                || string.IsNullOrWhiteSpace(path)
                || !Get(arguments, "-stage3Ratio", "16x9", false, out var ratioText)
                || !TryRatio(ratioText, out var ratio)
                || !Get(arguments, "-stage3Child", "girl", false, out var childText)
                || !TryChild(childText, out var child)
                || !Get(arguments, "-stage3River", "middle", false, out var riverText)
                || !TryRiver(riverText, out var river)
                || !Get(arguments, "-stage3Plan", "grain", false, out var planText)
                || !TryPlan(planText, out var plan)
                || !Get(arguments, "-stage3Sound", "on", false, out var soundText)
                || !TryOnOff(soundText, out var sound)
                || !Get(arguments, "-stage3Captions", "on", false, out var captionText)
                || !TryOnOff(captionText, out var captions)
                || !Get(arguments, "-stage3Motion", "full", false, out var motionText)
                || !TryMotion(motionText, out var reduced)
                || !Get(arguments, "-stage3Beat", "intro", false, out var beatText)
                || !TryBeat(beatText, out var beat)
                || !Get(arguments, "-stage3Overlay", "none", false, out var overlayText)
                || !TryOverlay(overlayText, out var overlay)
                || !Get(arguments, "-stage3Failure", "none", false, out var failureText)
                || !TryFailure(failureText, out var failure)
                || !Get(arguments, "-stage3Recovery", "none", false, out var recoveryText)
                || !TryRecovery(recoveryText, out var recovery)
                || !Get(arguments, "-stage3CaptureDelay", "0", false, out var delayText)
                || !double.TryParse(delayText, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
                || double.IsNaN(delay) || double.IsInfinity(delay) || delay < 0d || delay > 30d)
                return false;

            var completionFailure = failure == Stage3EvidenceFailure.CompletionTimeout
                || failure == Stage3EvidenceFailure.CompletionAsset
                || failure == Stage3EvidenceFailure.CompletionLayout;
            if ((failure == Stage3EvidenceFailure.None && overlay == Stage3EvidenceOverlay.Recovery)
                || (failure != Stage3EvidenceFailure.None && overlay != Stage3EvidenceOverlay.Recovery)
                || (failure == Stage3EvidenceFailure.None && recovery != Stage3EvidenceRecoveryAction.None)
                || (completionFailure && beat != Stage3EvidenceBeat.CompletionLoading))
                return false;

            options = new Stage3EvidenceCaptureOptions(path, ratio, child, river, plan,
                sound, captions, reduced, beat, overlay, failure, recovery, delay);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-stage3", StringComparison.Ordinal)) continue;
                var known = false;
                foreach (var option in KnownOptions)
                    if (string.Equals(argument, option, StringComparison.Ordinal)) known = true;
                if (!known) return false;
            }
            return true;
        }

        private static bool Get(string[] arguments, string option, string fallback,
            bool required, out string value)
        {
            value = fallback; var found = false;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                if (found || index + 1 >= arguments.Length
                    || arguments[index + 1].StartsWith("-stage3", StringComparison.Ordinal)) return false;
                value = arguments[index + 1]; found = true;
            }
            return found || !required;
        }

        private static bool TryRatio(string value, out Stage3EvidenceRatio result)
        { switch (value) { case "4x3": result=Stage3EvidenceRatio.FourThree;return true;case "16x9":result=Stage3EvidenceRatio.SixteenNine;return true;case "24x10":result=Stage3EvidenceRatio.UltraWide;return true;default:result=default;return false;} }
        private static bool TryChild(string value, out Stage3EvidenceChild result)
        { if(value=="boy"){result=Stage3EvidenceChild.Boy;return true;}if(value=="girl"){result=Stage3EvidenceChild.Girl;return true;}result=default;return false; }
        private static bool TryRiver(string value, out Stage3EvidenceRiver result)
        { switch(value){case "low":result=Stage3EvidenceRiver.Low;return true;case "middle":result=Stage3EvidenceRiver.Middle;return true;case "high":result=Stage3EvidenceRiver.High;return true;default:result=default;return false;} }
        private static bool TryPlan(string value, out Stage3EvidencePlan result)
        { switch(value){case "grain":result=Stage3EvidencePlan.Grain;return true;case "channel":result=Stage3EvidencePlan.Channel;return true;case "boat":result=Stage3EvidencePlan.Boat;return true;default:result=default;return false;} }
        private static bool TryOnOff(string value, out bool result)
        { if(value=="on"){result=true;return true;}if(value=="off"){result=false;return true;}result=false;return false; }
        private static bool TryMotion(string value, out bool result)
        { if(value=="full"){result=false;return true;}if(value=="reduced"){result=true;return true;}result=false;return false; }
        private static bool TryBeat(string value, out Stage3EvidenceBeat result)
        { switch(value){case "intro":result=Stage3EvidenceBeat.Intro;return true;case "choosing-plan":result=Stage3EvidenceBeat.ChoosingPlan;return true;case "outcome-start":result=Stage3EvidenceBeat.OutcomeStart;return true;case "outcome-reveal":result=Stage3EvidenceBeat.OutcomeReveal;return true;case "outcome-benefit":result=Stage3EvidenceBeat.OutcomeBenefit;return true;case "outcome-need":result=Stage3EvidenceBeat.OutcomeNeed;return true;case "outcome-ready":result=Stage3EvidenceBeat.OutcomeReady;return true;case "watch-again":result=Stage3EvidenceBeat.WatchAgain;return true;case "another-plan":result=Stage3EvidenceBeat.AnotherPlan;return true;case "change-river":result=Stage3EvidenceBeat.ChangeRiver;return true;case "river-back":result=Stage3EvidenceBeat.RiverBack;return true;case "completion-loading":result=Stage3EvidenceBeat.CompletionLoading;return true;default:result=default;return false;} }
        private static bool TryOverlay(string value, out Stage3EvidenceOverlay result)
        { switch(value){case "none":result=Stage3EvidenceOverlay.None;return true;case "help":result=Stage3EvidenceOverlay.Help;return true;case "pause":result=Stage3EvidenceOverlay.Pause;return true;case "pause-help":result=Stage3EvidenceOverlay.PauseHelp;return true;case "direct-leave":result=Stage3EvidenceOverlay.DirectLeave;return true;case "nested-leave":result=Stage3EvidenceOverlay.NestedLeave;return true;case "recovery":result=Stage3EvidenceOverlay.Recovery;return true;case "app-pause":result=Stage3EvidenceOverlay.AppBackgroundPause;return true;case "audio-focus-pause":result=Stage3EvidenceOverlay.AudioFocusPause;return true;case "help-app-pause":result=Stage3EvidenceOverlay.HelpAppBackgroundPause;return true;case "help-audio-focus-pause":result=Stage3EvidenceOverlay.HelpAudioFocusPause;return true;case "leave-app-pause":result=Stage3EvidenceOverlay.LeaveAppBackgroundPause;return true;case "leave-audio-focus-pause":result=Stage3EvidenceOverlay.LeaveAudioFocusPause;return true;default:result=default;return false;} }
        private static bool TryFailure(string value, out Stage3EvidenceFailure result)
        { switch(value){case "none":result=Stage3EvidenceFailure.None;return true;case "entry-timeout":result=Stage3EvidenceFailure.EntryTimeout;return true;case "entry-asset":result=Stage3EvidenceFailure.EntryAsset;return true;case "entry-layout":result=Stage3EvidenceFailure.EntryLayout;return true;case "completion-timeout":result=Stage3EvidenceFailure.CompletionTimeout;return true;case "completion-asset":result=Stage3EvidenceFailure.CompletionAsset;return true;case "completion-layout":result=Stage3EvidenceFailure.CompletionLayout;return true;default:result=default;return false;} }
        private static bool TryRecovery(string value, out Stage3EvidenceRecoveryAction result)
        { if(value=="none"){result=Stage3EvidenceRecoveryAction.None;return true;}if(value=="retry"){result=Stage3EvidenceRecoveryAction.Retry;return true;}if(value=="home"){result=Stage3EvidenceRecoveryAction.Home;return true;}result=default;return false; }
    }

    [DisallowMultipleComponent]
    public sealed class Stage3EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;
        private Guid? _failedTransactionId;
        private bool _recoveryActionAccepted;
        private bool _staleCompletionAccepted;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Stage3EvidenceOptions.TryGetCapture(Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            yield return null; yield return null;
            var deadline = Time.realtimeSinceStartup + 30f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline) yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Stage 3 evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == Stage3EvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar)
                || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics() || !_host.EnterBoat1ForDiagnostics())
            { Fail("Stage 3 evidence did not reach Boat 1.", 3); yield break; }
            yield return null;
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics())
            { Fail("Stage 3 evidence did not reach Stage 1 entry.", 4); yield break; }
            while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage1InputReady || !BeginBoat2Entry())
            { Fail("Stage 3 evidence did not start Boat 2 entry.", 5); yield break; }
            while ((!_host.Boat2InputReady || _host.CurrentRoute != RouteId.Boat2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Boat2InputReady || !BeginStage2Entry())
            { Fail("Stage 3 evidence did not start Stage 2 entry.", 6); yield break; }
            while ((!_host.Stage2InputReady || _host.CurrentRoute != RouteId.Stage2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage2InputReady || !BeginBoat3Entry(ToRiver(options.River)))
            { Fail("Stage 3 evidence did not start Boat 3 entry.", 7); yield break; }
            while ((!_host.Boat3InputReady || _host.CurrentRoute != RouteId.Boat3)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Boat3InputReady || !_host.PresentBoat3BoundaryForDiagnostics()
                || !_host.CompleteBoat3ArrivalForDiagnostics())
            { Fail("Stage 3 evidence did not start Stage 3 entry.", 8); yield break; }

            if (IsEntryFailure(options.Failure))
            {
                _failedTransactionId = _host.ActiveNavigationId;
                ApplyEntryFailure(options.Failure); yield return null;
                ApplyRecoveryAction(options.RecoveryAction); yield return null;
            }
            else
            {
                while ((!_host.Stage3InputReady || _host.CurrentRoute != RouteId.Stage3)
                       && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_host.Stage3InputReady || !DriveStage(options))
                { Fail("Stage 3 evidence could not create the requested beat.", 9); yield break; }
                if (IsCompletionFailure(options.Failure))
                {
                    _failedTransactionId = _host.ActiveNavigationId;
                    ApplyCompletionFailure(options.Failure); yield return null;
                    ApplyRecoveryAction(options.RecoveryAction); yield return null;
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

        private bool BeginBoat2Entry() =>
            _host.PresentStage1ControlForDiagnostics(Stage1StageView.PlayControlId)
            && _host.AttachStage1MarkerForDiagnostics(false)
            && _host.MoveStage1MarkerForDiagnostics(0.5d)
            && _host.PresentStage1ControlForDiagnostics(Stage1StageView.ConfirmControlId)
            && _host.CompleteStage1OutcomeForDiagnostics()
            && _host.PresentStage1ControlForDiagnostics(Stage1StageView.ContinueControlId);

        private bool BeginStage2Entry() =>
            _host.PresentBoat2ChoiceForDiagnostics(Boat2Choice.Dock)
            && _host.PresentBoat2BoundaryForDiagnostics()
            && _host.CompleteBoat2ArrivalForDiagnostics();

        private bool BeginBoat3Entry(Stage2River river)
        {
            if (!_host.PresentStage2PlayForDiagnostics() || !_host.PresentStage2RiverForDiagnostics(river)) return false;
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
                if (!_host.PresentStage2HotspotForDiagnostics(hotspot)
                    || !_host.CompleteStage2ConsequenceForDiagnostics()) return false;
            return _host.PresentStage2FinishForDiagnostics();
        }

        private bool DriveStage(Stage3EvidenceCaptureOptions options)
        {
            if (options.Beat == Stage3EvidenceBeat.Intro) return true;
            if (!_host.PresentStage3PlayForDiagnostics()) return false;
            if (options.Beat == Stage3EvidenceBeat.ChoosingPlan) return true;
            if (!_host.PresentStage3PlanForDiagnostics(ToPlan(options.Plan))) return false;
            if (options.Beat == Stage3EvidenceBeat.OutcomeStart) return true;
            if (options.Beat == Stage3EvidenceBeat.OutcomeReveal)
                return _host.AdvanceStage3OutcomeForDiagnostics(0.8d);
            if (options.Beat == Stage3EvidenceBeat.OutcomeBenefit)
                return _host.AdvanceStage3OutcomeForDiagnostics(2.0d);
            if (options.Beat == Stage3EvidenceBeat.OutcomeNeed)
                return _host.AdvanceStage3OutcomeForDiagnostics(3.6d);
            if (!_host.CompleteStage3OutcomeForDiagnostics()) return false;
            if (options.Beat == Stage3EvidenceBeat.OutcomeReady) return true;
            if (options.Beat == Stage3EvidenceBeat.WatchAgain)
                return _host.PresentStage3WatchAgainForDiagnostics()
                    && _host.AdvanceStage3OutcomeForDiagnostics(0.8d);
            if (options.Beat == Stage3EvidenceBeat.AnotherPlan)
                return _host.PresentStage3AnotherPlanForDiagnostics();
            if (options.Beat == Stage3EvidenceBeat.ChangeRiver)
                return _host.PresentStage3AnotherRiverForDiagnostics();
            if (options.Beat == Stage3EvidenceBeat.RiverBack)
                return _host.PresentStage3AnotherRiverForDiagnostics()
                    && _host.PresentStage3RiverBackForDiagnostics();
            return options.Beat == Stage3EvidenceBeat.CompletionLoading
                && _host.PresentStage3FinishForDiagnostics();
        }

        private void ApplyOverlay(Stage3EvidenceOverlay overlay)
        {
            if (overlay == Stage3EvidenceOverlay.Help) _host.PresentStage3HelpForDiagnostics();
            else if (overlay == Stage3EvidenceOverlay.Pause) _host.PresentStage3PauseForDiagnostics();
            else if (overlay == Stage3EvidenceOverlay.PauseHelp) _host.PresentStage3PauseHelpForDiagnostics();
            else if (overlay == Stage3EvidenceOverlay.DirectLeave) _host.PresentStage3LeaveForDiagnostics(false);
            else if (overlay == Stage3EvidenceOverlay.NestedLeave) _host.PresentStage3LeaveForDiagnostics(true);
            else if (overlay == Stage3EvidenceOverlay.AppBackgroundPause)
                _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AppBackground);
            else if (overlay == Stage3EvidenceOverlay.AudioFocusPause)
                _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AudioFocusLoss);
            else if (overlay == Stage3EvidenceOverlay.HelpAppBackgroundPause)
            { _host.PresentStage3HelpForDiagnostics(); _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AppBackground); }
            else if (overlay == Stage3EvidenceOverlay.HelpAudioFocusPause)
            { _host.PresentStage3HelpForDiagnostics(); _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AudioFocusLoss); }
            else if (overlay == Stage3EvidenceOverlay.LeaveAppBackgroundPause)
            { _host.PresentStage3LeaveForDiagnostics(false); _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AppBackground); }
            else if (overlay == Stage3EvidenceOverlay.LeaveAudioFocusPause)
            { _host.PresentStage3LeaveForDiagnostics(false); _host.PresentStage3InterruptionForDiagnostics(Stage3PauseReason.AudioFocusLoss); }
        }

        private void ApplyEntryFailure(Stage3EvidenceFailure failure)
        {
            if (failure == Stage3EvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage3EvidenceFailure.EntryAsset) _host.ReportBoat3BoundaryAssetFailure();
            else _host.TryApplyBoat3Layout(360, 182, 360, 182, 429, 144);
        }

        private void ApplyCompletionFailure(Stage3EvidenceFailure failure)
        {
            if (failure == Stage3EvidenceFailure.CompletionTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage3EvidenceFailure.CompletionAsset) _host.ReportStage3BoundaryAssetFailure();
            else _host.TryApplyStage3Layout(360, 182, 0, 0, 360, 182, 429, 144);
        }

        private void ApplyRecoveryAction(Stage3EvidenceRecoveryAction action)
        {
            if (action == Stage3EvidenceRecoveryAction.None) return;
            var control = action == Stage3EvidenceRecoveryAction.Retry
                ? LeaveConfirmationView.RetryControlId : LeaveConfirmationView.HomeControlId;
            _recoveryActionAccepted = _host.HandleStage3RecoveryActionForDiagnostics(control);
            if (_recoveryActionAccepted && _failedTransactionId.HasValue)
                _staleCompletionAccepted = _host.TryCompleteNavigation(_failedTransactionId.Value);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            if (_host.ActiveStage3View == null) return;
            _host.TryApplyStage3Layout(resolution.x, resolution.y, 32, 32,
                resolution.x - 64, resolution.y - 64, 326, 88);
        }

        private void WriteCapture(Stage3EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Stage3EvidenceCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(20, 39, 55, 255); camera.cullingMask = ~0;
            var target = new RenderTexture(resolution.x, resolution.y, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            { if (!canvas.gameObject.activeInHierarchy) continue; canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f; }
            Canvas.ForceUpdateCanvases(); camera.Render(); var previous=RenderTexture.active; RenderTexture.active=target;
            var texture=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);
            texture.ReadPixels(new Rect(0,0,resolution.x,resolution.y),0,0);texture.Apply();File.WriteAllBytes(options.Path,texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Destroy(texture);Destroy(target);Destroy(cameraObject);

            var performance=FoundationPerformanceMetrics.Summarize(_frameMilliseconds,
                Profiler.GetTotalAllocatedMemoryLong(),Profiler.GetTotalReservedMemoryLong(),resolution.x,resolution.y);
            var snapshot=_host.Stage3Snapshot;var view=_host.ActiveStage3View;
            var cue=view==null?default:view.LastMediaCue;var outcome=view==null?default:view.LastOutcomePresentation;
            var record=new EvidenceRecord
            {
                width=resolution.x,height=resolution.y,requested_ratio=options.Ratio.ToString(),selected_child=options.Child.ToString(),
                requested_river=options.River.ToString(),requested_plan=options.Plan.ToString(),sound_enabled=options.SoundEnabled,
                captions_enabled=options.CaptionsEnabled,reduced_motion=_host.ReducedMotionEnabled,requested_beat=options.Beat.ToString(),
                requested_overlay=options.Overlay.ToString(),failure=options.Failure.ToString(),recovery_action=options.RecoveryAction.ToString(),
                route=_host.CurrentRoute.ToString(),session_location=_host.SessionLocation.ToString(),stage_state=snapshot.State.ToString(),
                selected_river=snapshot.SelectedRiver.ToString(),selected_plan=snapshot.SelectedPlan.ToString(),
                outcome_river=snapshot.CurrentOutcome.River.ToString(),outcome_plan=snapshot.CurrentOutcome.Plan.ToString(),
                outcome_benefit=snapshot.CurrentOutcome.Benefit.ToString(),outcome_need=snapshot.CurrentOutcome.Need.ToString(),
                tried_combinations=(int)snapshot.TriedCombinations,outcome_elapsed_seconds=snapshot.OutcomeElapsedSeconds,
                option_count=snapshot.OptionCount,completion_recorded=snapshot.CompletionRecorded,input_ready=_host.Stage3InputReady,
                boundary_loading=_host.Stage3BoundaryLoading,navigation_transaction_id=_host.ActiveNavigationId?.ToString()??string.Empty,
                navigation_caller=_host.ActiveNavigationCaller?.ToString()??string.Empty,
                navigation_destination=_host.ActiveNavigationDestination?.ToString()??string.Empty,
                completion_type_present=Type.GetType("Exloris.Runtime.Completion.CompletionStageHost, Exloris.Runtime")!=null,
                recovery_action_accepted=_recoveryActionAccepted,stale_completion_accepted=_staleCompletionAccepted,
                pause_state=_host.Stage3PauseState.ToString(),pause_reason=_host.Stage3LastPauseReason?.ToString()??string.Empty,
                pause_caller_state=_host.Stage3PauseCallerState.ToString(),leave_caller_state=_host.Stage3DirectLeaveCallerState.ToString(),
                help_visible=_host.ActiveStage3HelpView!=null&&_host.ActiveStage3HelpView.IsVisible,
                help_mode=_host.ActiveStage3HelpView==null?string.Empty:_host.ActiveStage3HelpView.LastMode.ToString(),
                help_river_label_count=_host.ActiveStage3HelpView==null?0:_host.ActiveStage3HelpView.VisibleRiverLabelCount,
                help_uses_outcome=_host.ActiveStage3HelpView!=null&&_host.ActiveStage3HelpView.UsesOutcome(snapshot.CurrentOutcome),
                leave_visible=_host.ActiveLeaveConfirmationView!=null&&_host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state=EffectiveRecovery().ToString(),media_kind=cue.Kind.ToString(),media_plan=cue.Plan.ToString(),
                caption_id=cue.CaptionId??string.Empty,caption_visible=cue.CaptionVisible,
                visual_equivalent_present=cue.VisualEquivalent!=null,voice_playing=_host.Stage3VoicePlaying,loops_playing=_host.Stage3LoopsPlaying,
                outcome_visual_state=outcome.VisualState.ToString(),outcome_reduced_motion=outcome.ReducedMotion,
                selected_child_present=view!=null&&view.SelectedChildSprite!=null,selected_plan_present=view!=null&&view.SelectedPlanSprite!=null,
                equal_outcome_layer_color=view!=null&&view.OutcomeLayerColorsEqual,
                semantic_control_count=view==null?0:view.SemanticControlCount,
                display_semantic_control_count=view==null?0:view.DisplaySemanticControlCount,
                accepted_command_count=_host.Stage3AcceptedCommandCount,diagnostic_control_present=false,
                frame_samples=performance.SampleCount,p95_frame_ms=performance.P95FrameMilliseconds,
                p99_frame_ms=performance.P99FrameMilliseconds,maximum_frame_ms=performance.MaximumFrameMilliseconds,
                maximum_consecutive_over_33_3_ms=performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib=performance.AllocatedMemoryMiB,reserved_mib=performance.ReservedMemoryMiB,
            };
            var jsonPath=Path.ChangeExtension(options.Path,".json");File.WriteAllText(jsonPath,JsonUtility.ToJson(record,true));
            Debug.Log($"STAGE3_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery() =>
            _host.Stage3RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Stage3RecoveryState : _host.Boat3RecoveryState;
        private static bool IsEntryFailure(Stage3EvidenceFailure value) => value==Stage3EvidenceFailure.EntryTimeout||value==Stage3EvidenceFailure.EntryAsset||value==Stage3EvidenceFailure.EntryLayout;
        private static bool IsCompletionFailure(Stage3EvidenceFailure value) => value==Stage3EvidenceFailure.CompletionTimeout||value==Stage3EvidenceFailure.CompletionAsset||value==Stage3EvidenceFailure.CompletionLayout;
        private static Stage2River ToRiver(Stage3EvidenceRiver value) => value==Stage3EvidenceRiver.Low?Stage2River.Low:value==Stage3EvidenceRiver.High?Stage2River.High:Stage2River.Middle;
        private static Stage3Plan ToPlan(Stage3EvidencePlan value) => value==Stage3EvidencePlan.Grain?Stage3Plan.Grain:value==Stage3EvidencePlan.Channel?Stage3Plan.Channel:Stage3Plan.Boat;
        private static Vector2Int ResolutionFor(Stage3EvidenceRatio value) => value==Stage3EvidenceRatio.FourThree?new Vector2Int(1440,1080):value==Stage3EvidenceRatio.UltraWide?new Vector2Int(1920,800):new Vector2Int(1600,900);
        private static void Fail(string message,int exitCode){Debug.LogError(message);Application.Quit(exitCode);}

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width,height,tried_combinations,option_count,semantic_control_count,display_semantic_control_count,accepted_command_count,frame_samples,maximum_consecutive_over_33_3_ms;
            public string requested_ratio,selected_child,requested_river,requested_plan,requested_beat,requested_overlay,failure,recovery_action;
            public string route,session_location,stage_state,selected_river,selected_plan,outcome_river,outcome_plan,outcome_benefit,outcome_need;
            public string navigation_transaction_id,navigation_caller,navigation_destination,pause_state,pause_reason,pause_caller_state,leave_caller_state;
            public string recovery_state,media_kind,media_plan,caption_id,outcome_visual_state,help_mode;
            public bool sound_enabled,captions_enabled,reduced_motion,completion_recorded,input_ready,boundary_loading,completion_type_present;
            public bool recovery_action_accepted,stale_completion_accepted,help_visible,leave_visible,caption_visible,visual_equivalent_present,help_uses_outcome;
            public bool voice_playing,loops_playing,outcome_reduced_motion,selected_child_present,selected_plan_present,equal_outcome_layer_color,diagnostic_control_present;
            public double outcome_elapsed_seconds;
            public float p95_frame_ms,p99_frame_ms,maximum_frame_ms,allocated_mib,reserved_mib;
            public int help_river_label_count;
        }
    }
}
