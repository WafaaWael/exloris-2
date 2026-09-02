using System;
using Exloris.Runtime.Completion;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionMotionTests
    {
        [Test]
        public void FullMotionUsesOnlyOpacityAcrossTheFourFrozenKeys()
        {
            var sink = new Sink();
            var presenter = new CompletionMotionPresenter(sink);
            var token = Guid.NewGuid();
            var opacities = new double[4];
            var keys = new[] { 0d, 0.6d, 1.8d, 2.4d };
            for (var index = 0; index < keys.Length; index++)
            {
                Assert.That(presenter.TryPresent(Snapshot(token, keys[index]), false), Is.True);
                opacities[index] = sink.Frame.Opacity;
                Assert.That(sink.Frame.SpatialMotionEnabled, Is.False);
                Assert.That(sink.Frame.Scale, Is.EqualTo(1f));
            }
            Assert.That(opacities[0], Is.Zero);
            Assert.That(opacities[1], Is.GreaterThan(opacities[0]));
            Assert.That(opacities[2], Is.GreaterThan(opacities[1]));
            Assert.That(opacities[3], Is.EqualTo(1d));
        }

        [Test]
        public void ReducedMotionIsStaticBeforeFirstFrameWithoutChangingSemanticTime()
        {
            var sink = new Sink();
            var token = Guid.NewGuid();
            var presenter = new CompletionMotionPresenter(sink);
            Assert.That(presenter.TryPresent(Snapshot(token, 0d), true), Is.True);
            Assert.That(sink.Frame.Opacity, Is.EqualTo(1d));
            Assert.That(sink.Frame.StaticEquivalent, Is.True);
            Assert.That(sink.Frame.ElapsedSeconds, Is.Zero);
        }

        [Test]
        public void NewNonZeroAndStaleTokensFailClosed()
        {
            var sink = new Sink();
            var presenter = new CompletionMotionPresenter(sink);
            var first = Guid.NewGuid();
            Assert.That(presenter.TryPresent(Snapshot(first, 0d), false), Is.True);
            Assert.That(presenter.TryPresent(Snapshot(Guid.NewGuid(), 0.6d), false), Is.False);
            Assert.That(presenter.TryPresent(Snapshot(first, double.NaN), false), Is.False);
        }

        private static CompletionSnapshot Snapshot(Guid token, double elapsed) =>
            new CompletionSnapshot(token, CompletionState.Presenting, token, 0d, elapsed, "completion.home", false);

        private sealed class Sink : ICompletionMotionSink
        {
            public CompletionMotionFrame Frame;
            public void PresentCompletionMotion(CompletionMotionFrame frame) { Frame = frame; }
        }
    }
}
