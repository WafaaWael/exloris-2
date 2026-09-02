using System;
using Exloris.Editor;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3MotionTests
    {
        [Test]
        public void EveryChoiceUsesTheSameZeroTwoFourSixProgressClock()
        {
            var sink = new Sink(); var presenter = new Boat3MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            foreach (var choice in new[] { Boat3Choice.GrainStore, Boat3Choice.Irrigation, Boat3Choice.FollowIbis })
                foreach (var item in new[] { (0d, 0), (2d, 1), (4d, 2), (6d, 3) })
                {
                    Assert.That(presenter.Present(Snapshot(token, choice, item.Item1), false, 1000f), Is.True);
                    Assert.That(sink.Frame.ProgressIndex, Is.EqualTo(item.Item2));
                    Assert.That(Math.Abs(sink.Frame.BoatYOffsetPixels), Is.LessThanOrEqualTo(6f));
                }
            Assert.That(sink.Frame.ChildSocket, Is.EqualTo(new UnityEngine.Vector2(0.42f, 0.42f)));
            Assert.That(sink.Frame.BoatkeeperSocket, Is.EqualTo(new UnityEngine.Vector2(0.66f, 0.44f)));
            Assert.That(sink.Frame.MaximumHandToOarErrorPixels, Is.EqualTo(4f));
        }

        [Test]
        public void LeftIbisCueAndBoatkeeperLoopUseExactOrder()
        {
            var sink = new Sink(); var presenter = new Boat3MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            var times = new[] { 0d, 0.24d, 0.48d, 0.72d, 0.96d };
            var phases = new[] { Boat3IbisPhase.Idle, Boat3IbisPhase.WingsOpen, Boat3IbisPhase.HeadLift, Boat3IbisPhase.DirectionBeak, Boat3IbisPhase.CalmSettle };
            for (var i = 0; i < times.Length; i++) { presenter.Present(Snapshot(token, Boat3Choice.FollowIbis, times[i]), false, 1000); Assert.That(sink.Frame.IbisPhase, Is.EqualTo(phases[i])); }
            presenter.Present(Snapshot(token, Boat3Choice.GrainStore, 0d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(0));
            presenter.Present(Snapshot(token, Boat3Choice.GrainStore, 1.2d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(1));
        }

        [Test]
        public void ReducedMotionUsesStaticLeftAssetsAndTokenBoundFreeze()
        {
            var sink = new Sink(); var presenter = new Boat3MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            presenter.Present(Snapshot(token, Boat3Choice.Irrigation, 2d), true, 1000);
            Assert.That(sink.Frame.SpatialMotionEnabled, Is.False); Assert.That(sink.Frame.BoatYOffsetPixels, Is.Zero);
            Assert.That(sink.Frame.BoatSprite, Is.SameAs(Content().BoatSprites[4]));
            Assert.That(sink.Frame.IbisSprite, Is.SameAs(Content().IbisSprites[7]));
            var before = sink.Frame; Assert.That(presenter.Freeze(token), Is.True);
            presenter.Present(Snapshot(token, Boat3Choice.Irrigation, 6d), false, 1000); Assert.That(sink.Frame.ProgressIndex, Is.EqualTo(before.ProgressIndex));
            Assert.That(presenter.Resume(Guid.NewGuid()), Is.False); Assert.That(presenter.Resume(token), Is.True);
        }

        private static Boat3Snapshot Snapshot(Guid token, Boat3Choice choice, double elapsed) => new Boat3Snapshot(token, Boat3State.Traveling, choice, BoatDirection.Left, 0, elapsed, null, false);
        private static Boat3Content Content() => AssetDatabase.LoadAssetAtPath<Boat3Content>(Boat3ContentGenerator.ContentAssetPath);
        private sealed class Sink : IBoat3MotionSink { public Boat3MotionFrame Frame { get; private set; } public void PresentMotion(Boat3MotionFrame frame) => Frame = frame; }
    }
}
