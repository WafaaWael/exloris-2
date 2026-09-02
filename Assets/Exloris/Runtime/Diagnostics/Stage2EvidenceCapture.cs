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
    public enum Stage2EvidenceRatio { FourThree, SixteenNine, UltraWide }
    public enum Stage2EvidenceChild { Boy, Girl }
    public enum Stage2EvidenceRiver { Low, Middle, High }
    public enum Stage2EvidenceHotspot { Field, Path, Boat }
    public enum Stage2EvidenceBeat
    {
        Intro, Choosing, Inspecting, ConsequenceStart, ConsequenceReveal,
        ConsequenceSettle, ConsequenceComplete, Compared, FinishGuard, Boat3Loading,
    }
    public enum Stage2EvidenceOverlay
    {
        None, Help, Pause, PauseHelp, DirectLeave, NestedLeave, Recovery,
        AppBackgroundPause, AudioFocusPause, HelpAppBackgroundPause,
        HelpAudioFocusPause, LeaveAppBackgroundPause, LeaveAudioFocusPause,
    }
    public enum Stage2EvidenceFailure
    {
        None, EntryTimeout, EntryAsset, EntryLayout, Boat3Timeout, Boat3Asset, Boat3Layout,
    }
    public enum Stage2EvidenceRecoveryAction { None, Retry, Home }

    public readonly struct Stage2EvidenceCaptureOptions
    {
        public Stage2EvidenceCaptureOptions(
            string path, Stage2EvidenceRatio ratio, Stage2EvidenceChild child,
            Stage2EvidenceRiver river, Stage2EvidenceHotspot hotspot,
            bool soundEnabled, bool captionsEnabled, bool reducedMotion,
            Stage2EvidenceBeat beat, int visitedBefore, bool repeat, bool attemptFinish,
            Stage2EvidenceOverlay overlay, Stage2EvidenceFailure failure,
            Stage2EvidenceRecoveryAction recoveryAction, double delaySeconds)
        {
            Path = path; Ratio = ratio; Child = child; River = river; Hotspot = hotspot;
            SoundEnabled = soundEnabled; CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion; Beat = beat; VisitedBefore = visitedBefore;
            Repeat = repeat; AttemptFinish = attemptFinish; Overlay = overlay;
            Failure = failure; RecoveryAction = recoveryAction; DelaySeconds = delaySeconds;
        }

        public string Path { get; }
        public Stage2EvidenceRatio Ratio { get; }
        public Stage2EvidenceChild Child { get; }
        public Stage2EvidenceRiver River { get; }
        public Stage2EvidenceHotspot Hotspot { get; }
        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }
        public Stage2EvidenceBeat Beat { get; }
        public int VisitedBefore { get; }
        public bool Repeat { get; }
        public bool AttemptFinish { get; }
        public Stage2EvidenceOverlay Overlay { get; }
        public Stage2EvidenceFailure Failure { get; }
        public Stage2EvidenceRecoveryAction RecoveryAction { get; }
        public double DelaySeconds { get; }
    }

    public static class Stage2EvidenceOptions
    {
        private static readonly string[] KnownOptions =
        {
            "-stage2Capture", "-stage2Ratio", "-stage2Child", "-stage2River",
            "-stage2Hotspot", "-stage2Sound", "-stage2Captions", "-stage2Motion",
            "-stage2Beat", "-stage2Visited", "-stage2Repeat", "-stage2Finish",
            "-stage2Overlay", "-stage2Failure", "-stage2Recovery", "-stage2CaptureDelay",
        };

        public static bool TryGetCapture(string[] arguments, out Stage2EvidenceCaptureOptions options)
        {
            options = default;
            if (!HasOnlyKnownOptions(arguments)
                || !Get(arguments, "-stage2Capture", null, true, out var path)
                || string.IsNullOrWhiteSpace(path)
                || !Get(arguments, "-stage2Ratio", "16x9", false, out var ratioText)
                || !TryRatio(ratioText, out var ratio)
                || !Get(arguments, "-stage2Child", "girl", false, out var childText)
                || !TryChild(childText, out var child)
                || !Get(arguments, "-stage2River", "middle", false, out var riverText)
                || !TryRiver(riverText, out var river)
                || !Get(arguments, "-stage2Hotspot", "field", false, out var hotspotText)
                || !TryHotspot(hotspotText, out var hotspot)
                || !Get(arguments, "-stage2Sound", "on", false, out var soundText)
                || !TryOnOff(soundText, out var sound)
                || !Get(arguments, "-stage2Captions", "on", false, out var captionText)
                || !TryOnOff(captionText, out var captions)
                || !Get(arguments, "-stage2Motion", "full", false, out var motionText)
                || !TryMotion(motionText, out var reduced)
                || !Get(arguments, "-stage2Beat", "intro", false, out var beatText)
                || !TryBeat(beatText, out var beat)
                || !Get(arguments, "-stage2Visited", "0", false, out var visitedText)
                || !int.TryParse(visitedText, NumberStyles.None, CultureInfo.InvariantCulture, out var visited)
                || visited < 0 || visited > 3
                || !Get(arguments, "-stage2Repeat", "off", false, out var repeatText)
                || !TryOnOff(repeatText, out var repeat)
                || !Get(arguments, "-stage2Finish", "off", false, out var finishText)
                || !TryOnOff(finishText, out var finish)
                || !Get(arguments, "-stage2Overlay", "none", false, out var overlayText)
                || !TryOverlay(overlayText, out var overlay)
                || !Get(arguments, "-stage2Failure", "none", false, out var failureText)
                || !TryFailure(failureText, out var failure)
                || !Get(arguments, "-stage2Recovery", "none", false, out var recoveryText)
                || !TryRecovery(recoveryText, out var recovery)
                || !Get(arguments, "-stage2CaptureDelay", "0", false, out var delayText)
                || !double.TryParse(delayText, NumberStyles.Float, CultureInfo.InvariantCulture, out var delay)
                || double.IsNaN(delay) || double.IsInfinity(delay) || delay < 0d || delay > 30d)
                return false;

            var boat3Failure = failure == Stage2EvidenceFailure.Boat3Timeout
                || failure == Stage2EvidenceFailure.Boat3Asset
                || failure == Stage2EvidenceFailure.Boat3Layout;
            if ((failure == Stage2EvidenceFailure.None && overlay == Stage2EvidenceOverlay.Recovery)
                || (failure != Stage2EvidenceFailure.None && overlay != Stage2EvidenceOverlay.Recovery)
                || (failure == Stage2EvidenceFailure.None && recovery != Stage2EvidenceRecoveryAction.None)
                || (boat3Failure && beat != Stage2EvidenceBeat.Boat3Loading)
                || (beat == Stage2EvidenceBeat.Compared && visited < 1)
                || (beat == Stage2EvidenceBeat.FinishGuard && (!finish || visited >= 3))
                || (beat == Stage2EvidenceBeat.Boat3Loading && (!finish || visited != 3))
                || (finish && beat != Stage2EvidenceBeat.FinishGuard
                    && beat != Stage2EvidenceBeat.Boat3Loading))
                return false;

            options = new Stage2EvidenceCaptureOptions(
                path, ratio, child, river, hotspot, sound, captions, reduced,
                beat, visited, repeat, finish, overlay, failure, recovery, delay);
            return true;
        }

        private static bool HasOnlyKnownOptions(string[] arguments)
        {
            if (arguments == null) return false;
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-stage2", StringComparison.Ordinal)) continue;
                var known = false;
                foreach (var option in KnownOptions)
                    if (string.Equals(argument, option, StringComparison.Ordinal)) known = true;
                if (!known) return false;
            }
            return true;
        }

        private static bool Get(
            string[] arguments, string option, string fallback, bool required, out string value)
        {
            value = fallback; var found = false;
            if (arguments == null) return false;
            for (var index = 0; index < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], option, StringComparison.Ordinal)) continue;
                if (found || index + 1 >= arguments.Length
                    || arguments[index + 1].StartsWith("-stage2", StringComparison.Ordinal)) return false;
                value = arguments[index + 1]; found = true;
            }
            return found || !required;
        }

        private static bool TryRatio(string value, out Stage2EvidenceRatio result)
        { switch (value) { case "4x3": result=Stage2EvidenceRatio.FourThree;return true; case "16x9":result=Stage2EvidenceRatio.SixteenNine;return true;case "24x10":result=Stage2EvidenceRatio.UltraWide;return true;default:result=default;return false;} }
        private static bool TryChild(string value, out Stage2EvidenceChild result)
        { if(value=="boy"){result=Stage2EvidenceChild.Boy;return true;}if(value=="girl"){result=Stage2EvidenceChild.Girl;return true;}result=default;return false; }
        private static bool TryRiver(string value, out Stage2EvidenceRiver result)
        { switch(value){case "low":result=Stage2EvidenceRiver.Low;return true;case "middle":result=Stage2EvidenceRiver.Middle;return true;case "high":result=Stage2EvidenceRiver.High;return true;default:result=default;return false;} }
        private static bool TryHotspot(string value, out Stage2EvidenceHotspot result)
        { switch(value){case "field":result=Stage2EvidenceHotspot.Field;return true;case "path":result=Stage2EvidenceHotspot.Path;return true;case "boat":result=Stage2EvidenceHotspot.Boat;return true;default:result=default;return false;} }
        private static bool TryOnOff(string value, out bool result)
        { if(value=="on"){result=true;return true;}if(value=="off"){result=false;return true;}result=false;return false; }
        private static bool TryMotion(string value, out bool result)
        { if(value=="full"){result=false;return true;}if(value=="reduced"){result=true;return true;}result=false;return false; }
        private static bool TryBeat(string value, out Stage2EvidenceBeat result)
        { switch(value){case "intro":result=Stage2EvidenceBeat.Intro;return true;case "choosing":result=Stage2EvidenceBeat.Choosing;return true;case "inspecting":result=Stage2EvidenceBeat.Inspecting;return true;case "consequence-start":result=Stage2EvidenceBeat.ConsequenceStart;return true;case "consequence-reveal":result=Stage2EvidenceBeat.ConsequenceReveal;return true;case "consequence-settle":result=Stage2EvidenceBeat.ConsequenceSettle;return true;case "consequence-complete":result=Stage2EvidenceBeat.ConsequenceComplete;return true;case "compared":result=Stage2EvidenceBeat.Compared;return true;case "finish-guard":result=Stage2EvidenceBeat.FinishGuard;return true;case "boat3-loading":result=Stage2EvidenceBeat.Boat3Loading;return true;default:result=default;return false;} }
        private static bool TryOverlay(string value, out Stage2EvidenceOverlay result)
        { switch(value){case "none":result=Stage2EvidenceOverlay.None;return true;case "help":result=Stage2EvidenceOverlay.Help;return true;case "pause":result=Stage2EvidenceOverlay.Pause;return true;case "pause-help":result=Stage2EvidenceOverlay.PauseHelp;return true;case "direct-leave":result=Stage2EvidenceOverlay.DirectLeave;return true;case "nested-leave":result=Stage2EvidenceOverlay.NestedLeave;return true;case "recovery":result=Stage2EvidenceOverlay.Recovery;return true;case "app-pause":result=Stage2EvidenceOverlay.AppBackgroundPause;return true;case "audio-focus-pause":result=Stage2EvidenceOverlay.AudioFocusPause;return true;case "help-app-pause":result=Stage2EvidenceOverlay.HelpAppBackgroundPause;return true;case "help-audio-focus-pause":result=Stage2EvidenceOverlay.HelpAudioFocusPause;return true;case "leave-app-pause":result=Stage2EvidenceOverlay.LeaveAppBackgroundPause;return true;case "leave-audio-focus-pause":result=Stage2EvidenceOverlay.LeaveAudioFocusPause;return true;default:result=default;return false;} }
        private static bool TryFailure(string value, out Stage2EvidenceFailure result)
        { switch(value){case "none":result=Stage2EvidenceFailure.None;return true;case "entry-timeout":result=Stage2EvidenceFailure.EntryTimeout;return true;case "entry-asset":result=Stage2EvidenceFailure.EntryAsset;return true;case "entry-layout":result=Stage2EvidenceFailure.EntryLayout;return true;case "boat3-timeout":result=Stage2EvidenceFailure.Boat3Timeout;return true;case "boat3-asset":result=Stage2EvidenceFailure.Boat3Asset;return true;case "boat3-layout":result=Stage2EvidenceFailure.Boat3Layout;return true;default:result=default;return false;} }
        private static bool TryRecovery(string value, out Stage2EvidenceRecoveryAction result)
        { if(value=="none"){result=Stage2EvidenceRecoveryAction.None;return true;}if(value=="retry"){result=Stage2EvidenceRecoveryAction.Retry;return true;}if(value=="home"){result=Stage2EvidenceRecoveryAction.Home;return true;}result=default;return false; }
    }

    [DisallowMultipleComponent]
    public sealed class Stage2EvidenceCapture : MonoBehaviour
    {
        private readonly List<float> _frameMilliseconds = new List<float>();
        private ScreenHost _host;
        private Guid? _failedTransactionId;
        private bool _recoveryActionAccepted;
        private bool _staleCompletionAccepted;
        private bool _repeatAccepted;
        private bool _repeatReplacedToken;
        private bool _finishAttemptAccepted;

        private void Awake() => _host = GetComponent<ScreenHost>();

        private IEnumerator Start()
        {
            if (!Stage2EvidenceOptions.TryGetCapture(
                    Environment.GetCommandLineArgs(), out var options)) yield break;
            var resolution = ResolutionFor(options.Ratio);
            Screen.SetResolution(resolution.x, resolution.y, false);
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = 60;
            yield return null; yield return null;
            var deadline = Time.realtimeSinceStartup + 30f;
            while (_host.CurrentRoute == RouteId.StartupLoading && Time.realtimeSinceStartup < deadline)
                yield return null;
            if (_host.CurrentRoute == RouteId.IntroVideo) _host.SkipIntroForTests();
            while (!_host.WelcomeInputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.WelcomeInputReady) { Fail("Stage 2 evidence did not reach Welcome.", 2); yield break; }

            _host.ConfigureWelcomeEvidenceForDiagnostics(
                options.SoundEnabled, options.CaptionsEnabled, options.ReducedMotion);
            var avatar = options.Child == Stage2EvidenceChild.Girl ? AvatarChoice.Girl : AvatarChoice.Boy;
            if (!_host.EnterAvatarForDiagnostics(avatar)
                || !_host.PresentAvatarPreviewForDiagnostics(avatar)
                || !_host.EnterJourneyForDiagnostics() || !_host.EnterBoat1ForDiagnostics())
            { Fail("Stage 2 evidence did not reach Boat 1.", 3); yield break; }
            yield return null;
            while (!_host.Boat1InputReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.PresentBoat1BoundaryForDiagnostics() || !_host.CompleteBoat1ArrivalForDiagnostics())
            { Fail("Stage 2 evidence did not reach Stage 1 entry.", 4); yield break; }
            while ((!_host.Stage1InputReady || _host.CurrentRoute != RouteId.Stage1)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Stage1InputReady || !BeginBoat2Entry())
            { Fail("Stage 2 evidence did not start Boat 2 entry.", 5); yield break; }
            while ((!_host.Boat2InputReady || _host.CurrentRoute != RouteId.Boat2)
                   && Time.realtimeSinceStartup < deadline) yield return null;
            if (!_host.Boat2InputReady || !BeginStage2Entry())
            { Fail("Stage 2 evidence did not start its entry boundary.", 6); yield break; }

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
                while ((!_host.Stage2InputReady || _host.CurrentRoute != RouteId.Stage2)
                       && Time.realtimeSinceStartup < deadline) yield return null;
                if (!_host.Stage2InputReady)
                { Fail("Stage 2 evidence did not enter Stage 2.", 7); yield break; }
                if (!DriveStage(options))
                { Fail("Stage 2 evidence could not create the requested beat.", 8); yield break; }
                if (IsBoat3Failure(options.Failure))
                {
                    _failedTransactionId = _host.ActiveNavigationId;
                    ApplyBoat3Failure(options.Failure);
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

        private bool DriveStage(Stage2EvidenceCaptureOptions options)
        {
            if (options.Beat == Stage2EvidenceBeat.Intro)
            {
                RepeatInstructionIfRequested(options.Repeat);
                return true;
            }
            if (!_host.PresentStage2PlayForDiagnostics()) return false;
            if (options.Beat == Stage2EvidenceBeat.Choosing)
            {
                RepeatInstructionIfRequested(options.Repeat);
                return true;
            }
            if (!_host.PresentStage2RiverForDiagnostics(ToRiver(options.River))) return false;
            for (var index = 0; index < options.VisitedBefore; index++)
            {
                var hotspot = (Stage2Hotspot)(index + 1);
                if (!_host.PresentStage2HotspotForDiagnostics(hotspot)
                    || !_host.CompleteStage2ConsequenceForDiagnostics()) return false;
            }
            if (options.Beat == Stage2EvidenceBeat.Compared)
                return _host.PresentStage2CompareForDiagnostics();
            if (options.Beat == Stage2EvidenceBeat.FinishGuard)
            {
                _finishAttemptAccepted = _host.PresentStage2FinishForDiagnostics();
                return !_finishAttemptAccepted && _host.CurrentRoute == RouteId.Stage2;
            }
            if (options.Beat == Stage2EvidenceBeat.Boat3Loading)
            {
                _finishAttemptAccepted = _host.PresentStage2FinishForDiagnostics();
                return _finishAttemptAccepted && _host.Stage2BoundaryLoading;
            }
            if (options.Beat == Stage2EvidenceBeat.Inspecting)
            {
                RepeatConsequenceIfRequested(options.Repeat);
                return true;
            }
            if (!_host.PresentStage2HotspotForDiagnostics(ToHotspot(options.Hotspot))) return false;
            if (options.Repeat)
            {
                if (!_host.CompleteStage2ConsequenceForDiagnostics()) return false;
                var before = _host.Stage2Snapshot.ConsequenceTokenId;
                _repeatAccepted = _host.PresentStage2RepeatForDiagnostics();
                _repeatReplacedToken = _repeatAccepted
                    && before != _host.Stage2Snapshot.ConsequenceTokenId;
                if (!_repeatAccepted) return false;
            }
            return _host.AdvanceStage2ConsequenceForDiagnostics(ElapsedFor(options.Beat));
        }

        private void RepeatInstructionIfRequested(bool repeat)
        {
            if (!repeat) return;
            var before = _host.Stage2Snapshot.InstructionTokenId;
            _repeatAccepted = _host.PresentStage2RepeatForDiagnostics();
            _repeatReplacedToken = _repeatAccepted
                && before != _host.Stage2Snapshot.InstructionTokenId;
        }

        private void RepeatConsequenceIfRequested(bool repeat)
        {
            if (!repeat || _host.Stage2Snapshot.CurrentHotspot == Stage2Hotspot.None) return;
            var before = _host.Stage2Snapshot.ConsequenceTokenId;
            _repeatAccepted = _host.PresentStage2RepeatForDiagnostics();
            _repeatReplacedToken = _repeatAccepted
                && before != _host.Stage2Snapshot.ConsequenceTokenId;
        }

        private void ApplyOverlay(Stage2EvidenceOverlay overlay)
        {
            if (overlay == Stage2EvidenceOverlay.Help) _host.PresentStage2HelpForDiagnostics();
            else if (overlay == Stage2EvidenceOverlay.Pause) _host.PresentStage2PauseForDiagnostics();
            else if (overlay == Stage2EvidenceOverlay.PauseHelp) _host.PresentStage2PauseHelpForDiagnostics();
            else if (overlay == Stage2EvidenceOverlay.DirectLeave) _host.PresentStage2LeaveForDiagnostics(false);
            else if (overlay == Stage2EvidenceOverlay.NestedLeave) _host.PresentStage2LeaveForDiagnostics(true);
            else if (overlay == Stage2EvidenceOverlay.AppBackgroundPause)
                _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AppBackground);
            else if (overlay == Stage2EvidenceOverlay.AudioFocusPause)
                _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AudioFocusLoss);
            else if (overlay == Stage2EvidenceOverlay.HelpAppBackgroundPause)
            { _host.PresentStage2HelpForDiagnostics(); _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AppBackground); }
            else if (overlay == Stage2EvidenceOverlay.HelpAudioFocusPause)
            { _host.PresentStage2HelpForDiagnostics(); _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AudioFocusLoss); }
            else if (overlay == Stage2EvidenceOverlay.LeaveAppBackgroundPause)
            { _host.PresentStage2LeaveForDiagnostics(false); _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AppBackground); }
            else if (overlay == Stage2EvidenceOverlay.LeaveAudioFocusPause)
            { _host.PresentStage2LeaveForDiagnostics(false); _host.PresentStage2InterruptionForDiagnostics(Stage2PauseReason.AudioFocusLoss); }
        }

        private void ApplyEntryFailure(Stage2EvidenceFailure failure)
        {
            if (failure == Stage2EvidenceFailure.EntryTimeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage2EvidenceFailure.EntryAsset) _host.ReportBoat2BoundaryAssetFailure();
            else _host.ReportStage2EntryLayoutFailureForDiagnostics();
        }

        private void ApplyBoat3Failure(Stage2EvidenceFailure failure)
        {
            if (failure == Stage2EvidenceFailure.Boat3Timeout) _host.ForceNavigationTimeoutForTests();
            else if (failure == Stage2EvidenceFailure.Boat3Asset) _host.ReportStage2BoundaryAssetFailure();
            else _host.TryApplyStage2Layout(360, 182, 0, 0, 360, 182, 326, 88);
        }

        private void ApplyRecoveryAction(Stage2EvidenceRecoveryAction action)
        {
            if (action == Stage2EvidenceRecoveryAction.None) return;
            var control = action == Stage2EvidenceRecoveryAction.Retry
                ? LeaveConfirmationView.RetryControlId : LeaveConfirmationView.HomeControlId;
            _recoveryActionAccepted = _host.HandleStage2RecoveryActionForDiagnostics(control);
            if (_recoveryActionAccepted && _failedTransactionId.HasValue)
                _staleCompletionAccepted = _host.TryCompleteNavigation(_failedTransactionId.Value);
        }

        private void ApplyCaptureLayout(Vector2Int resolution)
        {
            if (_host.ActiveStage2View == null) return;
            _host.TryApplyStage2Layout(
                resolution.x, resolution.y, 32, 32,
                resolution.x - 64, resolution.y - 64, 326, 88);
        }

        private void WriteCapture(Stage2EvidenceCaptureOptions options, Vector2Int resolution)
        {
            var directory = Path.GetDirectoryName(options.Path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var cameraObject = new GameObject("Stage2EvidenceCamera");
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
            var snapshot=_host.Stage2Snapshot;var cue=_host.ActiveStage2View==null?default:_host.ActiveStage2View.LastMediaCue;
            var outcome=_host.ActiveStage2View==null?default:_host.ActiveStage2View.LastOutcomeFrame;
            var record=new EvidenceRecord
            {
                width=resolution.x,height=resolution.y,requested_ratio=options.Ratio.ToString(),selected_child=options.Child.ToString(),
                requested_river=options.River.ToString(),requested_hotspot=options.Hotspot.ToString(),sound_enabled=options.SoundEnabled,
                captions_enabled=options.CaptionsEnabled,reduced_motion=_host.ReducedMotionEnabled,requested_beat=options.Beat.ToString(),
                requested_visited_before=options.VisitedBefore,requested_repeat=options.Repeat,requested_finish=options.AttemptFinish,
                requested_overlay=options.Overlay.ToString(),failure=options.Failure.ToString(),recovery_action=options.RecoveryAction.ToString(),
                route=_host.CurrentRoute.ToString(),session_location=_host.SessionLocation.ToString(),stage_state=snapshot.State.ToString(),
                selected_river=snapshot.SelectedRiver.ToString(),current_hotspot=snapshot.CurrentHotspot.ToString(),
                next_unvisited_hotspot=_host.Stage2NextUnvisitedHotspot.ToString(),low_observed=(int)snapshot.LowObserved,
                middle_observed=(int)snapshot.MiddleObserved,high_observed=(int)snapshot.HighObserved,
                consequence_elapsed_seconds=snapshot.ConsequenceElapsedSeconds,option_count=snapshot.OptionCount,
                completion_recorded=snapshot.CompletionRecorded,input_ready=_host.Stage2InputReady,boundary_loading=_host.Stage2BoundaryLoading,
                navigation_transaction_id=_host.ActiveNavigationId?.ToString()??string.Empty,navigation_caller=_host.ActiveNavigationCaller?.ToString()??string.Empty,
                navigation_destination=_host.ActiveNavigationDestination?.ToString()??string.Empty,boat3_type_present=Type.GetType("Exloris.Runtime.Boat.Boat3StageHost, Exloris.Runtime")!=null,
                stage3_type_present=Type.GetType("Exloris.Runtime.Stage3.Stage3StageHost, Exloris.Runtime")!=null,
                repeat_accepted=_repeatAccepted,repeat_replaced_token=_repeatReplacedToken,finish_attempt_accepted=_finishAttemptAccepted,
                recovery_action_accepted=_recoveryActionAccepted,stale_completion_accepted=_staleCompletionAccepted,
                pause_state=_host.Stage2PauseState.ToString(),pause_reason=_host.Stage2LastPauseReason?.ToString()??string.Empty,
                pause_caller_state=_host.Stage2PauseCallerState.ToString(),leave_caller_state=_host.Stage2DirectLeaveCallerState.ToString(),
                help_visible=_host.ActiveStage2HelpView!=null&&_host.ActiveStage2HelpView.IsVisible,
                help_mode=_host.ActiveStage2HelpView==null?string.Empty:_host.ActiveStage2HelpView.LastMode.ToString(),
                leave_visible=_host.ActiveLeaveConfirmationView!=null&&_host.ActiveLeaveConfirmationView.IsVisible,
                recovery_state=EffectiveRecovery().ToString(),media_mode=cue.Mode.ToString(),caption_id=cue.CaptionId??string.Empty,
                caption_visible=cue.CaptionVisible,voice_playing=_host.Stage2VoicePlaying,loops_playing=_host.Stage2LoopsPlaying,
                stone_sfx_playing=_host.Stage2StoneSfxPlaying,outcome_phase=outcome.Phase.ToString(),
                outcome_river=outcome.River.ToString(),outcome_hotspot=outcome.Hotspot.ToString(),outcome_reduced_motion=outcome.ReducedMotion,
                semantic_control_count=_host.ActiveStage2View==null?0:_host.ActiveStage2View.SemanticControlCount,
                display_semantic_control_count=_host.ActiveStage2View==null?0:_host.ActiveStage2View.DisplaySemanticControlCount,
                accepted_command_count=_host.Stage2AcceptedCommandCount,frame_samples=performance.SampleCount,
                p95_frame_ms=performance.P95FrameMilliseconds,p99_frame_ms=performance.P99FrameMilliseconds,
                maximum_frame_ms=performance.MaximumFrameMilliseconds,maximum_consecutive_over_33_3_ms=performance.MaximumConsecutiveFramesOver33Milliseconds,
                allocated_mib=performance.AllocatedMemoryMiB,reserved_mib=performance.ReservedMemoryMiB,
            };
            var jsonPath=Path.ChangeExtension(options.Path,".json");File.WriteAllText(jsonPath,JsonUtility.ToJson(record,true));
            Debug.Log($"STAGE2_EVIDENCE_CAPTURE path={options.Path} metadata={jsonPath}");
        }

        private AvatarTechnicalOverlayState EffectiveRecovery() =>
            _host.Stage2RecoveryState != AvatarTechnicalOverlayState.Hidden
                ? _host.Stage2RecoveryState : _host.Boat2RecoveryState;
        private static bool IsEntryFailure(Stage2EvidenceFailure value) => value==Stage2EvidenceFailure.EntryTimeout||value==Stage2EvidenceFailure.EntryAsset||value==Stage2EvidenceFailure.EntryLayout;
        private static bool IsBoat3Failure(Stage2EvidenceFailure value) => value==Stage2EvidenceFailure.Boat3Timeout||value==Stage2EvidenceFailure.Boat3Asset||value==Stage2EvidenceFailure.Boat3Layout;
        private static Stage2River ToRiver(Stage2EvidenceRiver value) => value==Stage2EvidenceRiver.Low?Stage2River.Low:value==Stage2EvidenceRiver.High?Stage2River.High:Stage2River.Middle;
        private static Stage2Hotspot ToHotspot(Stage2EvidenceHotspot value) => value==Stage2EvidenceHotspot.Field?Stage2Hotspot.Field:value==Stage2EvidenceHotspot.Path?Stage2Hotspot.Path:Stage2Hotspot.Boat;
        private static double ElapsedFor(Stage2EvidenceBeat value) => value==Stage2EvidenceBeat.ConsequenceReveal?0.6d:value==Stage2EvidenceBeat.ConsequenceSettle?1.8d:value==Stage2EvidenceBeat.ConsequenceComplete?2.4d:0d;
        private static Vector2Int ResolutionFor(Stage2EvidenceRatio value) => value==Stage2EvidenceRatio.FourThree?new Vector2Int(1440,1080):value==Stage2EvidenceRatio.UltraWide?new Vector2Int(1920,800):new Vector2Int(1600,900);
        private static void Fail(string message,int exitCode){Debug.LogError(message);Application.Quit(exitCode);}

        [Serializable]
        private sealed class EvidenceRecord
        {
            public int width,height,requested_visited_before,low_observed,middle_observed,high_observed,option_count;
            public int semantic_control_count,display_semantic_control_count,accepted_command_count,frame_samples,maximum_consecutive_over_33_3_ms;
            public string requested_ratio,selected_child,requested_river,requested_hotspot,requested_beat,requested_overlay,failure,recovery_action;
            public string route,session_location,stage_state,selected_river,current_hotspot,next_unvisited_hotspot;
            public string navigation_transaction_id,navigation_caller,navigation_destination,pause_state,pause_reason,pause_caller_state,leave_caller_state;
            public string help_mode,recovery_state,media_mode,caption_id,outcome_phase,outcome_river,outcome_hotspot;
            public bool sound_enabled,captions_enabled,reduced_motion,requested_repeat,requested_finish,completion_recorded,input_ready,boundary_loading;
            public bool boat3_type_present,stage3_type_present,repeat_accepted,repeat_replaced_token,finish_attempt_accepted,recovery_action_accepted,stale_completion_accepted;
            public bool help_visible,leave_visible,caption_visible,voice_playing,loops_playing,stone_sfx_playing,outcome_reduced_motion;
            public double consequence_elapsed_seconds;
            public float p95_frame_ms,p99_frame_ms,maximum_frame_ms,allocated_mib,reserved_mib;
        }
    }
}
