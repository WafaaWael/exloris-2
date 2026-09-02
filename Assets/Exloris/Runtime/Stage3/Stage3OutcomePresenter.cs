using System;

namespace Exloris.Runtime.Stage3
{
    public sealed class Stage3OutcomePresenter
    {
        public const double DurationSeconds = 6.735d;
        public const double PreparedKeySeconds = 0.8d;
        public const double FinalKeySeconds = 2.0d;
        public const double HoldKeySeconds = 3.6d;
        private const double ClockEpsilon = 0.000000001d;

        private readonly IStage3OutcomeSink _sink;
        private Guid _routeTokenId;
        private Guid _outcomeTokenId;
        private Stage3OutcomeKey _outcome;
        private double _startedAt;
        private double _elapsed;
        private double _pausedAt;
        private bool _reducedMotion;
        private bool _active;
        private bool _paused;

        public Stage3OutcomePresenter(IStage3OutcomeSink sink) =>
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));

        public double ElapsedSeconds => _elapsed;
        public bool IsPaused => _paused;
        public Guid ActivePresentationTokenId { get; private set; }
        public bool CanFreeze(Guid routeTokenId, Guid outcomeTokenId, double now) =>
            Current(routeTokenId, outcomeTokenId) && !_paused && Finite(now) && now >= _startedAt;
        public bool CanResume(Guid routeTokenId, Guid outcomeTokenId, double now) =>
            Current(routeTokenId, outcomeTokenId) && _paused && Finite(now) && now >= _pausedAt;

        public bool Start(Stage3Snapshot snapshot, bool reducedMotion, double now)
        {
            if (_active || snapshot.TokenId == Guid.Empty || snapshot.OutcomeTokenId == Guid.Empty
                || snapshot.State != Stage3State.OutcomePlaying || !Valid(snapshot.CurrentOutcome) || !Finite(now))
                return false;
            _routeTokenId = snapshot.TokenId; _outcomeTokenId = snapshot.OutcomeTokenId;
            _outcome = snapshot.CurrentOutcome; _startedAt = now; _elapsed = 0;
            _reducedMotion = reducedMotion; _active = true; _paused = false;
            ActivePresentationTokenId = Guid.NewGuid();
            Present();
            return true;
        }

        public bool Advance(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || _paused || !Finite(now) || now < _startedAt) return false;
            var next = Math.Min(DurationSeconds, now - _startedAt);
            if (next < _elapsed - ClockEpsilon) return false;
            _elapsed = next; Present(); return true;
        }

        public bool Freeze(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || _paused || !Advance(routeTokenId, outcomeTokenId, now))
                return false;
            _paused = true; _pausedAt = now; return true;
        }

        public bool Resume(Guid routeTokenId, Guid outcomeTokenId, double now)
        {
            if (!Current(routeTokenId, outcomeTokenId) || !_paused || !Finite(now) || now < _pausedAt) return false;
            _startedAt = now - _elapsed; _paused = false;
            ActivePresentationTokenId = Guid.NewGuid();
            Present(); return true;
        }

        public bool Stop(Guid routeTokenId, Guid outcomeTokenId)
        {
            if (!Current(routeTokenId, outcomeTokenId)) return false;
            _active = false; _paused = false; ActivePresentationTokenId = Guid.Empty; return true;
        }

        private bool Current(Guid routeTokenId, Guid outcomeTokenId) => _active
            && routeTokenId == _routeTokenId && outcomeTokenId == _outcomeTokenId;

        private void Present() => _sink.Present(new Stage3OutcomePresentation(
            _outcome, ResolveState(), _elapsed, _reducedMotion));

        private Stage3OutcomeVisualState ResolveState()
        {
            if (_elapsed < PreparedKeySeconds - ClockEpsilon) return Stage3OutcomeVisualState.Choice;
            if (_elapsed < FinalKeySeconds - ClockEpsilon) return Stage3OutcomeVisualState.Prepared;
            return Stage3OutcomeVisualState.Final;
        }

        private static bool Valid(Stage3OutcomeKey key) => key.River >= Exloris.Runtime.Stage2.Stage2River.Low
            && key.River <= Exloris.Runtime.Stage2.Stage2River.High && key.Plan >= Stage3Plan.Grain
            && key.Plan <= Stage3Plan.Boat && key.Benefit >= Stage3Benefit.Grain
            && key.Benefit <= Stage3Benefit.Boat && key.Need >= Stage3Need.WaterRoute
            && key.Need <= Stage3Need.FieldWater;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
