using System;
using Exloris.Runtime.Stage2;
using UnityEngine;

namespace Exloris.Runtime.Stage3
{
    public enum Stage3State
    {
        Hidden,
        Intro,
        ChoosingPlan,
        OutcomePlaying,
        OutcomeReady,
        ChoosingRiver,
        Help,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public enum Stage3Plan
    {
        None,
        Grain,
        Channel,
        Boat,
    }

    public enum Stage3Benefit
    {
        None,
        Grain,
        Channel,
        Boat,
    }

    public enum Stage3Need
    {
        None,
        WaterRoute,
        GrainStorage,
        FieldWater,
    }

    [Flags]
    public enum Stage3TriedCombinations
    {
        None = 0,
        LowGrain = 1 << 0,
        LowChannel = 1 << 1,
        LowBoat = 1 << 2,
        MiddleGrain = 1 << 3,
        MiddleChannel = 1 << 4,
        MiddleBoat = 1 << 5,
        HighGrain = 1 << 6,
        HighChannel = 1 << 7,
        HighBoat = 1 << 8,
        All = (1 << 9) - 1,
    }

    public enum Stage3HelpMode
    {
        EqualPlans,
        OutcomeFacts,
        RiverSelection,
    }

    public enum Stage3MediaCueKind
    {
        None,
        Intro,
        Outcome,
    }

    public enum Stage3OutcomeVisualState
    {
        Choice,
        Prepared,
        Final,
    }

    public readonly struct Stage3EntryContext
    {
        public Stage3EntryContext(Stage2River river) => River = river;
        public Stage2River River { get; }
    }

