using System;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using UnityEngine;

namespace Exloris.Runtime.Completion
{
    [DisallowMultipleComponent]
    public sealed class CompletionAdultReportStageHost : MonoBehaviour,
        IAdultGateRequestSink,
        IAdultGateReturnSink,
        IConfirmedSessionResetSink
    {
        private ChildInputRouter _input;
        private SessionState _session;
        private NavigationAuthority _navigation;
        private AudioDirector _audioDirector;
        private IAudioClipRegistry _audioRegistry;
        private AccessibilityState _accessibility;
        private AccessibilityCoordinator _accessibilityCoordinator;
        private AccessibilityPreferenceStore _preferences;
        private CompletionStageView _completionPrefab;
        private AdultGateView _adultPrefab;
        private ParentReportView _reportPrefab;
        private PauseOverlayView _pausePrefab;
        private LeaveConfirmationView _leavePrefab;

        private CompletionStageView _completionView;
        private AdultGateView _adultView;
        private ParentReportView _reportView;
        private PauseOverlayView _pauseView;
        private LeaveConfirmationView _leaveView;
        private CompletionController _completion;
        private CompletionMotionPresenter _motion;
        private CompletionMediaPresenter _media;
        private CompletionMediaHandle _mediaHandle;
        private CompletionPauseAdapter _pauseAdapter;
        private PauseOverlayController _pause;
        private LeaveConfirmationController _leave;
        private AdultGateController _adult;
        private ParentReportController _report;
        private AdultGateRequest _activeAdultRequest;
        private AdultGateRequest _reportSourceRequest;
        private CompletionSnapshot _leaveSnapshot;

        public event Action WelcomeRequested;
        public event Action<AdultGateRequest> WelcomeReturnRequested;
        public event Action ConfirmedResetRequested;

        public CompletionStageView CompletionView => _completionView;
        public AdultGateView AdultView => _adultView;
        public ParentReportView ParentReportView => _reportView;
        public PauseOverlayView PauseView => _pauseView;
        public LeaveConfirmationView LeaveView => _leaveView;
        public CompletionSnapshot CompletionSnapshot => _completion == null ? default : _completion.Snapshot;
        public AdultGateSnapshot AdultSnapshot => _adult == null ? default : _adult.Snapshot;
        public ParentReportSnapshot ParentReportSnapshot => _report == null ? default : _report.Snapshot;
        public PauseLayerState PauseState => _pause?.State ?? PauseLayerState.Hidden;
        public bool IsActive => _completionView != null || _adultView != null || _reportView != null;

        public void Configure(
            ChildInputRouter input,
            SessionState session,
            NavigationAuthority navigation,
            AudioDirector audioDirector,
            IAudioClipRegistry audioRegistry,
            AccessibilityState accessibility,
            AccessibilityCoordinator accessibilityCoordinator,
            AccessibilityPreferenceStore preferences,
            CompletionStageView completionPrefab,
            AdultGateView adultPrefab,
            ParentReportView reportPrefab,
            PauseOverlayView pausePrefab,
            LeaveConfirmationView leavePrefab)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _audioDirector = audioDirector ?? throw new ArgumentNullException(nameof(audioDirector));
            _audioRegistry = audioRegistry ?? throw new ArgumentNullException(nameof(audioRegistry));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator = accessibilityCoordinator ?? throw new ArgumentNullException(nameof(accessibilityCoordinator));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _completionPrefab = completionPrefab ?? throw new ArgumentNullException(nameof(completionPrefab));
            _adultPrefab = adultPrefab ?? throw new ArgumentNullException(nameof(adultPrefab));
            _reportPrefab = reportPrefab ?? throw new ArgumentNullException(nameof(reportPrefab));
            _pausePrefab = pausePrefab ?? throw new ArgumentNullException(nameof(pausePrefab));
            _leavePrefab = leavePrefab ?? throw new ArgumentNullException(nameof(leavePrefab));
        }

