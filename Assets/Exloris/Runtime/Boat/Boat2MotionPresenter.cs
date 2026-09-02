using System;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    public enum Boat2IbisPhase { Idle, WingsOpen, HeadLift, DirectionBeak, CalmSettle }

    public readonly struct Boat2MotionFrame
    {
        public Boat2MotionFrame(Guid token, Boat2Choice choice, int progress, float boatY, int keeperFrame,
            Boat2IbisPhase ibisPhase, bool reduced, Sprite boat, Sprite keeper, Sprite ibis)
        { TokenId = token; Choice = choice; ProgressIndex = progress; BoatYOffsetPixels = boatY; BoatkeeperFrameIndex = keeperFrame; IbisPhase = ibisPhase; ReducedMotion = reduced; BoatSprite = boat; BoatkeeperSprite = keeper; IbisSprite = ibis; }
        public Guid TokenId { get; }
        public Boat2Choice Choice { get; }
        public int ProgressIndex { get; }
        public float BoatYOffsetPixels { get; }
        public int BoatkeeperFrameIndex { get; }
        public Boat2IbisPhase IbisPhase { get; }
        public bool ReducedMotion { get; }
        public bool SpatialMotionEnabled => !ReducedMotion;
        public float FadeSeconds => 0.150f;
        public Sprite BoatSprite { get; }
        public Sprite BoatkeeperSprite { get; }
        public Sprite IbisSprite { get; }
        public Vector2 ChildSocket => new Vector2(0.42f, 0.42f);
        public Vector2 BoatkeeperSocket => new Vector2(0.66f, 0.44f);
        public Vector2 OarGripSocket => new Vector2(0.72f, 0.50f);
        public float MaximumHandToOarErrorPixels => 4f;
    }

    public interface IBoat2MotionSink { void PresentMotion(Boat2MotionFrame frame); }

    public sealed class Boat2MotionPresenter
    {
        public const double BoatBobSeconds = 2d;
        public const double BoatkeeperLoopSeconds = 2.4d;
        public const float MaximumBobSafeHeight = 0.006f;
        private readonly Boat2Content _content;
        private readonly IBoat2MotionSink _sink;
        private Boat2MotionFrame _last;
        private bool _hasFrame, _frozen;
        public Boat2MotionPresenter(Boat2Content content, IBoat2MotionSink sink) { _content = content ?? throw new ArgumentNullException(nameof(content)); _sink = sink ?? throw new ArgumentNullException(nameof(sink)); }
        public Boat2MotionFrame LastFrame => _last;

        public bool Present(Boat2Snapshot snapshot, bool reducedMotion, float safeHeightPixels)
        {
            if (snapshot.TokenId == Guid.Empty || safeHeightPixels <= 0 || float.IsNaN(safeHeightPixels) || float.IsInfinity(safeHeightPixels)) return false;
            if (_frozen) { _sink.PresentMotion(_last); return true; }
            var elapsed = Math.Max(0d, Math.Min(Boat2Controller.TravelSeconds, snapshot.TravelElapsedSeconds));
            var progress = elapsed >= 6d ? 3 : elapsed >= 4d ? 2 : elapsed >= 2d ? 1 : 0;
            var y = reducedMotion ? 0f : (float)(Math.Sin(elapsed / BoatBobSeconds * Math.PI * 2d) * safeHeightPixels * MaximumBobSafeHeight);
            var keeperFrame = reducedMotion ? 2 : (elapsed % BoatkeeperLoopSeconds < BoatkeeperLoopSeconds / 2d ? 0 : 1);
            var phase = ResolvePhase(elapsed);
            var boat = reducedMotion ? _content.BoatSprites[4] : _content.BoatSprites[0];
            var keeper = reducedMotion ? _content.BoatkeeperSprites[2] : _content.BoatkeeperSprites[keeperFrame];
            var ibis = reducedMotion ? _content.IbisSprites[7] : ResolveIbis(phase);
            _last = new Boat2MotionFrame(snapshot.TokenId, snapshot.Choice, progress, y, keeperFrame, phase, reducedMotion, boat, keeper, ibis);
            _hasFrame = true; _sink.PresentMotion(_last); return true;
        }

        public bool Freeze(Guid token) { if (!_hasFrame || _frozen || _last.TokenId != token) return false; _frozen = true; return true; }
        public bool CanFreeze(Guid token) => _hasFrame && !_frozen && _last.TokenId == token;
        public bool Resume(Guid token) { if (!_frozen || _last.TokenId != token) return false; _frozen = false; _sink.PresentMotion(_last); return true; }
        private Sprite ResolveIbis(Boat2IbisPhase phase)
        {
            switch (phase) { case Boat2IbisPhase.WingsOpen: return _content.IbisSprites[1]; case Boat2IbisPhase.HeadLift: return _content.IbisSprites[2]; case Boat2IbisPhase.DirectionBeak: return _content.IbisSprites[3]; case Boat2IbisPhase.CalmSettle: return _content.IbisSprites[5]; default: return _content.IbisSprites[0]; }
        }
        private static Boat2IbisPhase ResolvePhase(double elapsed) => elapsed < 0.24d ? Boat2IbisPhase.Idle
            : elapsed < 0.48d ? Boat2IbisPhase.WingsOpen : elapsed < 0.72d ? Boat2IbisPhase.HeadLift
            : elapsed < 0.96d ? Boat2IbisPhase.DirectionBeak : Boat2IbisPhase.CalmSettle;
    }
}