    public readonly struct Stage3OutcomeKey : IEquatable<Stage3OutcomeKey>
    {
        public Stage3OutcomeKey(Stage2River river, Stage3Plan plan, Stage3Benefit benefit, Stage3Need need)
        {
            River = river;
            Plan = plan;
            Benefit = benefit;
            Need = need;
        }

        public Stage2River River { get; }
        public Stage3Plan Plan { get; }
        public Stage3Benefit Benefit { get; }
        public Stage3Need Need { get; }

        public bool Equals(Stage3OutcomeKey other) => River == other.River
            && Plan == other.Plan
            && Benefit == other.Benefit
            && Need == other.Need;

        public override bool Equals(object obj) => obj is Stage3OutcomeKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)River;
                hash = (hash * 397) ^ (int)Plan;
                hash = (hash * 397) ^ (int)Benefit;
                return (hash * 397) ^ (int)Need;
            }
        }

        public static bool operator ==(Stage3OutcomeKey left, Stage3OutcomeKey right) => left.Equals(right);
        public static bool operator !=(Stage3OutcomeKey left, Stage3OutcomeKey right) => !left.Equals(right);
    }

    public readonly struct Stage3MediaCue
    {
        public Stage3MediaCue(Guid routeTokenId, Guid tokenId, Stage3MediaCueKind kind,
            Stage3Plan plan, Sprite visualEquivalent, string captionId,
            bool captionVisible, double captionCueStartSeconds, double elapsedSeconds)
        {
            RouteTokenId = routeTokenId; TokenId = tokenId; Kind = kind; Plan = plan;
            VisualEquivalent = visualEquivalent; CaptionId = captionId;
            CaptionVisible = captionVisible; CaptionCueStartSeconds = captionCueStartSeconds;
            ElapsedSeconds = elapsedSeconds;
        }

        public Guid RouteTokenId { get; }
        public Stage3MediaCueKind Kind { get; }
        public Guid TokenId { get; }
        public Stage3Plan Plan { get; }
        public Sprite VisualEquivalent { get; }
        public string CaptionId { get; }
        public bool CaptionVisible { get; }
        public double CaptionCueStartSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool RequiresReading => false;
        public bool UsesEvaluationLanguage => false;
        public bool UsesSuccessOrFailureAudio => false;
    }

    public readonly struct Stage3OutcomePresentation
    {
        public Stage3OutcomePresentation(
            Stage3OutcomeKey outcome,
            Stage3OutcomeVisualState visualState,
            double elapsedSeconds,
            bool reducedMotion)
        {
            Outcome = outcome;
            VisualState = visualState;
            ElapsedSeconds = elapsedSeconds;
            ReducedMotion = reducedMotion;
        }

        public Stage3OutcomeKey Outcome { get; }
        public Stage3OutcomeVisualState VisualState { get; }
        public double ElapsedSeconds { get; }
        public bool ReducedMotion { get; }
    }

    public readonly struct Stage3Snapshot : IEquatable<Stage3Snapshot>
    {
        public Stage3Snapshot(
            Guid tokenId,
            Stage3State state,
            Stage2River selectedRiver,
            Stage3Plan selectedPlan,
            Stage3OutcomeKey currentOutcome,
            Stage3TriedCombinations triedCombinations,
            Guid outcomeTokenId,
            Guid instructionTokenId,
            double stateStartedAtSeconds,
            double outcomeElapsedSeconds,
            string focusedControlId,
            int optionCount,
            bool completionRecorded)
        {
            TokenId = tokenId;
            State = state;
            SelectedRiver = selectedRiver;
            SelectedPlan = selectedPlan;
            CurrentOutcome = currentOutcome;
            TriedCombinations = triedCombinations;
            OutcomeTokenId = outcomeTokenId;
            InstructionTokenId = instructionTokenId;
            StateStartedAtSeconds = stateStartedAtSeconds;
            OutcomeElapsedSeconds = outcomeElapsedSeconds;
            FocusedControlId = focusedControlId;
            OptionCount = optionCount;
            CompletionRecorded = completionRecorded;
        }

        public Guid TokenId { get; }
        public Stage3State State { get; }
        public Stage2River SelectedRiver { get; }
        public Stage3Plan SelectedPlan { get; }
        public Stage3OutcomeKey CurrentOutcome { get; }
        public Stage3TriedCombinations TriedCombinations { get; }
        public Guid OutcomeTokenId { get; }
        public Guid InstructionTokenId { get; }
        public double StateStartedAtSeconds { get; }
        public double OutcomeElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public int OptionCount { get; }
        public bool CompletionRecorded { get; }

        public bool Equals(Stage3Snapshot other) => TokenId == other.TokenId
            && State == other.State
            && SelectedRiver == other.SelectedRiver
            && SelectedPlan == other.SelectedPlan
            && CurrentOutcome.Equals(other.CurrentOutcome)
            && TriedCombinations == other.TriedCombinations
            && OutcomeTokenId == other.OutcomeTokenId
            && InstructionTokenId == other.InstructionTokenId
            && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
            && OutcomeElapsedSeconds.Equals(other.OutcomeElapsedSeconds)
            && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
            && OptionCount == other.OptionCount
            && CompletionRecorded == other.CompletionRecorded;

        public override bool Equals(object obj) => obj is Stage3Snapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TokenId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ (int)SelectedRiver;
                hash = (hash * 397) ^ (int)SelectedPlan;
                hash = (hash * 397) ^ CurrentOutcome.GetHashCode();
                hash = (hash * 397) ^ (int)TriedCombinations;
                hash = (hash * 397) ^ OutcomeTokenId.GetHashCode();
                hash = (hash * 397) ^ InstructionTokenId.GetHashCode();
                hash = (hash * 397) ^ StateStartedAtSeconds.GetHashCode();
                hash = (hash * 397) ^ OutcomeElapsedSeconds.GetHashCode();
                hash = (hash * 397) ^ (FocusedControlId?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ OptionCount;
                return (hash * 397) ^ CompletionRecorded.GetHashCode();
            }
        }
    }

    public interface IStage3View
    {
        void Present(Stage3Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IStage3HelpView
    {
        void Present(Stage3HelpMode mode, Stage3Snapshot caller, bool reducedMotion);
        void SetVisible(bool visible);
    }

    public interface IStage3MediaCueSink
    {
        void Present(Stage3MediaCue cue);
    }

    public interface IStage3OutcomeSink
    {
        void Present(Stage3OutcomePresentation presentation);
    }

    public interface IStage3PauseSink
    {
        bool TryShow(Stage3Snapshot snapshot);
    }
}
