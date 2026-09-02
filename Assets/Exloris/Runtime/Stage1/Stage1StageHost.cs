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

namespace Exloris.Runtime.Stage1
{
    [DisallowMultipleComponent]
    public sealed class Stage1StageHost : MonoBehaviour
    {
        private ChildInputRouter _input;
        private SessionState _session;
        private NavigationAuthority _navigation;
        private AudioDirector _audioDirector;
        private IAudioClipRegistry _audioRegistry;
        private AccessibilityState _accessibility;
        private AccessibilityCoordinator _accessibilityCoordinator;
        private AccessibilityPreferenceStore _preferences;
        private AppLifecycleCoordinator _lifecycle;
        private RecoveryPresenter _recoveryPresenter;
        private Stage1StageView _stagePrefab;
        private PauseOverlayView _pausePrefab;
        private Stage1HelpView _helpPrefab;
        private LeaveConfirmationController _leaveController;
        private LeaveConfirmationView _leaveView;
        private Stage1StageView _view;
        private PauseOverlayView _pauseView;
        private Stage1HelpView _helpView;
        private Stage1Controller _controller;
        private Stage1MediaPresenter _media;
        private Stage1OutcomePresenter _outcome;
        private Stage1PauseAdapter _adapter;
        private PauseOverlayController _pause;
        private Stage1HelpController _directHelp;
        private PauseRelay _pauseRelay;
        private HelpRelay _helpRelay;
        private Stage1MediaHandle _mediaHandle;
        private Stage1Snapshot _directLeaveSnapshot;
        private PauseOverlaySnapshot _pauseHelpSnapshot;
        private Guid _entryTransactionId;
        private Guid _pendingTapInteractionId;
        private bool _prepared;
        private bool _entered;
        private bool _boundaryLoading;
        private bool _pauseHelpVisible;
        private int _inputReadyFrame = -1;
        private string _lastLayoutFailure;

        public event Action<Guid> WelcomeRequested;
        public event Action BoundaryRequested;

        public Stage1StageView View => _view;
        public PauseOverlayView PauseView => _pauseView;
        public Stage1HelpView HelpView => _helpView;
        public bool IsPrepared => _prepared;
        public bool IsEntered => _entered;
        public bool InputReady => _entered && !_boundaryLoading && _inputReadyFrame < 0
            && _input.InputsActive && CallerLayerInputReady();
        public bool BoundaryLoading => _boundaryLoading;
        public Stage1Snapshot Snapshot => _controller == null ? default : _controller.Snapshot;
        public Guid CurrentTokenId => _controller?.CurrentTokenId ?? Guid.Empty;
        public PauseLayerState PauseState => _pause?.State ?? PauseLayerState.Hidden;
        public int AcceptedCommandCount { get; private set; }
        public Stage1PauseReason? LastPauseReason => _view?.LastPauseReason;
        public bool VoicePlaying => _media != null && _media.HasActiveVoice;
        public bool LoopsPlaying => _media != null && _media.HasActiveLoops;
        public bool AttachSfxPlaying => _media != null && _media.AttachSfxPlaying;
        public bool StepSfxPlaying => _media != null && _media.StepSfxPlaying;
        public Stage1State PauseCallerState => _adapter == null ? Stage1State.Hidden : _adapter.CapturedSnapshot.State;
        public Stage1State DirectLeaveCallerState => _directLeaveSnapshot.State;
        public AvatarTechnicalOverlayState RecoveryState => _leaveView == null
            ? AvatarTechnicalOverlayState.Hidden : _leaveView.TechnicalOverlayState;
        public string LastPreparationFailure { get; private set; }

