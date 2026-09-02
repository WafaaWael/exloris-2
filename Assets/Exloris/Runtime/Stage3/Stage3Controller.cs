using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Stage2;

namespace Exloris.Runtime.Stage3
{
    public sealed class Stage3Controller
    {
        public const double OutcomeSeconds = 6.735d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IStage3View _view;
        private readonly IStage3PauseSink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();
        private readonly HashSet<Guid> _consumedInteractions = new HashSet<Guid>();

        private bool _reducedMotion;
        private Stage2River _selectedRiver;
        private Stage3Plan _selectedPlan;
        private Stage3OutcomeKey _currentOutcome;
        private Stage3TriedCombinations _triedCombinations;
        private double _stateStartedAtSeconds;
        private double _outcomeElapsedSeconds;
        private string _focusedControlId;
        private int _optionCount;
        private bool _completionRecorded;
        private double _pauseCapturedAtSeconds;
        private double _leaveCapturedAtSeconds;
        private Stage3Snapshot _helpSnapshot;
        private Stage3Snapshot _pauseSnapshot;
        private Stage3Snapshot _leaveSnapshot;
        private Stage3Snapshot _riverSelectionSnapshot;

        public Stage3Controller(SessionState session, NavigationAuthority navigation, IStage3View view,
            IStage3PauseSink pauseSink, ILeaveConfirmationSink leaveSink, Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public Stage3State State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public Guid CurrentOutcomeTokenId { get; private set; }
        public Guid CurrentInstructionTokenId { get; private set; }
        public Stage3Snapshot Snapshot => new Stage3Snapshot(
            CurrentTokenId,
            State,
            _selectedRiver,
            _selectedPlan,
            _currentOutcome,
            _triedCombinations,
            CurrentOutcomeTokenId,
            CurrentInstructionTokenId,
            _stateStartedAtSeconds,
            _outcomeElapsedSeconds,
            _focusedControlId,
            _optionCount,
            _completionRecorded);

        public bool Enter(Guid transactionId, Stage3EntryContext entryContext, double nowSeconds, bool reducedMotion)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage3State.Hidden
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != RouteId.Boat3
                || transaction.Destination != RouteId.Stage3
                || _navigation.CurrentRoute != RouteId.Boat3
                || !_session.IsOpen
                || _session.CurrentOpenSessionLocation != RouteId.Boat3
                || _navigation.Recovery != null
                || !IsRiver(entryContext.River)
                || !IsValidClock(nowSeconds))
                return false;

            CurrentTokenId = NextToken();
            CurrentInstructionTokenId = NextToken();
            CurrentOutcomeTokenId = Guid.Empty;
            _selectedRiver = entryContext.River;
            _selectedPlan = Stage3Plan.None;
            _currentOutcome = default;
            _triedCombinations = Stage3TriedCombinations.None;
            _stateStartedAtSeconds = nowSeconds;
            _outcomeElapsedSeconds = 0d;
            _focusedControlId = null;
            _optionCount = 0;
            _completionRecorded = false;
            _reducedMotion = reducedMotion;

            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return false;
            if (!_session.MoveTo(RouteId.Stage3))
                throw new InvalidOperationException("A prevalidated Stage 3 session failed to advance.");
            State = Stage3State.Intro;
            Present(true);
            return true;
        }

