using System;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Input;
using Exloris.Runtime.Lifecycle;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using UnityEngine;

namespace Exloris.Runtime.Overview
{
    [DisallowMultipleComponent]
    public sealed class JourneyOverviewStageHost : MonoBehaviour
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
        private JourneyOverviewStageView _overviewPrefab;
        private PauseOverlayView _pausePrefab;
        private JourneyOverviewPauseHelpView _helpPrefab;
        private LeaveConfirmationController _leaveController;
        private LeaveConfirmationView _leaveView;

        private JourneyOverviewStageView _view;
        private PauseOverlayView _pauseView;
        private JourneyOverviewPauseHelpView _helpView;
        private JourneyOverviewRevealPresenter _reveal;
        private JourneyOverviewMediaPresenter _media;
        private JourneyOverviewController _controller;
        private PauseOverlayController _pauseController;
        private JourneyOverviewPauseHelpController _helpController;
        private JourneyOverviewMediaHandle _mediaHandle;
        private JourneyOverviewSnapshot _directLeaveSnapshot;
        private int _inputReadyFrame = -1;
        private bool _prepared;
        private bool _entered;
        private bool _boundaryLoading;

        public event Action<Guid> WelcomeRequested;
        public event Action BoundaryRequested;

        public JourneyOverviewStageView View => _view;
        public PauseOverlayView PauseView => _pauseView;
        public JourneyOverviewPauseHelpView HelpView => _helpView;
        public bool IsPrepared => _prepared;
        public bool IsEntered => _entered;
        public bool InputReady => _entered && !_boundaryLoading && _inputReadyFrame < 0
            && _input != null && _input.InputsActive && _view != null && _view.ControlsEnabled;
        public bool BoundaryLoading => _boundaryLoading;
        public JourneyOverviewSnapshot Snapshot => _controller == null
            ? default : _controller.Snapshot;
        public Guid CurrentTokenId => _controller?.CurrentTokenId ?? Guid.Empty;
        public PauseLayerState PauseState => _pauseController?.State ?? PauseLayerState.Hidden;
        public int AcceptedCommandCount { get; private set; }
        public bool MediaPaused => _media != null && _media.IsPaused;
        public AvatarTechnicalOverlayState RecoveryState => _leaveView == null
            ? AvatarTechnicalOverlayState.Hidden : _leaveView.TechnicalOverlayState;