        public void Configure(
            ChildInputRouter input,
            SessionState session,
            NavigationAuthority navigation,
            AudioDirector audioDirector,
            IAudioClipRegistry audioRegistry,
            AccessibilityState accessibility,
            AccessibilityCoordinator accessibilityCoordinator,
            AccessibilityPreferenceStore preferences,
            AppLifecycleCoordinator lifecycle,
            RecoveryPresenter recoveryPresenter,
            Stage1StageView stagePrefab,
            PauseOverlayView pausePrefab,
            Stage1HelpView helpPrefab)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _audioDirector = audioDirector ?? throw new ArgumentNullException(nameof(audioDirector));
            _audioRegistry = audioRegistry ?? throw new ArgumentNullException(nameof(audioRegistry));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = accessibilityCoordinator ?? throw new ArgumentNullException(nameof(accessibilityCoordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _recoveryPresenter = recoveryPresenter ?? throw new ArgumentNullException(nameof(recoveryPresenter));
            _stagePrefab = stagePrefab ?? throw new ArgumentNullException(nameof(stagePrefab));
            _pausePrefab = pausePrefab ?? throw new ArgumentNullException(nameof(pausePrefab));
            _helpPrefab = helpPrefab ?? throw new ArgumentNullException(nameof(helpPrefab));
        }

        public bool Prepare(
            Guid transactionId,
            LeaveConfirmationController leaveController,
            LeaveConfirmationView leaveView,
            bool selectedGirl)
        {
            LastPreparationFailure = null;
            var transaction = _navigation?.ActiveTransaction;
            if (_prepared || transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat1 || transaction.Destination != RouteId.Stage1)
            {
                LastPreparationFailure = "route_precondition";
                return false;
            }
            _leaveController = leaveController ?? throw new ArgumentNullException(nameof(leaveController));
            _leaveView = leaveView ?? throw new ArgumentNullException(nameof(leaveView));
            var content = Stage1Content.LoadApproved();
            var shared = JourneyOverviewContent.LoadApproved();
            _view = Instantiate(_stagePrefab, transform, false);
            _view.Configure(_input, content, selectedGirl);
            _helpView = Instantiate(_helpPrefab, transform, false);
            _helpView.Configure(_input, content, shared);
            if (!TryApplyResponsiveLayout(
                    Screen.width, Screen.height, Screen.safeArea.x, Screen.safeArea.y,
                    Screen.safeArea.width, Screen.safeArea.height, Screen.dpi > 0f ? Screen.dpi : 326f, 88))
            {
                LastPreparationFailure = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "layout/{7} screen={0}x{1} safe={2},{3},{4}x{5} dpi={6}",
                    Screen.width, Screen.height, Screen.safeArea.x, Screen.safeArea.y,
                    Screen.safeArea.width, Screen.safeArea.height, Screen.dpi, _lastLayoutFailure);
                Cleanup();
                return false;
            }
            _view.SetVisible(false);
            _pauseRelay = new PauseRelay();
            _helpRelay = new HelpRelay();
            _controller = new Stage1Controller(_session, _navigation, _view, _pauseRelay, _leaveController, Guid.NewGuid);
            _media = new Stage1MediaPresenter(_audioDirector, _audioRegistry, content, _session, _accessibility, _view);
            _outcome = new Stage1OutcomePresenter(content, _view);
            _directHelp = new Stage1HelpController(_controller, _helpView);
            _entryTransactionId = transactionId;
            _prepared = true;
            return true;
        }

        public bool Enter(double now)
        {
            var transaction = _navigation?.ActiveTransaction;
            if (!_prepared || _entered || transaction == null || transaction.Id != _entryTransactionId
                || transaction.Caller != RouteId.Boat1 || transaction.Destination != RouteId.Stage1)
                return false;
            _pauseView = Instantiate(_pausePrefab, transform, false);
            _pauseView.Configure(_input, JourneyOverviewContent.LoadApproved(), _view);
            _adapter = new Stage1PauseAdapter(_controller, _media, _outcome);
            _pause = new PauseOverlayController(
                _session, _navigation, _adapter, _accessibility, _accessibilityCoordinator,
                _preferences, _pauseView, _helpRelay, _leaveController, Guid.NewGuid);
            _pauseRelay.Target = _pause;
            _helpRelay.Host = this;
            _leaveController.BindNestedReturnSink(_pause);
            _leaveView.BindStage1Caller(_view, _pauseView);
            _view.SetVisible(true);
            if (!_controller.Enter(_entryTransactionId, now, _accessibility.EffectiveReducedMotion))
            {
                _view.SetVisible(false);
                return false;
            }
            _mediaHandle = _media.StartIntro(_controller.CurrentTokenId, now);
            _adapter.BindMediaHandle(_mediaHandle);
            _inputReadyFrame = Time.frameCount + 1;
            _entered = true;
            return true;
        }

