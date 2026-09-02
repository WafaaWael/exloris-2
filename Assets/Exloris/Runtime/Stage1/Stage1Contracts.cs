using System;

namespace Exloris.Runtime.Stage1
{
    public enum Stage1State
    {
        Hidden,
        Intro,
        Playing,
        Help,
        OutcomePlaying,
        OutcomeReady,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public enum Stage1OutcomeKind
    {
        None,
        OutsideLow,
        InRange,
        OutsideHigh,
    }

    public enum Stage1MoveResult
    {
        Rejected,
        AttachCue,
        AtBoundary,
        Moved,
    }

    public enum Stage1PauseReason
    {
        Manual,
        AppBackground,
        AudioFocusLoss,
    }

    public readonly struct Stage1Snapshot : IEquatable<Stage1Snapshot>
    {
        public Stage1Snapshot(
            Guid tokenId,
            Stage1State state,
            bool markerAttached,
            double markerPosition,
            int supportTier,
            int optionCount,
            Stage1OutcomeKind outcome,
            Guid outcomeTokenId,
            Guid instructionTokenId,
            double stateStartedAtSeconds,
            double outcomeElapsedSeconds,
            string focusedControlId,
            bool outcomeFactRecorded,
            bool completionRecorded)
        {
            TokenId = tokenId;
            State = state;
            MarkerAttached = markerAttached;
            MarkerPosition = markerPosition;
            SupportTier = supportTier;
            OptionCount = optionCount;
            Outcome = outcome;
            OutcomeTokenId = outcomeTokenId;
            InstructionTokenId = instructionTokenId;
            StateStartedAtSeconds = stateStartedAtSeconds;
            OutcomeElapsedSeconds = outcomeElapsedSeconds;
            FocusedControlId = focusedControlId;
            OutcomeFactRecorded = outcomeFactRecorded;
            CompletionRecorded = completionRecorded;
        }

        public Guid TokenId { get; }
        public Stage1State State { get; }
        public bool MarkerAttached { get; }
        public double MarkerPosition { get; }
        public int SupportTier { get; }
        public int OptionCount { get; }
        public Stage1OutcomeKind Outcome { get; }
        public Guid OutcomeTokenId { get; }
        public Guid InstructionTokenId { get; }
        public double StateStartedAtSeconds { get; }
        public double OutcomeElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public bool OutcomeFactRecorded { get; }
        public bool CompletionRecorded { get; }

        public bool Equals(Stage1Snapshot other)
        {
            return TokenId == other.TokenId
                && State == other.State
                && MarkerAttached == other.MarkerAttached
                && MarkerPosition.Equals(other.MarkerPosition)
                && SupportTier == other.SupportTier
                && OptionCount == other.OptionCount
                && Outcome == other.Outcome
                && OutcomeTokenId == other.OutcomeTokenId
                && InstructionTokenId == other.InstructionTokenId
                && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
                && OutcomeElapsedSeconds.Equals(other.OutcomeElapsedSeconds)
                && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
                && OutcomeFactRecorded == other.OutcomeFactRecorded
                && CompletionRecorded == other.CompletionRecorded;
        }

        public override bool Equals(object obj) => obj is Stage1Snapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TokenId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ MarkerAttached.GetHashCode();
                hash = (hash * 397) ^ MarkerPosition.GetHashCode();
                hash = (hash * 397) ^ SupportTier;
                hash = (hash * 397) ^ OptionCount;
                hash = (hash * 397) ^ (int)Outcome;
                hash = (hash * 397) ^ OutcomeTokenId.GetHashCode();
                hash = (hash * 397) ^ InstructionTokenId.GetHashCode();
                hash = (hash * 397) ^ StateStartedAtSeconds.GetHashCode();
                hash = (hash * 397) ^ OutcomeElapsedSeconds.GetHashCode();
                hash = (hash * 397) ^ (FocusedControlId?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ OutcomeFactRecorded.GetHashCode();
                return (hash * 397) ^ CompletionRecorded.GetHashCode();
            }
        }
    }

    public interface IStage1View
    {
        void Present(Stage1Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IStage1PauseSink
    {
        bool TryShow(Stage1Snapshot snapshot);
    }
}
