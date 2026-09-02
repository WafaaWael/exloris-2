using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Overview;

namespace Exloris.Runtime.Recovery
{
    public readonly struct PauseCallerHandle : IEquatable<PauseCallerHandle>
    {
        public PauseCallerHandle(RouteId route, Guid callerTokenId, Guid snapshotToken)
        {
            if (callerTokenId == Guid.Empty) throw new ArgumentException("Caller token cannot be empty.", nameof(callerTokenId));
            if (snapshotToken == Guid.Empty) throw new ArgumentException("Snapshot token cannot be empty.", nameof(snapshotToken));
            Route = route; CallerTokenId = callerTokenId; SnapshotToken = snapshotToken;
        }
        public RouteId Route { get; }
        public Guid CallerTokenId { get; }
        public Guid SnapshotToken { get; }
        public bool Equals(PauseCallerHandle other) => Route == other.Route && CallerTokenId == other.CallerTokenId && SnapshotToken == other.SnapshotToken;
        public override bool Equals(object obj) => obj is PauseCallerHandle other && Equals(other);
        public override int GetHashCode() => ((int)Route * 397) ^ CallerTokenId.GetHashCode() ^ SnapshotToken.GetHashCode();
    }

    public interface IPauseCallerAdapter
    {
        RouteId Route { get; }
        bool TryCapture(out PauseCallerHandle handle);
        bool IsCurrent(PauseCallerHandle handle);
        bool Restore(PauseCallerHandle handle, double nowSeconds);
        bool PauseMedia(PauseCallerHandle handle, double nowSeconds);
        bool ResumeMedia(PauseCallerHandle handle, double nowSeconds);
        void RefreshSettings(double nowSeconds);
    }

    public enum PauseLayerState
    {
        Hidden,
        Pause,
        Help,
        Leave,
        Transitioning,
    }

    public readonly struct PauseSettings : IEquatable<PauseSettings>
    {
        public PauseSettings(bool soundEnabled, bool captionsEnabled, bool reducedMotion)
        {
            SoundEnabled = soundEnabled;
            CaptionsEnabled = captionsEnabled;
            ReducedMotion = reducedMotion;
        }

        public bool SoundEnabled { get; }
        public bool CaptionsEnabled { get; }
        public bool ReducedMotion { get; }

        public bool Equals(PauseSettings other) =>
            SoundEnabled == other.SoundEnabled
            && CaptionsEnabled == other.CaptionsEnabled
            && ReducedMotion == other.ReducedMotion;
        public override bool Equals(object obj) => obj is PauseSettings other && Equals(other);
        public override int GetHashCode() =>
            (SoundEnabled ? 1 : 0) | (CaptionsEnabled ? 2 : 0) | (ReducedMotion ? 4 : 0);
    }

