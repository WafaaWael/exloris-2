using System;
using Exloris.Editor;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1MotionTests
    {
        [Test]
        public void FullMotionUsesExactTimingProgressSocketsAndRouteIndependentSpeed()
        {
            var sink = new Sink();
            var presenter = new Boat1MotionPresenter(Content(), sink);
            var token = Guid.NewGuid();
            foreach (var item in new[] { (0d, 0), (2d, 1), (4d, 2), (6d, 3) })
            {
                Assert.That(presenter.Present(Snapshot(token, Boat1Choice.Reeds, item.Item1), false, 1000f), Is.True);
                Assert.That(sink.Frame.ProgressIndex, Is.EqualTo(item.Item2));
                Assert.That(Math.Abs(sink.Frame.BoatYOffsetPixels), Is.LessThanOrEqualTo(6f));
            }
            presenter.Present(Snapshot(token, Boat1Choice.Reeds, 0.25d), false, 1000f);
            var reeds = sink.Frame.BoatYOffsetPixels;
            presenter.Present(Snapshot(token, Boat1Choice.Village, 0.25d), false, 1000f);
            Assert.That(sink.Frame.BoatYOffsetPixels, Is.EqualTo(reeds).Within(0.0001f));
            Assert.That(sink.Frame.ChildSocket, Is.EqualTo(new UnityEngine.Vector2(0.42f, 0.42f)));
            Assert.That(sink.Frame.BoatkeeperSocket, Is.EqualTo(new UnityEngine.Vector2(0.66f, 0.44f)));
            Assert.That(sink.Frame.OarGripSocket, Is.EqualTo(new UnityEngine.Vector2(0.72f, 0.50f)));
            Assert.That(sink.Frame.MaximumHandToOarErrorPixels, Is.EqualTo(4f));
        }

        [Test]
        public void IbisCueAndKeeperLoopUseExactOrderAndPeriods()
        {
            var sink = new Sink(); var presenter = new Boat1MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            var times = new[] { 0d, 0.24d, 0.48d, 0.72d, 0.96d };
            var phases = new[] { Boat1IbisPhase.Idle, Boat1IbisPhase.WingsOpen, Boat1IbisPhase.HeadLift, Boat1IbisPhase.DirectionBeak, Boat1IbisPhase.CalmSettle };
            for (var i = 0; i < times.Length; i++) { presenter.Present(Snapshot(token, Boat1Choice.FollowIbis, times[i]), false, 1000); Assert.That(sink.Frame.IbisPhase, Is.EqualTo(phases[i])); }
            presenter.Present(Snapshot(token, Boat1Choice.Reeds, 0d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(0));
            presenter.Present(Snapshot(token, Boat1Choice.Reeds, 1.2d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(1));
            presenter.Present(Snapshot(token, Boat1Choice.Reeds, 2.4d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(0));
        }

        [Test]
        public void ReducedMotionSelectsStaticAssetsBeforeFirstFrameAndFreezeIsExact()
        {
            var sink = new Sink(); var presenter = new Boat1MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            presenter.Present(Snapshot(token, Boat1Choice.Village, 2d), true, 1000);
            Assert.That(sink.Frame.ReducedMotion, Is.True); Assert.That(sink.Frame.SpatialMotionEnabled, Is.False);
            Assert.That(sink.Frame.FadeSeconds, Is.LessThanOrEqualTo(0.150f)); Assert.That(sink.Frame.BoatYOffsetPixels, Is.Zero);
            Assert.That(sink.Frame.BoatSprite, Is.SameAs(Content().BoatSprites[4]));
            Assert.That(sink.Frame.BoatkeeperSprite, Is.SameAs(Content().BoatkeeperSprites[2]));
            Assert.That(sink.Frame.IbisSprite, Is.SameAs(Content().IbisSprites[8]));
            var frozen = sink.Frame; Assert.That(presenter.Freeze(token), Is.True);
            presenter.Present(Snapshot(token, Boat1Choice.Village, 5d), false, 1000); Assert.That(sink.Frame.ProgressIndex, Is.EqualTo(frozen.ProgressIndex));
            Assert.That(presenter.Resume(token), Is.True);
        }

        private static Boat1Snapshot Snapshot(Guid token, Boat1Choice choice, double elapsed) =>
            new Boat1Snapshot(token, Boat1State.Traveling, choice, BoatDirection.Right, 0, elapsed, null, false);
        private static Boat1Content Content() => AssetDatabase.LoadAssetAtPath<Boat1Content>(Boat1ContentGenerator.ContentAssetPath);
        private sealed class Sink : IBoat1MotionSink { public Boat1MotionFrame Frame { get; private set; } public void PresentMotion(Boat1MotionFrame frame) => Frame = frame; }
    }
}