        public bool HandlePlay(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage3State.Intro || !CanActAt(nowSeconds)) return false;
            State = Stage3State.ChoosingPlan;
            _stateStartedAtSeconds = nowSeconds;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleRepeat(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || !CanActAt(nowSeconds)) return false;
            if ((State == Stage3State.OutcomePlaying || State == Stage3State.OutcomeReady)
                && IsPlan(_selectedPlan))
            {
                StartOutcome(nowSeconds);
                return true;
            }
            if (State != Stage3State.Intro
                && State != Stage3State.ChoosingPlan
                && State != Stage3State.ChoosingRiver)
                return false;
            CurrentInstructionTokenId = NextToken();
            Present(true);
            return true;
        }

        public bool HandleHelp(Guid tokenId)
        {
            if (!IsCurrent(tokenId) || !IsHelpCaller(State) || !CanPresent()) return false;
            _helpSnapshot = Snapshot;
            State = Stage3State.Help;
            Present(false);
            return true;
        }

        public bool RestoreFromHelp(Stage3Snapshot snapshot)
        {
            if (State != Stage3State.Help || !snapshot.Equals(_helpSnapshot) || !IsRestorable(snapshot.State) || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, 0d, 0d, false);
            return true;
        }

        public bool HandleSelectPlan(Guid tokenId, Stage3Plan plan, Guid interactionId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage3State.ChoosingPlan
                || !IsPlan(plan)
                || !CanActAt(nowSeconds)
                || !ConsumeInteraction(interactionId))
                return false;
            _selectedPlan = plan;
            _currentOutcome = OutcomeFor(_selectedRiver, plan);
            StartOutcome(nowSeconds);
            return true;
        }

        public bool AdvanceOutcome(Guid tokenId, Guid outcomeTokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage3State.OutcomePlaying
                || outcomeTokenId == Guid.Empty
                || outcomeTokenId != CurrentOutcomeTokenId
                || !CanActAt(nowSeconds))
                return false;

            var nextElapsed = Math.Min(OutcomeSeconds, nowSeconds - _stateStartedAtSeconds);
            if (nextElapsed < _outcomeElapsedSeconds - ClockEpsilon) return false;
            _outcomeElapsedSeconds = nextElapsed;
            if (_outcomeElapsedSeconds < OutcomeSeconds - ClockEpsilon)
            {
                Present(true);
                return true;
            }

            _outcomeElapsedSeconds = OutcomeSeconds;
            var flag = Flag(_selectedRiver, _selectedPlan);
            if ((_triedCombinations & flag) == 0)
            {
                _triedCombinations |= flag;
                _optionCount++;
                _session.Record(ObservationEvent.ChoiceOutcome(
                    ObservationEventType.Stage3Option,
                    "choice_" + RiverToken(_selectedRiver) + "_" + PlanToken(_selectedPlan),
                    "outcome_benefit_" + BenefitToken(_currentOutcome.Benefit) + "_need_" + NeedToken(_currentOutcome.Need)));
            }
            State = Stage3State.OutcomeReady;
            Present(true);
            return true;
        }

        public bool HandleWatchAgain(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage3State.OutcomeReady || !CanActAt(nowSeconds)) return false;
            StartOutcome(nowSeconds);
            return true;
        }

        public bool HandleAnotherPlan(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage3State.OutcomeReady || !CanActAt(nowSeconds)) return false;
            _selectedPlan = Stage3Plan.None;
            _currentOutcome = default;
            CurrentOutcomeTokenId = Guid.Empty;
            CurrentInstructionTokenId = NextToken();
            _outcomeElapsedSeconds = 0d;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage3State.ChoosingPlan;
            Present(true);
            return true;
        }

        public bool HandleChangeRiver(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId) || State != Stage3State.OutcomeReady || !CanActAt(nowSeconds)) return false;
            _riverSelectionSnapshot = Snapshot;
            _selectedPlan = Stage3Plan.None;
            _currentOutcome = default;
            CurrentOutcomeTokenId = Guid.Empty;
            CurrentInstructionTokenId = NextToken();
            _outcomeElapsedSeconds = 0d;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage3State.ChoosingRiver;
            Present(true);
            return true;
        }

        public bool HandleRiverBack(Guid tokenId)
        {
            if (!IsCurrent(tokenId)
                || State != Stage3State.ChoosingRiver
                || _riverSelectionSnapshot.TokenId != tokenId
                || _riverSelectionSnapshot.State != Stage3State.OutcomeReady
                || !CanPresent())
                return false;
            var completed = _riverSelectionSnapshot;
            _riverSelectionSnapshot = default;
            RestoreSnapshot(completed, 0d, 0d, false);
            return true;
        }

        public bool HandleSelectRiver(Guid tokenId, Stage2River river, Guid interactionId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage3State.ChoosingRiver
                || !IsRiver(river)
                || !CanActAt(nowSeconds)
                || !ConsumeInteraction(interactionId))
                return false;
            _selectedRiver = river;
            _riverSelectionSnapshot = default;
            CurrentInstructionTokenId = NextToken();
            _stateStartedAtSeconds = nowSeconds;
            State = Stage3State.ChoosingPlan;
            Present(true);
            return true;
        }

        public bool HandleFinish(Guid tokenId, double nowSeconds)
        {
            if (!IsCurrent(tokenId)
                || State != Stage3State.OutcomeReady
                || _completionRecorded
                || !CanActAt(nowSeconds))
                return false;
            var request = _navigation.Request(RouteId.Completion, nowSeconds);
            if (!request.Accepted) return false;
            _session.Record(ObservationEvent.OptionsTried(ObservationEventType.Stage3Option, "stage_3", _optionCount));
            _session.Record(ObservationEvent.CompletedStages("stage_3"));
            _completionRecorded = true;
            State = Stage3State.Navigating;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandlePause(double nowSeconds)
        {
            if (!CanPause() || !CanActAt(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_pauseSink.TryShow(captured)) return false;
            _pauseSnapshot = captured;
            _pauseCapturedAtSeconds = nowSeconds;
            State = Stage3State.Paused;
            Present(false);
            return true;
        }

        public bool RestoreFromPause(Stage3Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage3State.Paused
                || !snapshot.Equals(_pauseSnapshot)
                || !IsRestorable(snapshot.State)
                || !IsValidClock(nowSeconds)
                || nowSeconds < _pauseCapturedAtSeconds
                || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, _pauseCapturedAtSeconds, nowSeconds, true);
            return true;
        }

        public bool HandleHome(double nowSeconds)
        {
            if (!CanLeave() || !CanActAt(nowSeconds)) return false;
            var captured = Snapshot;
            if (!_leaveSink.TryShow(RouteId.Stage3)) return false;
            _leaveSnapshot = captured;
            _leaveCapturedAtSeconds = nowSeconds;
            State = Stage3State.Leaving;
            Present(false);
            return true;
        }

        public bool RestoreFromLeave(Stage3Snapshot snapshot, double nowSeconds)
        {
            if (State != Stage3State.Leaving
                || !snapshot.Equals(_leaveSnapshot)
                || !IsRestorable(snapshot.State)
                || !IsValidClock(nowSeconds)
                || nowSeconds < _leaveCapturedAtSeconds
                || !CanPresent())
                return false;
            RestoreSnapshot(snapshot, _leaveCapturedAtSeconds, nowSeconds, true);
            return true;
        }

        public bool TrySetFocus(Guid tokenId, string controlId)
        {
            if (!IsCurrent(tokenId) || !CanPresent() || string.IsNullOrWhiteSpace(controlId) || controlId.Length > 64)
                return false;
            _focusedControlId = controlId;
            Present(State != Stage3State.Help && State != Stage3State.Paused && State != Stage3State.Leaving);
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != Stage3State.Navigating || _navigation.Recovery == null) return false;
            State = Stage3State.Recovery;
            _view.SetInputReady(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != Stage3State.Recovery
                || transaction == null
                || transaction.Caller != RouteId.Stage3
                || transaction.Destination != RouteId.Completion)
                return false;
            State = Stage3State.Navigating;
            _view.SetInputReady(false);
            return true;
        }

        private void StartOutcome(double nowSeconds)
        {
            CurrentOutcomeTokenId = NextToken();
            _outcomeElapsedSeconds = 0d;
            _stateStartedAtSeconds = nowSeconds;
            State = Stage3State.OutcomePlaying;
            Present(true);
        }

        private bool ConsumeInteraction(Guid interactionId) => interactionId != Guid.Empty && _consumedInteractions.Add(interactionId);

        private bool IsCurrent(Guid tokenId) => tokenId != Guid.Empty && tokenId == CurrentTokenId;

        private bool CanActAt(double nowSeconds) => CanPresent()
            && IsValidClock(nowSeconds)
            && nowSeconds >= _stateStartedAtSeconds - ClockEpsilon;

        private bool CanPresent() => _session.IsOpen
            && _session.CurrentOpenSessionLocation == RouteId.Stage3
            && _navigation.CurrentRoute == RouteId.Stage3
            && !_navigation.IsLocked
            && _navigation.Recovery == null;

        private bool CanPause() => IsHelpCaller(State) || State == Stage3State.Help || State == Stage3State.Leaving;

        private bool CanLeave() => State == Stage3State.Intro
            || State == Stage3State.ChoosingPlan
            || State == Stage3State.OutcomePlaying
            || State == Stage3State.OutcomeReady
            || State == Stage3State.ChoosingRiver;

        private static bool IsHelpCaller(Stage3State state) => state == Stage3State.Intro
            || state == Stage3State.ChoosingPlan
            || state == Stage3State.OutcomePlaying
            || state == Stage3State.OutcomeReady
            || state == Stage3State.ChoosingRiver;

        private static bool IsRestorable(Stage3State state) => IsHelpCaller(state)
            || state == Stage3State.Help
            || state == Stage3State.Leaving;

        private void RestoreSnapshot(Stage3Snapshot snapshot, double capturedAtSeconds, double nowSeconds, bool reanchorTimedState)
        {
            State = snapshot.State;
            _selectedRiver = snapshot.SelectedRiver;
            _selectedPlan = snapshot.SelectedPlan;
            _currentOutcome = snapshot.CurrentOutcome;
            _triedCombinations = snapshot.TriedCombinations;
            CurrentOutcomeTokenId = snapshot.OutcomeTokenId;
            CurrentInstructionTokenId = snapshot.InstructionTokenId;
            _stateStartedAtSeconds = snapshot.StateStartedAtSeconds;
            _outcomeElapsedSeconds = snapshot.OutcomeElapsedSeconds;
            _focusedControlId = snapshot.FocusedControlId;
            _optionCount = snapshot.OptionCount;
            _completionRecorded = snapshot.CompletionRecorded;
            if (reanchorTimedState && State == Stage3State.OutcomePlaying)
                _stateStartedAtSeconds = nowSeconds - _outcomeElapsedSeconds;
            Present(State != Stage3State.Help && State != Stage3State.Leaving);
        }

        private void Present(bool inputReady)
        {
            _view.Present(Snapshot, _reducedMotion);
            _view.SetInputReady(inputReady);
        }

        private Guid NextToken()
        {
            var token = _tokenSource();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Stage 3 tokens must be non-empty and unique.");
            return token;
        }

        private static Stage3OutcomeKey OutcomeFor(Stage2River river, Stage3Plan plan)
        {
            if (!IsRiver(river)) throw new ArgumentOutOfRangeException(nameof(river));
            switch (plan)
            {
                case Stage3Plan.Grain:
                    return new Stage3OutcomeKey(river, plan, Stage3Benefit.Grain, Stage3Need.WaterRoute);
                case Stage3Plan.Channel:
                    return new Stage3OutcomeKey(river, plan, Stage3Benefit.Channel, Stage3Need.GrainStorage);
                case Stage3Plan.Boat:
                    return new Stage3OutcomeKey(river, plan, Stage3Benefit.Boat, Stage3Need.FieldWater);
                default:
                    throw new ArgumentOutOfRangeException(nameof(plan));
            }
        }

        private static Stage3TriedCombinations Flag(Stage2River river, Stage3Plan plan)
        {
            if (!IsRiver(river) || !IsPlan(plan)) throw new ArgumentOutOfRangeException();
            var index = (((int)river - 1) * 3) + ((int)plan - 1);
            return (Stage3TriedCombinations)(1 << index);
        }

        private static string RiverToken(Stage2River river)
        {
            switch (river)
            {
                case Stage2River.Low: return "low";
                case Stage2River.Middle: return "middle";
                case Stage2River.High: return "high";
                default: throw new ArgumentOutOfRangeException(nameof(river));
            }
        }

        private static string PlanToken(Stage3Plan plan)
        {
            switch (plan)
            {
                case Stage3Plan.Grain: return "grain";
                case Stage3Plan.Channel: return "channel";
                case Stage3Plan.Boat: return "boat";
                default: throw new ArgumentOutOfRangeException(nameof(plan));
            }
        }

        private static string BenefitToken(Stage3Benefit benefit)
        {
            switch (benefit)
            {
                case Stage3Benefit.Grain: return "grain";
                case Stage3Benefit.Channel: return "channel";
                case Stage3Benefit.Boat: return "boat";
                default: throw new ArgumentOutOfRangeException(nameof(benefit));
            }
        }

        private static string NeedToken(Stage3Need need)
        {
            switch (need)
            {
                case Stage3Need.WaterRoute: return "water_route";
                case Stage3Need.GrainStorage: return "grain_storage";
                case Stage3Need.FieldWater: return "field_water";
                default: throw new ArgumentOutOfRangeException(nameof(need));
            }
        }

        private static bool IsRiver(Stage2River river) => river == Stage2River.Low
            || river == Stage2River.Middle
            || river == Stage2River.High;

        private static bool IsPlan(Stage3Plan plan) => plan == Stage3Plan.Grain
            || plan == Stage3Plan.Channel
            || plan == Stage3Plan.Boat;

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool IsValidClock(double value) => IsFinite(value) && value >= 0d;
    }
}