    public readonly struct PauseOverlaySnapshot : IEquatable<PauseOverlaySnapshot>
    {
        public PauseOverlaySnapshot(
            Guid stackToken,
            JourneyOverviewSnapshot callerSnapshot,
            PauseSettings settings)
        {
            if (stackToken == Guid.Empty)
                throw new ArgumentException("Pause stack token cannot be empty.", nameof(stackToken));
            StackToken = stackToken;
            CallerSnapshot = callerSnapshot;
            CallerHandle = new PauseCallerHandle(RouteId.JourneyOverview, callerSnapshot.TokenId, stackToken);
            Settings = settings;
        }

        public PauseOverlaySnapshot(Guid stackToken, PauseCallerHandle callerHandle, PauseSettings settings)
        {
            if (stackToken == Guid.Empty) throw new ArgumentException("Pause stack token cannot be empty.", nameof(stackToken));
            StackToken = stackToken; CallerHandle = callerHandle; CallerSnapshot = default; Settings = settings;
        }

        public PauseOverlaySnapshot(Guid stackToken, PauseCallerHandle callerHandle, JourneyOverviewSnapshot callerSnapshot, PauseSettings settings)
        {
            if (stackToken == Guid.Empty) throw new ArgumentException("Pause stack token cannot be empty.", nameof(stackToken));
            StackToken = stackToken; CallerHandle = callerHandle; CallerSnapshot = callerSnapshot; Settings = settings;
        }

        public Guid StackToken { get; }
        public JourneyOverviewSnapshot CallerSnapshot { get; }
        public PauseCallerHandle CallerHandle { get; }
        public PauseSettings Settings { get; }

        public bool Equals(PauseOverlaySnapshot other) =>
            StackToken == other.StackToken
            && CallerHandle.Equals(other.CallerHandle)
            && CallerSnapshot.TokenId == other.CallerSnapshot.TokenId
            && CallerSnapshot.State == other.CallerSnapshot.State
            && CallerSnapshot.ElapsedSeconds.Equals(other.CallerSnapshot.ElapsedSeconds)
            && CallerSnapshot.RevealedBeatCount == other.CallerSnapshot.RevealedBeatCount
            && Settings.Equals(other.Settings);
        public override bool Equals(object obj) =>
            obj is PauseOverlaySnapshot other && Equals(other);
        public override int GetHashCode() => StackToken.GetHashCode();

        public PauseOverlaySnapshot WithSettings(PauseSettings settings) =>
            CallerHandle.Route == RouteId.JourneyOverview && CallerSnapshot.TokenId != Guid.Empty
                ? new PauseOverlaySnapshot(StackToken, CallerHandle, CallerSnapshot, settings)
                : new PauseOverlaySnapshot(StackToken, CallerHandle, settings);
    }

    public interface IPauseOverlayView
    {
        void ShowAndBlockCaller(PauseOverlaySnapshot snapshot);
        void RestoreCaller(PauseOverlaySnapshot snapshot);
        void HoldBlockedForChild(PauseOverlaySnapshot snapshot);
        void RestoreFromChild(PauseOverlaySnapshot snapshot);
        void HoldBlockedForTransition(PauseOverlaySnapshot snapshot);
        void ApplySettings(PauseSettings settings);
    }

    public interface IJourneyOverviewPauseHelpSink
    {
        bool TryShow(PauseOverlaySnapshot snapshot);
    }

    public interface IPauseOverlayReturnSink
    {
        bool IsCurrentPause(PauseOverlaySnapshot snapshot);
        bool RestoreFromHelp(PauseOverlaySnapshot snapshot);
    }

    public interface IJourneyOverviewPauseHelpView
    {
        void ShowAbovePause(PauseOverlaySnapshot snapshot);
        void Hide();
    }

    public enum LeaveReturnLayer
    {
        RouteCaller,
        PauseOverlay,
    }

    public readonly struct LeaveRequest : IEquatable<LeaveRequest>
    {
        public LeaveRequest(RouteId caller, LeaveReturnLayer returnLayer, Guid stackToken)
        {
            Caller = caller;
            ReturnLayer = returnLayer;
            StackToken = stackToken;
        }

        public RouteId Caller { get; }
        public LeaveReturnLayer ReturnLayer { get; }
        public Guid StackToken { get; }
        public bool Equals(LeaveRequest other) => Caller == other.Caller
            && ReturnLayer == other.ReturnLayer && StackToken == other.StackToken;
        public override bool Equals(object obj) => obj is LeaveRequest other && Equals(other);
        public override int GetHashCode() => StackToken.GetHashCode() ^ (int)Caller ^ (int)ReturnLayer;
    }

    public interface INestedLeaveConfirmationSink
    {
        bool TryShow(LeaveRequest request);
    }

    public interface INestedLeaveReturnSink
    {
        bool RestoreFromLeave(LeaveRequest request);
        bool HoldForLeaveTransition(LeaveRequest request);
    }

    public interface INestedLeaveConfirmationView
    {
        void ShowAndBlockPause(LeaveRequest request);
        void HideAndRestorePause(LeaveRequest request);
        void HoldPauseBlockedForTransition(LeaveRequest request);
    }

    public enum LeaveConfirmationState
    {
        Hidden,
        Visible,
        Transitioning,
    }
}