        public void Tick(double now, float deltaSeconds)
        {
            if (!_entered) return;
            if (_boundaryLoading)
            {
                if (_lifecycle.Tick(now)) ShowRecovery();
                return;
            }
            if (_inputReadyFrame >= 0 && Time.frameCount >= _inputReadyFrame)
            {
                _inputReadyFrame = -1;
                _view.SetInputReady(true);
                _input.SetVisibleStateReady(true);
            }
            if (_pause.State != PauseLayerState.Hidden
                || _controller.State == Stage1State.Help
                || _controller.State == Stage1State.Leaving)
                return;

            _media.Advance(_controller.CurrentTokenId, _media.CurrentMediaTokenId, now);
            if (_controller.State == Stage1State.OutcomePlaying)
            {
                var routeToken = _controller.CurrentTokenId;
                var outcomeToken = _controller.CurrentOutcomeTokenId;
                _outcome.Advance(routeToken, outcomeToken, now);
                var previous = _controller.State;
                if (_controller.AdvanceOutcome(routeToken, outcomeToken, now)
                    && previous == Stage1State.OutcomePlaying
                    && _controller.State == Stage1State.OutcomeReady
                    && _controller.Snapshot.Outcome != Stage1OutcomeKind.InRange)
                {
                    _mediaHandle = _media.StartOutside(routeToken, now);
                    _adapter.BindMediaHandle(_mediaHandle);
                }
            }
            if (_controller.State == Stage1State.Navigating) BeginBoundary();
        }

        public bool OwnsControl(string controlId) => _view != null && (
            _view.ControlIds.Contains(controlId)
            || controlId == Stage1HelpView.BackControlId
            || controlId == PauseOverlayView.HelpControlId
            || controlId == PauseOverlayView.SoundControlId
            || controlId == PauseOverlayView.CaptionsControlId
            || controlId == PauseOverlayView.ResumeControlId
            || controlId == PauseOverlayView.MotionControlId
            || controlId == PauseOverlayView.HomeControlId
            || controlId == LeaveConfirmationView.StayControlId
            || controlId == LeaveConfirmationView.CancelControlId
            || controlId == LeaveConfirmationView.LeaveControlId
            || controlId == LeaveConfirmationView.RetryControlId
            || controlId == LeaveConfirmationView.HomeControlId);

        public bool HandleSemanticCommand(SemanticInputCommand command) =>
            HandleSemanticCommand(command.ControlId, command.InputTime, command.Kind, command.StartPosition, command.EndPosition);

        public bool HandleSemanticCommand(SemanticInputCommand command, double now) =>
            HandleSemanticCommand(command.ControlId, now, command.Kind, command.StartPosition, command.EndPosition);

        public bool HandleSemanticCommand(string controlId, double now) =>
            HandleSemanticCommand(controlId, now, SemanticInputKind.Tap, default, default);

