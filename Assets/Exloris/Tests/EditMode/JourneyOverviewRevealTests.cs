using System;
using System.Collections.Generic;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewRevealTests
    {
        private const string ContentPath =
            "Assets/Exloris/Resources/Content/JourneyOverviewContent.asset";

        [Test]
        public void FullMotionRevealsCumulativeMeaningAtExactBeatBoundaries()
        {
            var sink = new RecordingRevealSink();
            var presenter = new JourneyOverviewRevealPresenter(Content(), sink);
            var token = Guid.NewGuid();

            Assert.That(presenter.TryPresent(Snapshot(token, 0d, 1,
                JourneyOverviewRevealState.RevealingMeasure), false), Is.True);
            AssertFrame(sink.Last, 1, 256f, true, false, false, true);

            Assert.That(presenter.TryPresent(Snapshot(token, 3.288d, 1,
                JourneyOverviewRevealState.RevealingMeasure), false), Is.True);
            AssertFrame(sink.Last, 1, 256f, true, false, false, true);

            Assert.That(presenter.TryPresent(Snapshot(token, 3.289d, 2,
                JourneyOverviewRevealState.RevealingRead), false), Is.True);
            AssertFrame(sink.Last, 2, 622f, true, true, false, true);

            Assert.That(presenter.TryPresent(Snapshot(token, 6.182d, 3,
                JourneyOverviewRevealState.RevealingHelp), false), Is.True);
            AssertFrame(sink.Last, 3, 1024f, true, true, true, true);
        }

        [Test]
        public void ReducedMotionRendersAllStaticMeaningBeforeInputBecomesReady()
        {
            var sink = new RecordingRevealSink();
            var presenter = new JourneyOverviewRevealPresenter(Content(), sink);
            var token = Guid.NewGuid();

            presenter.Present(Snapshot(token, 0d, 3, JourneyOverviewRevealState.Ready), true);
            presenter.SetInputReady(true);

            Assert.That(sink.Events, Is.EqualTo(new[] { "frame", "input:true" }));
            AssertFrame(sink.Last, 3, 1024f, true, true, true, false);
            Assert.That(sink.Last.MeasureTreatment, Is.EqualTo(JourneyOverviewTimelineTreatment.Current));
            Assert.That(sink.Last.ReadTreatment, Is.EqualTo(JourneyOverviewTimelineTreatment.EncouragingNotYet));
            Assert.That(sink.Last.HelpTreatment, Is.EqualTo(JourneyOverviewTimelineTreatment.EncouragingNotYet));
            Assert.That(sink.Last.CurrentPulseEnabled, Is.False);
            Assert.That(sink.Last.SpatialMotionEnabled, Is.False);
            Assert.That(sink.Last.HasSemanticTimelineControl, Is.False);
            Assert.That(sink.Last.RequiresReading, Is.False);
            Assert.That(sink.Last.UsesNegativeSfx, Is.False);
        }

        [Test]
        public void OldRevealTokenCannotOverwriteReplacementFrame()
        {
            var sink = new RecordingRevealSink();
            var presenter = new JourneyOverviewRevealPresenter(Content(), sink);
            var first = Guid.NewGuid();
            var replacement = Guid.NewGuid();
            presenter.TryPresent(Snapshot(first, 0d, 1,
                JourneyOverviewRevealState.RevealingMeasure), false);
            presenter.TryPresent(Snapshot(replacement, 0d, 1,
                JourneyOverviewRevealState.RevealingMeasure), false);
            var count = sink.Frames.Count;

            Assert.That(presenter.TryPresent(Snapshot(first, 6.182d, 3,
                JourneyOverviewRevealState.RevealingHelp), false), Is.False);
            Assert.That(sink.Frames, Has.Count.EqualTo(count));
            Assert.That(sink.Last.TokenId, Is.EqualTo(replacement));
        }

        [Test]
        public void RecoveryRendersTheExactCumulativeFrameWithoutMotionOrInput()
        {
            var sink = new RecordingRevealSink();
            var presenter = new JourneyOverviewRevealPresenter(Content(), sink);
            var token = Guid.NewGuid();
            presenter.Present(Snapshot(token, 0d, 1,
                JourneyOverviewRevealState.RevealingMeasure), false);

            presenter.Present(Snapshot(token, 6.5d, 3,
                JourneyOverviewRevealState.Recovery), false);
            presenter.SetInputReady(false);

            AssertFrame(sink.Last, 3, 1024f, true, true, true, false);
            Assert.That(sink.Last.CurrentPulseEnabled, Is.False);
            Assert.That(sink.Events[sink.Events.Count - 1], Is.EqualTo("input:false"));
        }

        private static JourneyOverviewSnapshot Snapshot(
            Guid token, double elapsed, int beats, JourneyOverviewRevealState state)
        {
            return new JourneyOverviewSnapshot(token, state, elapsed, beats);
        }

        private static JourneyOverviewContent Content()
        {
            var content = AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(ContentPath);
            Assert.That(content, Is.Not.Null);
            return content;
        }

        private static void AssertFrame(
            JourneyOverviewRevealFrame frame,
            int beats,
            float maskPixels,
            bool measure,
            bool read,
            bool help,
            bool animated)
        {
            Assert.That(frame.RevealedBeatCount, Is.EqualTo(beats));
            Assert.That(frame.VisualEquivalentMaskPixels, Is.EqualTo(maskPixels).Within(0.001f));
            Assert.That(frame.MeasureVisible, Is.EqualTo(measure));
            Assert.That(frame.ReadVisible, Is.EqualTo(read));
            Assert.That(frame.HelpVisible, Is.EqualTo(help));
            Assert.That(frame.SpatialMotionEnabled, Is.EqualTo(animated));
            Assert.That(frame.MeasureTreatment, Is.EqualTo(JourneyOverviewTimelineTreatment.Current));
            if (read) Assert.That(frame.ReadTreatment,
                Is.EqualTo(JourneyOverviewTimelineTreatment.EncouragingNotYet));
            if (help) Assert.That(frame.HelpTreatment,
                Is.EqualTo(JourneyOverviewTimelineTreatment.EncouragingNotYet));
        }

        private sealed class RecordingRevealSink : IJourneyOverviewRevealSink
        {
            public List<JourneyOverviewRevealFrame> Frames { get; } =
                new List<JourneyOverviewRevealFrame>();
            public List<string> Events { get; } = new List<string>();
            public JourneyOverviewRevealFrame Last => Frames[Frames.Count - 1];

            public void PresentReveal(JourneyOverviewRevealFrame frame)
            {
                Frames.Add(frame);
                Events.Add("frame");
            }

            public void SetInputReady(bool ready)
            {
                Events.Add("input:" + ready.ToString().ToLowerInvariant());
            }
        }
    }
}
