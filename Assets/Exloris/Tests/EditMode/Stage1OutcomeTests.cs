using System;
using Exloris.Editor;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1OutcomeTests
    {
        [TestCase(Stage1OutcomeKind.OutsideLow, 1)]
        [TestCase(Stage1OutcomeKind.InRange, 0)]
        [TestCase(Stage1OutcomeKind.OutsideHigh, 2)]
        public void LowInRangeAndHighUseExactFactualAssets(Stage1OutcomeKind outcome, int layerIndex)
        {
            var sink = new Sink(); var content = Content(); var presenter = new Stage1OutcomePresenter(content, sink);
            var snapshot = Snapshot(outcome, 0.333d);
            Assert.That(presenter.Start(snapshot, false, 10d), Is.True);
            Assert.That(sink.Last.OutcomeLayer, Is.SameAs(content.OutcomeLayers[layerIndex]));
            Assert.That(sink.Last.MarkerPosition, Is.EqualTo(0.333d));
            Assert.That(sink.Last.UsesEvaluationLanguage, Is.False);
            Assert.That(sink.Last.UsesSuccessOrFailureAudio, Is.False);
        }

        [Test]
        public void FullAndReducedUseFrozenSemanticTiming()
        {
            var fullSink = new Sink(); var full = new Stage1OutcomePresenter(Content(), fullSink);
            var snapshot = Snapshot(Stage1OutcomeKind.InRange, 0.5d);
            Assert.That(full.Start(snapshot, false, 10d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Reveal));
            Assert.That(full.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 10.799d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Reveal));
            Assert.That(full.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 10.8d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Hold));
            Assert.That(full.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 12.4d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Settle));
            Assert.That(full.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 13.2d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Complete));

            var reducedSink = new Sink(); var reduced = new Stage1OutcomePresenter(Content(), reducedSink);
            Assert.That(reduced.Start(snapshot, true, 20d), Is.True);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Before));
            Assert.That(reducedSink.Last.SpatialMotionEnabled, Is.False);
            Assert.That(reduced.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 20.8d), Is.True);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.After));
            Assert.That(reduced.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 23.2d), Is.True);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Complete));
            Assert.That(reducedSink.Last.MarkerPosition, Is.EqualTo(snapshot.MarkerPosition));
        }

        [Test]
        public void PauseFreezeAndResumePreserveExactRemainingSemanticTime()
        {
            var sink = new Sink(); var presenter = new Stage1OutcomePresenter(Content(), sink);
            var snapshot = Snapshot(Stage1OutcomeKind.OutsideLow, 0.1d);
            presenter.Start(snapshot, false, 0d);
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 1.2d);
            Assert.That(presenter.Freeze(snapshot.TokenId, snapshot.OutcomeTokenId, 1.2d), Is.True);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 5d), Is.False);
            Assert.That(presenter.Resume(snapshot.TokenId, snapshot.OutcomeTokenId, 10d), Is.True);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 11.999d), Is.True);
            Assert.That(sink.Last.Phase, Is.Not.EqualTo(Stage1OutcomePhase.Complete));
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 12d), Is.True);
            Assert.That(sink.Last.Phase, Is.EqualTo(Stage1OutcomePhase.Complete));
        }

        [Test]
        public void StaleDuplicateAndWrongOutcomeTokensAreInert()
        {
            var sink = new Sink(); var presenter = new Stage1OutcomePresenter(Content(), sink);
            var snapshot = Snapshot(Stage1OutcomeKind.OutsideHigh, 0.9d);
            Assert.That(presenter.Start(snapshot, false, 0d), Is.True);
            var count = sink.Count;
            Assert.That(presenter.Advance(Guid.NewGuid(), snapshot.OutcomeTokenId, 1d), Is.False);
            Assert.That(presenter.Advance(snapshot.TokenId, Guid.NewGuid(), 1d), Is.False);
            Assert.That(sink.Count, Is.EqualTo(count));
            Assert.That(presenter.Start(snapshot, false, 1d), Is.False);
        }

        private static Stage1Content Content() => AssetDatabase.LoadAssetAtPath<Stage1Content>(Stage1ContentGenerator.ContentAssetPath);
        private static Stage1Snapshot Snapshot(Stage1OutcomeKind outcome, double position) =>
            new Stage1Snapshot(Guid.Parse("10000000-0000-0000-0000-000000000001"), Stage1State.OutcomePlaying, true, position, 0, 1, outcome,
                Guid.Parse("20000000-0000-0000-0000-000000000001"), Guid.NewGuid(), 0, 0, null, false, false);

        private sealed class Sink : IStage1OutcomeSink
        {
            internal Stage1OutcomeFrame Last { get; private set; }
            internal int Count { get; private set; }
            public void PresentOutcome(Stage1OutcomeFrame frame) { Last = frame; Count++; }
        }
    }
}
