using System;

namespace Exloris.Runtime.Boat
{
    public enum Boat3State
    {
        Hidden,
        Choice,
        Help,
        Traveling,
        Arriving,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public enum Boat3Choice
    {
        None,
        GrainStore,
        Irrigation,
        FollowIbis,
    }

    public enum Boat3PauseReason { Manual, AppBackground, AudioFocusLoss }

    public readonly struct Boat3Snapshot : IEquatable<Boat3Snapshot>
    {
        public Boat3Snapshot(Guid tokenId, Boat3State state, Boat3Choice choice,
            BoatDirection ibisDirection, double stateStartedAtSeconds,
            double travelElapsedSeconds, string focusedControlId, bool arrivalCommitted)
        {
            TokenId = tokenId;
            State = state;
            Choice = choice;
            IbisDirection = ibisDirection;
            StateStartedAtSeconds = stateStartedAtSeconds;
            TravelElapsedSeconds = travelElapsedSeconds;
            FocusedControlId = focusedControlId;
            ArrivalCommitted = arrivalCommitted;
        }

        public Guid TokenId { get; }
        public Boat3State State { get; }
        public Boat3Choice Choice { get; }
        public BoatDirection IbisDirection { get; }
        public double StateStartedAtSeconds { get; }
        public double TravelElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public bool ArrivalCommitted { get; }

        public bool Equals(Boat3Snapshot other) => TokenId == other.TokenId
            && State == other.State
            && Choice == other.Choice
            && IbisDirection == other.IbisDirection
            && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
            && TravelElapsedSeconds.Equals(other.TravelElapsedSeconds)
            && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
            && ArrivalCommitted == other.ArrivalCommitted;

        public override bool Equals(object obj) => obj is Boat3Snapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TokenId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ (int)Choice;
                hash = (hash * 397) ^ (int)IbisDirection;
                hash = (hash * 397) ^ StateStartedAtSeconds.GetHashCode();
                hash = (hash * 397) ^ TravelElapsedSeconds.GetHashCode();
                hash = (hash * 397) ^ (FocusedControlId?.GetHashCode() ?? 0);
                return (hash * 397) ^ ArrivalCommitted.GetHashCode();
            }
        }
    }

    public interface IBoat3View
    {
        void Present(Boat3Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IBoat3PauseSink
    {
        bool TryShow(Boat3Snapshot snapshot);
    }
}