        private bool HandleSemanticCommand(
            string controlId,
            double now,
            SemanticInputKind kind,
            Vector2 startPosition,
            Vector2 endPosition)
        {
            if (!_entered || string.IsNullOrEmpty(controlId)) return false;
            var token = _controller.CurrentTokenId;
            var accepted = false;
            switch (controlId)
            {
                case Stage1StageView.PlayControlId:
                    accepted = _controller.HandlePlay(token, now);
                    break;
                case Stage1StageView.MarkerControlId:
                    accepted = HandleMarker(token, kind, endPosition, now);
                    break;
                case Stage1StageView.SocketControlId:
                    accepted = HandleSocket(token, kind, now);
                    break;
                case Stage1StageView.UpControlId:
                    accepted = HandleArrow(token, 1, now);
                    break;
                case Stage1StageView.DownControlId:
                    accepted = HandleArrow(token, -1, now);
                    break;
                case Stage1StageView.ConfirmControlId:
                    accepted = HandleConfirm(token, now);
                    break;
                case Stage1StageView.HelpControlId:
                    accepted = _directHelp.TryShow();
                    break;
                case Stage1StageView.RepeatControlId:
                    accepted = HandleRepeat(token, now);
                    break;
                case Stage1StageView.PauseControlId:
                    accepted = PauseStage(now, Stage1PauseReason.Manual);
                    break;
                case Stage1StageView.HomeControlId:
                    _directLeaveSnapshot = _controller.Snapshot;
                    accepted = _controller.HandleHome(now);
                    break;
                case Stage1StageView.TryAgainControlId:
                    accepted = HandleTryAgain(token, now);
                    break;
                case Stage1StageView.WatchAgainControlId:
                    accepted = HandleWatchAgain(token, now);
                    break;
                case Stage1StageView.ContinueControlId:
                    accepted = _controller.HandleContinue(token, now);
                    if (accepted) BeginBoundary();
                    break;
                case Stage1HelpView.BackControlId:
                    accepted = HandleHelpBack();
                    break;
                case PauseOverlayView.HelpControlId:
                    accepted = _pause.HandleHelp();
                    break;
                case PauseOverlayView.SoundControlId:
                    accepted = _pause.HandleSound();
                    break;
                case PauseOverlayView.CaptionsControlId:
                    accepted = _pause.HandleCaptions();
                    break;
                case PauseOverlayView.MotionControlId:
                    accepted = _pause.HandleMotion();
                    break;
                case PauseOverlayView.ResumeControlId:
                    accepted = ResumePause(now);
                    break;
                case PauseOverlayView.HomeControlId:
                    accepted = _adapter.CapturedSnapshot.State == Stage1State.Leaving
                        ? ResumePause(now) : _pause.HandleHome();
                    break;
                case LeaveConfirmationView.StayControlId:
                    accepted = RestoreLeave(false, now);
                    break;
                case LeaveConfirmationView.CancelControlId:
                    accepted = RestoreLeave(true, now);
                    break;
                case LeaveConfirmationView.LeaveControlId:
                    accepted = RequestWelcome(now);
                    break;
                case LeaveConfirmationView.RetryControlId:
                    accepted = RetryRecovery(now);
                    break;
                case LeaveConfirmationView.HomeControlId:
                    accepted = HomeRecovery(now);
                    break;
            }
            if (accepted) AcceptedCommandCount++;
            return accepted;
        }

        public bool HandleInterruption(double now, Stage1PauseReason reason) =>
            _entered && !_boundaryLoading && (_pause.State != PauseLayerState.Hidden || PauseStage(now, reason));

        public bool CompleteOutcomeForDiagnostics()
        {
            if (!_entered || _controller.State != Stage1State.OutcomePlaying) return false;
            var now = _controller.Snapshot.StateStartedAtSeconds + Stage1Controller.OutcomeSeconds;
            _outcome.Advance(_controller.CurrentTokenId, _controller.CurrentOutcomeTokenId, now);
            if (!_controller.AdvanceOutcome(_controller.CurrentTokenId, _controller.CurrentOutcomeTokenId, now)) return false;
            if (_controller.Snapshot.Outcome != Stage1OutcomeKind.InRange)
            {
                _mediaHandle = _media.StartOutside(_controller.CurrentTokenId, now);
                _adapter.BindMediaHandle(_mediaHandle);
            }
            return _controller.State == Stage1State.OutcomeReady;
        }

        public bool AttachForDiagnostics(bool drag, double now)
        {
            if (!_entered || _controller.State != Stage1State.Playing || _controller.Snapshot.MarkerAttached)
                return false;
            var token = _controller.CurrentTokenId;
            var interaction = Guid.NewGuid();
            var attached = drag
                ? _controller.HandleDragAttach(token, interaction, true, true, now)
                : _controller.BeginTapAttach(token, interaction, true, now)
                  && _controller.CompleteTapAttach(token, interaction, true, now);
            if (attached) _media.PlayAttachSfx(token, interaction, now);
            return attached;
        }

        public bool MoveForDiagnostics(double normalizedPosition, double now)
        {
            if (!_entered || _controller.State != Stage1State.Playing) return false;
            var result = _controller.HandleDrag(_controller.CurrentTokenId, normalizedPosition, now);
            if (result == Stage1MoveResult.Moved)
                _media.PlayStepSfx(_controller.CurrentTokenId, Guid.NewGuid(), now);
            return result == Stage1MoveResult.Moved || result == Stage1MoveResult.AtBoundary;
        }

        public bool HandleRecoveryActionForDiagnostics(string controlId, double now) =>
            HandleSemanticCommand(controlId, now);

