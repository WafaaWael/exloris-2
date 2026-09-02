using System;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Input;
using Exloris.Runtime.Lifecycle;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using UnityEngine;

namespace Exloris.Runtime.Stage2
{
    [DisallowMultipleComponent]
    public sealed class Stage2StageHost : MonoBehaviour
    {
        private ChildInputRouter _input; private SessionState _session; private NavigationAuthority _navigation;
        private AudioDirector _audioDirector; private IAudioClipRegistry _audioRegistry; private AccessibilityState _accessibility;
        private AccessibilityCoordinator _accessibilityCoordinator; private AccessibilityPreferenceStore _preferences;
        private AppLifecycleCoordinator _lifecycle; private RecoveryPresenter _recoveryPresenter;
        private Stage2StageView _stagePrefab; private PauseOverlayView _pausePrefab; private Stage2HelpView _helpPrefab;
        private LeaveConfirmationController _leaveController; private LeaveConfirmationView _leaveView;
        private Stage2StageView _view; private PauseOverlayView _pauseView; private Stage2HelpView _helpView;
        private Stage2Controller _controller; private Stage2MediaPresenter _media; private Stage2OutcomePresenter _outcome;
        private Stage2PauseAdapter _adapter; private PauseOverlayController _pause; private Stage2HelpController _directHelp;
        private PauseRelay _pauseRelay; private HelpRelay _helpRelay;
        private Stage2MediaHandle _mediaHandle; private Stage2Snapshot _directLeaveSnapshot; private PauseOverlaySnapshot _pauseHelpSnapshot;
        private Guid _entryTransactionId; private bool _prepared, _entered, _boundaryLoading, _pauseHelpVisible;
        private int _inputReadyFrame = -1;
        private string _lastLayoutFailure;

        public event Action<Guid> WelcomeRequested;
        public event Action BoundaryRequested;
        public Stage2StageView View => _view;
        public PauseOverlayView PauseView => _pauseView;
        public Stage2HelpView HelpView => _helpView;
        public bool IsPrepared => _prepared;
        public bool IsEntered => _entered;
        public bool InputReady => _entered && !_boundaryLoading && _inputReadyFrame < 0
            && _input.InputsActive && CallerLayerInputReady();
        public bool BoundaryLoading => _boundaryLoading;
        public Stage2Snapshot Snapshot => _controller == null ? default : _controller.Snapshot;
        public Stage2Hotspot NextUnvisitedHotspot => _controller?.NextUnvisitedHotspot ?? Stage2Hotspot.None;
        public bool VoicePlaying => _media != null && _media.HasActiveVoice;
        public bool LoopsPlaying => _media != null && _media.HasActiveLoops;
        public bool StoneSfxPlaying => _media != null && _media.HasActiveStoneSfx;
        public Guid CurrentTokenId => _controller?.CurrentTokenId ?? Guid.Empty;
        public PauseLayerState PauseState => _pause?.State ?? PauseLayerState.Hidden;
        public int AcceptedCommandCount { get; private set; }
        public Stage2PauseReason? LastPauseReason => _view?.LastPauseReason;
        public Stage2State PauseCallerState => _adapter == null ? Stage2State.Hidden : _adapter.CapturedSnapshot.State;
        public Stage2State DirectLeaveCallerState => _directLeaveSnapshot.State;
        public AvatarTechnicalOverlayState RecoveryState => _leaveView == null
            ? AvatarTechnicalOverlayState.Hidden : _leaveView.TechnicalOverlayState;
        public string LastPreparationFailure { get; private set; }

