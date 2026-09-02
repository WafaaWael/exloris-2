using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;

namespace Exloris.Runtime.Overview
{
    public sealed class JourneyOverviewController
    {
        public const double ReadBeatSeconds = 3.289d;
        public const double HelpBeatSeconds = 6.182d;
        public const double CompletionSeconds = 9.000d;

        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IJourneyOverviewView _view;
        private readonly IPauseOverlaySink _pauseSink;
        private readonly ILeaveConfirmationSink _leaveSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();

        private double _startedAtSeconds;
        private double _elapsedSeconds;
        private int _revealedBeatCount;
        private bool _reducedMotion;
        private bool _reanchorOnNextAdvance;
        private JourneyOverviewSnapshot _pauseSnapshot;
        private JourneyOverviewSnapshot _leaveSnapshot;

        public JourneyOverviewController(
            SessionState session,
            NavigationAuthority navigation,
            IJourneyOverviewView view,
            IPauseOverlaySink pauseSink,
            ILeaveConfirmationSink leaveSink,
            Func<Guid> tokenSource)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _pauseSink = pauseSink ?? throw new ArgumentNullException(nameof(pauseSink));
            _leaveSink = leaveSink ?? throw new ArgumentNullException(nameof(leaveSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public JourneyOverviewRevealState State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public JourneyOverviewSnapshot Snapshot => new JourneyOverviewSnapshot(
            CurrentTokenId,
            State,
            _elapsedSeconds,
            _revealedBeatCount);

        public bool Enter(double nowSeconds, bool reducedMotion)
        {
            if (State != JourneyOverviewRevealState.Hidden || !CanPresent())
            {
                return false;
            }

            StartReveal(nowSeconds, reducedMotion);
            return true;
        }

        public bool Advance(Guid tokenId, double nowSeconds)
        {
            if (!IsActiveReveal() || tokenId != CurrentTokenId || !CanPresent())
            {
                return false;
            }

            if (!TrySetElapsed(nowSeconds))
            {
                return false;
            }

            if (_elapsedSeconds >= CompletionSeconds)
            {
                return RequestBoat(nowSeconds);
            }

            UpdateRevealState();
            Present(true);
            return true;
        }

        public bool HandlePlaybackCompleted(Guid tokenId, double nowSeconds)
        {
            if (!IsActiveReveal() || tokenId != CurrentTokenId || !CanPresent())
            {
                return false;
            }

            if (!TrySetElapsed(nowSeconds) || _elapsedSeconds < CompletionSeconds)
            {
                return false;
            }

            return RequestBoat(nowSeconds);
        }

        public bool HandleSkip(Guid tokenId, double nowSeconds)
        {
            return IsActiveReveal()
                && tokenId == CurrentTokenId
                && CanPresent()
                && RequestBoat(nowSeconds);
        }

        public bool HandleRepeat(double nowSeconds)
        {
            if (!IsActiveReveal() || !CanPresent())
            {
                return false;
            }

            StartReveal(nowSeconds, _reducedMotion);
            return true;
        }

        public bool HandlePause()
        {
            if (!IsActiveReveal() || !CanPresent())
            {
                return false;
            }

            var captured = Snapshot;
            if (!_pauseSink.TryShow(captured))
            {
                return false;
            }

            _pauseSnapshot = captured;
            State = JourneyOverviewRevealState.Paused;
            Present(false);
            return true;
        }

        public bool HandleHome()
        {
            if (!IsActiveReveal() || !CanPresent())
            {
                return false;
            }

            var captured = Snapshot;
            if (!_leaveSink.TryShow(RouteId.JourneyOverview))
            {
                return false;
            }

            _leaveSnapshot = captured;
            State = JourneyOverviewRevealState.Leaving;
            Present(false);
            return true;
        }

        public bool RestoreFromLeave(JourneyOverviewSnapshot snapshot)
        {
            if (State != JourneyOverviewRevealState.Leaving
                || !SnapshotsMatch(snapshot, _leaveSnapshot)
                || !IsRevealState(snapshot.State)
                || !CanPresent())
            {
                return false;
            }

            State = snapshot.State;
            _elapsedSeconds = snapshot.ElapsedSeconds;
            _revealedBeatCount = snapshot.RevealedBeatCount;
            _reanchorOnNextAdvance = true;
            Present(true);
            return true;
        }

        public bool ApplyReducedMotion(bool reducedMotion)
        {
            if (!IsActiveReveal() && State != JourneyOverviewRevealState.Paused)
                return false;
            _reducedMotion = reducedMotion;
            Present(IsActiveReveal());
            return true;
        }

        public bool EnterRecovery()
        {
            if (State != JourneyOverviewRevealState.Navigating
                || _navigation.Recovery == null)
                return false;
            State = JourneyOverviewRevealState.Recovery;
            Present(false);
            return true;
        }

        public bool RetryBoundary()
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != JourneyOverviewRevealState.Recovery
                || transaction == null
                || transaction.Caller != RouteId.JourneyOverview
                || transaction.Destination != RouteId.Boat1)
                return false;
            State = JourneyOverviewRevealState.Navigating;
            Present(false);
            return true;
        }

