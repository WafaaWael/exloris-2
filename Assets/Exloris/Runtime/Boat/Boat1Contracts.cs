using System;

namespace Exloris.Runtime.Boat
{
    public enum Boat1State
    {
        Hidden,
        Preparing,
        Choice,
        Help,
        Traveling,
        Arriving,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public enum Boat1Choice
    {
        None,
        Reeds,
        Village,
        FollowIbis,
    }

    public enum BoatDirection
    {
        Left,
        Right,
    }

    public enum Boat1PauseReason
    {
        Manual,
        AppBackground,
        AudioFocusLoss,
    }

    public readonly struct Boat1Snapshot : IEquatable<Boat1Snapshot>
    {
        public Boat1Snapshot(
            Guid tokenId,
            Boat1State state,
            Boat1Choice choice,
            BoatDirection ibisDirection,
            double stateStartedAtSeconds,
            double travelElapsedSeconds,
            string focusedControlId,
            bool arrivalCommitted)
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
        public Boat1State State { get; }
        public Boat1Choice Choice { get; }
        public BoatDirection IbisDirection { get; }
        public double StateStartedAtSeconds { get; }
        public double TravelElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public bool ArrivalCommitted { get; }

        public bool Equals(Boat1Snapshot other)
        {
            return TokenId == other.TokenId
                && State == other.State
                && Choice == other.Choice
                && IbisDirection == other.IbisDirection
                && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
                && TravelElapsedSeconds.Equals(other.TravelElapsedSeconds)
                && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
                && ArrivalCommitted == other.ArrivalCommitted;
        }

        public override bool Equals(object obj) => obj is Boat1Snapshot other && Equals(other);

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

    public interface IBoat1View
    {
        void Present(Boat1Snapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IBoat1PauseSink
    {
        bool TryShow(Boat1Snapshot snapshot);
    }
}