        public void Configure(ChildInputRouter input, SessionState session, NavigationAuthority navigation,
            AudioDirector audioDirector, IAudioClipRegistry audioRegistry, AccessibilityState accessibility,
            AccessibilityCoordinator coordinator, AccessibilityPreferenceStore preferences,
            AppLifecycleCoordinator lifecycle, RecoveryPresenter recovery, Stage2StageView stagePrefab,
            PauseOverlayView pausePrefab, Stage2HelpView helpPrefab)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _audioDirector = audioDirector ?? throw new ArgumentNullException(nameof(audioDirector));
            _audioRegistry = audioRegistry ?? throw new ArgumentNullException(nameof(audioRegistry));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _recoveryPresenter = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _stagePrefab = stagePrefab ?? throw new ArgumentNullException(nameof(stagePrefab));
            _pausePrefab = pausePrefab ?? throw new ArgumentNullException(nameof(pausePrefab));
            _helpPrefab = helpPrefab ?? throw new ArgumentNullException(nameof(helpPrefab));
        }

        public bool Prepare(Guid transactionId, LeaveConfirmationController leaveController,
            LeaveConfirmationView leaveView, bool selectedGirl)
        {
            LastPreparationFailure = null;
            var transaction = _navigation?.ActiveTransaction;
            if (_prepared || transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat2 || transaction.Destination != RouteId.Stage2) return false;
            _leaveController = leaveController ?? throw new ArgumentNullException(nameof(leaveController));
            _leaveView = leaveView ?? throw new ArgumentNullException(nameof(leaveView));
            var content = Stage2Content.LoadApproved();
            _view = Instantiate(_stagePrefab, transform, false); _view.Configure(_input, content, selectedGirl);
            _helpView = Instantiate(_helpPrefab, transform, false); _helpView.Configure(_input, content, JourneyOverviewContent.LoadApproved());
            var screenDpi = Screen.dpi > 0 ? Screen.dpi : 326;
            if (!TryApplyResponsiveLayout(Screen.width, Screen.height, Screen.safeArea.x, Screen.safeArea.y,
                    Screen.safeArea.width, Screen.safeArea.height, screenDpi, 88))
            {
                LastPreparationFailure = $"{_lastLayoutFailure ?? "layout"} screen={Screen.width}x{Screen.height} "
                    + $"safe={Screen.safeArea.x},{Screen.safeArea.y},{Screen.safeArea.width},{Screen.safeArea.height} dpi={screenDpi}";
                Cleanup();
                return false;
            }
            _view.SetVisible(false);
            _pauseRelay = new PauseRelay(); _helpRelay = new HelpRelay();
            _controller = new Stage2Controller(_session, _navigation, _view, _pauseRelay, _leaveController, Guid.NewGuid);
            _media = new Stage2MediaPresenter(_audioDirector, _audioRegistry, content, _session, _accessibility, _view);
            _outcome = new Stage2OutcomePresenter(content, _view);
            _directHelp = new Stage2HelpController(_controller, _helpView);
            _entryTransactionId = transactionId; _prepared = true; return true;
        }

        public bool Enter(double now)
        {
            var transaction = _navigation?.ActiveTransaction;
            if (!_prepared || _entered || transaction == null || transaction.Id != _entryTransactionId
                || transaction.Caller != RouteId.Boat2 || transaction.Destination != RouteId.Stage2) return false;
            _pauseView = Instantiate(_pausePrefab, transform, false);
            _pauseView.Configure(_input, JourneyOverviewContent.LoadApproved(), _view);
            _adapter = new Stage2PauseAdapter(_controller, _media, _outcome);
            _pause = new PauseOverlayController(_session, _navigation, _adapter, _accessibility,
                _accessibilityCoordinator, _preferences, _pauseView, _helpRelay, _leaveController, Guid.NewGuid);
            _pauseRelay.Target = _pause; _helpRelay.Host = this;
            _leaveController.BindNestedReturnSink(_pause); _leaveView.BindStage2Caller(_view, _pauseView);
            _view.SetVisible(true);
            if (!_controller.Enter(_entryTransactionId, now, _accessibility.EffectiveReducedMotion))
            { _view.SetVisible(false); return false; }
            _mediaHandle = _media.StartIntro(_controller.CurrentTokenId, now); _adapter.BindMediaHandle(_mediaHandle);
            _inputReadyFrame = Time.frameCount + 1; _entered = true; return true;
        }

