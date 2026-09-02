using System;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    public enum Boat1IbisPhase { Idle, WingsOpen, HeadLift, DirectionBeak, CalmSettle }

    public readonly struct Boat1MotionFrame
    {
        public Boat1MotionFrame(Guid token, Boat1Choice choice, int progress, float boatY, int keeperFrame,
            Boat1IbisPhase ibisPhase, bool reduced, Sprite boat, Sprite keeper, Sprite ibis)
        {
            TokenId = token; Choice = choice; ProgressIndex = progress; BoatYOffsetPixels = boatY;
            BoatkeeperFrameIndex = keeperFrame; IbisPhase = ibisPhase; ReducedMotion = reduced;
            BoatSprite = boat; BoatkeeperSprite = keeper; IbisSprite = ibis;
        }
        public Guid TokenId { get; }
        public Boat1Choice Choice { get; }
        public int ProgressIndex { get; }
        public float BoatYOffsetPixels { get; }
        public int BoatkeeperFrameIndex { get; }
        public Boat1IbisPhase IbisPhase { get; }
        public bool ReducedMotion { get; }
        public bool SpatialMotionEnabled => !ReducedMotion;
        public float FadeSeconds => 0.150f;
        public Sprite BoatSprite { get; }
        public Sprite BoatkeeperSprite { get; }
        public Sprite IbisSprite { get; }
        public Vector2 ChildSocket => new Vector2(0.42f, 0.42f);
        public Vector2 BoatkeeperSocket => new Vector2(0.66f, 0.44f);
        public Vector2 OarGripSocket => new Vector2(0.72f, 0.50f);
        public Vector2 WakeSocket => new Vector2(0.50f, 0.12f);
        public Vector2 WaterlineSocket => new Vector2(0.50f, 0.18f);
        public float MaximumHandToOarErrorPixels => 4f;
    }

    public interface IBoat1MotionSink { void PresentMotion(Boat1MotionFrame frame); }

    public sealed class Boat1MotionPresenter
    {
        public const double BoatBobSeconds = 2d;
        public const double BoatkeeperLoopSeconds = 2.4d;
        public const double IbisCueSeconds = 1.2d;
        public const float MaximumBobSafeHeight = 0.006f;
        private readonly Boat1Content _content;
        private readonly IBoat1MotionSink _sink;
        private Boat1MotionFrame _last;
        private bool _hasFrame, _frozen;

        public Boat1MotionPresenter(Boat1Content content, IBoat1MotionSink sink)
        { _content = content ?? throw new ArgumentNullException(nameof(content)); _sink = sink ?? throw new ArgumentNullException(nameof(sink)); }
        public Boat1MotionFrame LastFrame => _last;

        public bool Present(Boat1Snapshot snapshot, bool reducedMotion, float safeHeightPixels)
        {
            if (snapshot.TokenId == Guid.Empty || safeHeightPixels <= 0 || float.IsNaN(safeHeightPixels) || float.IsInfinity(safeHeightPixels)) return false;
            if (_frozen) { _sink.PresentMotion(_last); return true; }
            var elapsed = Math.Max(0d, Math.Min(Boat1Controller.TravelSeconds, snapshot.TravelElapsedSeconds));
            var progress = elapsed >= 6d ? 3 : elapsed >= 4d ? 2 : elapsed >= 2d ? 1 : 0;
            var y = reducedMotion ? 0f : (float)(Math.Sin(elapsed / BoatBobSeconds * Math.PI * 2d) * safeHeightPixels * MaximumBobSafeHeight);
            var keeperFrame = reducedMotion ? 2 : (elapsed % BoatkeeperLoopSeconds < BoatkeeperLoopSeconds / 2d ? 0 : 1);
            var phase = ResolveIbisPhase(elapsed);
            var boat = reducedMotion ? _content.BoatSprites[4] : _content.BoatSprites[0];
            var keeper = reducedMotion ? _content.BoatkeeperSprites[2] : _content.BoatkeeperSprites[keeperFrame];
            var ibis = reducedMotion ? _content.IbisSprites[snapshot.IbisDirection == BoatDirection.Right ? 8 : 7] : ResolveIbisSprite(phase, snapshot.IbisDirection);
            _last = new Boat1MotionFrame(snapshot.TokenId, snapshot.Choice, progress, y, keeperFrame, phase, reducedMotion, boat, keeper, ibis);
            _hasFrame = true; _sink.PresentMotion(_last); return true;
        }

        public bool CanFreeze(Guid tokenId) => _hasFrame && _last.TokenId == tokenId && !_frozen;
        public bool Freeze(Guid tokenId) { if (!CanFreeze(tokenId)) return false; _frozen = true; return true; }
        public bool Resume(Guid tokenId) { if (!_frozen || _last.TokenId != tokenId) return false; _frozen = false; _sink.PresentMotion(_last); return true; }

        private Sprite ResolveIbisSprite(Boat1IbisPhase phase, BoatDirection direction)
        {
            switch (phase)
            {
                case Boat1IbisPhase.WingsOpen: return _content.IbisSprites[1];
                case Boat1IbisPhase.HeadLift: return _content.IbisSprites[2];
                case Boat1IbisPhase.DirectionBeak: return _content.IbisSprites[direction == BoatDirection.Right ? 4 : 3];
                case Boat1IbisPhase.CalmSettle: return _content.IbisSprites[5];
                default: return _content.IbisSprites[0];
            }
        }

        private static Boat1IbisPhase ResolveIbisPhase(double elapsed)
        {
            if (elapsed < 0.24d) return Boat1IbisPhase.Idle;
            if (elapsed < 0.48d) return Boat1IbisPhase.WingsOpen;
            if (elapsed < 0.72d) return Boat1IbisPhase.HeadLift;
            if (elapsed < 0.96d) return Boat1IbisPhase.DirectionBeak;
            return Boat1IbisPhase.CalmSettle;
        }
    }
}
