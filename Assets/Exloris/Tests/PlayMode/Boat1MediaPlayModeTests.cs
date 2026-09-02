using System;
using System.Collections;
using Exloris.Runtime.Boat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Boat1MediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReducedMotionUsesStaticSemanticFrameBeforeFirstRenderedFrame()
        {
            var content = Resources.Load<Boat1Content>(Boat1Content.ResourcePath);
            Assert.That(content, Is.Not.Null);
            var sink = new Sink();
            var presenter = new Boat1MotionPresenter(content, sink);
            var snapshot = new Boat1Snapshot(Guid.NewGuid(), Boat1State.Traveling, Boat1Choice.FollowIbis,
                BoatDirection.Right, 0d, 0d, null, false);
            Assert.That(presenter.Present(snapshot, true, 1000f), Is.True);
            Assert.That(sink.Frame.ReducedMotion, Is.True);
            Assert.That(sink.Frame.SpatialMotionEnabled, Is.False);
            Assert.That(sink.Frame.BoatSprite, Is.SameAs(content.BoatSprites[4]));
            Assert.That(sink.Frame.BoatkeeperSprite, Is.SameAs(content.BoatkeeperSprites[2]));
            Assert.That(sink.Frame.IbisSprite, Is.SameAs(content.IbisSprites[8]));
            yield return null;
            Assert.That(sink.Count, Is.EqualTo(1), "No hidden full-motion frame may appear first.");
        }

        private sealed class Sink : IBoat1MotionSink
        {
            public Boat1MotionFrame Frame { get; private set; }
            public int Count { get; private set; }
            public void PresentMotion(Boat1MotionFrame frame) { Frame = frame; Count++; }
        }
    }
}