        public bool Restore(JourneyOverviewSnapshot snapshot)
        {
            if (State != JourneyOverviewRevealState.Paused
                || snapshot.TokenId != CurrentTokenId
                || !SnapshotsMatch(snapshot, _pauseSnapshot)
                || !IsRevealState(snapshot.State)
                || !CanPresent())
            {
                return false;
            }

            State = snapshot.State;
            _elapsedSeconds = snapshot.ElapsedSeconds;
            _revealedBeatCount = snapshot.RevealedBeatCount;
            _reanchorOnNextAdvance = true;
            Present(true);
            return true;
        }

        private void StartReveal(double nowSeconds, bool reducedMotion)
        {
            if (double.IsNaN(nowSeconds) || double.IsInfinity(nowSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(nowSeconds));
            }

            CurrentTokenId = NextToken();
            _startedAtSeconds = nowSeconds;
            _reanchorOnNextAdvance = false;
            _elapsedSeconds = 0d;
            _reducedMotion = reducedMotion;
            if (reducedMotion)
            {
                State = JourneyOverviewRevealState.Ready;
                _revealedBeatCount = 3;
            }
            else
            {
                State = JourneyOverviewRevealState.RevealingMeasure;
                _revealedBeatCount = 1;
            }

            Present(true);
        }

        private bool TrySetElapsed(double nowSeconds)
        {
            if (double.IsNaN(nowSeconds)
                || double.IsInfinity(nowSeconds)
                || nowSeconds < _startedAtSeconds)
            {
                return false;
            }

            if (_reanchorOnNextAdvance)
            {
                _startedAtSeconds = nowSeconds - _elapsedSeconds;
                _reanchorOnNextAdvance = false;
            }

            _elapsedSeconds = nowSeconds - _startedAtSeconds;
            return true;
        }

        private void UpdateRevealState()
        {
            if (_reducedMotion)
            {
                State = JourneyOverviewRevealState.Ready;
                _revealedBeatCount = 3;
            }
            else if (_elapsedSeconds >= HelpBeatSeconds)
            {
                State = JourneyOverviewRevealState.RevealingHelp;
                _revealedBeatCount = 3;
            }
            else if (_elapsedSeconds >= ReadBeatSeconds)
            {
                State = JourneyOverviewRevealState.RevealingRead;
                _revealedBeatCount = 2;
            }
            else
            {
                State = JourneyOverviewRevealState.RevealingMeasure;
                _revealedBeatCount = 1;
            }
        }

        private bool RequestBoat(double nowSeconds)
        {
            var request = _navigation.Request(RouteId.Boat1, nowSeconds);
            if (!request.Accepted)
            {
                return false;
            }

            _elapsedSeconds = Math.Max(_elapsedSeconds, CompletionSeconds);
            _revealedBeatCount = 3;
            State = JourneyOverviewRevealState.Navigating;
            Present(false);
            return true;
        }

        private bool CanPresent()
        {
            return _session.IsOpen
                && _session.CurrentOpenSessionLocation == RouteId.JourneyOverview
                && _navigation.CurrentRoute == RouteId.JourneyOverview
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private bool IsActiveReveal()
        {
            return IsRevealState(State);
        }

        private static bool IsRevealState(JourneyOverviewRevealState state)
        {
            return state == JourneyOverviewRevealState.RevealingMeasure
                || state == JourneyOverviewRevealState.RevealingRead
                || state == JourneyOverviewRevealState.RevealingHelp
                || state == JourneyOverviewRevealState.Ready;
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
            {
                throw new InvalidOperationException(
                    "Journey Overview tokens must be non-empty and unique.");
            }

            return token;
        }

        private static bool SnapshotsMatch(
            JourneyOverviewSnapshot left,
            JourneyOverviewSnapshot right)
        {
            return left.TokenId == right.TokenId
                && left.State == right.State
                && Math.Abs(left.ElapsedSeconds - right.ElapsedSeconds) < 0.000001d
                && left.RevealedBeatCount == right.RevealedBeatCount;
        }
    }
}