        public void Tick(double now, float delta)
        {
            if (!_entered) return;
            if (_boundaryLoading) { if (_lifecycle.Tick(now)) ShowRecovery(); return; }
            if (_inputReadyFrame >= 0 && Time.frameCount >= _inputReadyFrame)
            { _inputReadyFrame = -1; _view.SetInputReady(true); _input.SetVisibleStateReady(true); }
            if (_pause.State != PauseLayerState.Hidden || _controller.State == Stage2State.Help
                || _controller.State == Stage2State.Leaving) return;
            _media.Advance(_controller.CurrentTokenId, _media.CurrentMediaTokenId, now);
            if (_controller.State == Stage2State.ConsequencePlaying)
            {
                var routeToken = _controller.CurrentTokenId; var consequenceToken = _controller.CurrentConsequenceTokenId;
                _outcome.Advance(routeToken, consequenceToken, now);
                var previous = _controller.State;
                if (_controller.AdvanceConsequence(routeToken, consequenceToken, now)
                    && previous == Stage2State.ConsequencePlaying && _controller.State == Stage2State.Inspecting)
                    _outcome.Stop(routeToken, consequenceToken);
            }
            if (_controller.State == Stage2State.Navigating) BeginBoundary();
        }

        public bool OwnsControl(string id) => _view != null && (_view.ControlIds.Contains(id)
            || id == Stage2HelpView.BackControlId || id == PauseOverlayView.HelpControlId
            || id == PauseOverlayView.SoundControlId || id == PauseOverlayView.CaptionsControlId
            || id == PauseOverlayView.ResumeControlId || id == PauseOverlayView.MotionControlId
            || id == PauseOverlayView.HomeControlId || id == LeaveConfirmationView.StayControlId
            || id == LeaveConfirmationView.CancelControlId || id == LeaveConfirmationView.LeaveControlId
            || id == LeaveConfirmationView.RetryControlId || id == LeaveConfirmationView.HomeControlId);

        public bool HandleSemanticCommand(string id, double now)
        {
            if (!_entered || string.IsNullOrEmpty(id)) return false;
            var token = _controller.CurrentTokenId; var accepted = false;
            switch (id)
            {
                case Stage2StageView.PlayControlId: accepted = _controller.HandlePlay(token, now); break;
                case Stage2StageView.LowControlId: accepted = SelectRiver(Stage2River.Low, now); break;
                case Stage2StageView.MiddleControlId: accepted = SelectRiver(Stage2River.Middle, now); break;
                case Stage2StageView.HighControlId: accepted = SelectRiver(Stage2River.High, now); break;
                case Stage2StageView.FieldControlId: accepted = Inspect(Stage2Hotspot.Field, now); break;
                case Stage2StageView.PathControlId: accepted = Inspect(Stage2Hotspot.Path, now); break;
                case Stage2StageView.BoatControlId: accepted = Inspect(Stage2Hotspot.Boat, now); break;
                case Stage2StageView.CompareControlId: accepted = _controller.HandleCompare(token, now); break;
                case Stage2StageView.FinishControlId: accepted = _controller.HandleFinish(token, now); if (accepted) BeginBoundary(); break;
                case Stage2StageView.HelpControlId: accepted = ShowDirectHelp(now); break;
                case Stage2StageView.RepeatControlId: accepted = Repeat(now); break;
                case Stage2StageView.PauseControlId: accepted = PauseStage(now, Stage2PauseReason.User); break;
                case Stage2StageView.HomeControlId: _directLeaveSnapshot = _controller.Snapshot; accepted = _controller.HandleHome(now); break;
                case Stage2HelpView.BackControlId: accepted = HandleHelpBack(now); break;
                case PauseOverlayView.HelpControlId: accepted = _pause.HandleHelp(); break;
                case PauseOverlayView.SoundControlId: accepted = _pause.HandleSound(); break;
                case PauseOverlayView.CaptionsControlId: accepted = _pause.HandleCaptions(); break;
                case PauseOverlayView.MotionControlId: accepted = _pause.HandleMotion(); break;
                case PauseOverlayView.ResumeControlId: accepted = ResumePause(now); break;
                case PauseOverlayView.HomeControlId: accepted = _adapter.CapturedSnapshot.State == Stage2State.Leaving
                    ? ResumePause(now) : _pause.HandleHome(); break;
                case LeaveConfirmationView.StayControlId: accepted = RestoreLeave(false, now); break;
                case LeaveConfirmationView.CancelControlId: accepted = RestoreLeave(true, now); break;
                case LeaveConfirmationView.LeaveControlId: accepted = RequestWelcome(now); break;
                case LeaveConfirmationView.RetryControlId: accepted = RetryRecovery(now); break;
                case LeaveConfirmationView.HomeControlId: accepted = HomeRecovery(now); break;
            }
            if (accepted) AcceptedCommandCount++;
            return accepted;
        }