        public bool HandleBoundaryAssetFailure()
        {
            if (!_entered || !_boundaryLoading || !_lifecycle.HandleAssetFailure()) return false;
            ShowRecovery();
            return true;
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            _lastLayoutFailure = null;
            try
            {
                screenWidth = Mathf.Max(1f, screenWidth);
                screenHeight = Mathf.Max(1f, screenHeight);
                var scale = Exloris.Runtime.Layout.ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
                _lastLayoutFailure = "stage_exception";
                if (!_view.TryApplyResponsiveLayout(
                        screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimumPixels))
                {
                    _lastLayoutFailure = "stage";
                    throw new InvalidOperationException("Stage 1 safe area is infeasible.");
                }
                if (_pauseView != null)
                {
                    _lastLayoutFailure = "pause_exception";
                    _pauseView.ApplyResponsiveLayout(screenWidth, screenHeight, safeWidth / scale, safeHeight / scale, ppi, platformMinimumPixels);
                }
                _lastLayoutFailure = "help_exception";
                if (!_helpView.TryApplyResponsiveLayout(screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels))
                {
                    _lastLayoutFailure = "help";
                    throw new InvalidOperationException("Stage 1 Help safe area is infeasible.");
                }
                _lastLayoutFailure = null;
                return true;
            }
            catch (InvalidOperationException)
            {
                if (string.IsNullOrEmpty(_lastLayoutFailure)) _lastLayoutFailure = "overlay";
                if (_entered && _boundaryLoading) HandleBoundaryAssetFailure();
                return false;
            }
        }

        public void ShowRecovery()
        {
            if (!_entered || !_boundaryLoading) return;
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing)
                _recoveryPresenter.MarkRecoveryVisible();
            if (_recoveryPresenter.State != RecoveryPresentationState.Visible) return;
            _controller.EnterRecovery();
            _leaveView.ShowTechnicalOverlay(RouteId.Stage1, AvatarTechnicalOverlayState.Recovery);
        }

        public void CompleteWelcomeTransition(Guid transactionId)
        {
            _leaveController?.CompleteTransition(transactionId);
            _leaveView?.CompleteTransitionAndHide();
        }

        public void Cleanup()
        {
            if (_view != null) foreach (var id in _view.ControlIds) _input.Unregister(id);
            if (_pauseView != null) foreach (var id in _pauseView.ControlIds) _input.Unregister(id);
            if (_helpView != null) foreach (var id in _helpView.ControlIds) _input.Unregister(id);
            if (_media != null && _controller != null && _controller.CurrentTokenId != Guid.Empty)
                _media.Stop(_controller.CurrentTokenId);
            if (_helpView != null) Destroy(_helpView.gameObject);
            if (_pauseView != null) Destroy(_pauseView.gameObject);
            if (_view != null) Destroy(_view.gameObject);
            _view = null;
            _pauseView = null;
            _helpView = null;
            _controller = null;
            _media = null;
            _outcome = null;
            _adapter = null;
            _pause = null;
            _directHelp = null;
            _pauseRelay = null;
            _helpRelay = null;
            _prepared = false;
            _entered = false;
            _boundaryLoading = false;
            _inputReadyFrame = -1;
            _entryTransactionId = Guid.Empty;
            _pendingTapInteractionId = Guid.Empty;
        }

        private bool HandleMarker(Guid token, SemanticInputKind kind, Vector2 endPosition, double now)
        {
            if (kind == SemanticInputKind.Drag)
            {
                var interaction = Guid.NewGuid();
                if (!_controller.Snapshot.MarkerAttached)
                {
                    var socketHit = _view.GetControlScreenBounds(Stage1StageView.SocketControlId).Contains(endPosition);
                    var attached = _controller.HandleDragAttach(token, interaction, true, socketHit, now);
                    if (attached) _media.PlayAttachSfx(token, interaction, now);
                    return attached;
                }
                if (!_view.TryMapScreenPointToMarkerPosition(endPosition, out var normalized)) return false;
                var moved = _controller.HandleDrag(token, normalized, now);
                if (moved == Stage1MoveResult.Moved) _media.PlayStepSfx(token, interaction, now);
                return moved == Stage1MoveResult.Moved || moved == Stage1MoveResult.AtBoundary;
            }
            if (_controller.Snapshot.MarkerAttached) return false;
            _pendingTapInteractionId = Guid.NewGuid();
            if (_controller.BeginTapAttach(token, _pendingTapInteractionId, true, now)) return true;
            _pendingTapInteractionId = Guid.Empty;
            return false;
        }