        public bool EnterCompletion(Guid transactionId, double nowSeconds)
        {
            if (IsActive || _navigation.ActiveTransaction == null) return false;
            var content = CompletionAdultReportContent.LoadApproved();
            _completionView = Instantiate(_completionPrefab, transform, false);
            _completionView.Configure(_input, content, _session.SelectedAvatarId);
            if (!ApplyCurrentLayout(_completionView))
            { CleanupView(_completionView); _completionView = null; return false; }
            _completion = new CompletionController(
                _session, _navigation, _completionView, this,
                new LazyCompletionPauseSink(this), new LazyCompletionLeaveSink(this), Guid.NewGuid);
            _motion = new CompletionMotionPresenter(_completionView);
            _media = new CompletionMediaPresenter(
                _audioDirector, _audioRegistry, content, _session, _accessibility,
                _completionView, Guid.NewGuid);
            _pauseAdapter = new CompletionPauseAdapter(_completion, _media, Guid.NewGuid);
            if (!_completion.Enter(transactionId, nowSeconds, _accessibility.EffectiveReducedMotion))
            { CleanupAll(); return false; }
            _motion.TryPresent(_completion.Snapshot, _accessibility.EffectiveReducedMotion);
            _mediaHandle = _media.Start(_completion.CurrentTokenId, nowSeconds);
            _pauseAdapter.BindMediaHandle(_mediaHandle);
            _input.SetVisibleStateReady(true);
            return true;
        }

        public void Tick(double nowSeconds)
        {
            if (_adult != null && _navigation.CurrentRoute == RouteId.AdultGate)
            {
                TickAdultHold();
                return;
            }
            if (_completion == null || _navigation.CurrentRoute != RouteId.Completion
                || (_pause != null && _pause.State != PauseLayerState.Hidden)
                || _completion.State == CompletionState.Leaving
                || _completion.State == CompletionState.NavigatingAdult) return;
            _completion.Advance(
                _completion.CurrentTokenId, _completion.CurrentPresentationTokenId, nowSeconds);
            _motion.TryPresent(_completion.Snapshot, _accessibility.EffectiveReducedMotion);
            _media.Advance(_completion.CurrentTokenId, _mediaHandle.MediaTokenId, nowSeconds);
        }

        private void TickAdultHold()
        {
            if (_adult.State != AdultGateState.Holding
                || !_input.TryGetActiveGesture(
                    AdultGateView.HoldControlId,
                    out var startedAt,
                    out var inputTime,
                    out var pointerInside))
                return;
            var token = _adult.CurrentTokenId;
            if (!pointerInside)
            {
                if (_adult.Snapshot.ActivePointerId == 0)
                    _adult.HandlePointerExit(token, 0);
                _input.CancelActiveGesture(AdultGateView.HoldControlId);
                return;
            }
            if (_adult.Snapshot.ActivePointerId < 0
                && !_adult.HandleHoldBegin(token, 0, startedAt))
                return;
            _adult.AdvanceHold(token, 0, Math.Max(startedAt, inputTime));
            if (_adult.State == AdultGateState.AwaitingConfirm)
                _input.CancelActiveGesture(AdultGateView.HoldControlId);
        }

        public bool OwnsControl(string controlId)
        {
            if (string.IsNullOrEmpty(controlId)) return false;
            return _completionView != null && _completionView.ControlIds.Contains(controlId)
                || _adultView != null && _adultView.ControlIds.Contains(controlId)
                || _reportView != null && _reportView.ControlIds.Contains(controlId)
                || _pauseView != null && _pauseView.ControlIds.Contains(controlId)
                || _leaveView != null && (_leaveView.ControlIds.Contains(controlId)
                    || _leaveView.TechnicalControlIds.Contains(controlId));
        }