        public bool HandleInterruption(double now, Stage2PauseReason reason) =>
            _entered && !_boundaryLoading && (_pause.State != PauseLayerState.Hidden || PauseStage(now, reason));

        public bool CompleteConsequenceForDiagnostics()
        {
            return AdvanceConsequenceForDiagnostics(Stage2Controller.ConsequenceSeconds)
                && _controller.State == Stage2State.Inspecting;
        }

        public bool AdvanceConsequenceForDiagnostics(double elapsedSeconds)
        {
            if (!_entered || _controller.State != Stage2State.ConsequencePlaying
                || double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds)
                || elapsedSeconds < 0d || elapsedSeconds > Stage2Controller.ConsequenceSeconds)
                return false;
            var route = _controller.CurrentTokenId;
            var consequence = _controller.CurrentConsequenceTokenId;
            var now = _controller.Snapshot.StateStartedAtSeconds + elapsedSeconds;
            if (!_outcome.Advance(route, consequence, now)
                || !_controller.AdvanceConsequence(route, consequence, now)) return false;
            if (_controller.State == Stage2State.Inspecting) _outcome.Stop(route, consequence);
            return true;
        }

        public bool HandleBoundaryAssetFailure()
        { if (!_entered || !_boundaryLoading || !_lifecycle.HandleAssetFailure()) return false; ShowRecovery(); return true; }

        public bool TryApplyResponsiveLayout(float sw, float sh, float safeX, float safeY,
            float safeW, float safeH, float ppi, int minimum)
        {
            _lastLayoutFailure = null;
            try
            {
                sw = Mathf.Max(1, sw); sh = Mathf.Max(1, sh);
                var scale = Exloris.Runtime.Layout.ResponsiveLayoutProfile.CalculateCanvasScale(sw, sh);
                _lastLayoutFailure = "stage";
                if (!_view.TryApplyResponsiveLayout(sw, sh, safeX, safeY, safeW, safeH, ppi, minimum))
                    throw new InvalidOperationException("Stage 2 safe area is infeasible.");
                _lastLayoutFailure = "pause";
                if (_pauseView != null) _pauseView.ApplyResponsiveLayout(sw, sh, safeW / scale, safeH / scale, ppi, minimum);
                _lastLayoutFailure = "help";
                if (!_helpView.TryApplyResponsiveLayout(sw, sh, safeW, safeH, ppi, minimum))
                    throw new InvalidOperationException("Stage 2 Help safe area is infeasible.");
                _lastLayoutFailure = null;
                return true;
            }
            catch (InvalidOperationException exception)
            {
                _lastLayoutFailure = $"{_lastLayoutFailure}:{exception.Message}";
                if (_entered && _boundaryLoading) HandleBoundaryAssetFailure();
                return false;
            }
        }

        public void ShowRecovery()
        {
            if (!_entered || !_boundaryLoading) return;
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing) _recoveryPresenter.MarkRecoveryVisible();
            if (_recoveryPresenter.State != RecoveryPresentationState.Visible) return;
            _controller.EnterRecovery(); _leaveView.ShowTechnicalOverlay(RouteId.Stage2, AvatarTechnicalOverlayState.Recovery);
        }

        public void CompleteWelcomeTransition(Guid id)
        { _leaveController?.CompleteTransition(id); _leaveView?.CompleteTransitionAndHide(); }

        public void Cleanup()
        {
            if (_view != null) foreach (var id in _view.ControlIds) _input.Unregister(id);
            if (_pauseView != null) foreach (var id in _pauseView.ControlIds) _input.Unregister(id);
            if (_helpView != null) foreach (var id in _helpView.ControlIds) _input.Unregister(id);
            if (_media != null && _controller != null && _controller.CurrentTokenId != Guid.Empty) _media.Stop(_controller.CurrentTokenId);
            if (_helpView != null) Destroy(_helpView.gameObject); if (_pauseView != null) Destroy(_pauseView.gameObject); if (_view != null) Destroy(_view.gameObject);
            _view = null; _pauseView = null; _helpView = null; _controller = null; _media = null; _outcome = null;
            _adapter = null; _pause = null; _directHelp = null; _pauseRelay = null; _helpRelay = null;
            _prepared = false; _entered = false; _boundaryLoading = false; _pauseHelpVisible = false;
            _inputReadyFrame = -1; _entryTransactionId = Guid.Empty;
        }

        private bool SelectRiver(Stage2River river, double now)
        {
            var action = Guid.NewGuid();
            if (!_controller.HandleSelectRiver(_controller.CurrentTokenId, river, action, now)) return false;
            _media.PlayStoneSfx(_controller.CurrentTokenId, river, action, now); return true;
        }
        private bool Inspect(Stage2Hotspot hotspot, double now)
        {
            if (!_controller.HandleHotspot(_controller.CurrentTokenId, hotspot, Guid.NewGuid(), now)) return false;
            return _outcome.Start(_controller.Snapshot, _accessibility.EffectiveReducedMotion, now);
        }
        private bool Repeat(double now)
        {
            if (!_controller.HandleRepeat(_controller.CurrentTokenId, now)) return false;
            if (_controller.State == Stage2State.ConsequencePlaying)
                return _outcome.Start(_controller.Snapshot, _accessibility.EffectiveReducedMotion, now);
            _mediaHandle = _media.StartIntro(_controller.CurrentTokenId, now); _adapter.BindMediaHandle(_mediaHandle); return true;
        }
        private bool ShowDirectHelp(double now)
        {
            if (!_directHelp.TryShow()) return false;
            _mediaHandle = _media.StartHelp(_controller.CurrentTokenId, now); _adapter.BindMediaHandle(_mediaHandle); return true;
        }
        private bool HandleHelpBack(double now)
        {
            if (!_pauseHelpVisible)
            {
                if (!_directHelp.HandleBack()) return false;
                _mediaHandle = _media.StartIntro(_controller.CurrentTokenId, now); _adapter.BindMediaHandle(_mediaHandle); return true;
            }
            if (!_pause.RestoreFromHelp(_pauseHelpSnapshot)) return false;
            _pauseHelpVisible = false; _helpView.SetPauseOverlayPriority(false);
            if (_adapter.CapturedSnapshot.State == Stage2State.Help)
            { _helpView.Show(_adapter.CapturedSnapshot, _directHelp.LastMode); _helpView.SetInterruptionBlocked(_pause.State == PauseLayerState.Pause); }
            else _helpView.Hide();
            return true;
        }
        private bool PauseStage(double now, Stage2PauseReason reason)
        {
            if (!_adapter.PreparePause(now, reason)) return false;
            if (!_controller.HandlePause(now)) { _adapter.CancelPreparedPause(); return false; }
            var caller = _adapter.CapturedSnapshot.State; var stacked = caller == Stage2State.Help || caller == Stage2State.Leaving;
            _pauseView.SetBoatInterruptionPriority(stacked);
            if (caller == Stage2State.Help) _helpView.SetInterruptionBlocked(true);
            else if (caller == Stage2State.Leaving) _leaveView.SetInterruptionBlocked(true);
            return true;
        }
        private bool ResumePause(double now)
        {
            var caller = _adapter.CapturedSnapshot.State;
            if (!_pause.HandleResume(now)) return false;
            _pauseView.SetBoatInterruptionPriority(false);
            if (caller == Stage2State.Help) _helpView.SetInterruptionBlocked(false);
            else if (caller == Stage2State.Leaving) _leaveView.SetInterruptionBlocked(false);
            if (caller == Stage2State.Help || caller == Stage2State.Leaving) _view.SetInputReady(false);
            _mediaHandle = new Stage2MediaHandle(_controller.CurrentTokenId, _media.CurrentMediaTokenId, null, null, null);
            _adapter.BindMediaHandle(_mediaHandle); return true;
        }
        private bool ShowPauseHelp(PauseOverlaySnapshot snapshot)
        {
            _pauseHelpSnapshot = snapshot; _pauseHelpVisible = true; _pauseView.SetControlsEnabled(false);
            _helpView.SetInterruptionBlocked(false); _helpView.SetPauseOverlayPriority(true);
            var captured = _adapter.CapturedSnapshot;
            var mode = captured.State == Stage2State.Inspecting ? Stage2HelpMode.InspectEachPlace : Stage2HelpMode.ChooseStoneThenPlaces;
            _helpView.Show(captured, mode); return true;
        }
        private bool RestoreLeave(bool cancel, double now)
        { var nested = _pause.State == PauseLayerState.Leave; var ok = cancel ? _leaveController.HandleCancel() : _leaveController.HandleStay(); return ok && (nested || _controller.RestoreFromLeave(_directLeaveSnapshot, now)); }
        private bool RequestWelcome(double now)
        { if (!_leaveController.HandleLeave(now)) return false; var tx = _navigation.ActiveTransaction; if (tx == null || tx.Destination != RouteId.Welcome) return false; _input.SetVisibleStateReady(false); WelcomeRequested?.Invoke(tx.Id); return true; }
        private bool RetryRecovery(double now)
        { var result = _lifecycle.Retry(now); if (!result.Accepted || result.Transaction.Caller != RouteId.Stage2 || result.Transaction.Destination != RouteId.Boat3) return false; if (!_controller.RetryBoundary()) return false; _leaveView.ShowTechnicalOverlay(RouteId.Stage2, AvatarTechnicalOverlayState.Loading); return true; }
        private bool HomeRecovery(double now)
        { var result = _lifecycle.Home(now); if (!result.Accepted || result.Transaction.Caller != RouteId.Stage2 || result.Transaction.Destination != RouteId.Welcome) return false; _leaveView.ShowTechnicalOverlay(RouteId.Stage2, AvatarTechnicalOverlayState.Loading); WelcomeRequested?.Invoke(result.Transaction.Id); return true; }
        private void BeginBoundary()
        { if (_boundaryLoading) return; _boundaryLoading = true; _view.SetInputReady(false); _input.SetVisibleStateReady(false); _media.Stop(_controller.CurrentTokenId); BoundaryRequested?.Invoke(); }
        private bool CallerLayerInputReady()
        {
            if (_controller == null) return false;
            if (_controller.State == Stage2State.Help) return _helpView != null && _helpView.IsVisible && _helpView.BackInputEnabled;
            if (_controller.State == Stage2State.Leaving) return _leaveView != null && _leaveView.IsVisible;
            return _view != null && _view.EnabledControlIds.Count > 0;
        }
        private sealed class PauseRelay : IStage2PauseSink
        { internal IStage2PauseSink Target; public bool TryShow(Stage2Snapshot s) => Target != null && Target.TryShow(s); }
        private sealed class HelpRelay : IJourneyOverviewPauseHelpSink
        { internal Stage2StageHost Host; public bool TryShow(PauseOverlaySnapshot s) => Host != null && Host.ShowPauseHelp(s); }
    }
}
