using System;
using Exloris.Editor;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2MotionTests
    {
        [Test]
        public void EveryChoiceUsesTheSameZeroTwoFourSixProgressClock()
        {
            var sink = new Sink(); var presenter = new Boat2MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            foreach (var choice in new[] { Boat2Choice.FieldBank, Boat2Choice.Dock, Boat2Choice.FollowIbis })
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
            var sink = new Sink(); var presenter = new Boat2MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            var times = new[] { 0d, 0.24d, 0.48d, 0.72d, 0.96d };
            var phases = new[] { Boat2IbisPhase.Idle, Boat2IbisPhase.WingsOpen, Boat2IbisPhase.HeadLift, Boat2IbisPhase.DirectionBeak, Boat2IbisPhase.CalmSettle };
            for (var i = 0; i < times.Length; i++) { presenter.Present(Snapshot(token, Boat2Choice.FollowIbis, times[i]), false, 1000); Assert.That(sink.Frame.IbisPhase, Is.EqualTo(phases[i])); }
            presenter.Present(Snapshot(token, Boat2Choice.FieldBank, 0d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(0));
            presenter.Present(Snapshot(token, Boat2Choice.FieldBank, 1.2d), false, 1000); Assert.That(sink.Frame.BoatkeeperFrameIndex, Is.EqualTo(1));
        }

        [Test]
        public void ReducedMotionUsesStaticLeftAssetsAndTokenBoundFreeze()
        {
            var sink = new Sink(); var presenter = new Boat2MotionPresenter(Content(), sink); var token = Guid.NewGuid();
            presenter.Present(Snapshot(token, Boat2Choice.Dock, 2d), true, 1000);
            Assert.That(sink.Frame.SpatialMotionEnabled, Is.False); Assert.That(sink.Frame.BoatYOffsetPixels, Is.Zero);
            Assert.That(sink.Frame.BoatSprite, Is.SameAs(Content().BoatSprites[4]));
            Assert.That(sink.Frame.IbisSprite, Is.SameAs(Content().IbisSprites[7]));
            var before = sink.Frame; Assert.That(presenter.Freeze(token), Is.True);
            presenter.Present(Snapshot(token, Boat2Choice.Dock, 6d), false, 1000); Assert.That(sink.Frame.ProgressIndex, Is.EqualTo(before.ProgressIndex));
            Assert.That(presenter.Resume(Guid.NewGuid()), Is.False); Assert.That(presenter.Resume(token), Is.True);
        }

        private static Boat2Snapshot Snapshot(Guid token, Boat2Choice choice, double elapsed) => new Boat2Snapshot(token, Boat2State.Traveling, choice, BoatDirection.Left, 0, elapsed, null, false);
        private static Boat2Content Content() => AssetDatabase.LoadAssetAtPath<Boat2Content>(Boat2ContentGenerator.ContentAssetPath);
        private sealed class Sink : IBoat2MotionSink { public Boat2MotionFrame Frame { get; private set; } public void PresentMotion(Boat2MotionFrame frame) => Frame = frame; }
    }
}