        public bool HandleSemanticCommand(SemanticInputCommand command, double nowSeconds)
        {
            if (string.IsNullOrEmpty(command.ControlId)) return false;
            if (_adultView != null && _adultView.ControlIds.Contains(command.ControlId))
                return HandleAdultCommand(command, nowSeconds);
            if (_reportView != null && _reportView.ControlIds.Contains(command.ControlId)
                && _navigation.CurrentRoute == RouteId.ParentReport)
                return HandleReportCommand(command, nowSeconds);
            if (_pause != null && _pause.State != PauseLayerState.Hidden)
                return HandlePauseCommand(command.ControlId, nowSeconds);
            if (_leave != null && _leave.IsVisible)
                return HandleLeaveCommand(command.ControlId, nowSeconds);
            if (_completion != null && _navigation.CurrentRoute == RouteId.Completion)
                return HandleCompletionCommand(command.ControlId, nowSeconds);
            return false;
        }

        public bool HandleInterruption(double nowSeconds)
        {
            if (_navigation == null) return false;
            if (_navigation.CurrentRoute == RouteId.AdultGate && _adult != null)
            {
                var interrupted = _adult.HandleInterruption();
                _input.CancelActiveGesture(AdultGateView.HoldControlId);
                return interrupted;
            }
            if (_navigation.CurrentRoute != RouteId.Completion || _completion == null
                || (_completion.State != CompletionState.Presenting
                    && _completion.State != CompletionState.Ready)
                || !_pauseAdapter.PreparePause(nowSeconds))
                return false;
            return _completion.HandlePause(_completion.CurrentTokenId, nowSeconds);
        }

        public bool PresentAdultHoldProgressForDiagnostics(double elapsedSeconds)
        {
            if (_adult == null || _navigation.CurrentRoute != RouteId.AdultGate
                || elapsedSeconds < 0d || elapsedSeconds > AdultGateController.HoldSeconds)
                return false;
            var now = Time.unscaledTimeAsDouble;
            var token = _adult.CurrentTokenId;
            return _adult.HandleHoldBegin(token, 0, Math.Max(0d, now - elapsedSeconds))
                && _adult.AdvanceHold(token, 0, now);
        }

        public bool AdvanceCompletionForDiagnostics(double elapsedSeconds)
        {
            if (_completion == null || _navigation.CurrentRoute != RouteId.Completion
                || elapsedSeconds < 0d || elapsedSeconds > CompletionController.SemanticSeconds)
                return false;
            var snapshot = _completion.Snapshot;
            var now = snapshot.StateStartedAtSeconds + elapsedSeconds;
            if (!_completion.Advance(snapshot.TokenId, snapshot.PresentationTokenId, now)) return false;
            _motion.TryPresent(_completion.Snapshot, _accessibility.EffectiveReducedMotion);
            _media.Advance(snapshot.TokenId, _mediaHandle.MediaTokenId, now);
            return true;
        }

        private bool TryShowCompletionPause(CompletionSnapshot snapshot)
        {
            return EnsureCompletionOverlays() && _pause.TryShow(snapshot);
        }

        private bool TryShowCompletionLeave(RouteId caller)
        {
            return EnsureCompletionOverlays() && _leave.TryShow(caller);
        }

        private bool EnsureCompletionOverlays()
        {
            if (_pause != null && _leave != null) return true;
            if (_completionView == null || _pauseAdapter == null) return false;
            _pauseView = Instantiate(_pausePrefab, transform, false);
            _pauseView.Configure(_input, JourneyOverviewContent.LoadApproved(), _completionView);
            _leaveView = Instantiate(_leavePrefab, transform, false);
            _leaveView.Configure(_input, AvatarContent.LoadApproved());
            ApplyCurrentOverlayLayouts();
            _leave = new LeaveConfirmationController(_session, _navigation, _leaveView);
            _pause = new PauseOverlayController(
                _session, _navigation, _pauseAdapter, _accessibility,
                _accessibilityCoordinator, _preferences, _pauseView,
                new RejectHelpSink(), _leave, Guid.NewGuid);
            _leave.BindNestedReturnSink(_pause);
            _leaveView.BindCompletionCaller(_completionView, _pauseView);
            return true;
        }