        public bool OwnsControl(string controlId)
        {
            return controlId == JourneyOverviewStageView.SkipControlId
                || controlId == JourneyOverviewStageView.RepeatControlId
                || controlId == JourneyOverviewStageView.PauseControlId
                || controlId == JourneyOverviewStageView.HomeControlId
                || controlId == PauseOverlayView.HelpControlId
                || controlId == PauseOverlayView.SoundControlId
                || controlId == PauseOverlayView.CaptionsControlId
                || controlId == PauseOverlayView.ResumeControlId
                || controlId == PauseOverlayView.MotionControlId
                || controlId == PauseOverlayView.HomeControlId
                || controlId == JourneyOverviewPauseHelpView.BackControlId
                || controlId == LeaveConfirmationView.StayControlId
                || controlId == LeaveConfirmationView.CancelControlId
                || controlId == LeaveConfirmationView.LeaveControlId
                || controlId == LeaveConfirmationView.RetryControlId
                || controlId == LeaveConfirmationView.HomeControlId;
        }

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
            JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab,
            JourneyOverviewPauseHelpView helpPrefab)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _audioDirector = audioDirector ?? throw new ArgumentNullException(nameof(audioDirector));
            _audioRegistry = audioRegistry ?? throw new ArgumentNullException(nameof(audioRegistry));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = accessibilityCoordinator
                ?? throw new ArgumentNullException(nameof(accessibilityCoordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _recoveryPresenter = recoveryPresenter
                ?? throw new ArgumentNullException(nameof(recoveryPresenter));
            _overviewPrefab = overviewPrefab ?? throw new ArgumentNullException(nameof(overviewPrefab));
            _pausePrefab = pausePrefab ?? throw new ArgumentNullException(nameof(pausePrefab));
            _helpPrefab = helpPrefab ?? throw new ArgumentNullException(nameof(helpPrefab));
        }

        public bool Prepare(
            Guid transactionId,
            LeaveConfirmationController leaveController,
            LeaveConfirmationView leaveView)
        {
            var transaction = _navigation?.ActiveTransaction;
            if (_prepared || transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.AvatarPreview
                || transaction.Destination != RouteId.JourneyOverview)
                return false;

            _leaveController = leaveController ?? throw new ArgumentNullException(nameof(leaveController));
            _leaveView = leaveView ?? throw new ArgumentNullException(nameof(leaveView));
            var content = JourneyOverviewContent.LoadApproved();
            _view = Instantiate(_overviewPrefab, transform, false);
            _view.Configure(_input, content);
            _pauseView = Instantiate(_pausePrefab, transform, false);
            _pauseView.Configure(_input, content, _view);
            _helpView = Instantiate(_helpPrefab, transform, false);
            _helpView.Configure(_input, content, _pauseView);
            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            var scale = Exloris.Runtime.Layout.ResponsiveLayoutProfile.CalculateCanvasScale(
                screenWidth, screenHeight);
            if (!TryApplyResponsiveLayout(
                    screenWidth,
                    screenHeight,
                    Mathf.Max(1f, Screen.safeArea.width / scale),
                    Mathf.Max(1f, Screen.safeArea.height / scale),
                    Screen.dpi > 0f ? Screen.dpi : 326f,
                    88))
            {
                Cleanup();
                return false;
            }

            var pauseRelay = new PauseRelay();
            var helpRelay = new HelpRelay();
            _reveal = new JourneyOverviewRevealPresenter(content, _view);
            _controller = new JourneyOverviewController(
                _session, _navigation, _reveal, pauseRelay, _leaveController, Guid.NewGuid);
            _media = new JourneyOverviewMediaPresenter(
                _audioDirector, _audioRegistry, content, _session, _accessibility, _view);
            _pauseController = new PauseOverlayController(
                _session, _navigation, _controller, _media, _accessibility,
                _accessibilityCoordinator, _preferences, _pauseView, helpRelay,
                _leaveController, Guid.NewGuid);
            _helpController = new JourneyOverviewPauseHelpController(_pauseController, _helpView);
            pauseRelay.Target = _pauseController;
            helpRelay.Target = _helpController;
            _leaveController.BindNestedReturnSink(_pauseController);
            _leaveView.BindJourneyCaller(_view, _pauseView);
            _view.PauseOverlayRequested += HandlePauseOverlayRequested;
            _view.SetVisible(true);
            _view.SetInputReady(false);
            _prepared = true;
            return true;
        }

        public bool Enter(double nowSeconds)
        {
            if (!_prepared || _entered || _navigation.CurrentRoute != RouteId.JourneyOverview
                || _session.CurrentOpenSessionLocation != RouteId.JourneyOverview
                || !_controller.Enter(nowSeconds, _accessibility.EffectiveReducedMotion))
                return false;
            _mediaHandle = _media.Start(_controller.CurrentTokenId, nowSeconds);
            _view.SetInputReady(false);
            _inputReadyFrame = Time.frameCount + 1;
            _entered = true;
            return true;
        }

        public void Tick(double nowSeconds, float unscaledDeltaSeconds)
        {
            if (!_entered) return;
            if (_boundaryLoading)
            {
                if (_lifecycle.Tick(nowSeconds)) ShowRecovery();
                return;
            }
            if (_inputReadyFrame >= 0 && Time.frameCount >= _inputReadyFrame)
            {
                _inputReadyFrame = -1;
                _view.SetInputReady(true);
                _input.SetVisibleStateReady(true);
            }
            if (_pauseController.State != PauseLayerState.Hidden) return;

            var token = _controller.CurrentTokenId;
            _media.Advance(token, _mediaHandle.MediaTokenId, nowSeconds);
            _controller.Advance(token, nowSeconds);
            _view.AdvanceMotion(unscaledDeltaSeconds);
            if (_controller.State == JourneyOverviewRevealState.Navigating)
                BeginBoundary();
        }

        public bool HandleSemanticCommand(string controlId, double nowSeconds)
        {
            if (!_entered || string.IsNullOrEmpty(controlId)) return false;
            var accepted = false;
            switch (controlId)
            {
                case JourneyOverviewStageView.SkipControlId:
                    accepted = _controller.HandleSkip(_controller.CurrentTokenId, nowSeconds);
                    if (accepted) BeginBoundary();
                    break;
                case JourneyOverviewStageView.RepeatControlId:
                    accepted = _controller.HandleRepeat(nowSeconds);
                    if (accepted)
                        _mediaHandle = _media.Start(_controller.CurrentTokenId, nowSeconds);
                    break;
                case JourneyOverviewStageView.PauseControlId:
                    accepted = PauseMedia(nowSeconds, JourneyOverviewPauseReason.Manual);
                    break;
                case JourneyOverviewStageView.HomeControlId:
                    _directLeaveSnapshot = _controller.Snapshot;
                    accepted = _controller.HandleHome();
                    break;
                case PauseOverlayView.HelpControlId:
                    accepted = _pauseController.HandleHelp();
                    break;
                case PauseOverlayView.SoundControlId:
                    accepted = _pauseController.HandleSound();
                    break;
                case PauseOverlayView.CaptionsControlId:
                    accepted = _pauseController.HandleCaptions();
                    break;
                case PauseOverlayView.ResumeControlId:
                    accepted = _pauseController.HandleResume(nowSeconds);
                    if (accepted) RefreshMediaHandle();
                    break;
                case PauseOverlayView.MotionControlId:
                    accepted = _pauseController.HandleMotion();
                    break;
                case PauseOverlayView.HomeControlId:
                    accepted = _pauseController.HandleHome();
                    break;
                case JourneyOverviewPauseHelpView.BackControlId:
                    accepted = _helpController.HandleBack();
                    break;
                case LeaveConfirmationView.StayControlId:
                    accepted = RestoreFromLeave(cancel: false);
                    break;
                case LeaveConfirmationView.CancelControlId:
                    accepted = RestoreFromLeave(cancel: true);
                    break;
                case LeaveConfirmationView.LeaveControlId:
                    accepted = RequestWelcome(nowSeconds);
                    break;
                case LeaveConfirmationView.RetryControlId:
                    accepted = RetryRecovery(nowSeconds);
                    break;
                case LeaveConfirmationView.HomeControlId:
                    accepted = HomeFromRecovery(nowSeconds);
                    break;
            }
            if (accepted) AcceptedCommandCount++;
            return accepted;
        }

        public bool HandleInterruption(double nowSeconds, JourneyOverviewPauseReason reason)
        {
            if (!_entered || _boundaryLoading) return false;
            if (_pauseController.State != PauseLayerState.Hidden || _media.IsPaused) return true;
            return PauseMedia(nowSeconds, reason);
        }

        public bool HandleBoundaryAssetFailure()
        {
            if (!_entered || !_boundaryLoading || !_lifecycle.HandleAssetFailure()) return false;
            ShowRecovery();
            return true;
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            try
            {
                _view.ApplyResponsiveLayout(
                    screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
                _pauseView.ApplyResponsiveLayout(
                    screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
                _helpView.ApplyResponsiveLayout(
                    screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
                return true;
            }
            catch (InvalidOperationException)
            {
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
            _leaveView.ShowTechnicalOverlay(
                RouteId.JourneyOverview, AvatarTechnicalOverlayState.Recovery);
        }

        public void ApplyReducedMotion(bool reducedMotion)
        {
            if (_controller == null) return;
            _controller.ApplyReducedMotion(reducedMotion);
        }

        public void CompleteWelcomeTransition(Guid transactionId)
        {
            _leaveController?.CompleteTransition(transactionId);
            _leaveView?.CompleteTransitionAndHide();
        }

        public void Cleanup()
        {
            if (_view == null && _pauseView == null && _helpView == null) return;
            if (_view != null)
            {
                _view.PauseOverlayRequested -= HandlePauseOverlayRequested;
                foreach (var id in _view.ControlIds) _input.Unregister(id);
            }
            if (_pauseView != null)
                foreach (var id in _pauseView.ControlIds) _input.Unregister(id);
            if (_helpView != null)
                foreach (var id in _helpView.ControlIds) _input.Unregister(id);
            if (_media != null && _controller != null && _controller.CurrentTokenId != Guid.Empty)
                _media.Stop(_controller.CurrentTokenId);
            if (_helpView != null) Destroy(_helpView.gameObject);
            if (_pauseView != null) Destroy(_pauseView.gameObject);
            if (_view != null) Destroy(_view.gameObject);
            _view = null;
            _pauseView = null;
            _helpView = null;
            _controller = null;
            _pauseController = null;
            _helpController = null;
            _media = null;
            _reveal = null;
            _prepared = false;
            _entered = false;
            _boundaryLoading = false;
            _inputReadyFrame = -1;
        }

        private bool RetryRecovery(double nowSeconds)
        {
            if (RecoveryState != AvatarTechnicalOverlayState.Recovery) return false;
            var result = _lifecycle.Retry(nowSeconds);
            if (!result.Accepted || result.Transaction.Caller != RouteId.JourneyOverview
                || result.Transaction.Destination != RouteId.Boat1)
                return false;
            if (!_controller.RetryBoundary()) return false;
            _leaveView.ShowTechnicalOverlay(
                RouteId.JourneyOverview, AvatarTechnicalOverlayState.Loading);
            return true;
        }

        private bool HomeFromRecovery(double nowSeconds)
        {
            if (RecoveryState != AvatarTechnicalOverlayState.Recovery) return false;
            var result = _lifecycle.Home(nowSeconds);
            if (!result.Accepted || result.Transaction.Caller != RouteId.JourneyOverview
                || result.Transaction.Destination != RouteId.Welcome)
                return false;
            _leaveView.ShowTechnicalOverlay(
                RouteId.JourneyOverview, AvatarTechnicalOverlayState.Loading);
            WelcomeRequested?.Invoke(result.Transaction.Id);
            return true;
        }

        private bool PauseMedia(double nowSeconds, JourneyOverviewPauseReason reason)
        {
            if (_media == null || _controller == null || _media.IsPaused) return false;
            return _media.Pause(
                _controller.CurrentTokenId, _mediaHandle.MediaTokenId, nowSeconds, reason);
        }

        private void HandlePauseOverlayRequested(JourneyOverviewPauseReason reason)
        {
            _controller.HandlePause();
        }

        private bool RestoreFromLeave(bool cancel)
        {
            var nested = _pauseController.State == PauseLayerState.Leave;
            var restored = cancel ? _leaveController.HandleCancel() : _leaveController.HandleStay();
            if (!restored) return false;
            return nested || _controller.RestoreFromLeave(_directLeaveSnapshot);
        }

        private bool RequestWelcome(double nowSeconds)
        {
            if (!_leaveController.HandleLeave(nowSeconds)) return false;
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.Welcome) return false;
            _input.SetVisibleStateReady(false);
            WelcomeRequested?.Invoke(transaction.Id);
            return true;
        }

        private void RefreshMediaHandle()
        {
            _mediaHandle = new JourneyOverviewMediaHandle(
                _media.CurrentRevealTokenId,
                _media.CurrentMediaTokenId,
                _media.CurrentVoiceTokenId,
                null,
                null);
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

        private sealed class PauseRelay : IPauseOverlaySink
        {
            public IPauseOverlaySink Target { private get; set; }
            public bool TryShow(JourneyOverviewSnapshot snapshot) =>
                Target != null && Target.TryShow(snapshot);
        }

        private sealed class HelpRelay : IJourneyOverviewPauseHelpSink
        {
            public IJourneyOverviewPauseHelpSink Target { private get; set; }
            public bool TryShow(PauseOverlaySnapshot snapshot) =>
                Target != null && Target.TryShow(snapshot);
        }
    }
}
