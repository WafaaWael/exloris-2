using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Diagnostics;
using Exloris.Runtime.Input;
using Exloris.Runtime.Intro;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Lifecycle;
using Exloris.Runtime.Localization;
using Exloris.Runtime.Media;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using Exloris.Runtime.Welcome;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class ScreenHost : MonoBehaviour, IReducedMotionParticipant, IAdultGateRequestSink
    {
        public const string InputProbeId = "foundation.input";
        public const string RouteProbeId = "foundation.route";
        public const string RetryId = "foundation.retry";
        public const string HomeId = "foundation.home";
        public const string ReducedMotionId = "foundation.reduced-motion";
        public const string PseudolocalizationId = "foundation.pseudolocalization";
        public const string AudioProbeId = "foundation.audio";
        public const string InterruptionId = "foundation.interruption";
        public const string ResumeId = "foundation.resume";

        private const float ControlSize = 224f;
        private const float VisibleSize = 104f;

        [SerializeField] private bool startupIntroEnabled;
        [SerializeField] private GameObject startupLoadingPrefab;
        [SerializeField] private IntroStageView introStagePrefab;
        [SerializeField] private WelcomeStageView welcomeStagePrefab;
        [SerializeField] private SessionChoiceView sessionChoicePrefab;
        [SerializeField] private AvatarSelectView avatarSelectPrefab;
        [SerializeField] private AvatarPreviewView avatarPreviewPrefab;
        [SerializeField] private LeaveConfirmationView leaveConfirmationPrefab;
        [SerializeField] private JourneyOverviewStageView journeyOverviewPrefab;
        [SerializeField] private PauseOverlayView pauseOverlayPrefab;
        [SerializeField] private JourneyOverviewPauseHelpView journeyPauseHelpPrefab;
        [SerializeField] private Boat1StageView boat1Prefab;
        [SerializeField] private Boat1HelpView boat1HelpPrefab;
        [SerializeField] private Stage1StageView stage1Prefab;
        [SerializeField] private Stage1HelpView stage1HelpPrefab;
        [SerializeField] private Boat2StageView boat2Prefab;
        [SerializeField] private Boat2HelpView boat2HelpPrefab;
        [SerializeField] private Stage2StageView stage2Prefab;
        [SerializeField] private Stage2HelpView stage2HelpPrefab;
        [SerializeField] private Boat3StageView boat3Prefab;
        [SerializeField] private Boat3HelpView boat3HelpPrefab;
        [SerializeField] private Stage3StageView stage3Prefab;
        [SerializeField] private Stage3HelpView stage3HelpPrefab;
        [SerializeField] private CompletionStageView completionPrefab;
        [SerializeField] private AdultGateView adultGatePrefab;
        [SerializeField] private ParentReportView parentReportPrefab;

        private readonly Dictionary<string, ControlView> _controls = new Dictionary<string, ControlView>();
        private ChildInputRouter _inputRouter;
        private InputActionAsset _inputActions;
        private InputAction _pointAction;
        private InputAction _pressAction;
        private NavigationAuthority _navigation;
        private RecoveryPresenter _recoveryPresenter;
        private AppLifecycleCoordinator _lifecycle;
        private AccessibilityState _accessibilityState;
        private AccessibilityCoordinator _accessibilityCoordinator;
        private AccessibilityPreferenceStore _accessibilityStore;
        private LocalizationCoordinator _localization;
        private AudioDirector _audioDirector;
        private UnityAudioBackend _audioBackend;
        private MediaPlaybackCoordinator _mediaCoordinator;
        private AudioConfigurationMonitor _audioConfigurationMonitor;
        private TextMeshProUGUI _header;
        private Image _statePanel;
        private double _autoCompleteAt = double.PositiveInfinity;
        private bool _pseudo;
        private bool _audioInterrupted;
        private GameObject _diagnosticCanvas;
        private IntroStageController _introController;
        private IntroStageView _introView;
        private UnityIntroPlayback _introPlayback;
        private bool _introResumePending;
        private long _lastPreparedIntroFrameCount;
        private long _lastPresentedIntroFrameIndex = -1;
        private int _lastIntroCompletionEventCount;
        private GameObject _startupLoadingInstance;
        private double _startupRealtimeSeconds;
        private SessionState _session;
        private WelcomeStageView _welcomeView;
        private SessionChoiceView _sessionChoiceView;
        private WelcomeController _welcomeController;
        private SessionChoiceController _sessionChoiceController;
        private WelcomeMediaPresenter _welcomeMedia;
        private WelcomePresentationState _welcomePresentation = WelcomePresentationState.Welcome;
        private AdultGateRequest? _pendingAdultGateRequest;
        private bool _welcomeEntered;
        private bool _welcomeInterruptionResumePending;
        private int _welcomeInputReadyFrame = -1;
        private AvatarSelectView _avatarSelectView;
        private AvatarPreviewView _avatarPreviewView;
        private LeaveConfirmationView _leaveConfirmationView;
        private AvatarChoiceController _avatarController;
        private LeaveConfirmationController _leaveController;
        private AvatarMotionPresenter _avatarMotion;
        private AvatarMediaPresenter _avatarMedia;
        private AvatarChoice? _avatarInitialLeftOverride;
        private int _avatarInputReadyFrame = -1;
        private double _avatarPresentationStartedAt;
        private bool _avatarBoundaryLoading;
        private Guid? _lastCompletedAvatarEntryTransactionId;
        private bool _deferNextAvatarTransition;
        private RouteId? _pendingAvatarTransitionDestination;
        private bool _avatarMotionPaused;
        private bool _avatarInterruptionResumePending;
        private bool _avatarInterruptionRestoreLeave;
        private JourneyOverviewStageHost _journeyStageHost;
        private Guid? _lastCompletedJourneyEntryTransactionId;
        private bool _deferNextJourneyBoundary;
        private Boat1StageHost _boat1StageHost;
        private Guid? _lastCompletedBoat1EntryTransactionId;
        private Stage1StageHost _stage1StageHost;
        private Guid? _lastCompletedStage1EntryTransactionId;
        private Guid? _pendingStage1EntryTransactionId;
        private int _stage1EntryReadyFrame = -1;
        private Boat2StageHost _boat2StageHost;
        private Guid? _lastCompletedBoat2EntryTransactionId;
        private Guid? _pendingBoat2EntryTransactionId;
        private int _boat2EntryReadyFrame = -1;
        private Stage2StageHost _stage2StageHost;
        private Guid? _lastCompletedStage2EntryTransactionId;
        private Guid? _pendingStage2EntryTransactionId;
        private int _stage2EntryReadyFrame = -1;
        private Boat3StageHost _boat3StageHost;
        private Guid? _lastCompletedBoat3EntryTransactionId;
        private Guid? _pendingBoat3EntryTransactionId;
        private int _boat3EntryReadyFrame = -1;
        private Guid? _pendingStage3EntryTransactionId;
        private Stage3StageHost _stage3StageHost;
        private Guid? _lastCompletedStage3EntryTransactionId;
        private int _stage3EntryReadyFrame = -1;
        private Stage2River _stage3EntryRiver = Stage2River.None;
        private Guid? _pendingCompletionEntryTransactionId;
        private int _completionEntryReadyFrame = -1;
        private CompletionAdultReportStageHost _closingStageHost;

        public int AcceptedInputCount { get; private set; }
        public bool ReducedMotionEnabled => _accessibilityState != null && _accessibilityState.EffectiveReducedMotion;
        public bool AccessibilityCoordinatorConnected => _accessibilityCoordinator != null;
        public string ReducedMotionStaticEquivalentId => "foundation_diagnostic_surface_static";
        public string StaticEquivalentId => ReducedMotionStaticEquivalentId;
        public bool PseudolocalizationEnabled => _pseudo;
        public bool AudioProbeStarted { get; private set; }
        public int ActiveVoiceCount => _audioDirector?.ActiveVoiceCount ?? 0;
        public bool AudioResumeRampActive => _audioBackend != null && _audioBackend.MasterRampActive;
        public float AudioResumeRampDurationSeconds => _audioBackend?.MasterRampDurationSeconds ?? 0f;
        public bool AudioInterrupted => _audioInterrupted;
        public bool AvatarVoicePlaying => _avatarMedia != null && _avatarMedia.VoiceIsPlaying;
        public bool MediaLifecycleConnected => _mediaCoordinator != null && _audioConfigurationMonitor != null;
        public bool SynchronizedMediaResumePending => _mediaCoordinator != null && _mediaCoordinator.ResumePending;
        public bool InputsActive => _inputRouter != null && _inputRouter.InputsActive;
        public bool EnabledSemanticControlsOverlap =>
            _inputRouter != null && _inputRouter.EnabledControlsOverlap();
        public bool RecoveryVisible => _recoveryPresenter != null &&
            (_recoveryPresenter.State == RecoveryPresentationState.Visible ||
             _recoveryPresenter.State == RecoveryPresentationState.PausedVisible);
        public Guid? ActiveNavigationId => _navigation?.ActiveTransaction?.Id;
        public RouteId? ActiveNavigationCaller => _navigation?.ActiveTransaction?.Caller;
        public RouteId? ActiveNavigationDestination => _navigation?.ActiveTransaction?.Destination;
        public string VisibleStatusText => _header == null ? string.Empty : _header.text;
        public string LastAcceptedControlId { get; private set; }
        public bool StartupIntroEnabled => startupIntroEnabled;
        public RouteId CurrentRoute => _navigation == null ? RouteId.StartupLoading : _navigation.CurrentRoute;
        public IntroStageView ActiveIntroView => _introView;
        public bool IntroResumePending => _introResumePending;
        public long PreparedIntroFrameCount => _introPlayback == null
            ? _lastPreparedIntroFrameCount
            : _introPlayback.LastPreparedFrameCount;
        public long LastPresentedIntroFrameIndex => _introPlayback == null
            ? _lastPresentedIntroFrameIndex
            : _introPlayback.LastPresentedFrameIndex;
        public int IntroCompletionEventCount => _introPlayback == null
            ? _lastIntroCompletionEventCount
            : _introPlayback.CompletionEventCount;
        public string CurrentIntroVariantFile => _introController?.CurrentVariant?.FileName ?? string.Empty;
        public double CurrentIntroMediaTime => _introPlayback == null ? 0d : _introPlayback.CurrentTimeSeconds;
        public bool CurrentIntroReducedMotion =>
            _introController?.CurrentVariant?.FrameCount == IntroVariantSelector.ReducedFrameCount;
        public bool IntroVoiceIsPlaying => _introPlayback != null && _introPlayback.VoiceIsPlaying;
        public double IntroOverlayDriftSeconds => _introView == null ? 0d : _introView.MaximumObservedOverlayDriftSeconds;
        public double IntroVoiceVideoStartDriftSeconds => _introPlayback == null
            ? 0d
            : Math.Abs(_introPlayback.LastVideoStartDspTime - _introPlayback.LastVoiceStartDspTime);
        public double IntroFirstChildFrameElapsedSeconds { get; private set; } = -1d;
        public Guid? ActiveIntroToken => _introController?.CurrentPlaybackToken;
        public WelcomeStageView ActiveWelcomeView => _welcomeView;
        public SessionChoiceView ActiveSessionChoiceView => _sessionChoiceView;
        public bool WelcomeInputReady => _welcomeEntered && _welcomeInputReadyFrame < 0 && InputsActive &&
            _welcomeView != null &&
            _welcomeView.TechnicalOverlayState == WelcomeTechnicalOverlayState.Hidden &&
            _welcomeView.NormalControlsEnabled;
        public bool SessionOpen => _session != null && _session.IsOpen;
        public bool SessionSoundEnabled => _session == null || _session.SoundEnabled;
        public RouteId SessionLocation => _session == null ? RouteId.StartupLoading : _session.CurrentOpenSessionLocation;
        public AdultGateRequest? PendingAdultGateRequest => _pendingAdultGateRequest;
        public bool WelcomeInterruptionResumePending => _welcomeInterruptionResumePending;
        public bool WelcomeCaptionsEnabled => _accessibilityState != null && _accessibilityState.CaptionsEnabled;
        public AvatarSelectView ActiveAvatarSelectView => _avatarSelectView;
        public AvatarPreviewView ActiveAvatarPreviewView => _avatarPreviewView;
        public LeaveConfirmationView ActiveLeaveConfirmationView =>
            _closingStageHost != null && _closingStageHost.LeaveView != null
                ? _closingStageHost.LeaveView : _leaveConfirmationView;
        public bool AvatarInputReady => _avatarController != null && _avatarInputReadyFrame < 0
            && !_avatarBoundaryLoading && InputsActive
            && (CurrentRoute == RouteId.AvatarSelect || CurrentRoute == RouteId.AvatarPreview);
        public AvatarChoice? AvatarLeftChoice => _avatarSelectView == null
            ? (AvatarChoice?)null : _avatarSelectView.LeftChoice;
        public AvatarChoice? AvatarRightChoice => _avatarSelectView == null
            ? (AvatarChoice?)null : _avatarSelectView.RightChoice;
        public AvatarChoice? AvatarPreviewChoice => _avatarController?.PreviewChoice;
        public long CurrentAvatarPreviewToken => _avatarController?.CurrentPreviewToken ?? 0;
        public bool AvatarBoundaryLoading => _avatarBoundaryLoading;
        public bool AvatarMotionPaused => _avatarMotionPaused;
        public bool AvatarCaptionsEnabled => _accessibilityState != null && _accessibilityState.CaptionsEnabled;
        public AvatarTechnicalOverlayState AvatarRecoveryState => _leaveConfirmationView == null
            ? AvatarTechnicalOverlayState.Hidden
            : _leaveConfirmationView.TechnicalOverlayState;
        public string SessionSelectedAvatarId => _session?.SelectedAvatarId;
        public bool WelcomeMediaActive => _welcomeMedia != null;
        public Guid? LastCompletedAvatarEntryTransactionId => _lastCompletedAvatarEntryTransactionId;
        public JourneyOverviewStageView ActiveJourneyOverviewView => _journeyStageHost?.View;
        public PauseOverlayView ActivePauseOverlayView =>
            _closingStageHost != null && _closingStageHost.PauseView != null
                ? _closingStageHost.PauseView
            : _stage3StageHost != null && _stage3StageHost.IsEntered
            ? _stage3StageHost.PauseView
            : _boat3StageHost != null && _boat3StageHost.IsEntered
            ? _boat3StageHost.PauseView
            : _stage2StageHost != null && _stage2StageHost.IsEntered
            ? _stage2StageHost.PauseView
            : _boat2StageHost != null && _boat2StageHost.IsEntered
            ? _boat2StageHost.PauseView
            : _stage1StageHost != null && _stage1StageHost.IsEntered
            ? _stage1StageHost.PauseView
            : _boat1StageHost != null && _boat1StageHost.IsEntered ? _boat1StageHost.PauseView : _journeyStageHost?.PauseView;
        public JourneyOverviewPauseHelpView ActiveJourneyPauseHelpView => _journeyStageHost?.HelpView;
        public bool JourneyInputReady => _journeyStageHost != null && _journeyStageHost.InputReady;
        public bool JourneyBoundaryLoading => _journeyStageHost != null && _journeyStageHost.BoundaryLoading;
        public Guid CurrentJourneyOverviewToken => _journeyStageHost?.CurrentTokenId ?? Guid.Empty;
        public JourneyOverviewSnapshot JourneySnapshot => _journeyStageHost == null
            ? default : _journeyStageHost.Snapshot;
        public PauseLayerState JourneyPauseState => _journeyStageHost?.PauseState ?? PauseLayerState.Hidden;
        public int JourneyAcceptedCommandCount => _journeyStageHost?.AcceptedCommandCount ?? 0;
        public bool JourneyMediaPaused => _journeyStageHost != null && _journeyStageHost.MediaPaused;
        public AvatarTechnicalOverlayState JourneyRecoveryState => _journeyStageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _journeyStageHost.RecoveryState;
        public Guid? LastCompletedJourneyEntryTransactionId => _lastCompletedJourneyEntryTransactionId;
        public Boat1StageView ActiveBoat1View => _boat1StageHost?.View;
        public Boat1HelpView ActiveBoat1HelpView => _boat1StageHost?.HelpView;
        public bool Boat1InputReady => _boat1StageHost != null && _boat1StageHost.InputReady;
        public bool Boat1BoundaryLoading => _boat1StageHost != null && _boat1StageHost.BoundaryLoading;
        public bool Boat1ArrivalSfxPlaying => _boat1StageHost != null && _boat1StageHost.ArrivalSfxPlaying;
        public bool Boat1IbisSfxPlaying => _boat1StageHost != null && _boat1StageHost.IbisSfxPlaying;
        public Boat1PauseReason? Boat1LastPauseReason => _boat1StageHost?.LastPauseReason;
        public Boat1State Boat1PauseCallerState => _boat1StageHost?.PauseCallerState ?? Boat1State.Hidden;
        public Boat1State Boat1DirectLeaveCallerState =>
            _boat1StageHost?.DirectLeaveCallerState ?? Boat1State.Hidden;
        public Boat1Snapshot Boat1Snapshot => _boat1StageHost == null ? default : _boat1StageHost.Snapshot;
        public PauseLayerState Boat1PauseState => _boat1StageHost?.PauseState ?? PauseLayerState.Hidden;
        public int Boat1AcceptedCommandCount => _boat1StageHost?.AcceptedCommandCount ?? 0;
        public AvatarTechnicalOverlayState Boat1RecoveryState => _boat1StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _boat1StageHost.RecoveryState;
        public Guid? LastCompletedBoat1EntryTransactionId => _lastCompletedBoat1EntryTransactionId;
        public Stage1StageView ActiveStage1View => _stage1StageHost?.View;
        public Stage1HelpView ActiveStage1HelpView => _stage1StageHost?.HelpView;
        public bool Stage1InputReady => _stage1StageHost != null && _stage1StageHost.InputReady;
        public bool Stage1BoundaryLoading => _stage1StageHost != null && _stage1StageHost.BoundaryLoading;
        public Stage1Snapshot Stage1Snapshot => _stage1StageHost == null ? default : _stage1StageHost.Snapshot;
        public PauseLayerState Stage1PauseState => _stage1StageHost?.PauseState ?? PauseLayerState.Hidden;
        public Stage1PauseReason? Stage1LastPauseReason => _stage1StageHost?.LastPauseReason;
        public int Stage1AcceptedCommandCount => _stage1StageHost?.AcceptedCommandCount ?? 0;
        public bool Stage1VoicePlaying => _stage1StageHost != null && _stage1StageHost.VoicePlaying;
        public bool Stage1LoopsPlaying => _stage1StageHost != null && _stage1StageHost.LoopsPlaying;
        public bool Stage1AttachSfxPlaying => _stage1StageHost != null && _stage1StageHost.AttachSfxPlaying;
        public bool Stage1StepSfxPlaying => _stage1StageHost != null && _stage1StageHost.StepSfxPlaying;
        public bool Stage1Prepared => _stage1StageHost != null && _stage1StageHost.IsPrepared;
        public string Stage1PreparationFailure => _stage1StageHost?.LastPreparationFailure;
        public Guid? PendingStage1EntryTransactionId => _pendingStage1EntryTransactionId;
        public AvatarTechnicalOverlayState Stage1RecoveryState => _stage1StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _stage1StageHost.RecoveryState;
        public Guid? LastCompletedStage1EntryTransactionId => _lastCompletedStage1EntryTransactionId;
        public Boat2StageView ActiveBoat2View => _boat2StageHost?.View;
        public Boat2HelpView ActiveBoat2HelpView => _boat2StageHost?.HelpView;
        public bool Boat2InputReady => _boat2StageHost != null && _boat2StageHost.InputReady;
        public bool Boat2BoundaryLoading => _boat2StageHost != null && _boat2StageHost.BoundaryLoading;
        public Boat2Snapshot Boat2Snapshot => _boat2StageHost == null ? default : _boat2StageHost.Snapshot;
        public PauseLayerState Boat2PauseState => _boat2StageHost?.PauseState ?? PauseLayerState.Hidden;
        public int Boat2AcceptedCommandCount => _boat2StageHost?.AcceptedCommandCount ?? 0;
        public bool Boat2ArrivalSfxPlaying => _boat2StageHost != null && _boat2StageHost.ArrivalSfxPlaying;
        public bool Boat2IbisSfxPlaying => _boat2StageHost != null && _boat2StageHost.IbisSfxPlaying;
        public Boat2State Boat2PauseCallerState => _boat2StageHost?.PauseCallerState ?? Boat2State.Hidden;
        public Boat2State Boat2DirectLeaveCallerState => _boat2StageHost?.DirectLeaveCallerState ?? Boat2State.Hidden;
        public Boat2PauseReason? Boat2LastPauseReason => _boat2StageHost?.LastPauseReason;
        public Boat2Snapshot Boat2HelpCallerSnapshot => _boat2StageHost == null ? default : _boat2StageHost.DirectHelpSnapshot;
        public Boat2Snapshot Boat2LastHelpRestoreSnapshot => _boat2StageHost == null ? default : _boat2StageHost.LastHelpRestoreSnapshot;
        public AvatarTechnicalOverlayState Boat2RecoveryState => _boat2StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _boat2StageHost.RecoveryState;
        public Guid? LastCompletedBoat2EntryTransactionId => _lastCompletedBoat2EntryTransactionId;
        public Guid? PendingBoat2EntryTransactionId => _pendingBoat2EntryTransactionId;
        public Stage2StageView ActiveStage2View => _stage2StageHost?.View;
        public Stage2HelpView ActiveStage2HelpView => _stage2StageHost?.HelpView;
        public bool Stage2InputReady => _stage2StageHost != null && _stage2StageHost.InputReady;
        public bool Stage2BoundaryLoading => _stage2StageHost != null && _stage2StageHost.BoundaryLoading;
        public Stage2Snapshot Stage2Snapshot => _stage2StageHost == null ? default : _stage2StageHost.Snapshot;
        public Stage2Hotspot Stage2NextUnvisitedHotspot => _stage2StageHost?.NextUnvisitedHotspot ?? Stage2Hotspot.None;
        public bool Stage2VoicePlaying => _stage2StageHost != null && _stage2StageHost.VoicePlaying;
        public bool Stage2LoopsPlaying => _stage2StageHost != null && _stage2StageHost.LoopsPlaying;
        public bool Stage2StoneSfxPlaying => _stage2StageHost != null && _stage2StageHost.StoneSfxPlaying;
        public PauseLayerState Stage2PauseState => _stage2StageHost?.PauseState ?? PauseLayerState.Hidden;
        public int Stage2AcceptedCommandCount => _stage2StageHost?.AcceptedCommandCount ?? 0;
        public Stage2PauseReason? Stage2LastPauseReason => _stage2StageHost?.LastPauseReason;
        public Stage2State Stage2PauseCallerState => _stage2StageHost?.PauseCallerState ?? Stage2State.Hidden;
        public Stage2State Stage2DirectLeaveCallerState => _stage2StageHost?.DirectLeaveCallerState ?? Stage2State.Hidden;
        public AvatarTechnicalOverlayState Stage2RecoveryState => _stage2StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _stage2StageHost.RecoveryState;
        public Guid? LastCompletedStage2EntryTransactionId => _lastCompletedStage2EntryTransactionId;
        public Guid? PendingStage2EntryTransactionId => _pendingStage2EntryTransactionId;
        public int Stage2EntryReadyFrame => _stage2EntryReadyFrame;
        public string Stage2PreparationFailure => _stage2StageHost?.LastPreparationFailure;
        public Boat3StageView ActiveBoat3View => _boat3StageHost?.View;
        public Boat3HelpView ActiveBoat3HelpView => _boat3StageHost?.HelpView;
        public bool Boat3InputReady => _boat3StageHost != null && _boat3StageHost.InputReady;
        public bool Boat3BoundaryLoading => _boat3StageHost != null && _boat3StageHost.BoundaryLoading;
        public Boat3Snapshot Boat3Snapshot => _boat3StageHost == null ? default : _boat3StageHost.Snapshot;
        public PauseLayerState Boat3PauseState => _boat3StageHost?.PauseState ?? PauseLayerState.Hidden;
        public int Boat3AcceptedCommandCount => _boat3StageHost?.AcceptedCommandCount ?? 0;
        public bool Boat3ArrivalSfxPlaying => _boat3StageHost != null && _boat3StageHost.ArrivalSfxPlaying;
        public bool Boat3IbisSfxPlaying => _boat3StageHost != null && _boat3StageHost.IbisSfxPlaying;
        public Boat3State Boat3PauseCallerState => _boat3StageHost?.PauseCallerState ?? Boat3State.Hidden;
        public Boat3State Boat3DirectLeaveCallerState => _boat3StageHost?.DirectLeaveCallerState ?? Boat3State.Hidden;
        public Boat3PauseReason? Boat3LastPauseReason => _boat3StageHost?.LastPauseReason;
        public Boat3Snapshot Boat3HelpCallerSnapshot => _boat3StageHost == null
            ? default : _boat3StageHost.DirectHelpSnapshot;
        public Boat3Snapshot Boat3LastHelpRestoreSnapshot => _boat3StageHost == null
            ? default : _boat3StageHost.LastHelpRestoreSnapshot;
        public AvatarTechnicalOverlayState Boat3RecoveryState => _boat3StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _boat3StageHost.RecoveryState;
        public Guid? LastCompletedBoat3EntryTransactionId => _lastCompletedBoat3EntryTransactionId;
        public Guid? PendingBoat3EntryTransactionId => _pendingBoat3EntryTransactionId;
        public Guid? PendingStage3EntryTransactionId => _pendingStage3EntryTransactionId;
        public Guid? PendingCompletionEntryTransactionId => _pendingCompletionEntryTransactionId;
        public Stage3StageView ActiveStage3View => _stage3StageHost?.View;
        public Stage3HelpView ActiveStage3HelpView => _stage3StageHost?.HelpView;
        public bool Stage3InputReady => _stage3StageHost != null && _stage3StageHost.InputReady;
        public bool Stage3BoundaryLoading => _stage3StageHost != null && _stage3StageHost.BoundaryLoading;
        public Stage3Snapshot Stage3Snapshot => _stage3StageHost == null ? default : _stage3StageHost.Snapshot;
        public PauseLayerState Stage3PauseState => _stage3StageHost?.PauseState ?? PauseLayerState.Hidden;
        public Stage3PauseReason? Stage3LastPauseReason => _stage3StageHost?.LastPauseReason;
        public Stage3State Stage3PauseCallerState => _stage3StageHost?.PauseCallerState ?? Stage3State.Hidden;
        public Stage3State Stage3DirectLeaveCallerState => _stage3StageHost?.DirectLeaveCallerState ?? Stage3State.Hidden;
        public AvatarTechnicalOverlayState Stage3RecoveryState => _stage3StageHost == null
            ? AvatarTechnicalOverlayState.Hidden : _stage3StageHost.RecoveryState;
        public int Stage3AcceptedCommandCount => _stage3StageHost?.AcceptedCommandCount ?? 0;
        public CompletionStageView ActiveCompletionView => _closingStageHost?.CompletionView;
        public AdultGateView ActiveAdultGateView => _closingStageHost?.AdultView;
        public ParentReportView ActiveParentReportView => _closingStageHost?.ParentReportView;
        public CompletionSnapshot ClosingCompletionSnapshot => _closingStageHost == null ? default : _closingStageHost.CompletionSnapshot;
        public AdultGateSnapshot ClosingAdultSnapshot => _closingStageHost == null ? default : _closingStageHost.AdultSnapshot;
        public ParentReportSnapshot ClosingParentReportSnapshot => _closingStageHost == null ? default : _closingStageHost.ParentReportSnapshot;
        public PauseLayerState ClosingPauseState => _closingStageHost?.PauseState ?? PauseLayerState.Hidden;

        public bool PresentClosingControlForDiagnostics(string controlId, double durationSeconds = 0d)
        {
            return _closingStageHost != null && _closingStageHost.HandleSemanticCommand(
                new SemanticInputCommand(controlId, SemanticInputKind.Tap,
                    Vector2.zero, Vector2.zero, Time.unscaledTimeAsDouble, durationSeconds),
                Time.unscaledTimeAsDouble);
        }

        public bool PresentClosingAdultHoldProgressForDiagnostics(double elapsedSeconds) =>
            _closingStageHost != null
            && _closingStageHost.PresentAdultHoldProgressForDiagnostics(elapsedSeconds);

        public bool AdvanceClosingCompletionForDiagnostics(double elapsedSeconds) =>
            _closingStageHost != null
            && _closingStageHost.AdvanceCompletionForDiagnostics(elapsedSeconds);

        public bool PresentClosingInterruptionForDiagnostics() =>
            _closingStageHost != null
            && _closingStageHost.HandleInterruption(Time.unscaledTimeAsDouble);

        public bool PresentWelcomeAdultForDiagnostics()
        {
            if (!WelcomeInputReady || _welcomeController == null) return false;
            return _welcomeController.HandleAdult();
        }
        public bool Stage3VoicePlaying => _stage3StageHost != null && _stage3StageHost.VoicePlaying;
        public bool Stage3LoopsPlaying => _stage3StageHost != null && _stage3StageHost.LoopsPlaying;
        public Guid? LastCompletedStage3EntryTransactionId => _lastCompletedStage3EntryTransactionId;
        public int Stage3EntryReadyFrame => _stage3EntryReadyFrame;
        public string Stage3PreparationFailure => _stage3StageHost?.LastPreparationFailure;

        public void ConfigureStartupIntro(bool enabled)
        {
            startupIntroEnabled = enabled;
        }

        public void ConfigureStartupIntro(
            bool enabled,
            GameObject loadingPrefab,
            IntroStageView introPrefab)
        {
            startupIntroEnabled = enabled;
            startupLoadingPrefab = loadingPrefab;
            introStagePrefab = introPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled,
            GameObject loadingPrefab,
            IntroStageView introPrefab,
            WelcomeStageView welcomePrefab,
            SessionChoiceView sessionPrefab)
        {
            startupIntroEnabled = enabled;
            startupLoadingPrefab = loadingPrefab;
            introStagePrefab = introPrefab;
            welcomeStagePrefab = welcomePrefab;
            sessionChoicePrefab = sessionPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled,
            GameObject loadingPrefab,
            IntroStageView introPrefab,
            WelcomeStageView welcomePrefab,
            SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab,
            AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab);
            avatarSelectPrefab = selectPrefab;
            avatarPreviewPrefab = previewPrefab;
            leaveConfirmationPrefab = leavePrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled,
            GameObject loadingPrefab,
            IntroStageView introPrefab,
            WelcomeStageView welcomePrefab,
            SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab,
            AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab,
            JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab,
            JourneyOverviewPauseHelpView helpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab);
            journeyOverviewPrefab = overviewPrefab;
            pauseOverlayPrefab = pausePrefab;
            journeyPauseHelpPrefab = helpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab);
            boat1Prefab = boatPrefab; boat1HelpPrefab = boatHelpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab,
            Stage1StageView stagePrefab, Stage1HelpView stageHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab,
                boatPrefab, boatHelpPrefab);
            stage1Prefab = stagePrefab;
            stage1HelpPrefab = stageHelpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab,
            Stage1StageView stagePrefab, Stage1HelpView stageHelpPrefab,
            Boat2StageView secondBoatPrefab, Boat2HelpView secondBoatHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab,
                boatPrefab, boatHelpPrefab, stagePrefab, stageHelpPrefab);
            boat2Prefab = secondBoatPrefab;
            boat2HelpPrefab = secondBoatHelpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab,
            Stage1StageView stagePrefab, Stage1HelpView stageHelpPrefab,
            Boat2StageView secondBoatPrefab, Boat2HelpView secondBoatHelpPrefab,
            Stage2StageView secondStagePrefab, Stage2HelpView secondStageHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab,
                boatPrefab, boatHelpPrefab, stagePrefab, stageHelpPrefab, secondBoatPrefab, secondBoatHelpPrefab);
            stage2Prefab = secondStagePrefab;
            stage2HelpPrefab = secondStageHelpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab,
            Stage1StageView stagePrefab, Stage1HelpView stageHelpPrefab,
            Boat2StageView secondBoatPrefab, Boat2HelpView secondBoatHelpPrefab,
            Stage2StageView secondStagePrefab, Stage2HelpView secondStageHelpPrefab,
            Boat3StageView thirdBoatPrefab, Boat3HelpView thirdBoatHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab,
                boatPrefab, boatHelpPrefab, stagePrefab, stageHelpPrefab,
                secondBoatPrefab, secondBoatHelpPrefab, secondStagePrefab, secondStageHelpPrefab);
            boat3Prefab = thirdBoatPrefab;
            boat3HelpPrefab = thirdBoatHelpPrefab;
        }

        public void ConfigureStartupIntro(
            bool enabled, GameObject loadingPrefab, IntroStageView introPrefab,
            WelcomeStageView welcomePrefab, SessionChoiceView sessionPrefab,
            AvatarSelectView selectPrefab, AvatarPreviewView previewPrefab,
            LeaveConfirmationView leavePrefab, JourneyOverviewStageView overviewPrefab,
            PauseOverlayView pausePrefab, JourneyOverviewPauseHelpView journeyHelpPrefab,
            Boat1StageView boatPrefab, Boat1HelpView boatHelpPrefab,
            Stage1StageView stagePrefab, Stage1HelpView stageHelpPrefab,
            Boat2StageView secondBoatPrefab, Boat2HelpView secondBoatHelpPrefab,
            Stage2StageView secondStagePrefab, Stage2HelpView secondStageHelpPrefab,
            Boat3StageView thirdBoatPrefab, Boat3HelpView thirdBoatHelpPrefab,
            Stage3StageView thirdStagePrefab, Stage3HelpView thirdStageHelpPrefab)
        {
            ConfigureStartupIntro(enabled, loadingPrefab, introPrefab, welcomePrefab, sessionPrefab,
                selectPrefab, previewPrefab, leavePrefab, overviewPrefab, pausePrefab, journeyHelpPrefab,
                boatPrefab, boatHelpPrefab, stagePrefab, stageHelpPrefab, secondBoatPrefab, secondBoatHelpPrefab,
                secondStagePrefab, secondStageHelpPrefab, thirdBoatPrefab, thirdBoatHelpPrefab);
            stage3Prefab = thirdStagePrefab;
            stage3HelpPrefab = thirdStageHelpPrefab;
        }

        public void ConfigureAvatarInitialLeftForTests(AvatarChoice initialLeft)
        {
            if (initialLeft != AvatarChoice.Boy && initialLeft != AvatarChoice.Girl)
                throw new ArgumentOutOfRangeException(nameof(initialLeft));
            if (_avatarController != null) throw new InvalidOperationException("Avatar stage is already prepared.");
            _avatarInitialLeftOverride = initialLeft;
        }

        public void ConfigureClosingRoute(
            CompletionStageView completion,
            AdultGateView adultGate,
            ParentReportView parentReport)
        {
            completionPrefab = completion;
            adultGatePrefab = adultGate;
            parentReportPrefab = parentReport;
        }

        public bool TryCompleteAvatarEntryForTests(Guid transactionId)
        {
            return _navigation.Complete(transactionId) == NavigationCompletion.Completed;
        }

        public bool TryCompleteJourneyEntryForTests(Guid transactionId)
        {
            return CompleteJourneyEntry(transactionId);
        }

        public bool TryCompleteBoat1EntryForTests(Guid transactionId) => CompleteBoat1Entry(transactionId);

        public bool TryCompleteStage1EntryForTests(Guid transactionId) => CompleteStage1Entry(transactionId);

        public bool TryCompleteBoat2EntryForTests(Guid transactionId) => CompleteBoat2Entry(transactionId);
        public bool TryCompleteStage2EntryForTests(Guid transactionId) => CompleteStage2Entry(transactionId);
        public bool TryCompleteStage3EntryForTests(Guid transactionId) => CompleteStage3Entry(transactionId);

        public void DeferNextAvatarTransitionForTests()
        {
            _deferNextAvatarTransition = true;
        }

        public bool CompletePendingAvatarTransitionForTests()
        {
            if (!_pendingAvatarTransitionDestination.HasValue) return false;
            var destination = _pendingAvatarTransitionDestination.Value;
            if (destination == RouteId.Welcome)
                return CompletePendingAvatarLeaveTransition();
            if (destination == RouteId.JourneyOverview)
            {
                var transaction = _navigation.ActiveTransaction;
                return transaction != null && CompleteJourneyEntry(transaction.Id);
            }
            if (destination != RouteId.AvatarSelect && destination != RouteId.AvatarPreview) return false;
            return CompletePendingAvatarSurfaceTransition(destination);
        }

        public bool FailActiveAvatarTransitionForTests()
        {
            if (_avatarController == null || _navigation.ActiveTransaction == null) return false;
            if (!_lifecycle.HandleAssetFailure()) return false;
            ShowAvatarRecovery();
            return true;
        }

        public bool HandleAvatarInterruptionForTests()
        {
            return BeginAvatarInterruption();
        }

        public bool ReportJourneyBoundaryAssetFailure()
        {
            return _journeyStageHost != null && _journeyStageHost.HandleBoundaryAssetFailure();
        }

        public bool ReportBoat1BoundaryAssetFailure()
        {
            return _boat1StageHost != null && _boat1StageHost.HandleBoundaryAssetFailure();
        }

        public bool ReportStage1BoundaryAssetFailure() =>
            _stage1StageHost != null && _stage1StageHost.HandleBoundaryAssetFailure();

        public bool ReportBoat2BoundaryAssetFailure() =>
            _boat2StageHost != null && _boat2StageHost.HandleBoundaryAssetFailure();

        public bool ReportBoat3BoundaryAssetFailure() =>
            _boat3StageHost != null && _boat3StageHost.HandleBoundaryAssetFailure();

        public bool ReportBoat3EntryLayoutFailureForDiagnostics()
        {
            if (_boat3StageHost == null || !_boat3StageHost.IsPrepared
                || CurrentRoute != RouteId.Stage2 || !_lifecycle.HandleAssetFailure()) return false;
            _stage2StageHost.ShowRecovery();
            return true;
        }

        public bool ReportBoat2EntryLayoutFailureForDiagnostics()
        {
            if (_boat2StageHost == null || !_boat2StageHost.IsPrepared
                || CurrentRoute != RouteId.Stage1 || !_lifecycle.HandleAssetFailure()) return false;
            _stage1StageHost.ShowRecovery();
            return true;
        }

        public bool TryApplyJourneyLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            return _journeyStageHost != null && _journeyStageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
        }

        public bool TryApplyBoat1Layout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            return _boat1StageHost != null && _boat1StageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
        }

        public bool TryApplyStage1Layout(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels) =>
            _stage1StageHost != null && _stage1StageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimumPixels);

        public bool TryApplyBoat2Layout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels) =>
            _boat2StageHost != null && _boat2StageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);

        public bool TryApplyBoat3Layout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels) =>
            _boat3StageHost != null && _boat3StageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);

        public bool TryApplyStage3Layout(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels) =>
            _stage3StageHost != null && _stage3StageHost.TryApplyResponsiveLayout(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimumPixels);

        public bool EnterAvatarForDiagnostics(AvatarChoice initialLeft)
        {
            if (!WelcomeInputReady || _session.IsOpen || _avatarController != null) return false;
            ConfigureAvatarInitialLeftForTests(initialLeft);
            HandleWelcomePlay();
            return CurrentRoute == RouteId.AvatarSelect && _avatarController != null;
        }

        public bool PresentAvatarPreviewForDiagnostics(AvatarChoice choice)
        {
            if (CurrentRoute != RouteId.AvatarSelect || _avatarController == null) return false;
            HandleAvatarChoice(choice);
            return CurrentRoute == RouteId.AvatarPreview && _avatarController.PreviewChoice == choice;
        }

        public bool PresentAvatarLeaveForDiagnostics()
        {
            if (_avatarController == null ||
                (CurrentRoute != RouteId.AvatarSelect && CurrentRoute != RouteId.AvatarPreview))
                return false;
            HandleAvatarHome();
            return _leaveController != null && _leaveController.IsVisible;
        }

        public bool PresentAvatarRecoveryForDiagnostics()
        {
            if (CurrentRoute != RouteId.AvatarSelect || _avatarController == null) return false;
            DeferNextAvatarTransitionForTests();
            HandleAvatarChoice(AvatarChoice.Boy);
            return FailActiveAvatarTransitionForTests() &&
                   AvatarRecoveryState == AvatarTechnicalOverlayState.Recovery;
        }

        public bool PresentAvatarSessionResumeForDiagnostics()
        {
            if (CurrentRoute != RouteId.AvatarPreview || _avatarController == null) return false;
            var previewToken = _avatarController.CurrentPreviewToken;
            HandleAvatarHome();
            if (_leaveController == null || !_leaveController.IsVisible) return false;
            HandleLeaveToWelcome();
            if (CurrentRoute != RouteId.Welcome) return false;
            HandleWelcomePlay();
            HandleSessionContinue();
            return CurrentRoute == RouteId.AvatarPreview &&
                   _avatarController.CurrentPreviewToken == previewToken;
        }

        public bool EnterJourneyForDiagnostics()
        {
            if (CurrentRoute != RouteId.AvatarPreview || _avatarController == null) return false;
            HandleAvatarConfirm();
            return CurrentRoute == RouteId.JourneyOverview
                && _journeyStageHost != null && _journeyStageHost.IsEntered;
        }

        public bool PresentJourneyPauseForDiagnostics()
        {
            return _journeyStageHost != null && _journeyStageHost.HandleSemanticCommand(
                JourneyOverviewStageView.PauseControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentJourneyHelpForDiagnostics()
        {
            if (_journeyStageHost == null) return false;
            if (_journeyStageHost.PauseState == PauseLayerState.Hidden
                && !_journeyStageHost.HandleSemanticCommand(
                    JourneyOverviewStageView.PauseControlId, Time.unscaledTimeAsDouble))
                return false;
            return _journeyStageHost.HandleSemanticCommand(
                PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentJourneyLeaveForDiagnostics()
        {
            if (_journeyStageHost == null
                || _journeyStageHost.PauseState != PauseLayerState.Hidden
                || !_journeyStageHost.HandleSemanticCommand(
                    JourneyOverviewStageView.PauseControlId, Time.unscaledTimeAsDouble))
                return false;
            return _journeyStageHost.HandleSemanticCommand(
                PauseOverlayView.HomeControlId, Time.unscaledTimeAsDouble);
        }

        public void DeferNextJourneyBoundaryForTests()
        {
            _deferNextJourneyBoundary = true;
        }

        public bool EnterBoat1ForDiagnostics()
        {
            if (CurrentRoute != RouteId.JourneyOverview || _journeyStageHost == null) return false;
            return _journeyStageHost.HandleSemanticCommand(
                       JourneyOverviewStageView.SkipControlId, Time.unscaledTimeAsDouble)
                   && CurrentRoute == RouteId.Boat1
                   && _boat1StageHost != null && _boat1StageHost.IsEntered;
        }

        public bool PresentBoat1ChoiceForDiagnostics(Boat1Choice choice)
        {
            if (_boat1StageHost == null || CurrentRoute != RouteId.Boat1) return false;
            var control = choice == Boat1Choice.Reeds ? Boat1StageView.ReedsControlId
                : choice == Boat1Choice.Village ? Boat1StageView.VillageControlId
                : choice == Boat1Choice.FollowIbis ? Boat1StageView.FollowIbisControlId
                : null;
            return control != null && _boat1StageHost.HandleSemanticCommand(
                control, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1HelpForDiagnostics()
        {
            return _boat1StageHost != null && _boat1StageHost.HandleSemanticCommand(
                Boat1StageView.HelpControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1PauseForDiagnostics()
        {
            return _boat1StageHost != null && _boat1StageHost.HandleSemanticCommand(
                Boat1StageView.PauseControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1PauseHelpForDiagnostics()
        {
            if (!PresentBoat1PauseForDiagnostics()) return false;
            return _boat1StageHost.HandleSemanticCommand(
                PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1PauseHelpBackForDiagnostics()
        {
            if (_boat1StageHost == null || _boat1StageHost.PauseState != PauseLayerState.Pause)
                return false;
            return _boat1StageHost.HandleSemanticCommand(
                       PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble)
                   && _boat1StageHost.HandleSemanticCommand(
                       Boat1HelpView.BackControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1LeaveForDiagnostics(bool nested)
        {
            if (_boat1StageHost == null) return false;
            if (nested && !PresentBoat1PauseForDiagnostics()) return false;
            return _boat1StageHost.HandleSemanticCommand(
                nested ? PauseOverlayView.HomeControlId : Boat1StageView.HomeControlId,
                Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat1BoundaryForDiagnostics()
        {
            return _boat1StageHost != null && _boat1StageHost.HandleSemanticCommand(
                Boat1StageView.SkipControlId, Time.unscaledTimeAsDouble);
        }

        public bool CompleteBoat1ArrivalForDiagnostics()
        {
            return _boat1StageHost != null && _boat1StageHost.CompleteArrivalForDiagnostics();
        }

        public bool CompleteStage1OutcomeForDiagnostics()
        {
            return _stage1StageHost != null && _stage1StageHost.CompleteOutcomeForDiagnostics();
        }

        public bool PresentBoat2ChoiceForDiagnostics(Boat2Choice choice)
        {
            if (_boat2StageHost == null || CurrentRoute != RouteId.Boat2) return false;
            var control = choice == Boat2Choice.FieldBank ? Boat2StageView.FieldBankControlId
                : choice == Boat2Choice.Dock ? Boat2StageView.DockControlId
                : choice == Boat2Choice.FollowIbis ? Boat2StageView.FollowIbisControlId : null;
            return control != null && _boat2StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat2HelpForDiagnostics() => _boat2StageHost != null
            && _boat2StageHost.HandleSemanticCommand(Boat2StageView.HelpControlId, Time.unscaledTimeAsDouble);

        public bool PresentBoat2PauseForDiagnostics() => _boat2StageHost != null
            && _boat2StageHost.HandleSemanticCommand(Boat2StageView.PauseControlId, Time.unscaledTimeAsDouble);

        public bool PresentBoat2PauseHelpForDiagnostics()
        {
            if (!PresentBoat2PauseForDiagnostics()) return false;
            return _boat2StageHost.HandleSemanticCommand(
                PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat2PauseHelpBackForDiagnostics()
        {
            if (_boat2StageHost == null || _boat2StageHost.PauseState != PauseLayerState.Pause)
                return false;
            return _boat2StageHost.HandleSemanticCommand(
                       PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble)
                   && _boat2StageHost.HandleSemanticCommand(
                       Boat2HelpView.BackControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat2InterruptionForDiagnostics(Boat2PauseReason reason) =>
            _boat2StageHost != null && _boat2StageHost.HandleInterruption(
                Time.unscaledTimeAsDouble, reason);

        public bool PresentBoat2LeaveForDiagnostics(bool nested)
        {
            if (_boat2StageHost == null) return false;
            if (nested && !PresentBoat2PauseForDiagnostics()) return false;
            return _boat2StageHost.HandleSemanticCommand(
                nested ? PauseOverlayView.HomeControlId : Boat2StageView.HomeControlId,
                Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat2BoundaryForDiagnostics() => _boat2StageHost != null
            && _boat2StageHost.HandleSemanticCommand(Boat2StageView.SkipControlId, Time.unscaledTimeAsDouble);

        public bool CompleteBoat2ArrivalForDiagnostics() => _boat2StageHost != null
            && _boat2StageHost.CompleteArrivalForDiagnostics();

        public bool PresentBoat3BoundaryForDiagnostics() => _boat3StageHost != null
            && _boat3StageHost.HandleSemanticCommand(Boat3StageView.SkipControlId, Time.unscaledTimeAsDouble);

        public bool CompleteBoat3ArrivalForDiagnostics() => _boat3StageHost != null
            && _boat3StageHost.CompleteArrivalForDiagnostics();

        public bool PresentBoat3ChoiceForDiagnostics(Boat3Choice choice)
        {
            if (_boat3StageHost == null || CurrentRoute != RouteId.Boat3) return false;
            var control = choice == Boat3Choice.GrainStore ? Boat3StageView.GrainStoreControlId
                : choice == Boat3Choice.Irrigation ? Boat3StageView.IrrigationControlId
                : choice == Boat3Choice.FollowIbis ? Boat3StageView.FollowIbisControlId : null;
            return control != null && _boat3StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat3HelpForDiagnostics() => _boat3StageHost != null
            && _boat3StageHost.HandleSemanticCommand(Boat3StageView.HelpControlId, Time.unscaledTimeAsDouble);

        public bool PresentBoat3PauseForDiagnostics() => _boat3StageHost != null
            && _boat3StageHost.HandleSemanticCommand(Boat3StageView.PauseControlId, Time.unscaledTimeAsDouble);

        public bool PresentBoat3PauseHelpForDiagnostics()
        {
            if (!PresentBoat3PauseForDiagnostics()) return false;
            return _boat3StageHost.HandleSemanticCommand(PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat3PauseHelpBackForDiagnostics()
        {
            if (_boat3StageHost == null || _boat3StageHost.PauseState != PauseLayerState.Pause) return false;
            return _boat3StageHost.HandleSemanticCommand(PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble)
                && _boat3StageHost.HandleSemanticCommand(Boat3HelpView.BackControlId, Time.unscaledTimeAsDouble);
        }

        public bool PresentBoat3InterruptionForDiagnostics(Boat3PauseReason reason) =>
            _boat3StageHost != null && _boat3StageHost.HandleInterruption(Time.unscaledTimeAsDouble, reason);

        public bool PresentBoat3LeaveForDiagnostics(bool nested)
        {
            if (_boat3StageHost == null) return false;
            if (nested && !PresentBoat3PauseForDiagnostics()) return false;
            return _boat3StageHost.HandleSemanticCommand(
                nested ? PauseOverlayView.HomeControlId : Boat3StageView.HomeControlId,
                Time.unscaledTimeAsDouble);
        }

        public bool PresentStage3PlayForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.PlayControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage3PlanForDiagnostics(Stage3Plan plan)
        {
            if (_stage3StageHost == null) return false;
            var control = plan == Stage3Plan.Grain ? Stage3StageView.GrainControlId
                : plan == Stage3Plan.Channel ? Stage3StageView.ChannelControlId
                : plan == Stage3Plan.Boat ? Stage3StageView.BoatPlanControlId : null;
            return control != null && _stage3StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool PresentStage3RiverForDiagnostics(Stage2River river)
        {
            if (_stage3StageHost == null) return false;
            var control = river == Stage2River.Low ? Stage3StageView.LowControlId
                : river == Stage2River.Middle ? Stage3StageView.MiddleControlId
                : river == Stage2River.High ? Stage3StageView.HighControlId : null;
            return control != null && _stage3StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool CompleteStage3OutcomeForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.CompleteOutcomeForDiagnostics();
        public bool AdvanceStage3OutcomeForDiagnostics(double elapsed) => _stage3StageHost != null
            && _stage3StageHost.AdvanceOutcomeForDiagnostics(elapsed);
        public bool PresentStage3RepeatForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.RepeatControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3WatchAgainForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.WatchAgainControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3AnotherPlanForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.AnotherPlanControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3AnotherRiverForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.AnotherRiverControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3RiverBackForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.RiverBackControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3FinishForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.FinishControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3HelpForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.HelpControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3PauseForDiagnostics() => _stage3StageHost != null
            && _stage3StageHost.HandleSemanticCommand(Stage3StageView.PauseControlId, Time.unscaledTimeAsDouble);
        public bool PresentStage3PauseHelpForDiagnostics()
        {
            if (!PresentStage3PauseForDiagnostics()) return false;
            return _stage3StageHost.HandleSemanticCommand(PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);
        }
        public bool PresentStage3LeaveForDiagnostics(bool nested)
        {
            if (_stage3StageHost == null) return false;
            if (nested && !PresentStage3PauseForDiagnostics()) return false;
            return _stage3StageHost.HandleSemanticCommand(
                nested ? PauseOverlayView.HomeControlId : Stage3StageView.HomeControlId,
                Time.unscaledTimeAsDouble);
        }
        public bool PresentStage3InterruptionForDiagnostics(Stage3PauseReason reason) =>
            _stage3StageHost != null && _stage3StageHost.HandleInterruption(Time.unscaledTimeAsDouble, reason);
        public bool ReportStage3BoundaryAssetFailure() => _stage3StageHost != null
            && _stage3StageHost.HandleBoundaryAssetFailure();
        public bool HandleStage3RecoveryActionForDiagnostics(string controlId)
        {
            var now = Time.unscaledTimeAsDouble;
            if (CurrentRoute == RouteId.Stage3 && _stage3StageHost != null)
                return _stage3StageHost.HandleSemanticCommand(controlId, now);
            if (CurrentRoute == RouteId.Boat3 && _boat3StageHost != null)
                return _boat3StageHost.HandleSemanticCommand(controlId, now);
            return false;
        }

        public bool PresentStage2PlayForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(Stage2StageView.PlayControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2RiverForDiagnostics(Stage2River river)
        {
            if (_stage2StageHost == null) return false;
            var control = river == Stage2River.Low ? Stage2StageView.LowControlId
                : river == Stage2River.Middle ? Stage2StageView.MiddleControlId
                : river == Stage2River.High ? Stage2StageView.HighControlId : null;
            return control != null && _stage2StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool PresentStage2HotspotForDiagnostics(Stage2Hotspot hotspot)
        {
            if (_stage2StageHost == null) return false;
            var control = hotspot == Stage2Hotspot.Field ? Stage2StageView.FieldControlId
                : hotspot == Stage2Hotspot.Path ? Stage2StageView.PathControlId
                : hotspot == Stage2Hotspot.Boat ? Stage2StageView.BoatControlId : null;
            return control != null && _stage2StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool CompleteStage2ConsequenceForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.CompleteConsequenceForDiagnostics();

        public bool AdvanceStage2ConsequenceForDiagnostics(double elapsedSeconds) =>
            _stage2StageHost != null
            && _stage2StageHost.AdvanceConsequenceForDiagnostics(elapsedSeconds);

        public bool PresentStage2RepeatForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(
                Stage2StageView.RepeatControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2CompareForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(
                Stage2StageView.CompareControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2HelpForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(
                Stage2StageView.HelpControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2PauseForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(
                Stage2StageView.PauseControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2PauseHelpForDiagnostics() => PresentStage2PauseForDiagnostics()
            && _stage2StageHost.HandleSemanticCommand(
                PauseOverlayView.HelpControlId, Time.unscaledTimeAsDouble);

        public bool PresentStage2LeaveForDiagnostics(bool nested)
        {
            if (_stage2StageHost == null) return false;
            if (nested && !PresentStage2PauseForDiagnostics()) return false;
            var control = nested ? PauseOverlayView.HomeControlId : Stage2StageView.HomeControlId;
            return _stage2StageHost.HandleSemanticCommand(control, Time.unscaledTimeAsDouble);
        }

        public bool PresentStage2InterruptionForDiagnostics(Stage2PauseReason reason) =>
            _stage2StageHost != null
            && _stage2StageHost.HandleInterruption(Time.unscaledTimeAsDouble, reason);

        public bool PresentStage2FinishForDiagnostics() => _stage2StageHost != null
            && _stage2StageHost.HandleSemanticCommand(Stage2StageView.FinishControlId, Time.unscaledTimeAsDouble);

        public bool ReportStage2BoundaryAssetFailure() => _stage2StageHost != null
            && _stage2StageHost.HandleBoundaryAssetFailure();

        public bool TryApplyStage2Layout(float sw, float sh, float safeX, float safeY,
            float safeW, float safeH, float ppi, int minimum) => _stage2StageHost != null
            && _stage2StageHost.TryApplyResponsiveLayout(sw, sh, safeX, safeY, safeW, safeH, ppi, minimum);

        public bool ReportStage2EntryLayoutFailureForDiagnostics()
        {
            if (_stage2StageHost == null || !_stage2StageHost.IsPrepared
                || CurrentRoute != RouteId.Boat2 || !_lifecycle.HandleAssetFailure()) return false;
            _boat2StageHost.ShowRecovery();
            return true;
        }

        public bool HandleStage2RecoveryActionForDiagnostics(string controlId)
        {
            var now = Time.unscaledTimeAsDouble;
            if (CurrentRoute == RouteId.Stage2 && _stage2StageHost != null)
                return _stage2StageHost.HandleSemanticCommand(controlId, now);
            if (CurrentRoute == RouteId.Boat2 && _boat2StageHost != null)
                return _boat2StageHost.HandleSemanticCommand(controlId, now);
            return false;
        }

        public bool PresentStage1ControlForDiagnostics(string controlId) =>
            _stage1StageHost != null && _stage1StageHost.HandleSemanticCommand(
                controlId, Time.unscaledTimeAsDouble);

        public bool AttachStage1MarkerForDiagnostics(bool drag) =>
            _stage1StageHost != null && _stage1StageHost.AttachForDiagnostics(
                drag, Time.unscaledTimeAsDouble);

        public bool MoveStage1MarkerForDiagnostics(double normalizedPosition) =>
            _stage1StageHost != null && _stage1StageHost.MoveForDiagnostics(
                normalizedPosition, Time.unscaledTimeAsDouble);

        public bool PresentStage1InterruptionForDiagnostics(Stage1PauseReason reason) =>
            _stage1StageHost != null && _stage1StageHost.HandleInterruption(
                Time.unscaledTimeAsDouble, reason);

        public bool ReportStage1EntryLayoutFailureForDiagnostics()
        {
            if (_stage1StageHost == null || !_stage1StageHost.IsPrepared
                || CurrentRoute != RouteId.Boat1 || !_lifecycle.HandleAssetFailure())
                return false;
            _boat1StageHost.ShowRecovery();
            return true;
        }

        public bool HandleStage1RecoveryActionForDiagnostics(string controlId)
        {
            var now = Time.unscaledTimeAsDouble;
            if (CurrentRoute == RouteId.Stage1 && _stage1StageHost != null)
                return _stage1StageHost.HandleRecoveryActionForDiagnostics(controlId, now);
            if (CurrentRoute == RouteId.Boat1 && _boat1StageHost != null)
                return HandleBoat1SemanticCommand(controlId, now);
            return false;
        }

        public bool PresentBoat1InterruptionForDiagnostics(Boat1PauseReason reason)
        {
            return _boat1StageHost != null && _boat1StageHost.HandleInterruption(
                Time.unscaledTimeAsDouble, reason);
        }

        public bool PresentJourneyBoundaryForDiagnostics()
        {
            return _journeyStageHost != null && _journeyStageHost.HandleSemanticCommand(
                JourneyOverviewStageView.SkipControlId, Time.unscaledTimeAsDouble);
        }

        public void HandleIntroInterruptionForTests()
        {
            BeginInterruption();
        }

        public bool ResumeIntroAfterMappedUserActionForTests()
        {
            return ResumeIntroAfterMappedUserAction();
        }

        public bool SkipIntroForTests()
        {
            if (_introController?.CurrentPlaybackToken == null) return false;
            var accepted = _introController.HandleSkip(
                _introController.CurrentPlaybackToken.Value,
                Time.unscaledTimeAsDouble);
            if (accepted) EnterWelcome();
            return accepted;
        }

        public bool FailIntroForTests()
        {
            if (_introController?.CurrentPlaybackToken == null) return false;
            var accepted = _introController.HandlePlaybackFailed(
                _introController.CurrentPlaybackToken.Value,
                Time.unscaledTimeAsDouble);
            if (accepted) EnterWelcome();
            return accepted;
        }

        public bool HandleStaleIntroCompletionForTests(Guid tokenId)
        {
            var accepted = _introController != null &&
                _introController.HandlePlaybackCompleted(tokenId, Time.unscaledTimeAsDouble);
            if (accepted) EnterWelcome();
            return accepted;
        }

        private void Awake()
        {
            _startupRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            Debug.Log("Foundation ScreenHost initialization started.");
            _accessibilityStore = new AccessibilityPreferenceStore();
            var preferences = _accessibilityStore.Load();
            _accessibilityState = new AccessibilityState(preferences.AlwaysReduce, preferences.CaptionsEnabled);
            var commandLineArguments = Environment.GetCommandLineArgs();
            if (IntroEvidenceOptions.TryGetCapture(commandLineArguments, out var evidenceCapture) &&
                evidenceCapture.CaptionsEnabled)
            {
                _accessibilityState.SetCaptionsEnabled(true);
            }
            IReduceMotionAdapter reduceMotionAdapter;
            if (IntroEvidenceOptions.TryGetReducedMotion(commandLineArguments, out var evidenceReducedMotion))
            {
                var fixedAdapter = gameObject.AddComponent<FixedReduceMotionAdapter>();
                fixedAdapter.Configure(evidenceReducedMotion);
                reduceMotionAdapter = fixedAdapter;
            }
            else if (AvatarEvidenceOptions.TryGetCapture(
                commandLineArguments, out var avatarEvidenceOptions))
            {
                var fixedAdapter = gameObject.AddComponent<FixedReduceMotionAdapter>();
                fixedAdapter.Configure(avatarEvidenceOptions.ReducedMotion);
                reduceMotionAdapter = fixedAdapter;
            }
            else
            {
#if UNITY_IOS && !UNITY_EDITOR
                reduceMotionAdapter = gameObject.AddComponent<IOSReduceMotionAdapter>();
#else
                reduceMotionAdapter = gameObject.AddComponent<AndroidReduceMotionAdapter>();
#endif
            }
            _accessibilityCoordinator = new AccessibilityCoordinator(_accessibilityState, reduceMotionAdapter);
            _accessibilityCoordinator.Register(this);

            _localization = new LocalizationCoordinator(
                new UnityStringTableResolver());

            _inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("FoundationChild");
            _pointAction = map.AddAction(
                "FoundationPoint",
                InputActionType.PassThrough,
                expectedControlLayout: "Vector2");
            _pointAction.AddBinding("<Mouse>/position");
            _pointAction.AddBinding("<Touchscreen>/primaryTouch/position");
            _pointAction.AddBinding("<Pen>/position");
            _pressAction = map.AddAction(
                "FoundationPress",
                InputActionType.Button,
                expectedControlLayout: "Button");
            _pressAction.AddBinding("<Mouse>/leftButton");
            _pressAction.AddBinding("<Touchscreen>/primaryTouch/press");
            _pressAction.AddBinding("<Pen>/tip");
            _inputActions.AddActionMap(map);
            _inputRouter = new ChildInputRouter(_pointAction, _pressAction);
            _inputRouter.SemanticCommandIssued += HandleSemanticCommand;

            _navigation = new NavigationAuthority(RouteId.StartupLoading);
            _session = new SessionState();
            _recoveryPresenter = new RecoveryPresenter(_inputRouter);
            _audioBackend = new UnityAudioBackend(gameObject);
            _audioDirector = new AudioDirector(_audioBackend);
            _mediaCoordinator = new MediaPlaybackCoordinator(
                _audioBackend,
                new ISynchronizedMediaTrack[] { _audioBackend });
            _audioConfigurationMonitor = gameObject.AddComponent<AudioConfigurationMonitor>();
            _audioConfigurationMonitor.Configure(_mediaCoordinator);
            _audioConfigurationMonitor.OutputRouteChanged += HandleAudioFocusInterruption;
            _lifecycle = new AppLifecycleCoordinator(
                _navigation,
                _recoveryPresenter,
                new IAppLifecycleParticipant[] { _mediaCoordinator, _audioDirector });

            BuildDiagnosticSurface();
            if (startupIntroEnabled)
            {
                if (startupLoadingPrefab == null || introStagePrefab == null ||
                    welcomeStagePrefab == null || sessionChoicePrefab == null ||
                    avatarSelectPrefab == null || avatarPreviewPrefab == null ||
                    leaveConfirmationPrefab == null || journeyOverviewPrefab == null ||
                    pauseOverlayPrefab == null || journeyPauseHelpPrefab == null ||
                    boat1Prefab == null || boat1HelpPrefab == null ||
                    stage1Prefab == null || stage1HelpPrefab == null ||
                    boat2Prefab == null || boat2HelpPrefab == null ||
                    stage2Prefab == null || stage2HelpPrefab == null ||
                    boat3Prefab == null || boat3HelpPrefab == null ||
                    stage3Prefab == null || stage3HelpPrefab == null ||
                    completionPrefab == null || adultGatePrefab == null ||
                    parentReportPrefab == null)
                {
                    throw new InvalidOperationException(
                        "Startup through Parent Report screen prefabs are missing.");
                }

                _diagnosticCanvas.SetActive(false);
                _startupLoadingInstance = Instantiate(startupLoadingPrefab, transform, false);
                ConfigureStartupLoadingArtwork();
            }
            _journeyStageHost = GetComponent<JourneyOverviewStageHost>();
            if (_journeyStageHost == null)
                _journeyStageHost = gameObject.AddComponent<JourneyOverviewStageHost>();
            if (journeyOverviewPrefab != null && pauseOverlayPrefab != null
                && journeyPauseHelpPrefab != null)
            {
                _journeyStageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore,
                    _lifecycle, _recoveryPresenter,
                    journeyOverviewPrefab, pauseOverlayPrefab, journeyPauseHelpPrefab);
                _journeyStageHost.WelcomeRequested += CompleteJourneyLeaveToWelcome;
                _journeyStageHost.BoundaryRequested += BeginJourneyBoundary;
            }
            _boat1StageHost = GetComponent<Boat1StageHost>();
            if (_boat1StageHost == null) _boat1StageHost = gameObject.AddComponent<Boat1StageHost>();
            if (boat1Prefab != null && boat1HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _boat1StageHost.Configure(_inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, boat1Prefab, pauseOverlayPrefab, boat1HelpPrefab);
                _boat1StageHost.WelcomeRequested += CompleteBoat1LeaveToWelcome;
                _boat1StageHost.BoundaryRequested += BeginBoat1Boundary;
            }
            _stage1StageHost = GetComponent<Stage1StageHost>();
            if (_stage1StageHost == null) _stage1StageHost = gameObject.AddComponent<Stage1StageHost>();
            if (stage1Prefab != null && stage1HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _stage1StageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, stage1Prefab, pauseOverlayPrefab, stage1HelpPrefab);
                _stage1StageHost.WelcomeRequested += CompleteStage1LeaveToWelcome;
                _stage1StageHost.BoundaryRequested += BeginStage1Boundary;
            }
            _boat2StageHost = GetComponent<Boat2StageHost>();
            if (_boat2StageHost == null) _boat2StageHost = gameObject.AddComponent<Boat2StageHost>();
            if (boat2Prefab != null && boat2HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _boat2StageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, boat2Prefab, pauseOverlayPrefab, boat2HelpPrefab);
                _boat2StageHost.WelcomeRequested += CompleteBoat2LeaveToWelcome;
                _boat2StageHost.BoundaryRequested += BeginBoat2Boundary;
            }
            _stage2StageHost = GetComponent<Stage2StageHost>();
            if (_stage2StageHost == null) _stage2StageHost = gameObject.AddComponent<Stage2StageHost>();
            if (stage2Prefab != null && stage2HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _stage2StageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, stage2Prefab, pauseOverlayPrefab, stage2HelpPrefab);
                _stage2StageHost.WelcomeRequested += CompleteStage2LeaveToWelcome;
                _stage2StageHost.BoundaryRequested += BeginStage2Boundary;
            }
            _boat3StageHost = GetComponent<Boat3StageHost>();
            if (_boat3StageHost == null) _boat3StageHost = gameObject.AddComponent<Boat3StageHost>();
            if (boat3Prefab != null && boat3HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _boat3StageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, boat3Prefab, pauseOverlayPrefab, boat3HelpPrefab);
                _boat3StageHost.WelcomeRequested += CompleteBoat3LeaveToWelcome;
                _boat3StageHost.BoundaryRequested += BeginBoat3Boundary;
            }
            _stage3StageHost = GetComponent<Stage3StageHost>();
            if (_stage3StageHost == null) _stage3StageHost = gameObject.AddComponent<Stage3StageHost>();
            if (stage3Prefab != null && stage3HelpPrefab != null && pauseOverlayPrefab != null)
            {
                _stage3StageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore, _lifecycle,
                    _recoveryPresenter, stage3Prefab, pauseOverlayPrefab, stage3HelpPrefab);
                _stage3StageHost.WelcomeRequested += CompleteStage3LeaveToWelcome;
                _stage3StageHost.BoundaryRequested += BeginStage3Boundary;
            }
            _closingStageHost = GetComponent<CompletionAdultReportStageHost>();
            if (_closingStageHost == null)
                _closingStageHost = gameObject.AddComponent<CompletionAdultReportStageHost>();
            if (completionPrefab != null && adultGatePrefab != null && parentReportPrefab != null
                && pauseOverlayPrefab != null && leaveConfirmationPrefab != null)
            {
                _closingStageHost.Configure(
                    _inputRouter, _session, _navigation, _audioDirector, _audioBackend,
                    _accessibilityState, _accessibilityCoordinator, _accessibilityStore,
                    completionPrefab, adultGatePrefab, parentReportPrefab,
                    pauseOverlayPrefab, leaveConfirmationPrefab);
                _closingStageHost.WelcomeRequested += CompleteClosingRouteToWelcome;
                _closingStageHost.WelcomeReturnRequested += RestoreWelcomeFromAdultArea;
                _closingStageHost.ConfirmedResetRequested += RestartAfterConfirmedReset;
            }
            gameObject.AddComponent<FoundationPerformanceProbe>();
            gameObject.AddComponent<FoundationEvidenceCapture>();
            gameObject.AddComponent<IntroEvidenceCapture>();
            gameObject.AddComponent<WelcomeEvidenceCapture>();
            gameObject.AddComponent<AvatarEvidenceCapture>();
            gameObject.AddComponent<JourneyOverviewEvidenceCapture>();
            gameObject.AddComponent<Boat1EvidenceCapture>();
            gameObject.AddComponent<Stage1EvidenceCapture>();
            gameObject.AddComponent<Boat2EvidenceCapture>();
            gameObject.AddComponent<Stage2EvidenceCapture>();
            gameObject.AddComponent<Boat3EvidenceCapture>();
            gameObject.AddComponent<Stage3EvidenceCapture>();
            gameObject.AddComponent<CompletionAdultReportEvidenceCapture>();
            gameObject.AddComponent<ReleaseCandidateEvidenceCapture>();
            ConfigureControlAvailability(normal: true);
            _inputRouter.SetVisibleStateReady(true);
            _inputRouter.Enable();
            Debug.Log("Foundation ScreenHost ready.");
        }

        private async void Start()
        {
            if (!startupIntroEnabled)
            {
                await _accessibilityCoordinator.PreflightIntroAsync(AccessibilityCoordinator.MaximumPreflight);
                return;
            }

            if (ReleaseCandidateEvidenceCapture.ShouldHoldStartupLoading(
                    Environment.GetCommandLineArgs()))
                return;

            await StartStartupIntroAsync();
        }

        private void Update()
        {
            _audioBackend?.Advance(Time.unscaledDeltaTime);
            var now = Time.unscaledTimeAsDouble;
            if (startupIntroEnabled &&
                (CurrentRoute == RouteId.StartupLoading || CurrentRoute == RouteId.IntroVideo))
            {
                if (_introPlayback != null && _introView != null && CurrentRoute == RouteId.IntroVideo)
                {
                    _introView.BindVideoTexture(_introPlayback.VideoTexture);
                }

                var transaction = _navigation?.ActiveTransaction;
                if (transaction != null &&
                    now - transaction.StartedAtSeconds >= NavigationAuthority.TimeoutSeconds &&
                    _introController?.CurrentPlaybackToken != null)
                {
                    _introController.HandlePlaybackFailed(_introController.CurrentPlaybackToken.Value, now);
                }

                return;
            }

            if (_pendingStage1EntryTransactionId.HasValue
                && _stage1EntryReadyFrame >= 0
                && Time.frameCount >= _stage1EntryReadyFrame)
            {
                CompleteStage1Entry(_pendingStage1EntryTransactionId.Value);
            }

            if (_pendingBoat2EntryTransactionId.HasValue
                && _boat2EntryReadyFrame >= 0
                && Time.frameCount >= _boat2EntryReadyFrame)
            {
                CompleteBoat2Entry(_pendingBoat2EntryTransactionId.Value);
            }

            if (_pendingStage2EntryTransactionId.HasValue
                && _stage2EntryReadyFrame >= 0
                && Time.frameCount >= _stage2EntryReadyFrame)
            {
                CompleteStage2Entry(_pendingStage2EntryTransactionId.Value);
            }

            if (_pendingBoat3EntryTransactionId.HasValue
                && _boat3EntryReadyFrame >= 0
                && Time.frameCount >= _boat3EntryReadyFrame)
            {
                CompleteBoat3Entry(_pendingBoat3EntryTransactionId.Value);
            }

            if (_pendingStage3EntryTransactionId.HasValue
                && _stage3EntryReadyFrame >= 0
                && Time.frameCount >= _stage3EntryReadyFrame)
            {
                CompleteStage3Entry(_pendingStage3EntryTransactionId.Value);
            }

            if (_pendingCompletionEntryTransactionId.HasValue
                && _completionEntryReadyFrame >= 0
                && Time.frameCount >= _completionEntryReadyFrame)
            {
                CompleteCompletionEntry(_pendingCompletionEntryTransactionId.Value);
            }

            if (_closingStageHost != null && _closingStageHost.IsActive
                && (CurrentRoute == RouteId.Completion || CurrentRoute == RouteId.AdultGate
                    || CurrentRoute == RouteId.ParentReport))
            {
                _closingStageHost.Tick(now);
                return;
            }

            if (_stage3StageHost != null && _stage3StageHost.IsEntered && CurrentRoute == RouteId.Stage3)
            {
                _stage3StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_boat3StageHost != null && _boat3StageHost.IsEntered && CurrentRoute == RouteId.Boat3)
            {
                _boat3StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_stage2StageHost != null && _stage2StageHost.IsEntered && CurrentRoute == RouteId.Stage2)
            {
                _stage2StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_stage1StageHost != null && _stage1StageHost.IsEntered && CurrentRoute == RouteId.Stage1)
            {
                _stage1StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_boat2StageHost != null && _boat2StageHost.IsEntered && CurrentRoute == RouteId.Boat2)
            {
                _boat2StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_boat1StageHost != null && _boat1StageHost.IsEntered && CurrentRoute == RouteId.Boat1)
            {
                _boat1StageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_journeyStageHost != null && _journeyStageHost.IsEntered
                && CurrentRoute == RouteId.JourneyOverview)
            {
                _journeyStageHost.Tick(now, Time.unscaledDeltaTime);
                return;
            }

            if (_avatarController != null &&
                (CurrentRoute == RouteId.AvatarSelect || CurrentRoute == RouteId.AvatarPreview))
            {
                if (_lifecycle.Tick(now))
                {
                    ShowAvatarRecovery();
                    return;
                }

                if (!_avatarMotionPaused &&
                    _leaveConfirmationView.TechnicalOverlayState == AvatarTechnicalOverlayState.Hidden)
                    AdvanceAvatarPresentation(now);
                if (_avatarInputReadyFrame >= 0 && Time.frameCount >= _avatarInputReadyFrame)
                {
                    _avatarInputReadyFrame = -1;
                    if (!_avatarBoundaryLoading) _inputRouter.SetVisibleStateReady(true);
                }
                return;
            }

            if (_welcomeEntered && CurrentRoute == RouteId.Welcome)
            {
                if (_lifecycle.Tick(now))
                {
                    ShowWelcomeRecovery();
                    return;
                }

                _welcomeMedia?.Advance(now);
                if (_welcomeInputReadyFrame >= 0 && Time.frameCount >= _welcomeInputReadyFrame)
                {
                    _welcomeInputReadyFrame = -1;
                    _inputRouter.SetVisibleStateReady(true);
                }

                return;
            }

            if (_lifecycle.Tick(now))
            {
                ShowRecovery();
            }

            if (_navigation.ActiveTransaction != null && now >= _autoCompleteAt)
            {
                var id = _navigation.ActiveTransaction.Id;
                if (_navigation.Complete(id) == NavigationCompletion.Completed)
                {
                    _autoCompleteAt = double.PositiveInfinity;
                    _recoveryPresenter.CompleteLoadingAndShowContent();
                    ConfigureControlAvailability(normal: true);
                    SetState(FoundationStringIds.HarnessReady, new Color32(39, 120, 92, 255));
                }
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                BeginInterruption(JourneyOverviewPauseReason.AppBackground);
            }
            else
            {
                RefreshAccessibilityFromOs();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) return;
            _lifecycle?.HandleFocusRestored();
            RefreshAccessibilityFromOs();
        }

        private void OnDestroy()
        {
            if (_introPlayback != null)
            {
                _lastPreparedIntroFrameCount = _introPlayback.LastPreparedFrameCount;
                _lastPresentedIntroFrameIndex = _introPlayback.LastPresentedFrameIndex;
                _lastIntroCompletionEventCount = _introPlayback.CompletionEventCount;
                _introPlayback.Prepared -= HandleIntroPrepared;
                _introPlayback.Completed -= HandleIntroCompleted;
                _introPlayback.Failed -= HandleIntroFailed;
                _introPlayback.FramePresented -= HandleIntroFrame;
            }

            if (_audioConfigurationMonitor != null)
            {
                _audioConfigurationMonitor.OutputRouteChanged -= HandleAudioFocusInterruption;
            }
            if (_journeyStageHost != null)
            {
                _journeyStageHost.WelcomeRequested -= CompleteJourneyLeaveToWelcome;
                _journeyStageHost.BoundaryRequested -= BeginJourneyBoundary;
                _journeyStageHost.Cleanup();
            }
            if (_boat1StageHost != null)
            {
                _boat1StageHost.WelcomeRequested -= CompleteBoat1LeaveToWelcome;
                _boat1StageHost.BoundaryRequested -= BeginBoat1Boundary;
                _boat1StageHost.Cleanup();
            }
            if (_stage1StageHost != null)
            {
                _stage1StageHost.WelcomeRequested -= CompleteStage1LeaveToWelcome;
                _stage1StageHost.BoundaryRequested -= BeginStage1Boundary;
                _stage1StageHost.Cleanup();
            }
            if (_boat2StageHost != null)
            {
                _boat2StageHost.WelcomeRequested -= CompleteBoat2LeaveToWelcome;
                _boat2StageHost.BoundaryRequested -= BeginBoat2Boundary;
                _boat2StageHost.Cleanup();
            }
            if (_stage2StageHost != null)
            {
                _stage2StageHost.WelcomeRequested -= CompleteStage2LeaveToWelcome;
                _stage2StageHost.BoundaryRequested -= BeginStage2Boundary;
                _stage2StageHost.Cleanup();
            }
            if (_boat3StageHost != null)
            {
                _boat3StageHost.WelcomeRequested -= CompleteBoat3LeaveToWelcome;
                _boat3StageHost.BoundaryRequested -= BeginBoat3Boundary;
                _boat3StageHost.Cleanup();
            }
            if (_stage3StageHost != null)
            {
                _stage3StageHost.WelcomeRequested -= CompleteStage3LeaveToWelcome;
                _stage3StageHost.BoundaryRequested -= BeginStage3Boundary;
                _stage3StageHost.Cleanup();
            }
            if (_closingStageHost != null)
            {
                _closingStageHost.WelcomeRequested -= CompleteClosingRouteToWelcome;
                _closingStageHost.WelcomeReturnRequested -= RestoreWelcomeFromAdultArea;
                _closingStageHost.ConfirmedResetRequested -= RestartAfterConfirmedReset;
                _closingStageHost.CleanupAll();
            }
            if (_inputRouter != null)
            {
                _inputRouter.SemanticCommandIssued -= HandleSemanticCommand;
                _inputRouter.Dispose();
            }

            if (_inputActions != null) Destroy(_inputActions);
            _accessibilityCoordinator?.Dispose();
            _audioBackend?.Dispose();
        }

        public void ApplyReducedMotion(bool reducedMotion)
        {
            ReducedMotionAppliedToSurface = reducedMotion;
            _journeyStageHost?.ApplyReducedMotion(reducedMotion);
        }

        public bool ReducedMotionAppliedToSurface { get; private set; }

        public Rect GetControlBounds(string controlId)
        {
            if (!_controls.TryGetValue(controlId, out var view))
            {
                throw new KeyNotFoundException(controlId);
            }

            return ToScreenRect(view.Target);
        }

        public Rect GetControlCanvasRect(string controlId)
        {
            if (!_controls.TryGetValue(controlId, out var view))
            {
                throw new KeyNotFoundException(controlId);
            }

            var size = view.Target.sizeDelta;
            return new Rect(view.Target.anchoredPosition - size * 0.5f, size);
        }

        public bool IsControlEnabled(string controlId)
        {
            return _controls.TryGetValue(controlId, out var view) && view.Semantic.Enabled;
        }

        public void RefreshControlGeometry()
        {
            Canvas.ForceUpdateCanvases();
        }

        public bool AreTouchTargetsTransparent()
        {
            foreach (var view in _controls.Values)
            {
                if (view.TargetImage.color.a != 0f || view.TargetImage.raycastTarget) return false;
            }

            return true;
        }

        public bool DoTouchTargetsOverlap()
        {
            var views = new List<ControlView>(_controls.Values);
            for (var first = 0; first < views.Count; first++)
            {
                for (var second = first + 1; second < views.Count; second++)
                {
                    if (ToScreenRect(views[first].Target).Overlaps(ToScreenRect(views[second].Target))) return true;
                }
            }

            return false;
        }

        public bool DoesEveryTargetContainVisibleArt()
        {
            foreach (var view in _controls.Values)
            {
                var target = ToScreenRect(view.Target);
                var visible = ToScreenRect(view.VisibleArt);
                if (visible.xMin < target.xMin || visible.xMax > target.xMax ||
                    visible.yMin < target.yMin || visible.yMax > target.yMax) return false;
            }

            return true;
        }

        public void ForceNavigationTimeoutForTests()
        {
            if (_navigation.ActiveTransaction == null) throw new InvalidOperationException("No active navigation probe.");
            _lifecycle.Tick(_navigation.ActiveTransaction.StartedAtSeconds + NavigationAuthority.TimeoutSeconds);
            if (_stage3StageHost != null && _stage3StageHost.IsEntered
                && CurrentRoute == RouteId.Stage3)
                _stage3StageHost.ShowRecovery();
            else if (_boat3StageHost != null && _boat3StageHost.IsEntered
                && CurrentRoute == RouteId.Boat3)
                _boat3StageHost.ShowRecovery();
            else if (_stage1StageHost != null && _stage1StageHost.IsEntered
                && CurrentRoute == RouteId.Stage1)
                _stage1StageHost.ShowRecovery();
            else if (_boat2StageHost != null && _boat2StageHost.IsEntered
                && CurrentRoute == RouteId.Boat2)
                _boat2StageHost.ShowRecovery();
            else if (_stage2StageHost != null && _stage2StageHost.IsEntered
                && CurrentRoute == RouteId.Stage2)
                _stage2StageHost.ShowRecovery();
            else if (_boat1StageHost != null && _boat1StageHost.IsEntered
                && CurrentRoute == RouteId.Boat1)
                _boat1StageHost.ShowRecovery();
            else if (_journeyStageHost != null && _journeyStageHost.IsEntered
                && CurrentRoute == RouteId.JourneyOverview)
                _journeyStageHost.ShowRecovery();
            else if (_avatarController != null &&
                (CurrentRoute == RouteId.AvatarSelect || CurrentRoute == RouteId.AvatarPreview))
                ShowAvatarRecovery();
            else if (_welcomeEntered && CurrentRoute == RouteId.Welcome) ShowWelcomeRecovery();
            else ShowRecovery();
        }

        public bool TryCompleteNavigation(Guid transactionId)
        {
            return _navigation.Complete(transactionId) == NavigationCompletion.Completed;
        }

        public void StartSynchronizedMediaForTests(string semanticId)
        {
            _mediaCoordinator.Start(semanticId);
        }

        public void HandleAudioRouteChangeForTests()
        {
            _audioConfigurationMonitor.HandleConfigurationChanged(deviceWasChanged: true);
        }

        public void HandleFocusRestoredForTests()
        {
            _lifecycle.HandleFocusRestored();
        }

        public bool HandleWelcomeInterruptionForTests()
        {
            return BeginWelcomeInterruption();
        }

        public bool ResumeWelcomeAfterMappedUserActionForTests()
        {
            return ResumeWelcomeAfterMappedUserAction();
        }

        public void ConfigureWelcomeEvidenceForDiagnostics(
            bool soundEnabled,
            bool captionsEnabled,
            bool reducedMotion)
        {
            if (!_welcomeEntered || _welcomeController == null || _welcomeView == null)
                throw new InvalidOperationException("Welcome is not ready for evidence configuration.");

            if (_session.SoundEnabled != soundEnabled) HandleWelcomeSound();
            if (_accessibilityState.CaptionsEnabled != captionsEnabled) HandleWelcomeCaptions();
            var preferenceChanged = _accessibilityState.AlwaysReduce != reducedMotion;
            _accessibilityState.SetOsState(reducedMotion
                ? ReduceMotionOsState.Reduced
                : ReduceMotionOsState.NotReduced);
            _accessibilityCoordinator.SetAlwaysReduce(reducedMotion);
            if (preferenceChanged)
            {
                _accessibilityStore.Save(new AccessibilityPreferences(
                    _accessibilityState.AlwaysReduce,
                    _accessibilityState.CaptionsEnabled));
            }
            _welcomeView.SetReducedMotionEnabled(_accessibilityState.EffectiveReducedMotion);
        }

        public bool PresentSessionChoiceForDiagnostics()
        {
            if (!_welcomeEntered || _welcomeController == null || _session == null ||
                _sessionChoiceView == null || !_welcomeView.NormalControlsEnabled)
                return false;

            if (!_session.IsOpen && !_session.Begin(RouteId.AvatarSelect)) return false;
            HandleWelcomePlay();
            return _sessionChoiceView.IsVisible &&
                   _welcomePresentation == WelcomePresentationState.SessionChoice;
        }

        private void BuildDiagnosticSurface()
        {
            var canvasObject = new GameObject("FoundationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _diagnosticCanvas = canvasObject;
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ResponsiveLayoutProfile.ReferenceWidth, ResponsiveLayoutProfile.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreateImage("Background", canvasObject.transform, new Color32(20, 39, 55, 255));
            Stretch(background.rectTransform);

            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            Stretch(safeRoot);
            var safeArea = canvasObject.AddComponent<SafeAreaController>();
            safeArea.Configure(safeRoot, Vector4.zero);

            _statePanel = CreateImage("State", safeRoot, new Color32(39, 120, 92, 255));
            SetRect(_statePanel.rectTransform, new Vector2(0f, 365f), new Vector2(980f, 150f));
            _header = CreateText("Header", _statePanel.transform, _localization.Resolve(FoundationStringIds.HarnessReady));
            Stretch(_header.rectTransform);

            var ids = new[]
            {
                InputProbeId, RouteProbeId, RetryId, HomeId, ReducedMotionId,
                PseudolocalizationId, AudioProbeId, InterruptionId, ResumeId
            };
            var colors = new[]
            {
                new Color32(66, 174, 213, 255), new Color32(237, 176, 63, 255),
                new Color32(211, 98, 74, 255), new Color32(103, 176, 116, 255),
                new Color32(136, 113, 202, 255), new Color32(226, 116, 174, 255),
                new Color32(71, 161, 181, 255), new Color32(229, 121, 68, 255),
                new Color32(99, 190, 166, 255)
            };

            for (var index = 0; index < ids.Length; index++)
            {
                var row = index / 5;
                var column = index % 5;
                var x = (column - 2f) * 318f;
                var y = 90f - row * 318f;
                AddControl(ids[index], safeRoot, new Vector2(x, y), colors[index]);
            }
        }

        private void AddControl(string id, Transform parent, Vector2 position, Color visibleColor)
        {
            var targetImage = CreateImage(id + ".TouchArea", parent, Color.clear);
            targetImage.raycastTarget = false;
            SetRect(targetImage.rectTransform, position, new Vector2(ControlSize, ControlSize));

            var visibleImage = CreateImage(id + ".VisibleArt", targetImage.transform, visibleColor);
            visibleImage.raycastTarget = false;
            SetRect(visibleImage.rectTransform, Vector2.zero, new Vector2(VisibleSize, VisibleSize));

            var semantic = new SemanticControl(id, () => ToScreenRect(targetImage.rectTransform));
            _inputRouter.Register(semantic);
            _controls.Add(id, new ControlView(targetImage.rectTransform, visibleImage.rectTransform, targetImage, semantic));
        }

        private void HandleSemanticCommand(SemanticInputCommand command)
        {
            LastAcceptedControlId = command.ControlId;
            if (_closingStageHost != null && _closingStageHost.IsActive
                && _closingStageHost.OwnsControl(command.ControlId))
            {
                var accepted = _closingStageHost.HandleSemanticCommand(
                    command, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId)
                    _audioInterrupted = false;
                return;
            }
            if (_stage3StageHost != null && _stage3StageHost.IsEntered && _stage3StageHost.OwnsControl(command.ControlId))
            {
                var accepted = _stage3StageHost.HandleSemanticCommand(command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == LeaveConfirmationView.RetryControlId)
                {
                    _pendingCompletionEntryTransactionId = null;
                    _completionEntryReadyFrame = -1;
                    BeginStage3Boundary();
                }
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_boat3StageHost != null && _boat3StageHost.IsEntered && _boat3StageHost.OwnsControl(command.ControlId))
            {
                var accepted = _boat3StageHost.HandleSemanticCommand(command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == LeaveConfirmationView.RetryControlId)
                {
                    _pendingStage3EntryTransactionId = null;
                    BeginBoat3Boundary();
                }
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_stage2StageHost != null && _stage2StageHost.IsEntered && _stage2StageHost.OwnsControl(command.ControlId))
            {
                var accepted = _stage2StageHost.HandleSemanticCommand(command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_stage1StageHost != null && _stage1StageHost.IsEntered && _stage1StageHost.OwnsControl(command.ControlId))
            {
                var accepted = _stage1StageHost.HandleSemanticCommand(command, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == LeaveConfirmationView.RetryControlId)
                    BeginStage1Boundary();
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_boat2StageHost != null && _boat2StageHost.IsEntered && _boat2StageHost.OwnsControl(command.ControlId))
            {
                var accepted = _boat2StageHost.HandleSemanticCommand(command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_boat1StageHost != null && _boat1StageHost.IsEntered && _boat1StageHost.OwnsControl(command.ControlId))
            {
                var accepted = HandleBoat1SemanticCommand(command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId) _audioInterrupted = false;
                return;
            }
            if (_journeyStageHost != null && _journeyStageHost.IsEntered
                && _journeyStageHost.OwnsControl(command.ControlId))
            {
                var accepted = _journeyStageHost.HandleSemanticCommand(
                    command.ControlId, Time.unscaledTimeAsDouble);
                if (accepted && command.ControlId == PauseOverlayView.ResumeControlId)
                    _audioInterrupted = false;
                return;
            }
            switch (command.ControlId)
            {
                case InputProbeId:
                    AcceptedInputCount++;
                    break;
                case RouteProbeId:
                    BeginRouteProbe();
                    break;
                case RetryId:
                    RetryRecovery();
                    break;
                case HomeId:
                    GoHomeFromRecovery();
                    break;
                case ReducedMotionId:
                    _accessibilityCoordinator.SetAlwaysReduce(!_accessibilityState.AlwaysReduce);
                    _accessibilityStore.Save(new AccessibilityPreferences(
                        _accessibilityState.AlwaysReduce,
                        _accessibilityState.CaptionsEnabled));
                    break;
                case PseudolocalizationId:
                    _pseudo = !_pseudo;
                    SetState(FoundationStringIds.HarnessReady, _statePanel.color);
                    break;
                case AudioProbeId:
                    AudioProbeStarted = _audioDirector.TryStart(AudioBus.Sfx, FoundationStringIds.AudioTest, Time.unscaledTimeAsDouble) != null;
                    break;
                case InterruptionId:
                    BeginInterruption();
                    break;
                case ResumeId:
                    ResumeAfterInterruption();
                    break;
                case IntroFirstFramePresentation.SkipControlId:
                    if (_introController?.CurrentPlaybackToken != null)
                    {
                        if (_introController.HandleSkip(
                            _introController.CurrentPlaybackToken.Value,
                            Time.unscaledTimeAsDouble))
                        {
                            EnterWelcome();
                        }
                    }
                    break;
                case IntroStageView.ResumeControlId:
                    ResumeIntroAfterMappedUserAction();
                    break;
                case WelcomeStageView.PlayControlId:
                    HandleWelcomePlay();
                    break;
                case WelcomeStageView.AdultControlId:
                    _welcomeController?.HandleAdult();
                    break;
                case WelcomeStageView.SoundControlId:
                    HandleWelcomeSound();
                    break;
                case WelcomeStageView.CaptionsControlId:
                    HandleWelcomeCaptions();
                    break;
                case WelcomeStageView.MotionControlId:
                    HandleWelcomeMotion();
                    break;
                case SessionChoiceView.ContinueControlId:
                    HandleSessionContinue();
                    break;
                case SessionChoiceView.RestartControlId:
                    _sessionChoiceController?.HandleRestart();
                    break;
                case SessionChoiceView.BackControlId:
                    if (_sessionChoiceController != null && _sessionChoiceController.HandleBack())
                    {
                        _welcomePresentation = WelcomePresentationState.Welcome;
                        PresentWelcomeMedia();
                    }
                    break;
                case WelcomeStageView.ResumeControlId:
                    ResumeWelcomeAfterMappedUserAction();
                    break;
                case WelcomeStageView.RetryControlId:
                    RetryRecovery();
                    break;
                case WelcomeStageView.HomeControlId:
                    GoHomeFromRecovery();
                    break;
                case AvatarSelectView.BoyControlId:
                    HandleAvatarChoice(AvatarChoice.Boy);
                    break;
                case AvatarSelectView.GirlControlId:
                    HandleAvatarChoice(AvatarChoice.Girl);
                    break;
                case AvatarSelectView.HomeControlId:
                case AvatarPreviewView.HomeControlId:
                    HandleAvatarHome();
                    break;
                case AvatarPreviewView.RepeatControlId:
                    HandleAvatarRepeat();
                    break;
                case AvatarPreviewView.OtherControlId:
                    HandleAvatarOther();
                    break;
                case AvatarPreviewView.ConfirmControlId:
                    HandleAvatarConfirm();
                    break;
                case LeaveConfirmationView.StayControlId:
                    HandleLeaveStay();
                    break;
                case LeaveConfirmationView.CancelControlId:
                    HandleLeaveCancel();
                    break;
                case LeaveConfirmationView.LeaveControlId:
                    HandleLeaveToWelcome();
                    break;
                case LeaveConfirmationView.RetryControlId:
                    RetryAvatarRecovery();
                    break;
                case LeaveConfirmationView.HomeControlId:
                    GoHomeFromAvatarRecovery();
                    break;
                case LeaveConfirmationView.ResumeControlId:
                    ResumeAvatarAfterMappedUserAction();
                    break;
            }
        }

        private async System.Threading.Tasks.Task StartStartupIntroAsync()
        {
            _inputRouter.SetVisibleStateReady(false);
            _inputRouter.ClearControls();
            if (_diagnosticCanvas != null) _diagnosticCanvas.SetActive(false);

            var content = StartupIntroContent.LoadApproved();
            _introView = Instantiate(introStagePrefab, transform, false);
            _introView.Configure(
                _inputRouter,
                content,
                _localization.Resolve(FoundationStringIds.IntroVoiceCaption));
            _introView.SetCaptionsEnabled(_accessibilityState.CaptionsEnabled);
            _introPlayback = gameObject.AddComponent<UnityIntroPlayback>();
            _introPlayback.Configure(content);
            _introPlayback.Prepared += HandleIntroPrepared;
            _introPlayback.Completed += HandleIntroCompleted;
            _introPlayback.Failed += HandleIntroFailed;
            _introPlayback.FramePresented += HandleIntroFrame;
            _introController = new IntroStageController(
                _navigation,
                _accessibilityCoordinator,
                _introPlayback,
                _introView);

            await _introController.BeginAsync(
                Mathf.Max(1, Screen.width),
                Mathf.Max(1, Screen.height),
                preferLowResolution: IntroDeviceTier.ShouldPreferLowResolutionOnCurrentDevice(),
                nowSeconds: Time.unscaledTimeAsDouble);
        }

        private void HandleIntroPrepared(Guid tokenId, IntroPreparedMedia media)
        {
            if (_startupLoadingInstance != null) _startupLoadingInstance.SetActive(false);
            _introView.BindVideoTexture(_introPlayback.VideoTexture);
            if (_introController.HandlePrepared(tokenId, media, Time.unscaledTimeAsDouble) &&
                IntroFirstChildFrameElapsedSeconds < 0d)
            {
                IntroFirstChildFrameElapsedSeconds =
                    Time.realtimeSinceStartupAsDouble - _startupRealtimeSeconds;
            }
        }

        private void HandleIntroCompleted(Guid tokenId)
        {
            if (_introController.HandlePlaybackCompleted(tokenId, Time.unscaledTimeAsDouble))
            {
                EnterWelcome();
            }
        }

        private void HandleIntroFailed(Guid tokenId, string message)
        {
            Debug.LogWarning("Intro media failed safely: " + message);
            if (_startupLoadingInstance != null) _startupLoadingInstance.SetActive(false);
            if (_introController.HandlePlaybackFailed(tokenId, Time.unscaledTimeAsDouble))
            {
                EnterWelcome();
            }
        }

        private void HandleIntroFrame(Guid tokenId, long frameIndex, double mediaTimeSeconds)
        {
            if (_introController?.CurrentPlaybackToken != tokenId) return;
            _introView.BindVideoTexture(_introPlayback.VideoTexture);
            _introView.UpdateSynchronizedLayers(tokenId, mediaTimeSeconds);
        }

        public bool TryRequest(AdultGateRequest request)
        {
            if (request.ReturnRoute != RouteId.Welcome)
                return _closingStageHost != null && _closingStageHost.TryRequest(request);
            if (_pendingAdultGateRequest.HasValue || !_welcomeEntered || CurrentRoute != RouteId.Welcome)
            {
                return false;
            }

            _pendingAdultGateRequest = request;
            _inputRouter.SetVisibleStateReady(false);
            _welcomeMedia?.SetSoundEnabled(false);
            if (_closingStageHost != null && _closingStageHost.TryRequest(request))
            {
                _welcomeView?.SuspendForAdultGate();
                return true;
            }
            _pendingAdultGateRequest = null;
            PresentWelcomeMedia();
            _inputRouter.SetVisibleStateReady(true);
            return false;
        }

        private void EnterWelcome()
        {
            if (_welcomeEntered || CurrentRoute != RouteId.Welcome) return;
            _welcomeEntered = true;
            _inputRouter.SetVisibleStateReady(false);
            if (_avatarController == null) _inputRouter.ClearControls();
            CleanupIntroPresentation();

            var content = WelcomeContent.LoadApproved();
            _sessionChoiceView = Instantiate(sessionChoicePrefab, transform, false);
            _sessionChoiceView.Configure(_inputRouter, content);
            _welcomeView = Instantiate(welcomeStagePrefab, transform, false);
            _welcomeView.Configure(_inputRouter, content, _sessionChoiceView);
            ApplyWelcomeLayout();
            _welcomeController = new WelcomeController(
                _session,
                _navigation,
                _welcomeView,
                this,
                _accessibilityState,
                _accessibilityStore);
            _sessionChoiceController = new SessionChoiceController(
                _session,
                _navigation,
                _welcomeView,
                this);
            _welcomeMedia = new WelcomeMediaPresenter(
                _audioDirector,
                _audioBackend,
                content,
                _welcomeView);
            _welcomeView.SetSoundEnabled(_session.SoundEnabled);
            _welcomeView.SetCaptionsEnabled(_accessibilityState.CaptionsEnabled);
            _welcomeView.SetReducedMotionEnabled(_accessibilityState.EffectiveReducedMotion);
            _welcomePresentation = WelcomePresentationState.Welcome;
            PresentWelcomeMedia();
            _inputRouter.SetVisibleStateReady(false);
            _welcomeInputReadyFrame = Time.frameCount + 1;
        }

        private void ApplyWelcomeLayout()
        {
            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var safe = Screen.safeArea;
            var safeWidth = Mathf.Max(1f, safe.width / scale);
            var safeHeight = Mathf.Max(1f, safe.height / scale);
            var ppi = Screen.dpi > 0f ? Screen.dpi : 326f;
            const int conservativePlatformMinimumPixels = 88;
            _welcomeView.ApplyResponsiveLayout(
                screenWidth,
                screenHeight,
                safeWidth,
                safeHeight,
                ppi,
                conservativePlatformMinimumPixels);
            _sessionChoiceView.ApplyResponsiveLayout(
                screenWidth,
                screenHeight,
                safeWidth,
                safeHeight,
                ppi,
                conservativePlatformMinimumPixels);
        }

        private void CleanupIntroPresentation()
        {
            if (_introPlayback != null)
            {
                _lastPreparedIntroFrameCount = _introPlayback.LastPreparedFrameCount;
                _lastPresentedIntroFrameIndex = _introPlayback.LastPresentedFrameIndex;
                _lastIntroCompletionEventCount = _introPlayback.CompletionEventCount;
                _introPlayback.Prepared -= HandleIntroPrepared;
                _introPlayback.Completed -= HandleIntroCompleted;
                _introPlayback.Failed -= HandleIntroFailed;
                _introPlayback.FramePresented -= HandleIntroFrame;
                Destroy(_introPlayback);
                _introPlayback = null;
            }

            if (_introView != null)
            {
                Destroy(_introView.gameObject);
                _introView = null;
            }

            if (_startupLoadingInstance != null)
            {
                Destroy(_startupLoadingInstance);
                _startupLoadingInstance = null;
            }
        }

        private void HandleWelcomePlay()
        {
            if (_welcomeController == null) return;
            var hadOpenSession = _session.IsOpen;
            if (!_welcomeController.HandlePlay(Time.unscaledTimeAsDouble)) return;
            if (hadOpenSession)
            {
                _welcomePresentation = WelcomePresentationState.SessionChoice;
                PresentWelcomeMedia();
                return;
            }

            EnterOrResumeAvatarFromWelcome();
        }

        private void HandleSessionContinue()
        {
            if (_sessionChoiceController != null &&
                _sessionChoiceController.HandleContinue(Time.unscaledTimeAsDouble))
            {
                if (_navigation.ActiveTransaction != null &&
                    (_navigation.ActiveTransaction.Destination == RouteId.AvatarSelect ||
                     _navigation.ActiveTransaction.Destination == RouteId.AvatarPreview))
                    EnterOrResumeAvatarFromWelcome();
                else
                    SuspendWelcomeAtLaterStageBoundary();
            }
        }

        private void HandleWelcomeSound()
        {
            if (_welcomeController == null) return;
            var enabled = _welcomeController.HandleSound();
            _welcomeView.SetSoundEnabled(enabled);
            if (enabled) PresentWelcomeMedia();
            else _welcomeMedia.SetSoundEnabled(false);
        }

        private void HandleWelcomeCaptions()
        {
            if (_welcomeController == null) return;
            var enabled = _welcomeController.HandleCaptions();
            _welcomeView.SetCaptionsEnabled(enabled);
            PresentWelcomeMedia();
        }

        private void HandleWelcomeMotion()
        {
            if (_welcomeController == null) return;
            _welcomeController.HandleMotion();
            _welcomeView.SetReducedMotionEnabled(_accessibilityState.EffectiveReducedMotion);
        }

        private void PresentWelcomeMedia()
        {
            _welcomeMedia?.Present(
                _welcomePresentation,
                _session.SoundEnabled,
                _accessibilityState.CaptionsEnabled,
                Time.unscaledTimeAsDouble);
        }

        private void SuspendWelcomeAtLaterStageBoundary()
        {
            _inputRouter.SetVisibleStateReady(false);
            _welcomeMedia?.SetSoundEnabled(false);
            _welcomeView?.ShowTechnicalOverlay(WelcomeTechnicalOverlayState.Loading);
        }

        private void EnterOrResumeAvatarFromWelcome()
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Welcome ||
                (transaction.Destination != RouteId.AvatarSelect &&
                 transaction.Destination != RouteId.AvatarPreview))
                return;

            _inputRouter.SetVisibleStateReady(false);
            CleanupWelcomePresentation();
            if (_avatarController == null) PrepareAvatarStage();
            if (_navigation.Complete(transaction.Id) != NavigationCompletion.Completed) return;
            _lastCompletedAvatarEntryTransactionId = transaction.Id;
            if (_session.CurrentOpenSessionLocation != transaction.Destination)
                _session.MoveTo(transaction.Destination);

            _leaveConfirmationView.CompleteTransitionAndHide();
            _avatarBoundaryLoading = false;
            if (transaction.Destination == RouteId.AvatarSelect)
            {
                _avatarPreviewView.SetVisible(false);
                if (_avatarController.PresentationState == AvatarPresentationState.None)
                    _avatarController.EnterSelect();
                else
                    _avatarSelectView.SetVisible(true);
                PresentAvatarMedia(AvatarMediaState.Select);
            }
            else
            {
                _avatarSelectView.SetVisible(false);
                _avatarPreviewView.Show(
                    _avatarController.PreviewChoice.Value,
                    _avatarController.CurrentPreviewToken);
                BeginAvatarPreviewMotion();
                PresentAvatarMedia(AvatarMediaState.Preview);
            }
            ArmAvatarInputAfterVisibleFrame();
        }

        private void PrepareAvatarStage()
        {
            var content = AvatarContent.LoadApproved();
            _avatarSelectView = Instantiate(avatarSelectPrefab, transform, false);
            _avatarSelectView.Configure(_inputRouter, content);
            _avatarPreviewView = Instantiate(avatarPreviewPrefab, transform, false);
            _avatarPreviewView.Configure(_inputRouter, content);
            _leaveConfirmationView = Instantiate(leaveConfirmationPrefab, transform, false);
            _leaveConfirmationView.Configure(_inputRouter, content);
            _leaveConfirmationView.BindCallers(_avatarSelectView, _avatarPreviewView);
            ApplyAvatarLayout();
            _avatarSelectView.SetVisible(false);
            _avatarPreviewView.SetVisible(false);

            var view = new AvatarStageMediaSink(
                _avatarSelectView, _avatarPreviewView, _leaveConfirmationView);
            _leaveController = new LeaveConfirmationController(_session, _navigation, _leaveConfirmationView);
            _avatarController = new AvatarChoiceController(
                _session,
                _navigation,
                view,
                _leaveController,
                new HostAvatarOrderSource(() =>
                {
                    if (_avatarInitialLeftOverride.HasValue) return _avatarInitialLeftOverride.Value;
                    return UnityEngine.Random.value < 0.5f ? AvatarChoice.Boy : AvatarChoice.Girl;
                }));
            _avatarMotion = new AvatarMotionPresenter(content);
            _avatarMedia = new AvatarMediaPresenter(_audioDirector, _audioBackend, content, view);
        }

        private void ApplyAvatarLayout()
        {
            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var safe = Screen.safeArea;
            var safeWidth = Mathf.Max(1f, safe.width / scale);
            var safeHeight = Mathf.Max(1f, safe.height / scale);
            var ppi = Screen.dpi > 0f ? Screen.dpi : 326f;
            const int conservativePlatformMinimumPixels = 88;
            _avatarSelectView.ApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, conservativePlatformMinimumPixels);
            _avatarPreviewView.ApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, conservativePlatformMinimumPixels);
            _leaveConfirmationView.ApplyResponsiveLayout(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, conservativePlatformMinimumPixels);
        }

        private void CleanupWelcomePresentation()
        {
            if (!_welcomeEntered) return;
            _welcomeMedia?.SetSoundEnabled(false);
            if (_welcomeView != null)
            {
                foreach (var id in _welcomeView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _welcomeView.TechnicalControlIds) _inputRouter.Unregister(id);
                _welcomeView.gameObject.SetActive(false);
                Destroy(_welcomeView.gameObject);
            }
            if (_sessionChoiceView != null)
            {
                foreach (var id in _sessionChoiceView.ControlIds) _inputRouter.Unregister(id);
                _sessionChoiceView.gameObject.SetActive(false);
                Destroy(_sessionChoiceView.gameObject);
            }
            _welcomeView = null;
            _sessionChoiceView = null;
            _welcomeController = null;
            _sessionChoiceController = null;
            _welcomeMedia = null;
            _welcomeEntered = false;
            _welcomeInputReadyFrame = -1;
        }

        private void HandleAvatarChoice(AvatarChoice choice)
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Press, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_avatarController.HandleChoice(choice, Time.unscaledTimeAsDouble)) return;
            CompleteAvatarSurfaceTransition(RouteId.AvatarPreview);
        }

        private void HandleAvatarOther()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Back, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_avatarController.HandleOther(Time.unscaledTimeAsDouble)) return;
            CompleteAvatarSurfaceTransition(RouteId.AvatarSelect);
        }

        private void CompleteAvatarSurfaceTransition(RouteId destination)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != destination) return;
            _inputRouter.SetVisibleStateReady(false);
            _pendingAvatarTransitionDestination = destination;
            if (_deferNextAvatarTransition)
            {
                _deferNextAvatarTransition = false;
                _avatarBoundaryLoading = true;
                _recoveryPresenter.BeginLoading();
                _leaveConfirmationView.ShowTechnicalOverlay(CurrentRoute, AvatarTechnicalOverlayState.Loading);
                return;
            }

            CompletePendingAvatarSurfaceTransition(destination);
        }

        private bool CompletePendingAvatarSurfaceTransition(RouteId destination)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != destination) return false;
            if (_navigation.Complete(transaction.Id) != NavigationCompletion.Completed) return false;
            _session.MoveTo(destination);
            if (_leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Hidden)
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _avatarBoundaryLoading = false;
            _avatarMotionPaused = false;
            _pendingAvatarTransitionDestination = null;
            if (destination == RouteId.AvatarPreview)
            {
                _avatarController.EnterPreview();
                BeginAvatarPreviewMotion();
                PresentAvatarMedia(AvatarMediaState.Preview);
            }
            else
            {
                _avatarController.EnterSelect();
                PresentAvatarMedia(AvatarMediaState.Select);
            }
            ArmAvatarInputAfterVisibleFrame();
            return true;
        }

        private void HandleAvatarRepeat()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Press, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_avatarController.HandleRepeat()) return;
            BeginAvatarPreviewMotion();
            PresentAvatarMedia(AvatarMediaState.Preview);
        }

        private void HandleAvatarConfirm()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Confirm, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_avatarController.HandleConfirm(Time.unscaledTimeAsDouble)) return;
            _avatarBoundaryLoading = true;
            _pendingAvatarTransitionDestination = RouteId.JourneyOverview;
            _recoveryPresenter.BeginLoading();
            _avatarPreviewView.SetControlsEnabled(false);
            _inputRouter.SetVisibleStateReady(false);
            _avatarMedia?.Present(
                AvatarMediaState.Preview, false, _accessibilityState.CaptionsEnabled,
                Time.unscaledTimeAsDouble);
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.JourneyOverview) return;
            if (_deferNextAvatarTransition)
            {
                _deferNextAvatarTransition = false;
                _leaveConfirmationView.ShowTechnicalOverlay(
                    CurrentRoute, AvatarTechnicalOverlayState.Loading);
                return;
            }
            CompleteJourneyEntry(transaction.Id);
        }

        private bool CompleteJourneyEntry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.AvatarPreview
                || transaction.Destination != RouteId.JourneyOverview)
                return false;

            if (!_journeyStageHost.Prepare(
                    transactionId, _leaveController, _leaveConfirmationView))
            {
                if (_lifecycle.HandleAssetFailure()) ShowAvatarRecovery();
                return false;
            }

            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed)
            {
                _journeyStageHost.Cleanup();
                return false;
            }
            if (_session.CurrentOpenSessionLocation != RouteId.JourneyOverview)
                _session.MoveTo(RouteId.JourneyOverview);
            CleanupAvatarOwnedPresentation();
            _avatarBoundaryLoading = false;
            _pendingAvatarTransitionDestination = null;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            if (!_journeyStageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Journey Overview failed after its route completed.");
            _lastCompletedJourneyEntryTransactionId = transactionId;
            return true;
        }

        private void CleanupAvatarOwnedPresentation()
        {
            _avatarMedia?.Stop();
            if (_avatarSelectView != null)
            {
                foreach (var id in _avatarSelectView.ControlIds) _inputRouter.Unregister(id);
                _avatarSelectView.gameObject.SetActive(false);
                Destroy(_avatarSelectView.gameObject);
            }
            if (_avatarPreviewView != null)
            {
                foreach (var id in _avatarPreviewView.ControlIds) _inputRouter.Unregister(id);
                _avatarPreviewView.gameObject.SetActive(false);
                Destroy(_avatarPreviewView.gameObject);
            }
            _avatarSelectView = null;
            _avatarPreviewView = null;
            _avatarController = null;
            _avatarMotion = null;
            _avatarMedia = null;
            _avatarInputReadyFrame = -1;
            _avatarMotionPaused = false;
        }

        private void BeginJourneyBoundary()
        {
            _recoveryPresenter.BeginLoading();
            if (_deferNextJourneyBoundary)
            {
                _deferNextJourneyBoundary = false;
                return;
            }
            var transaction = _navigation.ActiveTransaction;
            if (transaction != null && transaction.Caller == RouteId.JourneyOverview
                && transaction.Destination == RouteId.Boat1)
                CompleteBoat1Entry(transaction.Id);
        }

        private bool CompleteBoat1Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.JourneyOverview || transaction.Destination != RouteId.Boat1)
                return false;
            var selectedGirl = string.Equals(_session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_boat1StageHost.Prepare(transactionId, _leaveController, _leaveConfirmationView, selectedGirl))
            { if (_lifecycle.HandleAssetFailure()) _journeyStageHost.ShowRecovery(); return false; }
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) { _boat1StageHost.Cleanup(); return false; }
            if (_session.CurrentOpenSessionLocation != RouteId.Boat1) _session.MoveTo(RouteId.Boat1);
            _journeyStageHost.Cleanup(); _recoveryPresenter.CompleteLoadingAndShowContent();
            if (!_boat1StageHost.Enter(Time.unscaledTimeAsDouble)) throw new InvalidOperationException("Boat 1 failed after its route completed.");
            _lastCompletedBoat1EntryTransactionId = transactionId; return true;
        }

        private void BeginBoat1Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Boat1
                || transaction.Destination != RouteId.Stage1)
                return;

            var selectedGirl = string.Equals(
                _session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_stage1StageHost.Prepare(
                    transaction.Id, _leaveController, _leaveConfirmationView, selectedGirl))
            {
                if (_lifecycle.HandleAssetFailure()) _boat1StageHost.ShowRecovery();
                return;
            }

            _pendingStage1EntryTransactionId = transaction.Id;
            _stage1EntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteStage1Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat1 || transaction.Destination != RouteId.Stage1
                || _pendingStage1EntryTransactionId != transactionId || !_stage1StageHost.IsPrepared)
                return false;

            _boat1StageHost.Cleanup();
            if (!_stage1StageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Stage 1 failed after its entry was prevalidated.");
            if (_leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Hidden)
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _lastCompletedStage1EntryTransactionId = transactionId;
            _pendingStage1EntryTransactionId = null;
            _stage1EntryReadyFrame = -1;
            return true;
        }

        private void BeginStage1Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Stage1
                || transaction.Destination != RouteId.Boat2) return;
            if (_boat2StageHost.IsPrepared)
            {
                if (_pendingBoat2EntryTransactionId == transaction.Id) return;
                _boat2StageHost.Cleanup();
                _pendingBoat2EntryTransactionId = null;
                _boat2EntryReadyFrame = -1;
            }
            var selectedGirl = string.Equals(_session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_boat2StageHost.Prepare(transaction.Id, _leaveController, _leaveConfirmationView, selectedGirl))
            {
                if (_lifecycle.HandleAssetFailure()) _stage1StageHost.ShowRecovery();
                return;
            }
            _pendingBoat2EntryTransactionId = transaction.Id;
            _boat2EntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteBoat2Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Stage1 || transaction.Destination != RouteId.Boat2)
                return false;
            if (_pendingBoat2EntryTransactionId != transactionId || !_boat2StageHost.IsPrepared) return false;
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed)
            {
                _boat2StageHost.Cleanup();
                return false;
            }
            if (_session.CurrentOpenSessionLocation != RouteId.Boat2) _session.MoveTo(RouteId.Boat2);
            _stage1StageHost.Cleanup();
            _recoveryPresenter.CompleteLoadingAndShowContent();
            if (!_boat2StageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Boat 2 failed after its route completed.");
            _lastCompletedBoat2EntryTransactionId = transactionId;
            _pendingBoat2EntryTransactionId = null;
            _boat2EntryReadyFrame = -1;
            return true;
        }

        private void BeginBoat2Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Boat2
                || transaction.Destination != RouteId.Stage2) return;
            if (_stage2StageHost.IsPrepared)
            {
                if (_pendingStage2EntryTransactionId == transaction.Id) return;
                _stage2StageHost.Cleanup();
                _pendingStage2EntryTransactionId = null;
                _stage2EntryReadyFrame = -1;
            }
            var selectedGirl = string.Equals(_session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_stage2StageHost.Prepare(transaction.Id, _leaveController, _leaveConfirmationView, selectedGirl))
            {
                if (_lifecycle.HandleAssetFailure()) _boat2StageHost.ShowRecovery();
                return;
            }
            _pendingStage2EntryTransactionId = transaction.Id;
            _stage2EntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteStage2Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat2 || transaction.Destination != RouteId.Stage2
                || _pendingStage2EntryTransactionId != transactionId || !_stage2StageHost.IsPrepared)
                return false;
            _boat2StageHost.Cleanup();
            if (!_stage2StageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Stage 2 failed after its entry was prevalidated.");
            if (_leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Hidden)
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _lastCompletedStage2EntryTransactionId = transactionId;
            _pendingStage2EntryTransactionId = null;
            _stage2EntryReadyFrame = -1;
            return true;
        }

        private void BeginStage2Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Stage2
                || transaction.Destination != RouteId.Boat3) return;
            if (_boat3StageHost.IsPrepared)
            {
                if (_pendingBoat3EntryTransactionId == transaction.Id) return;
                _boat3StageHost.Cleanup();
                _pendingBoat3EntryTransactionId = null;
                _boat3EntryReadyFrame = -1;
            }
            var selectedGirl = string.Equals(_session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_boat3StageHost.Prepare(transaction.Id, _leaveController, _leaveConfirmationView, selectedGirl))
            {
                if (_lifecycle.HandleAssetFailure()) _stage2StageHost.ShowRecovery();
                return;
            }
            _pendingBoat3EntryTransactionId = transaction.Id;
            _boat3EntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteBoat3Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Stage2 || transaction.Destination != RouteId.Boat3)
                return false;
            if (_pendingBoat3EntryTransactionId != transactionId || !_boat3StageHost.IsPrepared) return false;
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed)
            {
                _boat3StageHost.Cleanup();
                return false;
            }
            if (_session.CurrentOpenSessionLocation != RouteId.Boat3) _session.MoveTo(RouteId.Boat3);
            _stage3EntryRiver = _stage2StageHost.Snapshot.SelectedRiver;
            _stage2StageHost.Cleanup();
            _recoveryPresenter.CompleteLoadingAndShowContent();
            if (!_boat3StageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Boat 3 failed after its route completed.");
            _lastCompletedBoat3EntryTransactionId = transactionId;
            _pendingBoat3EntryTransactionId = null;
            _boat3EntryReadyFrame = -1;
            return true;
        }

        private void BeginBoat3Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Boat3
                || transaction.Destination != RouteId.Stage3) return;
            if (_stage3StageHost.IsPrepared)
            {
                if (_pendingStage3EntryTransactionId == transaction.Id) return;
                _stage3StageHost.Cleanup();
                _pendingStage3EntryTransactionId = null;
                _stage3EntryReadyFrame = -1;
            }
            var selectedGirl = string.Equals(_session.SelectedAvatarId, "avatar_girl", StringComparison.Ordinal);
            if (!_stage3StageHost.Prepare(transaction.Id, _stage3EntryRiver,
                    _leaveController, _leaveConfirmationView, selectedGirl))
            {
                if (_lifecycle.HandleAssetFailure()) _boat3StageHost.ShowRecovery();
                return;
            }
            _pendingStage3EntryTransactionId = transaction.Id;
            _stage3EntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteStage3Entry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat3 || transaction.Destination != RouteId.Stage3
                || _pendingStage3EntryTransactionId != transactionId || !_stage3StageHost.IsPrepared)
                return false;
            _boat3StageHost.Cleanup();
            if (!_stage3StageHost.Enter(Time.unscaledTimeAsDouble))
                throw new InvalidOperationException("Stage 3 failed after its entry was prevalidated.");
            if (_leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Hidden)
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _lastCompletedStage3EntryTransactionId = transactionId;
            _pendingStage3EntryTransactionId = null;
            _stage3EntryReadyFrame = -1;
            return true;
        }

        private void BeginStage3Boundary()
        {
            _recoveryPresenter.BeginLoading();
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Caller != RouteId.Stage3
                || transaction.Destination != RouteId.Completion) return;
            _pendingCompletionEntryTransactionId = transaction.Id;
            _completionEntryReadyFrame = Time.frameCount + 2;
        }

        private bool CompleteCompletionEntry(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Stage3 || transaction.Destination != RouteId.Completion
                || _pendingCompletionEntryTransactionId != transactionId)
                return false;
            if (!_closingStageHost.EnterCompletion(transaction.Id, Time.unscaledTimeAsDouble))
            {
                if (_lifecycle.HandleAssetFailure()) _stage3StageHost.ShowRecovery();
                return false;
            }
            _stage3StageHost.Cleanup();
            CleanupSharedLeavePresentation();
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _pendingCompletionEntryTransactionId = null;
            _completionEntryReadyFrame = -1;
            return true;
        }

        private void RestoreWelcomeFromAdultArea(AdultGateRequest request)
        {
            _pendingAdultGateRequest = null;
            _welcomeController?.RestoreFromAdultGate();
            _sessionChoiceController?.RestoreFromAdultGate();
            _welcomePresentation = string.Equals(
                request.ReturnPresentationId, "session_choice", StringComparison.Ordinal)
                ? WelcomePresentationState.SessionChoice
                : WelcomePresentationState.Welcome;
            _welcomeView?.RestoreFromAdultGate(_welcomePresentation);
            PresentWelcomeMedia();
            _inputRouter.SetVisibleStateReady(true);
        }

        private void CompleteClosingRouteToWelcome()
        {
            _pendingAdultGateRequest = null;
            _pendingCompletionEntryTransactionId = null;
            _completionEntryReadyFrame = -1;
            CleanupSharedLeavePresentation();
            _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
        }

        private async void RestartAfterConfirmedReset()
        {
            _pendingAdultGateRequest = null;
            _pendingStage3EntryTransactionId = null;
            _stage3EntryReadyFrame = -1;
            _pendingCompletionEntryTransactionId = null;
            _completionEntryReadyFrame = -1;
            _stage3StageHost?.Cleanup();
            CleanupSharedLeavePresentation();
            CleanupWelcomePresentation();
            CleanupIntroPresentation();
            _introController = null;
            _audioInterrupted = false;
            _startupRealtimeSeconds = Time.realtimeSinceStartupAsDouble;
            _startupLoadingInstance = Instantiate(startupLoadingPrefab, transform, false);
            ConfigureStartupLoadingArtwork();
            await StartStartupIntroAsync();
        }

        private void ConfigureStartupLoadingArtwork()
        {
            if (_startupLoadingInstance == null) return;
            Image background = null;
            foreach (var image in _startupLoadingInstance.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name != "TechnicalLoadingBackground") continue;
                background = image;
                break;
            }
            if (background == null)
                throw new InvalidOperationException("Startup loading background image is missing.");
            var content = WelcomeContent.LoadApproved();
            var presenter = background.GetComponent<ResponsiveBackgroundPresenter>();
            if (presenter == null) presenter = background.gameObject.AddComponent<ResponsiveBackgroundPresenter>();
            presenter.Configure(content.Background4x3, content.Background16x9, content.Background24x10);
            presenter.Present(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
        }

        private void CleanupSharedLeavePresentation()
        {
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null;
            _leaveController = null;
        }

        private void CompleteStage3LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId || transaction.Caller != RouteId.Stage3
                || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
            _stage3StageHost.CompleteWelcomeTransition(transactionId);
            _stage3StageHost.Cleanup();
            _pendingCompletionEntryTransactionId = null;
            _completionEntryReadyFrame = -1;
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null; _leaveController = null; _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent(); EnterWelcome();
        }

        private void CompleteBoat3LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId || transaction.Caller != RouteId.Boat3
                || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
            _boat3StageHost.CompleteWelcomeTransition(transactionId);
            _boat3StageHost.Cleanup();
            _pendingStage3EntryTransactionId = null;
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null;
            _leaveController = null;
            _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
        }

        private void CompleteStage2LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId || transaction.Caller != RouteId.Stage2
                || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
            _stage2StageHost.CompleteWelcomeTransition(transactionId);
            _stage2StageHost.Cleanup();
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null; _leaveController = null; _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent(); EnterWelcome();
        }

        private void CompleteBoat2LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId || transaction.Caller != RouteId.Boat2
                || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
            _boat2StageHost.CompleteWelcomeTransition(transactionId);
            _boat2StageHost.Cleanup();
            _stage2StageHost?.Cleanup();
            _pendingStage2EntryTransactionId = null;
            _stage2EntryReadyFrame = -1;
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null;
            _leaveController = null;
            _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
        }

        private void CompleteBoat1LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId || transaction.Caller != RouteId.Boat1
                || transaction.Destination != RouteId.Welcome || _navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
            _boat1StageHost.CompleteWelcomeTransition(transactionId); _boat1StageHost.Cleanup();
            _stage1StageHost?.Cleanup();
            _pendingStage1EntryTransactionId = null;
            _stage1EntryReadyFrame = -1;
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView=null;_leaveController=null;_audioInterrupted=false;_recoveryPresenter.CompleteLoadingAndShowContent();EnterWelcome();
        }

        private bool HandleBoat1SemanticCommand(string controlId, double now)
        {
            var accepted = _boat1StageHost != null
                && _boat1StageHost.HandleSemanticCommand(controlId, now);
            if (!accepted || controlId != LeaveConfirmationView.RetryControlId
                || CurrentRoute != RouteId.Boat1
                || _navigation.ActiveTransaction?.Destination != RouteId.Stage1)
                return accepted;

            _stage1StageHost?.Cleanup();
            _pendingStage1EntryTransactionId = null;
            _stage1EntryReadyFrame = -1;
            BeginBoat1Boundary();
            return true;
        }

        private void CompleteStage1LeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.Stage1 || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed)
                return;

            _stage1StageHost.CompleteWelcomeTransition(transactionId);
            _stage1StageHost.Cleanup();
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds) _inputRouter.Unregister(id);
                _leaveConfirmationView.gameObject.SetActive(false);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null;
            _leaveController = null;
            _audioInterrupted = false;
            _pendingStage1EntryTransactionId = null;
            _stage1EntryReadyFrame = -1;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
        }

        private void CompleteJourneyLeaveToWelcome(Guid transactionId)
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Id != transactionId
                || transaction.Caller != RouteId.JourneyOverview
                || transaction.Destination != RouteId.Welcome
                || _navigation.Complete(transactionId) != NavigationCompletion.Completed)
                return;

            _journeyStageHost.CompleteWelcomeTransition(transactionId);
            _journeyStageHost.Cleanup();
            if (_leaveConfirmationView != null)
            {
                foreach (var id in _leaveConfirmationView.ControlIds) _inputRouter.Unregister(id);
                foreach (var id in _leaveConfirmationView.TechnicalControlIds)
                    _inputRouter.Unregister(id);
                _leaveConfirmationView.gameObject.SetActive(false);
                Destroy(_leaveConfirmationView.gameObject);
            }
            _leaveConfirmationView = null;
            _leaveController = null;
            _audioInterrupted = false;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
        }

        private void HandleAvatarHome()
        {
            if (!_avatarController.HandleHome()) return;
            PresentAvatarMedia(AvatarMediaState.Leave);
        }

        private void HandleLeaveStay()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Back, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_leaveController.HandleStay()) return;
            PresentAvatarMedia(CurrentRoute == RouteId.AvatarSelect
                ? AvatarMediaState.Select : AvatarMediaState.Preview);
        }

        private void HandleLeaveCancel()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Back, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_leaveController.HandleCancel()) return;
            PresentAvatarMedia(CurrentRoute == RouteId.AvatarSelect
                ? AvatarMediaState.Select : AvatarMediaState.Preview);
        }

        private void HandleLeaveToWelcome()
        {
            _avatarMedia?.PlaySfx(AvatarSfxCue.Back, _session.SoundEnabled, Time.unscaledTimeAsDouble);
            if (!_leaveController.HandleLeave(Time.unscaledTimeAsDouble)) return;
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.Welcome) return;
            _inputRouter.SetVisibleStateReady(false);
            _pendingAvatarTransitionDestination = RouteId.Welcome;
            if (_deferNextAvatarTransition)
            {
                _deferNextAvatarTransition = false;
                _avatarBoundaryLoading = true;
                _recoveryPresenter.BeginLoading();
                _leaveConfirmationView.ShowTechnicalOverlay(CurrentRoute, AvatarTechnicalOverlayState.Loading);
                return;
            }

            CompletePendingAvatarLeaveTransition();
        }

        private bool CompletePendingAvatarLeaveTransition()
        {
            var transaction = _navigation.ActiveTransaction;
            if (transaction == null || transaction.Destination != RouteId.Welcome) return false;
            if (_navigation.Complete(transaction.Id) != NavigationCompletion.Completed) return false;
            _leaveController.CompleteTransition(transaction.Id);
            _leaveConfirmationView.CompleteTransitionAndHide();
            _avatarSelectView.SetVisible(false);
            _avatarPreviewView.SetVisible(false);
            _avatarBoundaryLoading = false;
            _avatarMotionPaused = false;
            _pendingAvatarTransitionDestination = null;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            EnterWelcome();
            return true;
        }

        private void PresentAvatarMedia(AvatarMediaState state)
        {
            _avatarPresentationStartedAt = Time.unscaledTimeAsDouble;
            _avatarMedia?.Present(
                state,
                _session.SoundEnabled,
                _accessibilityState.CaptionsEnabled,
                _avatarPresentationStartedAt);
        }

        private void BeginAvatarPreviewMotion()
        {
            if (!_avatarController.PreviewChoice.HasValue) return;
            _avatarPresentationStartedAt = Time.unscaledTimeAsDouble;
            _avatarMotion.BeginPreview(
                _avatarController.PreviewChoice.Value,
                _avatarController.CurrentPreviewToken,
                _accessibilityState.EffectiveReducedMotion);
        }

        private void AdvanceAvatarPresentation(double now)
        {
            _avatarMedia?.Advance(now);
            if (_avatarBoundaryLoading) return;
            if (CurrentRoute == RouteId.AvatarSelect)
            {
                var elapsed = Math.Max(0d, now - _avatarPresentationStartedAt);
                _avatarSelectView.SetAvatarSprite(AvatarChoice.Boy,
                    _avatarMotion.EvaluateSelect(AvatarChoice.Boy, elapsed, _accessibilityState.EffectiveReducedMotion).Sprite);
                _avatarSelectView.SetAvatarSprite(AvatarChoice.Girl,
                    _avatarMotion.EvaluateSelect(AvatarChoice.Girl, elapsed, _accessibilityState.EffectiveReducedMotion).Sprite);
            }
            else if (_avatarController.PreviewChoice.HasValue &&
                     _avatarMotion.TryEvaluatePreview(
                         _avatarController.CurrentPreviewToken,
                         Math.Max(0d, now - _avatarPresentationStartedAt),
                         out var frame))
                _avatarPreviewView.SetAvatarSprite(frame.Sprite);
        }

        private void ArmAvatarInputAfterVisibleFrame()
        {
            _inputRouter.SetVisibleStateReady(false);
            _avatarInputReadyFrame = Time.frameCount + 1;
        }

        private void BeginRouteProbe()
        {
            var request = _navigation.Request(RouteId.IntroVideo, Time.unscaledTimeAsDouble);
            if (!request.Accepted) return;
            _autoCompleteAt = double.PositiveInfinity;
            _recoveryPresenter.BeginLoading();
            ConfigureControlAvailability(normal: false);
            SetState(FoundationStringIds.Loading, new Color32(196, 126, 42, 255));
        }

        private void RetryRecovery()
        {
            var welcomeRecovery = _welcomeEntered && CurrentRoute == RouteId.Welcome &&
                _welcomeView != null &&
                _welcomeView.TechnicalOverlayState == WelcomeTechnicalOverlayState.Recovery;
            var result = _lifecycle.Retry(Time.unscaledTimeAsDouble);
            if (!result.Accepted) return;
            if (welcomeRecovery)
            {
                _autoCompleteAt = double.PositiveInfinity;
                _welcomeView.HideTechnicalOverlay(_welcomePresentation);
                SuspendWelcomeAtLaterStageBoundary();
                return;
            }

            _autoCompleteAt = Time.unscaledTimeAsDouble + 0.5d;
            ConfigureControlAvailability(normal: false);
            SetState(FoundationStringIds.Loading, new Color32(196, 126, 42, 255));
        }

        private void GoHomeFromRecovery()
        {
            var welcomeRecovery = _welcomeEntered && CurrentRoute == RouteId.Welcome &&
                _welcomeView != null &&
                _welcomeView.TechnicalOverlayState == WelcomeTechnicalOverlayState.Recovery;
            var result = _lifecycle.Home(Time.unscaledTimeAsDouble);
            if (!result.Accepted) return;
            if (welcomeRecovery)
            {
                var transactionId = result.Transaction.Id;
                if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return;
                _autoCompleteAt = double.PositiveInfinity;
                _recoveryPresenter.CompleteLoadingAndShowContent();
                _welcomeView.HideTechnicalOverlay(_welcomePresentation);
                PresentWelcomeMedia();
                return;
            }

            _autoCompleteAt = Time.unscaledTimeAsDouble + 0.5d;
            ConfigureControlAvailability(normal: false);
            SetState(FoundationStringIds.Loading, new Color32(196, 126, 42, 255));
        }

        private void BeginInterruption(
            JourneyOverviewPauseReason journeyReason = JourneyOverviewPauseReason.AppBackground)
        {
            if (startupIntroEnabled &&
                _introController?.CurrentPlaybackToken != null &&
                _introController.HandleInterruption(_introController.CurrentPlaybackToken.Value))
            {
                _introResumePending = true;
                _introView.ShowInterruptionResume();
                return;
            }

            if (BeginWelcomeInterruption()) return;

            if (BeginAvatarInterruption()) return;

            if (_closingStageHost != null
                && _closingStageHost.HandleInterruption(Time.unscaledTimeAsDouble))
            {
                _audioInterrupted = true;
                return;
            }

            if (_stage3StageHost != null && _stage3StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss
                        ? Stage3PauseReason.AudioFocusLoss : Stage3PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_boat3StageHost != null && _boat3StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss
                        ? Boat3PauseReason.AudioFocusLoss : Boat3PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_stage2StageHost != null && _stage2StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss
                        ? Stage2PauseReason.AudioFocusLoss : Stage2PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_boat2StageHost != null && _boat2StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss
                        ? Boat2PauseReason.AudioFocusLoss
                        : Boat2PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_stage1StageHost != null && _stage1StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss
                        ? Stage1PauseReason.AudioFocusLoss
                        : Stage1PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_boat1StageHost != null && _boat1StageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble,
                    journeyReason == JourneyOverviewPauseReason.AudioFocusLoss ? Boat1PauseReason.AudioFocusLoss : Boat1PauseReason.AppBackground))
            {
                _audioInterrupted = true;
                return;
            }

            if (_journeyStageHost != null && _journeyStageHost.HandleInterruption(
                    Time.unscaledTimeAsDouble, journeyReason))
            {
                _audioInterrupted = true;
                return;
            }

            if (_recoveryPresenter == null || RecoveryVisible) return;
            _audioInterrupted = true;
            _lifecycle.HandleInterruption();
            ShowRecovery();
        }

        private void HandleAudioFocusInterruption()
        {
            BeginInterruption(JourneyOverviewPauseReason.AudioFocusLoss);
        }

        private bool BeginAvatarInterruption()
        {
            if (_avatarController == null ||
                (CurrentRoute != RouteId.AvatarSelect && CurrentRoute != RouteId.AvatarPreview) ||
                _avatarInterruptionResumePending)
                return false;

            _audioInterrupted = true;
            _avatarMotionPaused = true;
            _avatarInterruptionResumePending = true;
            _avatarInterruptionRestoreLeave = _leaveController != null && _leaveController.IsVisible;
            _lifecycle.HandleInterruption();
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing)
            {
                _avatarInterruptionResumePending = false;
                _avatarInterruptionRestoreLeave = false;
                ShowAvatarRecovery();
                return true;
            }

            if (_recoveryPresenter.State == RecoveryPresentationState.PausedPreparing)
                _recoveryPresenter.MarkRecoveryVisible();
            _leaveConfirmationView.ShowTechnicalOverlay(CurrentRoute, AvatarTechnicalOverlayState.Paused);
            return true;
        }

        private bool ResumeAvatarAfterMappedUserAction()
        {
            if (!_avatarInterruptionResumePending ||
                !_lifecycle.ResumeCurrentScreenAfterUserAction()) return false;

            var restoreLeave = _avatarInterruptionRestoreLeave;
            _avatarInterruptionResumePending = false;
            _avatarInterruptionRestoreLeave = false;
            _avatarMotionPaused = false;
            _audioInterrupted = false;
            if (restoreLeave)
            {
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: true);
                PresentAvatarMedia(AvatarMediaState.Leave);
            }
            else
            {
                _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
                if (CurrentRoute == RouteId.AvatarPreview)
                {
                    _avatarController.HandleRepeat();
                    BeginAvatarPreviewMotion();
                    PresentAvatarMedia(AvatarMediaState.Preview);
                }
                else
                {
                    PresentAvatarMedia(AvatarMediaState.Select);
                }
            }
            return true;
        }

        private void RetryAvatarRecovery()
        {
            if (_leaveConfirmationView == null ||
                _leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Recovery)
                return;
            var result = _lifecycle.Retry(Time.unscaledTimeAsDouble);
            if (!result.Accepted) return;
            _pendingAvatarTransitionDestination = result.Transaction.Destination;
            _avatarBoundaryLoading = true;
            _leaveConfirmationView.ShowTechnicalOverlay(
                result.Transaction.Caller, AvatarTechnicalOverlayState.Loading);
        }

        private void GoHomeFromAvatarRecovery()
        {
            if (_leaveConfirmationView == null ||
                _leaveConfirmationView.TechnicalOverlayState != AvatarTechnicalOverlayState.Recovery)
                return;
            var result = _lifecycle.Home(Time.unscaledTimeAsDouble);
            if (!result.Accepted) return;
            if (_navigation.Complete(result.Transaction.Id) != NavigationCompletion.Completed) return;
            _recoveryPresenter.CompleteLoadingAndShowContent();
            _leaveConfirmationView.HideTechnicalOverlay(restoreLeaveOverlay: false);
            _avatarSelectView.SetVisible(false);
            _avatarPreviewView.SetVisible(false);
            _avatarBoundaryLoading = false;
            _avatarMotionPaused = false;
            _pendingAvatarTransitionDestination = null;
            EnterWelcome();
        }

        private bool BeginWelcomeInterruption()
        {
            if (!_welcomeEntered || CurrentRoute != RouteId.Welcome ||
                _welcomeInterruptionResumePending || _welcomeMedia == null)
            {
                return false;
            }

            if (!_welcomeMedia.Interrupt()) return false;
            _audioInterrupted = true;
            _welcomeInterruptionResumePending = true;
            _inputRouter.SetVisibleStateReady(false);
            _welcomeView.ShowTechnicalOverlay(WelcomeTechnicalOverlayState.Paused);
            _inputRouter.SetVisibleStateReady(true);
            return true;
        }

        private bool ResumeWelcomeAfterMappedUserAction()
        {
            if (!_welcomeInterruptionResumePending || _welcomeMedia == null) return false;
            if (!_welcomeMedia.ResumeAfterMappedUserAction(Time.unscaledTimeAsDouble)) return false;
            _welcomeInterruptionResumePending = false;
            _audioInterrupted = false;
            _welcomeView.HideTechnicalOverlay(_welcomePresentation);
            return true;
        }

        private bool ResumeIntroAfterMappedUserAction()
        {
            if (!_introResumePending || _introController == null) return false;
            var token = _introController.ResumeAfterMappedUserAction();
            if (!token.HasValue) return false;
            _introResumePending = false;
            _introView.HideInterruptionResume();
            return true;
        }

        private void ResumeAfterInterruption()
        {
            if (!_lifecycle.ResumeCurrentScreenAfterUserAction()) return;
            _audioInterrupted = false;
            ConfigureControlAvailability(normal: true);
            SetState(FoundationStringIds.HarnessReady, new Color32(39, 120, 92, 255));
        }

        private async void RefreshAccessibilityFromOs()
        {
            if (_accessibilityCoordinator != null)
            {
                await _accessibilityCoordinator.RefreshAsync();
            }
        }

        private void ShowRecovery()
        {
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing ||
                _recoveryPresenter.State == RecoveryPresentationState.PausedPreparing)
            {
                _recoveryPresenter.MarkRecoveryVisible();
            }

            ConfigureControlAvailability(normal: false);
            var paused = _recoveryPresenter.State == RecoveryPresentationState.PausedVisible;
            SetEnabled(RetryId, !paused);
            SetEnabled(HomeId, !paused);
            SetEnabled(ResumeId, paused);
            SetState(paused ? FoundationStringIds.AudioTest : FoundationStringIds.Retry, new Color32(170, 67, 61, 255));
        }

        private void ShowWelcomeRecovery()
        {
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing)
            {
                _recoveryPresenter.MarkRecoveryVisible();
            }

            _welcomeView.ShowTechnicalOverlay(WelcomeTechnicalOverlayState.Recovery);
        }

        private void ShowAvatarRecovery()
        {
            if (_recoveryPresenter.State == RecoveryPresentationState.Preparing)
                _recoveryPresenter.MarkRecoveryVisible();
            if (_recoveryPresenter.State != RecoveryPresentationState.Visible) return;

            _avatarBoundaryLoading = true;
            _avatarMotionPaused = true;
            _leaveConfirmationView.ShowTechnicalOverlay(CurrentRoute, AvatarTechnicalOverlayState.Recovery);
        }

        private void ConfigureControlAvailability(bool normal)
        {
            foreach (var pair in _controls) pair.Value.Semantic.Enabled = normal;
            if (!normal)
            {
                SetEnabled(RetryId, false);
                SetEnabled(HomeId, false);
                SetEnabled(ResumeId, false);
            }
            else
            {
                SetEnabled(RetryId, false);
                SetEnabled(HomeId, false);
                SetEnabled(ResumeId, false);
            }
        }

        private void SetEnabled(string id, bool enabled)
        {
            if (_controls.TryGetValue(id, out var view))
            {
                view.Semantic.Enabled = enabled;
                view.VisibleArt.gameObject.SetActive(enabled || id != RetryId && id != HomeId && id != ResumeId);
            }
        }

        private void SetState(string stringId, Color color)
        {
            var value = _localization.Resolve(stringId);
            _header.text = _pseudo ? Pseudolocalizer.Expand(value) : value;
            _statePanel.color = color;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value)
        {
            var gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.GetComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-SemiBold SDF");
            if (text.font == null)
            {
                throw new InvalidOperationException("Approved Noto TMP font asset is missing.");
            }
            text.text = value;
            text.fontSize = 46f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Rect ToScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private sealed class HostAvatarOrderSource : IAvatarOrderSource
        {
            private readonly Func<AvatarChoice> _choose;

            public HostAvatarOrderSource(Func<AvatarChoice> choose)
            {
                _choose = choose ?? throw new ArgumentNullException(nameof(choose));
            }

            public AvatarChoice ChooseInitialLeft() => _choose();
        }

        private sealed class ControlView
        {
            public ControlView(RectTransform target, RectTransform visibleArt, Image targetImage, SemanticControl semantic)
            {
                Target = target;
                VisibleArt = visibleArt;
                TargetImage = targetImage;
                Semantic = semantic;
            }

            public RectTransform Target { get; }
            public RectTransform VisibleArt { get; }
            public Image TargetImage { get; }
            public SemanticControl Semantic { get; }
        }

    }
}