        public bool TryRequest(AdultGateRequest request)
        {
            if (_adultView != null || _navigation.CurrentRoute != request.ReturnRoute) return false;
            var now = Time.unscaledTimeAsDouble;
            _adultView = Instantiate(_adultPrefab, transform, false);
            _adultView.Configure(_input, CompletionAdultReportContent.LoadApproved());
            if (!ApplyCurrentLayout(_adultView))
            { CleanupAdult(); return false; }
            var result = request.Purpose == AdultGatePurpose.Dashboard
                ? _navigation.RequestAdultDashboard(request.Origin, now)
                : _navigation.RequestAdultRestart(request.Origin, now);
            if (!result.Accepted) { CleanupAdult(); return false; }
            _activeAdultRequest = request;
            _adult = new AdultGateController(_navigation, _adultView, this, this, Guid.NewGuid);
            if (!_adult.Enter(request, result.Transaction.Id, now))
            { CleanupAdult(); return false; }
            if (_completionView != null) _completionView.gameObject.SetActive(false);
            if (_reportView != null) _reportView.SetVisible(false);
            _input.SetVisibleStateReady(true);
            return true;
        }

        public bool TryReturn(AdultGateRequest request)
        {
            var result = _navigation.RequestAdultGateReturn(
                request.Origin, request.ReturnRoute, Time.unscaledTimeAsDouble);
            if (!result.Accepted || _navigation.Complete(result.Transaction.Id) != NavigationCompletion.Completed)
                return false;
            CleanupAdult();
            RestoreCaller(request);
            return true;
        }

        public bool TryReset()
        {
            if (!_navigation.ConfirmedResetToStartup()) return false;
            _session.Reset();
            CleanupAll();
            ConfirmedResetRequested?.Invoke();
            return true;
        }

        public void CleanupAll()
        {
            CleanupAdult();
            CleanupView(_reportView);
            CleanupView(_completionView);
            CleanupPauseAndLeave();
            _reportView = null;
            _completionView = null;
            _report = null;
            _completion = null;
            _motion = null;
            _media = null;
            _pauseAdapter = null;
            _pause = null;
            _leave = null;
        }

        private bool HandleCompletionCommand(string id, double now)
        {
            if (id == CompletionStageView.AdultControlId)
                return _completion.HandleAdult(_completion.CurrentTokenId, now);
            if (id == CompletionStageView.PauseControlId)
            {
                if (!_pauseAdapter.PreparePause(now)) return false;
                return _completion.HandlePause(_completion.CurrentTokenId, now);
            }
            if (id == CompletionStageView.HomeControlId)
            {
                _leaveSnapshot = _completion.Snapshot;
                return _completion.HandleHome(_completion.CurrentTokenId, now);
            }
            return false;
        }

        private bool HandleAdultCommand(SemanticInputCommand command, double now)
        {
            var token = _adult.CurrentTokenId;
            if (command.ControlId == AdultGateView.HoldControlId)
            {
                var effectiveNow = Math.Max(command.InputTime, command.DurationSeconds);
                if (_adult.Snapshot.ActivePointerId == 0)
                    return command.DurationSeconds < AdultGateController.HoldSeconds
                        ? _adult.HandleHoldRelease(token, 0, effectiveNow)
                        : _adult.AdvanceHold(token, 0, effectiveNow);
                var start = effectiveNow - command.DurationSeconds;
                if (!_adult.HandleHoldBegin(token, 0, start)) return false;
                if (command.DurationSeconds < AdultGateController.HoldSeconds)
                    return _adult.HandleHoldRelease(token, 0, effectiveNow);
                return _adult.AdvanceHold(token, 0, effectiveNow);
            }
            if (command.ControlId == AdultGateView.CancelControlId)
                return _adult.State == AdultGateState.ResetConfirmation
                    ? _adult.HandleResetCancel(token) : _adult.HandleCancel(token);
            if (command.ControlId != AdultGateView.ConfirmControlId) return false;
            var accepted = _adult.State == AdultGateState.ResetConfirmation
                ? _adult.HandleResetConfirm(token)
                : _adult.HandleConfirm(token, now);
            if (accepted && _adult != null && _adult.State == AdultGateState.NavigatingReport)
                return EnterParentReport(now);
            return accepted;
        }

