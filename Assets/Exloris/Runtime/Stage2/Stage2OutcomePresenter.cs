using System;
using UnityEngine;

namespace Exloris.Runtime.Stage2
{
    public enum Stage2OutcomePhase
    {
        Reveal,
        Hold,
        Settle,
        Before,
        After,
        Complete,
    }

    public readonly struct Stage2OutcomeFrame
    {
        public Stage2OutcomeFrame(
            Guid routeTokenId, Guid outcomeTokenId, Stage2River river, Stage2Hotspot hotspot,
            Sprite outcomeLayer, Stage2OutcomePhase phase, double elapsedSeconds, bool reducedMotion)
        {
            RouteTokenId = routeTokenId;
            OutcomeTokenId = outcomeTokenId;
            River = river;
            Hotspot = hotspot;
            OutcomeLayer = outcomeLayer;
            Phase = phase;
            ElapsedSeconds = elapsedSeconds;
            ReducedMotion = reducedMotion;
        }

        public Guid RouteTokenId { get; }
        public Guid OutcomeTokenId { get; }
        public Stage2River River { get; }
        public Stage2Hotspot Hotspot { get; }
        public Sprite OutcomeLayer { get; }
        public Stage2OutcomePhase Phase { get; }
        public double ElapsedSeconds { get; }
        public bool ReducedMotion { get; }
        public bool SpatialMotionEnabled => !ReducedMotion && Phase != Stage2OutcomePhase.Complete;
        public bool UsesEvaluationLanguage => false;
        public bool UsesSuccessOrFailureAudio => false;
    }

    public interface IStage2OutcomeSink
    {
        void PresentOutcome(Stage2OutcomeFrame frame);
    }

    public sealed class Stage2OutcomePresenter
    {
        public const double DurationSeconds = 2.4d;
        public const double RevealSeconds = 0.6d;
        public const double SettleSeconds = 1.8d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly Stage2Content _content;
        private readonly IStage2OutcomeSink _sink;
        private Guid _routeTokenId;
        private Guid _outcomeTokenId;
        private Stage2River _river;
        private Stage2Hotspot _hotspot;
        private Sprite _outcomeLayer;
        private double _startedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _reducedMotion;
        private bool _active;
        private bool _paused;

        public Stage2OutcomePresenter(Stage2Content content, IStage2OutcomeSink sink)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public double ElapsedSeconds => _elapsed;
        public bool IsPaused => _paused;

        public bool Start(Stage2Snapshot snapshot, bool reducedMotion, double now)
        {
            if (_active || snapshot.TokenId == Guid.Empty || snapshot.ConsequenceTokenId == Guid.Empty
                || snapshot.State != Stage2State.ConsequencePlaying
                || !IsRiver(snapshot.SelectedRiver) || !IsHotspot(snapshot.CurrentHotspot) || !Finite(now))
                return false;
            _routeTokenId = snapshot.TokenId;
            _outcomeTokenId = snapshot.ConsequenceTokenId;
            _river = snapshot.SelectedRiver;
            _hotspot = snapshot.CurrentHotspot;
            _outcomeLayer = _content.OutcomeLayers[((int)_river - 1) * 3 + (int)_hotspot - 1];
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
            var nextElapsed = Math.Min(DurationSeconds, now - _startedAt);
            if (nextElapsed < _elapsed - ClockEpsilon) return false;
            _elapsed = nextElapsed;
            Present();
            return true;
        }

        public bool Freeze(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || _paused || !Finite(now) || now < _startedAt
                || !Advance(routeTokenId, outcomeTokenId, now)) return false;
            _paused = true;
            _pausedAt = now;
            return true;
        }

        public bool CanFreeze(Guid routeTokenId, Guid outcomeTokenId, double now) =>
            Current(routeTokenId, outcomeTokenId) && !_paused && Finite(now) && now >= _startedAt;

        public bool Resume(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || !_paused || !Finite(now) || now < _pausedAt) return false;
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

        private void Present()
        {
            _sink.PresentOutcome(new Stage2OutcomeFrame(
                _routeTokenId, _outcomeTokenId, _river, _hotspot, _outcomeLayer,
                ResolvePhase(), _elapsed, _reducedMotion));
        }

        private Stage2OutcomePhase ResolvePhase()
        {
            if (_elapsed >= DurationSeconds - ClockEpsilon) return Stage2OutcomePhase.Complete;
            if (_reducedMotion) return _elapsed < RevealSeconds - ClockEpsilon
                ? Stage2OutcomePhase.Before : Stage2OutcomePhase.After;
            if (_elapsed < RevealSeconds - ClockEpsilon) return Stage2OutcomePhase.Reveal;
            if (_elapsed < SettleSeconds - ClockEpsilon) return Stage2OutcomePhase.Hold;
            return Stage2OutcomePhase.Settle;
        }

        private static bool IsRiver(Stage2River value) => value >= Stage2River.Low && value <= Stage2River.High;
        private static bool IsHotspot(Stage2Hotspot value) => value >= Stage2Hotspot.Field && value <= Stage2Hotspot.Boat;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
