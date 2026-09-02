using System;
using Exloris.Editor;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEditor;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2OutcomeTests
    {
        [Test]
        public void AllNineRiverHotspotPairsUseTheirExactFactualLayer()
        {
            var content = Content();
            foreach (var river in new[] { Stage2River.Low, Stage2River.Middle, Stage2River.High })
            foreach (var hotspot in new[] { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat })
            {
                var sink = new Sink(); var presenter = new Stage2OutcomePresenter(content, sink);
                var snapshot = Snapshot(river, hotspot);
                Assert.That(presenter.Start(snapshot, false, 10d), Is.True);
                var index = ((int)river - 1) * 3 + (int)hotspot - 1;
                Assert.That(sink.Last.OutcomeLayer, Is.SameAs(content.OutcomeLayers[index]));
                Assert.That(sink.Last.UsesEvaluationLanguage, Is.False);
                Assert.That(sink.Last.UsesSuccessOrFailureAudio, Is.False);
            }
        }

        [Test]
        public void FullAndReducedUseExactFrozenTwoPointFourSecondTiming()
        {
            var snapshot = Snapshot(Stage2River.Middle, Stage2Hotspot.Path);
            var fullSink = new Sink(); var full = new Stage2OutcomePresenter(Content(), fullSink);
            Assert.That(full.Start(snapshot, false, 10d), Is.True);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Reveal));
            full.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 10.6d);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Hold));
            full.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 11.8d);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Settle));
            full.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 12.4d);
            Assert.That(fullSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Complete));

            var reducedSink = new Sink(); var reduced = new Stage2OutcomePresenter(Content(), reducedSink);
            reduced.Start(snapshot, true, 20d);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Before));
            Assert.That(reducedSink.Last.SpatialMotionEnabled, Is.False);
            reduced.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 20.6d);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.After));
            reduced.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 22.4d);
            Assert.That(reducedSink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Complete));
        }

        [Test]
        public void PauseResumeAndStaleTokensPreserveExactRemainingTime()
        {
            var sink = new Sink(); var presenter = new Stage2OutcomePresenter(Content(), sink);
            var snapshot = Snapshot(Stage2River.High, Stage2Hotspot.Boat);
            presenter.Start(snapshot, false, 0d);
            presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 1.2d);
            Assert.That(presenter.Freeze(snapshot.TokenId, snapshot.ConsequenceTokenId, 1.2d), Is.True);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 5d), Is.False);
            Assert.That(presenter.Resume(snapshot.TokenId, snapshot.ConsequenceTokenId, 10d), Is.True);
            Assert.That(presenter.Advance(Guid.NewGuid(), snapshot.ConsequenceTokenId, 11.2d), Is.False);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 11.2d), Is.True);
            Assert.That(sink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Complete));
        }

        [Test]
        public void OutcomeClockCannotMoveBackward()
        {
            var sink = new Sink(); var presenter = new Stage2OutcomePresenter(Content(), sink);
            var snapshot = Snapshot(Stage2River.Middle, Stage2Hotspot.Field);
            Assert.That(presenter.Start(snapshot, false, 10d), Is.True);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 11.8d), Is.True);
            Assert.That(sink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Settle));

            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.ConsequenceTokenId, 10.3d), Is.False);
            Assert.That(sink.Last.Phase, Is.EqualTo(Stage2OutcomePhase.Settle));
            Assert.That(sink.Last.ElapsedSeconds, Is.EqualTo(1.8d).Within(0.000001d));
        }

        private static Stage2Content Content() => AssetDatabase.LoadAssetAtPath<Stage2Content>(Stage2ContentGenerator.ContentAssetPath);
        private static Stage2Snapshot Snapshot(Stage2River river, Stage2Hotspot hotspot) =>
            new Stage2Snapshot(Guid.NewGuid(), Stage2State.ConsequencePlaying, river,
                Stage2ObservedHotspots.None, Stage2ObservedHotspots.None, Stage2ObservedHotspots.None,
                hotspot, Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, 1, false);

        private sealed class Sink : IStage2OutcomeSink
        {
            internal Stage2OutcomeFrame Last { get; private set; }
            public void PresentOutcome(Stage2OutcomeFrame frame) => Last = frame;
        }
    }
}