        private bool HandleSocket(Guid token, SemanticInputKind kind, double now)
        {
            if (kind != SemanticInputKind.Tap || _pendingTapInteractionId == Guid.Empty) return false;
            var interaction = _pendingTapInteractionId;
            _pendingTapInteractionId = Guid.Empty;
            var attached = _controller.CompleteTapAttach(token, interaction, true, now);
            if (attached) _media.PlayAttachSfx(token, interaction, now);
            return attached;
        }

        private bool HandleArrow(Guid token, int direction, double now)
        {
            var actionId = Guid.NewGuid();
            var result = _controller.HandleArrow(token, direction, now);
            if (result == Stage1MoveResult.AttachCue)
            {
                _mediaHandle = _media.StartAttachCue(token, now);
                _adapter.BindMediaHandle(_mediaHandle);
                return true;
            }
            if (result == Stage1MoveResult.Moved) _media.PlayStepSfx(token, actionId, now);
            return result == Stage1MoveResult.Moved || result == Stage1MoveResult.AtBoundary;
        }

        private bool HandleConfirm(Guid token, double now)
        {
            if (!_controller.HandleConfirm(token, now)) return false;
            if (!_media.SilenceNarration(token))
                throw new InvalidOperationException("Stage 1 narration could not stop at outcome lock.");
            return _outcome.Start(_controller.Snapshot, _accessibility.EffectiveReducedMotion, now);
        }

        private bool HandleRepeat(Guid token, double now)
        {
            var before = _controller.Snapshot;
            if (!_controller.HandleRepeat(token, now)) return false;
            _mediaHandle = before.State == Stage1State.OutcomeReady && before.Outcome != Stage1OutcomeKind.InRange
                ? _media.StartOutside(token, now)
                : _media.StartIntro(token, now);
            _adapter.BindMediaHandle(_mediaHandle);
            return true;
        }

        private bool HandleTryAgain(Guid token, double now)
        {
            var oldOutcome = _controller.CurrentOutcomeTokenId;
            if (!_controller.HandleTryAgain(token, now)) return false;
            _outcome.Stop(token, oldOutcome);
            _mediaHandle = _media.StartIntro(token, now);
            _adapter.BindMediaHandle(_mediaHandle);
            return true;
        }

        private bool HandleWatchAgain(Guid token, double now)
        {
            var oldOutcome = _controller.CurrentOutcomeTokenId;
            if (!_controller.HandleWatchAgain(token, now)) return false;
            _outcome.Stop(token, oldOutcome);
            _media.SilenceNarration(token);
            return _outcome.Start(_controller.Snapshot, _accessibility.EffectiveReducedMotion, now);
        }

        private bool PauseStage(double now, Stage1PauseReason reason)
        {
            if (!_adapter.PreparePause(now, reason)) return false;
            if (!_controller.HandlePause(now))
            {
                _adapter.CancelPreparedPause();
                return false;
            }
            var caller = _adapter.CapturedSnapshot.State;
            var stacked = caller == Stage1State.Help || caller == Stage1State.Leaving;
            _pauseView.SetBoatInterruptionPriority(stacked);
            if (caller == Stage1State.Help) _helpView.SetInterruptionBlocked(true);
            else if (caller == Stage1State.Leaving) _leaveView.SetInterruptionBlocked(true);
            return true;
        }

        private bool ResumePause(double now)
        {
            var caller = _adapter.CapturedSnapshot.State;
            if (!_pause.HandleResume(now)) return false;
            _pauseView.SetBoatInterruptionPriority(false);
            if (caller == Stage1State.Help) _helpView.SetInterruptionBlocked(false);
            else if (caller == Stage1State.Leaving) _leaveView.SetInterruptionBlocked(false);
            if (caller == Stage1State.Help || caller == Stage1State.Leaving) _view.SetInputReady(false);
            RefreshMediaHandle();
            return true;
        }

        private bool ShowPauseHelp(PauseOverlaySnapshot snapshot)
        {
            _pauseHelpSnapshot = snapshot;
            _pauseHelpVisible = true;
            _pauseView.SetControlsEnabled(false);
            _helpView.SetInterruptionBlocked(false);
            _helpView.SetPauseOverlayPriority(true);
            _helpView.Show(_adapter.CapturedSnapshot, ResolveCapturedHelpMode());
            return true;
        }

