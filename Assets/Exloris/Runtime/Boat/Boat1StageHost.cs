using System;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Input;
using Exloris.Runtime.Lifecycle;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    [DisallowMultipleComponent]
    public sealed class Boat1StageHost : MonoBehaviour
    {
        private ChildInputRouter _input; private SessionState _session; private NavigationAuthority _navigation;
        private AudioDirector _audioDirector; private IAudioClipRegistry _audioRegistry; private AccessibilityState _accessibility;
        private AccessibilityCoordinator _accessibilityCoordinator; private AccessibilityPreferenceStore _preferences;
        private AppLifecycleCoordinator _lifecycle; private RecoveryPresenter _recoveryPresenter;
        private Boat1StageView _stagePrefab; private PauseOverlayView _pausePrefab; private Boat1HelpView _helpPrefab;
        private LeaveConfirmationController _leaveController; private LeaveConfirmationView _leaveView;
        private Boat1StageView _view; private PauseOverlayView _pauseView; private Boat1HelpView _helpView;
        private Boat1Controller _controller; private Boat1MediaPresenter _media; private Boat1MotionPresenter _motion;
        private Boat1PauseAdapter _adapter; private PauseOverlayController _pause; private Boat1HelpController _directHelp;
        private PauseRelay _pauseRelay; private HelpRelay _helpRelay;
        private Boat1MediaHandle _mediaHandle; private Boat1Snapshot _directLeaveSnapshot; private PauseOverlaySnapshot _pauseHelpSnapshot;
        private bool _prepared, _entered, _boundaryLoading, _pauseHelpVisible; private int _inputReadyFrame = -1;
        public event Action<Guid> WelcomeRequested; public event Action BoundaryRequested;
        public Boat1StageView View => _view; public PauseOverlayView PauseView => _pauseView; public Boat1HelpView HelpView => _helpView;
        public bool IsPrepared => _prepared; public bool IsEntered => _entered;
        public bool InputReady => _entered && !_boundaryLoading && _inputReadyFrame < 0
            && _input.InputsActive && CallerLayerInputReady();
        public bool BoundaryLoading => _boundaryLoading; public Boat1Snapshot Snapshot => _controller == null ? default : _controller.Snapshot;
        public Guid CurrentTokenId => _controller?.CurrentTokenId ?? Guid.Empty; public PauseLayerState PauseState => _pause?.State ?? PauseLayerState.Hidden;
        public int AcceptedCommandCount { get; private set; }
        public bool ArrivalSfxPlaying => _media != null && _media.ArrivalSfxPlaying;
        public bool IbisSfxPlaying => _media != null && _media.IbisSfxPlaying;
        public Boat1PauseReason? LastPauseReason => _view?.LastPauseReason;
        public Boat1State PauseCallerState => _adapter == null
            ? Boat1State.Hidden : _adapter.CapturedSnapshot.State;
        public Boat1State DirectLeaveCallerState => _directLeaveSnapshot.State;
        public AvatarTechnicalOverlayState RecoveryState => _leaveView == null ? AvatarTechnicalOverlayState.Hidden : _leaveView.TechnicalOverlayState;

        public void Configure(ChildInputRouter input, SessionState session, NavigationAuthority navigation, AudioDirector audioDirector,
            IAudioClipRegistry audioRegistry, AccessibilityState accessibility, AccessibilityCoordinator coordinator,
            AccessibilityPreferenceStore preferences, AppLifecycleCoordinator lifecycle, RecoveryPresenter recovery,
            Boat1StageView stagePrefab, PauseOverlayView pausePrefab, Boat1HelpView helpPrefab)
        {
            _input=input??throw new ArgumentNullException(nameof(input)); _session=session??throw new ArgumentNullException(nameof(session));
            _navigation=navigation??throw new ArgumentNullException(nameof(navigation)); _audioDirector=audioDirector??throw new ArgumentNullException(nameof(audioDirector));
            _audioRegistry=audioRegistry??throw new ArgumentNullException(nameof(audioRegistry)); _accessibility=accessibility??throw new ArgumentNullException(nameof(accessibility));
            _accessibilityCoordinator=coordinator??throw new ArgumentNullException(nameof(coordinator)); _preferences=preferences??throw new ArgumentNullException(nameof(preferences));
            _lifecycle=lifecycle??throw new ArgumentNullException(nameof(lifecycle)); _recoveryPresenter=recovery??throw new ArgumentNullException(nameof(recovery));
            _stagePrefab=stagePrefab??throw new ArgumentNullException(nameof(stagePrefab)); _pausePrefab=pausePrefab??throw new ArgumentNullException(nameof(pausePrefab)); _helpPrefab=helpPrefab??throw new ArgumentNullException(nameof(helpPrefab));
        }

        public bool Prepare(Guid transactionId, LeaveConfirmationController leaveController, LeaveConfirmationView leaveView, bool selectedGirl)
        {
            var tx=_navigation?.ActiveTransaction;
            if (_prepared || tx==null || tx.Id!=transactionId || tx.Caller!=RouteId.JourneyOverview || tx.Destination!=RouteId.Boat1) return false;
            _leaveController=leaveController??throw new ArgumentNullException(nameof(leaveController)); _leaveView=leaveView??throw new ArgumentNullException(nameof(leaveView));
            var content=Boat1Content.LoadApproved(); _view=Instantiate(_stagePrefab,transform,false); _view.Configure(_input,content,selectedGirl);
            _helpView=Instantiate(_helpPrefab,transform,false); _helpView.Configure(_input,content);
            if (!TryApplyResponsiveLayout(Screen.width,Screen.height,Screen.safeArea.width,Screen.safeArea.height,Screen.dpi>0?Screen.dpi:326,88)) { Cleanup(); return false; }
            _pauseRelay=new PauseRelay(); _helpRelay=new HelpRelay();
            _controller=new Boat1Controller(_session,_navigation,_view,_pauseRelay,_leaveController,Guid.NewGuid);
            _media=new Boat1MediaPresenter(_audioDirector,_audioRegistry,content,_session,_accessibility,_view);
            _motion=new Boat1MotionPresenter(content,_view);
            _directHelp=new Boat1HelpController(_controller,_helpView);
            _view.SetVisible(true); _view.SetInputReady(false); _prepared=true; return true;
        }

        public bool Enter(double now)
        {
            if (!_prepared||_entered||_navigation.CurrentRoute!=RouteId.Boat1||_session.CurrentOpenSessionLocation!=RouteId.Boat1) return false;
            _pauseView=Instantiate(_pausePrefab,transform,false); _pauseView.Configure(_input,JourneyOverviewContent.LoadApproved(),_view);
            _adapter=new Boat1PauseAdapter(_controller,_media,_motion);
            _pause=new PauseOverlayController(_session,_navigation,_adapter,_accessibility,_accessibilityCoordinator,_preferences,_pauseView,_helpRelay,_leaveController,Guid.NewGuid);
            _pauseRelay.Target=_pause; _helpRelay.Host=this; _leaveController.BindNestedReturnSink(_pause); _leaveView.BindBoat1Caller(_view,_pauseView);
            if (!TryApplyResponsiveLayout(Screen.width,Screen.height,Screen.safeArea.width,Screen.safeArea.height,Screen.dpi>0?Screen.dpi:326,88)
                || !_controller.Enter(now,_accessibility.EffectiveReducedMotion)) return false;
            _mediaHandle=_media.StartChoice(_controller.CurrentTokenId,now); _adapter.BindMediaHandle(_mediaHandle);
            _motion.Present(_controller.Snapshot,_accessibility.EffectiveReducedMotion,Mathf.Max(1,Screen.safeArea.height));
            _inputReadyFrame=Time.frameCount+1; _entered=true; return true;
        }

        public void Tick(double now,float delta)
        {
            if(!_entered)return; if(_boundaryLoading){if(_lifecycle.Tick(now))ShowRecovery();return;}
            if(_inputReadyFrame>=0&&Time.frameCount>=_inputReadyFrame){_inputReadyFrame=-1;_view.SetInputReady(true);_input.SetVisibleStateReady(true);}
            if(_pause.State!=PauseLayerState.Hidden||_controller.State==Boat1State.Help||_controller.State==Boat1State.Leaving)return;
            _media.Advance(_controller.CurrentTokenId,_media.CurrentMediaTokenId,now);
            var previousState=_controller.State; _controller.Advance(_controller.CurrentTokenId,now);
            PlayArrivalFeedbackOnTransition(previousState,now);
            _motion.Present(_controller.Snapshot,_accessibility.EffectiveReducedMotion,Mathf.Max(1,Screen.safeArea.height));
            if(_controller.State==Boat1State.Navigating)BeginBoundary();
        }

        public bool OwnsControl(string id) => _view!=null&&(_view.ControlIds.Contains(id)||id==Boat1HelpView.BackControlId
            ||id==PauseOverlayView.HelpControlId||id==PauseOverlayView.SoundControlId||id==PauseOverlayView.CaptionsControlId
            ||id==PauseOverlayView.ResumeControlId||id==PauseOverlayView.MotionControlId||id==PauseOverlayView.HomeControlId
            ||id==LeaveConfirmationView.StayControlId||id==LeaveConfirmationView.CancelControlId||id==LeaveConfirmationView.LeaveControlId
            ||id==LeaveConfirmationView.RetryControlId||id==LeaveConfirmationView.HomeControlId);

        public bool HandleSemanticCommand(string id,double now)
        {
            if(!_entered||string.IsNullOrEmpty(id))return false; bool ok=false;
            switch(id)
            {
                case Boat1StageView.ReedsControlId: ok=_controller.HandleChoice(_controller.CurrentTokenId,Boat1Choice.Reeds,now); break;
                case Boat1StageView.VillageControlId: ok=_controller.HandleChoice(_controller.CurrentTokenId,Boat1Choice.Village,now); break;
                case Boat1StageView.FollowIbisControlId: ok=_controller.HandleFollowIbis(_controller.CurrentTokenId,now);if(ok)_media.PlayIbisCueSfx(_controller.CurrentTokenId,now);break;
                case Boat1StageView.HelpControlId: ok=_directHelp.TryShow(); break;
                case Boat1StageView.RepeatControlId: ok=_controller.HandleRepeat(now); if(ok){_mediaHandle=_media.Repeat(_controller.CurrentTokenId,now);_adapter.BindMediaHandle(_mediaHandle);} break;
                case Boat1StageView.SkipControlId: var previousState=_controller.State;ok=_controller.HandleSkip(_controller.CurrentTokenId,now);if(ok)PlayArrivalFeedbackOnTransition(previousState,now);break;
                case Boat1StageView.PauseControlId: ok=PauseStage(now,Boat1PauseReason.Manual);break;
                case Boat1StageView.HomeControlId: _directLeaveSnapshot=_controller.Snapshot;ok=_controller.HandleHome(now);break;
                case Boat1HelpView.BackControlId: ok=HandleHelpBack();break;
                case PauseOverlayView.HelpControlId: ok=_pause.HandleHelp();break;
                case PauseOverlayView.SoundControlId: ok=_pause.HandleSound();break;
                case PauseOverlayView.CaptionsControlId: ok=_pause.HandleCaptions();break;
                case PauseOverlayView.MotionControlId: ok=_pause.HandleMotion();break;
                case PauseOverlayView.ResumeControlId: ok=ResumePause(now);break;
                case PauseOverlayView.HomeControlId: ok=_adapter.CapturedSnapshot.State==Boat1State.Leaving?ResumePause(now):_pause.HandleHome();break;
                case LeaveConfirmationView.StayControlId: ok=RestoreLeave(false,now);break;
                case LeaveConfirmationView.CancelControlId: ok=RestoreLeave(true,now);break;
                case LeaveConfirmationView.LeaveControlId: ok=RequestWelcome(now);break;
                case LeaveConfirmationView.RetryControlId: ok=RetryRecovery(now);break;
                case LeaveConfirmationView.HomeControlId: ok=HomeRecovery(now);break;
            }
            if(ok){AcceptedCommandCount++;if((id==Boat1StageView.ReedsControlId||id==Boat1StageView.VillageControlId||id==Boat1StageView.FollowIbisControlId))_motion.Present(_controller.Snapshot,_accessibility.EffectiveReducedMotion,Mathf.Max(1,Screen.safeArea.height));}return ok;
        }

        public bool HandleInterruption(double now,Boat1PauseReason reason)=>_entered&&!_boundaryLoading&&(_pause.State!=PauseLayerState.Hidden||PauseStage(now,reason));
        public bool CompleteArrivalForDiagnostics()
        {
            if(!_entered||_boundaryLoading||_controller.State!=Boat1State.Arriving)return false;
            var now=_controller.Snapshot.StateStartedAtSeconds+Boat1Controller.ArrivalFeedbackSeconds;
            if(!_controller.Advance(_controller.CurrentTokenId,now)||_controller.State!=Boat1State.Navigating)return false;
            _motion.Present(_controller.Snapshot,_accessibility.EffectiveReducedMotion,Mathf.Max(1,Screen.safeArea.height));
            BeginBoundary();return _boundaryLoading;
        }
        public bool HandleBoundaryAssetFailure(){if(!_entered||!_boundaryLoading||!_lifecycle.HandleAssetFailure())return false;ShowRecovery();return true;}
        public bool TryApplyResponsiveLayout(float sw,float sh,float safeW,float safeH,float ppi,int minimum)
        { try { sw=Mathf.Max(1,sw);sh=Mathf.Max(1,sh);var scale=Exloris.Runtime.Layout.ResponsiveLayoutProfile.CalculateCanvasScale(sw,sh); safeW=Mathf.Max(1,safeW/scale);safeH=Mathf.Max(1,safeH/scale);if(!_view.TryApplyResponsiveLayout(sw,sh,safeW,safeH,ppi,minimum))throw new InvalidOperationException("Boat 1 safe area is infeasible.");if(_pauseView!=null)_pauseView.ApplyResponsiveLayout(sw,sh,safeW,safeH,ppi,minimum);_helpView.ApplyResponsiveLayout(sw,sh,safeW,safeH,ppi,minimum);return true;}catch(InvalidOperationException){if(_entered&&_boundaryLoading)HandleBoundaryAssetFailure();return false;} }
        public void ShowRecovery(){if(!_entered||!_boundaryLoading)return;if(_recoveryPresenter.State==RecoveryPresentationState.Preparing)_recoveryPresenter.MarkRecoveryVisible();if(_recoveryPresenter.State!=RecoveryPresentationState.Visible)return;_controller.EnterRecovery();_leaveView.ShowTechnicalOverlay(RouteId.Boat1,AvatarTechnicalOverlayState.Recovery);}
        public void CompleteWelcomeTransition(Guid id){_leaveController?.CompleteTransition(id);_leaveView?.CompleteTransitionAndHide();}
        public void Cleanup()
        {
            if(_view!=null)foreach(var id in _view.ControlIds)_input.Unregister(id);if(_pauseView!=null)foreach(var id in _pauseView.ControlIds)_input.Unregister(id);if(_helpView!=null)foreach(var id in _helpView.ControlIds)_input.Unregister(id);
            if(_media!=null&&_controller!=null&&_controller.CurrentTokenId!=Guid.Empty)_media.Stop(_controller.CurrentTokenId);
            if(_helpView!=null)Destroy(_helpView.gameObject);if(_pauseView!=null)Destroy(_pauseView.gameObject);if(_view!=null)Destroy(_view.gameObject);
            _view=null;_pauseView=null;_helpView=null;_controller=null;_media=null;_motion=null;_adapter=null;_pause=null;_directHelp=null;_pauseRelay=null;_helpRelay=null;_prepared=false;_entered=false;_boundaryLoading=false;_inputReadyFrame=-1;
        }

        private bool PauseStage(double now,Boat1PauseReason reason)
        {
            if(!_adapter.PreparePause(now,reason))return false;
            if(!_controller.HandlePause(now)){_adapter.CancelPreparedPause();return false;}
            var caller=_adapter.CapturedSnapshot.State;
            var stacked=caller==Boat1State.Help||caller==Boat1State.Leaving;
            _pauseView.SetBoatInterruptionPriority(stacked);
            if(caller==Boat1State.Help)_helpView.SetInterruptionBlocked(true);
            else if(caller==Boat1State.Leaving)_leaveView.SetInterruptionBlocked(true);
            return true;
        }
        private bool ResumePause(double now)
        {
            var caller=_adapter.CapturedSnapshot.State;
            if(!_pause.HandleResume(now))return false;
            _pauseView.SetBoatInterruptionPriority(false);
            if(caller==Boat1State.Help)_helpView.SetInterruptionBlocked(false);
            else if(caller==Boat1State.Leaving)_leaveView.SetInterruptionBlocked(false);
            if(caller==Boat1State.Help||caller==Boat1State.Leaving)_view.SetInputReady(false);
            RefreshMediaHandle();_motion.Present(_controller.Snapshot,_accessibility.EffectiveReducedMotion,Mathf.Max(1,Screen.safeArea.height));
            return true;
        }
        private bool ShowPauseHelp(PauseOverlaySnapshot snapshot)
        {
            _pauseHelpSnapshot=snapshot;_pauseHelpVisible=true;_pauseView.SetControlsEnabled(false);
            _helpView.SetInterruptionBlocked(false);_helpView.SetPauseOverlayPriority(true);
            _helpView.Show(_adapter.CapturedSnapshot);return true;
        }
        private bool HandleHelpBack()
        {
            if(!_pauseHelpVisible)return _directHelp.HandleBack();
            if(!_pause.RestoreFromHelp(_pauseHelpSnapshot))return false;
            _pauseHelpVisible=false;_helpView.SetPauseOverlayPriority(false);
            if(_adapter.CapturedSnapshot.State==Boat1State.Help)
            {
                _helpView.Show(_adapter.CapturedSnapshot);
                _helpView.SetInterruptionBlocked(_pause.State==PauseLayerState.Pause);
            }
            else _helpView.Hide();
            return true;
        }
        private bool RestoreLeave(bool cancel,double now){var nested=_pause.State==PauseLayerState.Leave;var ok=cancel?_leaveController.HandleCancel():_leaveController.HandleStay();return ok&&(nested||_controller.RestoreFromLeave(_directLeaveSnapshot,now));}
        private bool RequestWelcome(double now){if(!_leaveController.HandleLeave(now))return false;var tx=_navigation.ActiveTransaction;if(tx==null||tx.Destination!=RouteId.Welcome)return false;_input.SetVisibleStateReady(false);WelcomeRequested?.Invoke(tx.Id);return true;}
        private bool RetryRecovery(double now){var result=_lifecycle.Retry(now);if(!result.Accepted||result.Transaction.Caller!=RouteId.Boat1||result.Transaction.Destination!=RouteId.Stage1)return false;if(!_controller.RetryBoundary())return false;_leaveView.ShowTechnicalOverlay(RouteId.Boat1,AvatarTechnicalOverlayState.Loading);return true;}
        private bool HomeRecovery(double now){var result=_lifecycle.Home(now);if(!result.Accepted||result.Transaction.Caller!=RouteId.Boat1||result.Transaction.Destination!=RouteId.Welcome)return false;_leaveView.ShowTechnicalOverlay(RouteId.Boat1,AvatarTechnicalOverlayState.Loading);WelcomeRequested?.Invoke(result.Transaction.Id);return true;}
        private void RefreshMediaHandle(){_mediaHandle=new Boat1MediaHandle(_controller.CurrentTokenId,_media.CurrentMediaTokenId,null,null,null);_adapter.BindMediaHandle(_mediaHandle);}
        private void PlayArrivalFeedbackOnTransition(Boat1State previousState,double now){if(previousState!=Boat1State.Arriving&&_controller.State==Boat1State.Arriving)_media.PlayArrivalSfx(_controller.CurrentTokenId,now);}
        private void BeginBoundary(){if(_boundaryLoading)return;_boundaryLoading=true;_view.SetInputReady(false);_input.SetVisibleStateReady(false);_media.Stop(_controller.CurrentTokenId);BoundaryRequested?.Invoke();}
        private bool CallerLayerInputReady()
        {
            if(_controller==null)return false;
            if(_controller.State==Boat1State.Help)return _helpView!=null&&_helpView.IsVisible;
            if(_controller.State==Boat1State.Leaving)return _leaveView!=null&&_leaveView.IsVisible;
            return _view!=null&&_view.ControlsEnabled;
        }
        private sealed class PauseRelay:IBoat1PauseSink{public IBoat1PauseSink Target{private get;set;}public bool TryShow(Boat1Snapshot s)=>Target!=null&&Target.TryShow(s);}
        private sealed class HelpRelay:IJourneyOverviewPauseHelpSink{public Boat1StageHost Host{private get;set;}public bool TryShow(PauseOverlaySnapshot s)=>Host!=null&&Host.ShowPauseHelp(s);}
    }
}
