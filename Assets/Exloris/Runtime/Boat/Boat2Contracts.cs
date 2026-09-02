using System;

namespace Exloris.Runtime.Boat
{
    public enum Boat2State
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

    public enum Boat2Choice
    {
        None,
        FieldBank,
        Dock,
        FollowIbis,
    }

    public enum Boat2PauseReason { Manual, AppBackground, AudioFocusLoss }

    public readonly struct Boat2Snapshot : IEquatable<Boat2Snapshot>
    {
        public Boat2Snapshot(Guid tokenId, Boat2State state, Boat2Choice choice,
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
        public Boat2State State { get; }
        public Boat2Choice Choice { get; }
        public BoatDirection IbisDirection { get; }
        public double StateStartedAtSeconds { get; }
        public double TravelElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public bool ArrivalCommitted { get; }

        public bool Equals(Boat2Snapshot other) => TokenId == other.TokenId
            && State == other.State
            && Choice == other.Choice
            && IbisDirection == other.IbisDirection
            && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
            && TravelElapsedSeconds.Equals(other.TravelElapsedSeconds)
            && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
            && ArrivalCommitted == other.ArrivalCommitted;

        public override bool Equals(object obj) => obj is Boat2Snapshot other && Equals(other);

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

    public interface IBoat2View
    {
        void Present(Boat2Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IBoat2PauseSink
    {
        bool TryShow(Boat2Snapshot snapshot);
    }
}
