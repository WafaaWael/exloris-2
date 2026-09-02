using System;

namespace Exloris.Runtime.Stage2
{
    public enum Stage2State
    {
        Hidden,
        Intro,
        Choosing,
        Inspecting,
        ConsequencePlaying,
        Help,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public enum Stage2River
    {
        None,
        Low,
        Middle,
        High,
    }

    public enum Stage2Hotspot
    {
        None,
        Field,
        Path,
        Boat,
    }

    public enum Stage2PauseReason
    {
        User,
        AudioFocusLoss,
        AppBackground,
    }

    [Flags]
    public enum Stage2ObservedHotspots
    {
        None = 0,
        Field = 1,
        Path = 2,
        Boat = 4,
        All = Field | Path | Boat,
    }

    public readonly struct Stage2Snapshot : IEquatable<Stage2Snapshot>
    {
        public Stage2Snapshot(
            Guid tokenId,
            Stage2State state,
            Stage2River selectedRiver,
            Stage2ObservedHotspots lowObserved,
            Stage2ObservedHotspots middleObserved,
            Stage2ObservedHotspots highObserved,
            Stage2Hotspot currentHotspot,
            Guid consequenceTokenId,
            Guid instructionTokenId,
            double stateStartedAtSeconds,
            double consequenceElapsedSeconds,
            string focusedControlId,
            int optionCount,
            bool completionRecorded)
        {
            TokenId = tokenId;
            State = state;
            SelectedRiver = selectedRiver;
            LowObserved = lowObserved;
            MiddleObserved = middleObserved;
            HighObserved = highObserved;
            CurrentHotspot = currentHotspot;
            ConsequenceTokenId = consequenceTokenId;
            InstructionTokenId = instructionTokenId;
            StateStartedAtSeconds = stateStartedAtSeconds;
            ConsequenceElapsedSeconds = consequenceElapsedSeconds;
            FocusedControlId = focusedControlId;
            OptionCount = optionCount;
            CompletionRecorded = completionRecorded;
        }

        public Guid TokenId { get; }
        public Stage2State State { get; }
        public Stage2River SelectedRiver { get; }
        public Stage2ObservedHotspots LowObserved { get; }
        public Stage2ObservedHotspots MiddleObserved { get; }
        public Stage2ObservedHotspots HighObserved { get; }
        public Stage2Hotspot CurrentHotspot { get; }
        public Guid ConsequenceTokenId { get; }
        public Guid InstructionTokenId { get; }
        public double StateStartedAtSeconds { get; }
        public double ConsequenceElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public int OptionCount { get; }
        public bool CompletionRecorded { get; }

        public bool Equals(Stage2Snapshot other)
        {
            return TokenId == other.TokenId
                && State == other.State
                && SelectedRiver == other.SelectedRiver
                && LowObserved == other.LowObserved
                && MiddleObserved == other.MiddleObserved
                && HighObserved == other.HighObserved
                && CurrentHotspot == other.CurrentHotspot
                && ConsequenceTokenId == other.ConsequenceTokenId
                && InstructionTokenId == other.InstructionTokenId
                && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
                && ConsequenceElapsedSeconds.Equals(other.ConsequenceElapsedSeconds)
                && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
                && OptionCount == other.OptionCount
                && CompletionRecorded == other.CompletionRecorded;
        }

        public override bool Equals(object obj) => obj is Stage2Snapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TokenId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ (int)SelectedRiver;
                hash = (hash * 397) ^ (int)LowObserved;
                hash = (hash * 397) ^ (int)MiddleObserved;
                hash = (hash * 397) ^ (int)HighObserved;
                hash = (hash * 397) ^ (int)CurrentHotspot;
                hash = (hash * 397) ^ ConsequenceTokenId.GetHashCode();
                hash = (hash * 397) ^ InstructionTokenId.GetHashCode();
                hash = (hash * 397) ^ StateStartedAtSeconds.GetHashCode();
                hash = (hash * 397) ^ ConsequenceElapsedSeconds.GetHashCode();
                hash = (hash * 397) ^ (FocusedControlId?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ OptionCount;
                return (hash * 397) ^ CompletionRecorded.GetHashCode();
            }
        }
    }

    public interface IStage2View
    {
        void Present(Stage2Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IStage2PauseSink
    {
        bool TryShow(Stage2Snapshot snapshot);
    }
}