        private bool EnterParentReport(double now)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.ParentReport) return false;
            _reportSourceRequest = _activeAdultRequest;
            _reportView = Instantiate(_reportPrefab, transform, false);
            _reportView.Configure(_input, CompletionAdultReportContent.LoadApproved());
            if (!ApplyCurrentLayout(_reportView))
            { CleanupView(_reportView); _reportView = null; return false; }
            _report = new ParentReportController(_navigation, _reportView, this, Guid.NewGuid);
            if (!_report.Enter(_reportSourceRequest, transaction.Id, _session, now))
            { CleanupView(_reportView); _reportView = null; _report = null; return false; }
            CleanupAdult();
            _input.SetVisibleStateReady(true);
            return true;
        }

        private static void CurrentLayout(out float width, out float height,
            out float safeWidth, out float safeHeight, out float ppi)
        {
            width = Mathf.Max(1f, Screen.width);
            height = Mathf.Max(1f, Screen.height);
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(width, height);
            safeWidth = Mathf.Max(1f, Screen.safeArea.width / scale);
            safeHeight = Mathf.Max(1f, Screen.safeArea.height / scale);
            ppi = Screen.dpi > 0f ? Screen.dpi : 326f;
        }

        private static bool ApplyCurrentLayout(CompletionStageView view)
        {
            CurrentLayout(out var width, out var height, out var safeWidth, out var safeHeight, out var ppi);
            return view.TryApplyResponsiveLayout(width, height, safeWidth, safeHeight, ppi, 88);
        }

        private static bool ApplyCurrentLayout(AdultGateView view)
        {
            CurrentLayout(out var width, out var height, out var safeWidth, out var safeHeight, out var ppi);
            return view.TryApplyResponsiveLayout(width, height, safeWidth, safeHeight, ppi, 88);
        }

        private static bool ApplyCurrentLayout(ParentReportView view)
        {
            CurrentLayout(out var width, out var height, out var safeWidth, out var safeHeight, out var ppi);
            return view.TryApplyResponsiveLayout(width, height, safeWidth, safeHeight, ppi, 88, 1f, 1f);
        }

        private void ApplyCurrentOverlayLayouts()
        {
            CurrentLayout(out var width, out var height, out var safeWidth, out var safeHeight, out var ppi);
            _pauseView.ApplyResponsiveLayout(width, height, safeWidth, safeHeight, ppi, 88);
            _leaveView.ApplyResponsiveLayout(width, height, safeWidth, safeHeight, ppi, 88);
        }

        private bool HandleReportCommand(SemanticInputCommand command, double now)
        {
            if (command.ControlId == ParentReportView.CloseControlId)
            {
                if (!_report.HandleClose(_report.CurrentTokenId, now)) return false;
                var transaction = _navigation.ActiveTransaction;
                if (transaction == null || _navigation.Complete(transaction.Id) != NavigationCompletion.Completed)
                    return false;
                CleanupView(_reportView); _reportView = null; _report = null;
                RestoreCaller(_reportSourceRequest);
                return true;
            }
            if (command.ControlId == ParentReportView.RestartControlId)
                return _report.HandleRestart(
                    _report.CurrentTokenId, _report.Snapshot.ScrollNormalized,
                    ParentReportView.RestartControlId);
            return command.ControlId == ParentReportView.ScrollControlId
                && command.Kind == SemanticInputKind.Drag;
        }

        private bool HandlePauseCommand(string id, double now)
        {
            if (id == PauseOverlayView.ResumeControlId) return _pause.HandleResume(now);
            if (id == PauseOverlayView.SoundControlId) return _pause.HandleSound();
            if (id == PauseOverlayView.CaptionsControlId) return _pause.HandleCaptions();
            if (id == PauseOverlayView.MotionControlId) return _pause.HandleMotion();
            if (id == PauseOverlayView.HomeControlId) return _pause.HandleHome();
            return false;
        }

        private bool HandleLeaveCommand(string id, double now)
        {
            if (id == LeaveConfirmationView.StayControlId || id == LeaveConfirmationView.CancelControlId)
            {
                var nested = _pause != null && _pause.State == PauseLayerState.Leave;
                var accepted = id == LeaveConfirmationView.StayControlId
                    ? _leave.HandleStay() : _leave.HandleCancel();
                return accepted && (nested || _completion.RestoreFromLeave(_leaveSnapshot, now));
            }
            if (id != LeaveConfirmationView.LeaveControlId || !_leave.HandleLeave(now)) return false;
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || _navigation.Complete(transaction.Id) != NavigationCompletion.Completed
                || !_leave.CompleteTransition(transaction.Id)) return false;
            CleanupAll();
            WelcomeRequested?.Invoke();
            return true;
        }

        private void RestoreCaller(AdultGateRequest request)
        {
            if (request.Origin == AdultGateRequestOrigin.Completion && _completion != null)
            {
                _completionView.gameObject.SetActive(true);
                _completion.RestoreFromAdultGate(request.ReturnStateToken);
            }
            else if (request.Origin == AdultGateRequestOrigin.ParentReport && _report != null)
            {
                _reportView.SetVisible(true);
                _report.RestoreFromAdultGate(request.ReturnStateToken);
            }
            else WelcomeReturnRequested?.Invoke(request);
            _input.SetVisibleStateReady(true);
        }

        private void CleanupAdult()
        {
            if (_adultView != null)
            {
                foreach (var id in _adultView.ControlIds) _input?.Unregister(id);
                DestroyOwned(_adultView.gameObject);
            }
            _adultView = null;
            _adult = null;
        }

        private void CleanupPauseAndLeave()
        {
            if (_pauseView != null)
            {
                foreach (var id in _pauseView.ControlIds) _input?.Unregister(id);
                DestroyOwned(_pauseView.gameObject);
            }
            if (_leaveView != null)
            {
                foreach (var id in _leaveView.ControlIds) _input?.Unregister(id);
                foreach (var id in _leaveView.TechnicalControlIds) _input?.Unregister(id);
                DestroyOwned(_leaveView.gameObject);
            }
            _pauseView = null;
            _leaveView = null;
        }

        private void CleanupView(CompletionStageView view)
        {
            if (view == null) return;
            foreach (var id in view.ControlIds) _input?.Unregister(id);
            DestroyOwned(view.gameObject);
        }

        private void CleanupView(ParentReportView view)
        {
            if (view == null) return;
            foreach (var id in view.ControlIds) _input?.Unregister(id);
            DestroyOwned(view.gameObject);
        }

        private static void DestroyOwned(GameObject owner)
        {
            if (owner == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(owner);
            else UnityEngine.Object.DestroyImmediate(owner);
        }

        private sealed class LazyCompletionPauseSink : ICompletionPauseSink
        {
            private readonly CompletionAdultReportStageHost _owner;
            public LazyCompletionPauseSink(CompletionAdultReportStageHost owner) => _owner = owner;
            public bool TryShow(CompletionSnapshot snapshot) => _owner.TryShowCompletionPause(snapshot);
        }

        private sealed class LazyCompletionLeaveSink : ILeaveConfirmationSink
        {
            private readonly CompletionAdultReportStageHost _owner;
            public LazyCompletionLeaveSink(CompletionAdultReportStageHost owner) => _owner = owner;
            public bool TryShow(RouteId caller) => _owner.TryShowCompletionLeave(caller);
        }

        private sealed class RejectHelpSink : IJourneyOverviewPauseHelpSink
        {
            public bool TryShow(PauseOverlaySnapshot snapshot) => false;
        }
    }
}