        private bool HandleHelpBack()
        {
            if (!_pauseHelpVisible) return _directHelp.HandleBack();
            if (!_pause.RestoreFromHelp(_pauseHelpSnapshot)) return false;
            _pauseHelpVisible = false;
            _helpView.SetPauseOverlayPriority(false);
            if (_adapter.CapturedSnapshot.State == Stage1State.Help)
            {
                _helpView.Show(_adapter.CapturedSnapshot, ResolveCapturedHelpMode());
                _helpView.SetInterruptionBlocked(_pause.State == PauseLayerState.Pause);
            }
            else _helpView.Hide();
            return true;
        }

        private bool RestoreLeave(bool cancel, double now)
        {
            var nested = _pause.State == PauseLayerState.Leave;
            var restored = cancel ? _leaveController.HandleCancel() : _leaveController.HandleStay();
            return restored && (nested || _controller.RestoreFromLeave(_directLeaveSnapshot, now));
        }

        private bool RequestWelcome(double now)
        {
            if (!_leaveController.HandleLeave(now)) return false;
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.Welcome) return false;
            _input.SetVisibleStateReady(false);
            WelcomeRequested?.Invoke(transaction.Id);
            return true;
        }

        private bool RetryRecovery(double now)
        {
            var result = _lifecycle.Retry(now);
            if (!result.Accepted || result.Transaction.Caller != RouteId.Stage1 || result.Transaction.Destination != RouteId.Boat2)
                return false;
            if (!_controller.RetryBoundary()) return false;
            _leaveView.ShowTechnicalOverlay(RouteId.Stage1, AvatarTechnicalOverlayState.Loading);
            return true;
        }

        private bool HomeRecovery(double now)
        {
            var result = _lifecycle.Home(now);
            if (!result.Accepted || result.Transaction.Caller != RouteId.Stage1 || result.Transaction.Destination != RouteId.Welcome)
                return false;
            _leaveView.ShowTechnicalOverlay(RouteId.Stage1, AvatarTechnicalOverlayState.Loading);
            WelcomeRequested?.Invoke(result.Transaction.Id);
            return true;
        }

        private void BeginBoundary()
        {
            if (_boundaryLoading) return;
            _boundaryLoading = true;
            _view.SetInputReady(false);
            _input.SetVisibleStateReady(false);
            _media.Stop(_controller.CurrentTokenId);
            BoundaryRequested?.Invoke();
        }

        private void RefreshMediaHandle()
        {
            _mediaHandle = new Stage1MediaHandle(
                _controller.CurrentTokenId, _media.CurrentMediaTokenId, null, null, null);
            _adapter.BindMediaHandle(_mediaHandle);
        }

        private bool CallerLayerInputReady()
        {
            if (_controller == null) return false;
            if (_controller.State == Stage1State.Help) return _helpView != null && _helpView.IsVisible && _helpView.BackInputEnabled;
            if (_controller.State == Stage1State.Leaving) return _leaveView != null && _leaveView.IsVisible;
            return _view != null && _view.EnabledControlIds.Count > 0;
        }

        private Stage1HelpMode ResolveCapturedHelpMode()
        {
            var snapshot = _adapter.CapturedSnapshot;
            if (snapshot.State == Stage1State.Help && _directHelp.IsVisible) return _directHelp.LastMode;
            if (snapshot.Outcome == Stage1OutcomeKind.OutsideLow) return Stage1HelpMode.AdjustUp;
            if (snapshot.Outcome == Stage1OutcomeKind.OutsideHigh) return Stage1HelpMode.AdjustDown;
            return snapshot.MarkerAttached ? Stage1HelpMode.Move : Stage1HelpMode.AttachThenMove;
        }

        private sealed class PauseRelay : IStage1PauseSink
        {
            internal IStage1PauseSink Target;
            public bool TryShow(Stage1Snapshot snapshot) => Target != null && Target.TryShow(snapshot);
        }

        private sealed class HelpRelay : IJourneyOverviewPauseHelpSink
        {
            internal Stage1StageHost Host;
            public bool TryShow(PauseOverlaySnapshot snapshot) => Host != null && Host.ShowPauseHelp(snapshot);
        }
    }
}
