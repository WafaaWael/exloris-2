using System;

namespace Exloris.Runtime.Completion
{
    public enum CompletionState
    {
        Hidden,
        Presenting,
        Ready,
        Paused,
        Leaving,
        NavigatingAdult,
        Recovery,
    }

    public readonly struct CompletionSnapshot : IEquatable<CompletionSnapshot>
    {
        public CompletionSnapshot(
            Guid tokenId,
            CompletionState state,
            Guid presentationTokenId,
            double stateStartedAtSeconds,
            double elapsedSeconds,
            string focusedControlId,
            bool reducedMotion)
        {
            TokenId = tokenId;
            State = state;
            PresentationTokenId = presentationTokenId;
            StateStartedAtSeconds = stateStartedAtSeconds;
            ElapsedSeconds = elapsedSeconds;
            FocusedControlId = focusedControlId;
            ReducedMotion = reducedMotion;
        }

        public Guid TokenId { get; }
        public CompletionState State { get; }
        public Guid PresentationTokenId { get; }
        public double StateStartedAtSeconds { get; }
        public double ElapsedSeconds { get; }
        public string FocusedControlId { get; }
        public bool ReducedMotion { get; }

        public bool Equals(CompletionSnapshot other) => TokenId == other.TokenId
            && State == other.State
            && PresentationTokenId == other.PresentationTokenId
            && StateStartedAtSeconds.Equals(other.StateStartedAtSeconds)
            && ElapsedSeconds.Equals(other.ElapsedSeconds)
            && string.Equals(FocusedControlId, other.FocusedControlId, StringComparison.Ordinal)
            && ReducedMotion == other.ReducedMotion;

        public override bool Equals(object obj) => obj is CompletionSnapshot other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TokenId.GetHashCode();
                hash = (hash * 397) ^ (int)State;
                hash = (hash * 397) ^ PresentationTokenId.GetHashCode();
                hash = (hash * 397) ^ StateStartedAtSeconds.GetHashCode();
                hash = (hash * 397) ^ ElapsedSeconds.GetHashCode();
                hash = (hash * 397) ^ (FocusedControlId?.GetHashCode() ?? 0);
                return (hash * 397) ^ ReducedMotion.GetHashCode();
            }
        }
    }

    public interface ICompletionView
    {
        void Present(CompletionSnapshot snapshot);
        void SetInputReady(bool ready);
    }

    public interface ICompletionPauseSink
    {
        bool TryShow(CompletionSnapshot snapshot);
    }
}
