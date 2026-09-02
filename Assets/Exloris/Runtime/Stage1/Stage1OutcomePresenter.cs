using System;
using UnityEngine;

namespace Exloris.Runtime.Stage1
{
    public enum Stage1OutcomePhase
    {
        Reveal,
        Hold,
        Settle,
        Before,
        After,
        Complete,
    }

    public readonly struct Stage1OutcomeFrame
    {
        public Stage1OutcomeFrame(
            Guid routeTokenId,
            Guid outcomeTokenId,
            Stage1OutcomeKind outcome,
            Sprite outcomeLayer,
            double markerPosition,
            Stage1OutcomePhase phase,
            double elapsedSeconds,
            bool reducedMotion)
        {
            RouteTokenId = routeTokenId;
            OutcomeTokenId = outcomeTokenId;
            Outcome = outcome;
            OutcomeLayer = outcomeLayer;
            MarkerPosition = markerPosition;
            Phase = phase;
            ElapsedSeconds = elapsedSeconds;
            ReducedMotion = reducedMotion;
        }

        public Guid RouteTokenId { get; }
        public Guid OutcomeTokenId { get; }
        public Stage1OutcomeKind Outcome { get; }
        public Sprite OutcomeLayer { get; }
        public double MarkerPosition { get; }
        public Stage1OutcomePhase Phase { get; }
        public double ElapsedSeconds { get; }
        public bool ReducedMotion { get; }
        public bool SpatialMotionEnabled => !ReducedMotion && Phase != Stage1OutcomePhase.Complete;
        public bool UsesEvaluationLanguage => false;
        public bool UsesSuccessOrFailureAudio => false;
    }

    public interface IStage1OutcomeSink
    {
        void PresentOutcome(Stage1OutcomeFrame frame);
    }

    public sealed class Stage1OutcomePresenter
    {
        public const double DurationSeconds = 3.2d;
        public const double RevealSeconds = 0.8d;
        public const double SettleSeconds = 2.4d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly Stage1Content _content;
        private readonly IStage1OutcomeSink _sink;
        private Guid _routeTokenId;
        private Guid _outcomeTokenId;
        private Stage1OutcomeKind _outcome;
        private Sprite _outcomeLayer;
        private double _markerPosition;
        private double _startedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _reducedMotion;
        private bool _active;
        private bool _paused;

        public Stage1OutcomePresenter(Stage1Content content, IStage1OutcomeSink sink)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public double ElapsedSeconds => _elapsed;
        public bool IsPaused => _paused;

        public bool Start(Stage1Snapshot snapshot, bool reducedMotion, double now)
        {
            if (_active
                || snapshot.TokenId == Guid.Empty
                || snapshot.OutcomeTokenId == Guid.Empty
                || snapshot.Outcome == Stage1OutcomeKind.None
                || !Finite(snapshot.MarkerPosition)
                || snapshot.MarkerPosition < 0d
                || snapshot.MarkerPosition > 1d
                || !Finite(now))
                return false;

            _routeTokenId = snapshot.TokenId;
            _outcomeTokenId = snapshot.OutcomeTokenId;
            _outcome = snapshot.Outcome;
            _outcomeLayer = ResolveLayer(snapshot.Outcome);
            _markerPosition = snapshot.MarkerPosition;
            _startedAt = now;
            _elapsed = 0d;
            _reducedMotion = reducedMotion;
            _active = true;
            _paused = false;
            Present();
            return true;
        }

        public bool Advance(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || _paused || !Finite(now) || now < _startedAt) return false;
            _elapsed = Math.Min(DurationSeconds, now - _startedAt);
            Present();
            return true;
        }

        public bool CanFreeze(Guid routeTokenId, Guid outcomeTokenId, double now) =>
            Current(routeTokenId, outcomeTokenId) && !_paused && Finite(now) && now >= _startedAt;

        public bool Freeze(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!CanFreeze(routeTokenId, outcomeTokenId, now) || !Advance(routeTokenId, outcomeTokenId, now)) return false;
            _paused = true;
            _pausedAt = now;
            return true;
        }

        public bool Resume(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!CanResume(routeTokenId, outcomeTokenId, now)) return false;
            _startedAt = now - _elapsed;
            _paused = false;
            Present();
            return true;
        }

        public bool CanResume(Guid routeTokenId, Guid outcomeTokenId, double now) =>
            Current(routeTokenId, outcomeTokenId) && _paused && Finite(now) && now >= _pausedAt;

        public bool Stop(Guid routeTokenId, Guid outcomeTokenId)
        {
            if (!Current(routeTokenId, outcomeTokenId)) return false;
            _active = false;
            _paused = false;
            return true;
        }

        private bool Current(Guid routeTokenId, Guid outcomeTokenId) =>
            _active && routeTokenId == _routeTokenId && outcomeTokenId == _outcomeTokenId;

        private Sprite ResolveLayer(Stage1OutcomeKind outcome)
        {
            if (outcome == Stage1OutcomeKind.InRange) return _content.OutcomeLayers[0];
            if (outcome == Stage1OutcomeKind.OutsideLow) return _content.OutcomeLayers[1];
            if (outcome == Stage1OutcomeKind.OutsideHigh) return _content.OutcomeLayers[2];
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        private void Present()
        {
            _sink.PresentOutcome(new Stage1OutcomeFrame(
                _routeTokenId,
                _outcomeTokenId,
                _outcome,
                _outcomeLayer,
                _markerPosition,
                ResolvePhase(),
                _elapsed,
                _reducedMotion));
        }

        private Stage1OutcomePhase ResolvePhase()
        {
            if (_elapsed >= DurationSeconds - ClockEpsilon) return Stage1OutcomePhase.Complete;
            if (_reducedMotion) return _elapsed < RevealSeconds - ClockEpsilon ? Stage1OutcomePhase.Before : Stage1OutcomePhase.After;
            if (_elapsed < RevealSeconds - ClockEpsilon) return Stage1OutcomePhase.Reveal;
            if (_elapsed < SettleSeconds - ClockEpsilon) return Stage1OutcomePhase.Hold;
            return Stage1OutcomePhase.Settle;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
