using System;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;

namespace Exloris.Runtime.Recovery
{
    public sealed class PauseOverlayController :
        IPauseOverlaySink,
        Exloris.Runtime.Boat.IBoat1PauseSink,
        Exloris.Runtime.Boat.IBoat2PauseSink,
        Exloris.Runtime.Boat.IBoat3PauseSink,
        Exloris.Runtime.Stage1.IStage1PauseSink,
        Exloris.Runtime.Stage2.IStage2PauseSink,
        Exloris.Runtime.Stage3.IStage3PauseSink,
        Exloris.Runtime.Completion.ICompletionPauseSink,
        IPauseOverlayReturnSink,
        INestedLeaveReturnSink
    {
        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IPauseCallerAdapter _adapter;
        private readonly JourneyOverviewPauseAdapter _legacyJourneyAdapter;
        private readonly AccessibilityState _accessibility;
        private readonly AccessibilityCoordinator _accessibilityCoordinator;
        private readonly AccessibilityPreferenceStore _preferences;
        private readonly IPauseOverlayView _view;
        private readonly IJourneyOverviewPauseHelpSink _help;
        private readonly INestedLeaveConfirmationSink _leave;
        private readonly Func<Guid> _tokenSource;

        public PauseOverlayController(
            SessionState session,
            NavigationAuthority navigation,
            JourneyOverviewController journey,
            IJourneyOverviewPauseMedia media,
            AccessibilityState accessibility,
            AccessibilityCoordinator accessibilityCoordinator,
            AccessibilityPreferenceStore preferences,
            IPauseOverlayView view,
            IJourneyOverviewPauseHelpSink help,
            INestedLeaveConfirmationSink leave,
            Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            if (journey == null) throw new ArgumentNullException(nameof(journey));
            if (media == null) throw new ArgumentNullException(nameof(media));
            _legacyJourneyAdapter = new JourneyOverviewPauseAdapter(journey, media);
            _adapter = _legacyJourneyAdapter;
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = accessibilityCoordinator
                ?? throw new ArgumentNullException(nameof(accessibilityCoordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _help = help ?? throw new ArgumentNullException(nameof(help));
            _leave = leave ?? throw new ArgumentNullException(nameof(leave));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public PauseOverlayController(
            SessionState session,
            NavigationAuthority navigation,
            IPauseCallerAdapter adapter,
            AccessibilityState accessibility,
            AccessibilityCoordinator accessibilityCoordinator,
            AccessibilityPreferenceStore preferences,
            IPauseOverlayView view,
            IJourneyOverviewPauseHelpSink help,
            INestedLeaveConfirmationSink leave,
            Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = accessibilityCoordinator ?? throw new ArgumentNullException(nameof(accessibilityCoordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _help = help ?? throw new ArgumentNullException(nameof(help));
            _leave = leave ?? throw new ArgumentNullException(nameof(leave));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public PauseLayerState State { get; private set; }
        public PauseOverlaySnapshot Snapshot { get; private set; }

        public bool TryShow(JourneyOverviewSnapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.JourneyOverview || _legacyJourneyAdapter == null
                || !_adapter.TryCapture(out var handle)
                || !Matches(snapshot, _legacyJourneyAdapter.CapturedSnapshot)) return false;
            return TryShowCore(handle, snapshot);
        }

        public bool TryShow(Exloris.Runtime.Boat.Boat1Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Boat1 || !(_adapter is Boat1PauseAdapter boat)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(boat.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Boat.Boat2Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Boat2 || !(_adapter is Boat2PauseAdapter boat)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(boat.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Boat.Boat3Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Boat3 || !(_adapter is Boat3PauseAdapter boat)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(boat.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Stage1.Stage1Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Stage1 || !(_adapter is Stage1PauseAdapter stage1)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(stage1.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Stage2.Stage2Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Stage2
                || !(_adapter is Stage2PauseAdapter stage2)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(stage2.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Stage3.Stage3Snapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Stage3
                || !(_adapter is Stage3PauseAdapter stage3)
                || !_adapter.TryCapture(out var handle) || !snapshot.Equals(stage3.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        public bool TryShow(Exloris.Runtime.Completion.CompletionSnapshot snapshot)
        {
            if (State != PauseLayerState.Hidden || _adapter.Route != RouteId.Completion
                || !(_adapter is CompletionPauseAdapter completion)
                || !_adapter.TryCapture(out var handle)
                || !snapshot.Equals(completion.CapturedSnapshot)) return false;
            return TryShowCore(handle, default);
        }

        private bool TryShowCore(PauseCallerHandle handle, JourneyOverviewSnapshot legacySnapshot)
        {
            if (State != PauseLayerState.Hidden || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != _adapter.Route
                || _navigation.CurrentRoute != _adapter.Route || _navigation.IsLocked
                || _navigation.Recovery != null || !_adapter.IsCurrent(handle)) return false;
            var stackToken = _tokenSource(); if (stackToken == Guid.Empty) throw new InvalidOperationException("Pause stack token cannot be empty.");
            if (!_adapter.PauseMedia(handle, 0d)) return false;
            Snapshot = legacySnapshot.TokenId != Guid.Empty
                ? new PauseOverlaySnapshot(stackToken, handle, legacySnapshot, CurrentSettings())
                : new PauseOverlaySnapshot(stackToken, handle, CurrentSettings());
            State = PauseLayerState.Pause;
            _view.ApplySettings(Snapshot.Settings);
            _view.ShowAndBlockCaller(Snapshot);
            return true;
        }

        public bool HandleResume(double nowSeconds)
        {
            if (State != PauseLayerState.Pause || !_adapter.IsCurrent(Snapshot.CallerHandle)) return false;
            var captured = Snapshot;
            if (!_adapter.Restore(captured.CallerHandle, nowSeconds)) return false;
            if (!_adapter.ResumeMedia(captured.CallerHandle, nowSeconds)) return false;
            State = PauseLayerState.Hidden;
            _view.RestoreCaller(captured);
            return true;
        }

        public bool HandleSound()
        {
            if (State != PauseLayerState.Pause) return false;
            _session.SoundEnabled = !_session.SoundEnabled;
            _adapter.RefreshSettings(0d);
            RefreshSettings(save: false);
            return true;
        }

        public bool HandleCaptions()
        {
            if (State != PauseLayerState.Pause) return false;
            _accessibility.SetCaptionsEnabled(!_accessibility.CaptionsEnabled);
            _adapter.RefreshSettings(0d);
            RefreshSettings(save: true);
            return true;
        }

        public bool HandleMotion()
        {
            if (State != PauseLayerState.Pause) return false;
            _accessibilityCoordinator.SetAlwaysReduce(!_accessibility.AlwaysReduce);
            RefreshSettings(save: true);
            return true;
        }

        public bool HandleHelp()
        {
            if (State != PauseLayerState.Pause || !_help.TryShow(Snapshot)) return false;
            State = PauseLayerState.Help;
            _view.HoldBlockedForChild(Snapshot);
            return true;
        }

        public bool HandleHome()
        {
            if (State != PauseLayerState.Pause) return false;
            var request = new LeaveRequest(
                _adapter.Route, LeaveReturnLayer.PauseOverlay, Snapshot.StackToken);
            if (!_leave.TryShow(request)) return false;
            State = PauseLayerState.Leave;
            _view.HoldBlockedForChild(Snapshot);
            return true;
        }

        public bool IsCurrentPause(PauseOverlaySnapshot snapshot)
        {
            return State == PauseLayerState.Pause && Snapshot.Equals(snapshot);
        }

        public bool RestoreFromHelp(PauseOverlaySnapshot snapshot)
        {
            if (State != PauseLayerState.Help || !Snapshot.Equals(snapshot)) return false;
            State = PauseLayerState.Pause;
            _view.RestoreFromChild(Snapshot);
            return true;
        }

        public bool RestoreFromLeave(LeaveRequest request)
        {
            if (State != PauseLayerState.Leave
                || request.Caller != _adapter.Route
                || request.ReturnLayer != LeaveReturnLayer.PauseOverlay
                || request.StackToken != Snapshot.StackToken)
                return false;
            State = PauseLayerState.Pause;
            _view.RestoreFromChild(Snapshot);
            return true;
        }

        public bool HoldForLeaveTransition(LeaveRequest request)
        {
            if (State != PauseLayerState.Leave
                || request.StackToken != Snapshot.StackToken) return false;
            State = PauseLayerState.Transitioning;
            _view.HoldBlockedForTransition(Snapshot);
            return true;
        }

        private void RefreshSettings(bool save)
        {
            var settings = CurrentSettings();
            Snapshot = Snapshot.WithSettings(settings);
            if (save)
                _preferences.Save(new AccessibilityPreferences(
                    _accessibility.AlwaysReduce, _accessibility.CaptionsEnabled));
            _view.ApplySettings(settings);
        }

        private PauseSettings CurrentSettings() => new PauseSettings(
            _session.SoundEnabled,
            _accessibility.CaptionsEnabled,
            _accessibility.EffectiveReducedMotion);

        private static bool Matches(
            JourneyOverviewSnapshot first, JourneyOverviewSnapshot second)
        {
            return first.TokenId == second.TokenId
                && first.State == second.State
                && first.ElapsedSeconds.Equals(second.ElapsedSeconds)
                && first.RevealedBeatCount == second.RevealedBeatCount;
        }
    }
}
