using System;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3OutcomeTests
    {
        [Test]
        public void AllNineRiverPlanPairsPreserveExactBenefitAndNeedMeaning()
        {
            foreach (var river in new[] { Stage2River.Low, Stage2River.Middle, Stage2River.High })
            foreach (var plan in new[] { Stage3Plan.Grain, Stage3Plan.Channel, Stage3Plan.Boat })
            {
                var sink = new Sink();
                var presenter = new Stage3OutcomePresenter(sink);
                var snapshot = Snapshot(river, plan);
                Assert.That(presenter.Start(snapshot, false, 10d), Is.True);
                Assert.That(sink.Last.Outcome, Is.EqualTo(snapshot.CurrentOutcome));
                Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Choice));
            }
        }

        [Test]
        public void FullMotionUsesFrozenKeysAndHoldsFinalMeaningThroughSixPointSevenThreeFiveSeconds()
        {
            var sink = new Sink(); var presenter = new Stage3OutcomePresenter(sink);
            var snapshot = Snapshot(Stage2River.Middle, Stage3Plan.Channel);
            presenter.Start(snapshot, false, 10d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Choice));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 10.8d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Prepared));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 12d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Final));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 13.6d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Final));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 16.735d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Final));
            Assert.That(sink.Last.ElapsedSeconds, Is.EqualTo(6.735d).Within(0.000001d));
        }

        [Test]
        public void ReducedMotionUsesStaticEquivalentWithoutChangingMeaningOrDuration()
        {
            var sink = new Sink(); var presenter = new Stage3OutcomePresenter(sink);
            var snapshot = Snapshot(Stage2River.High, Stage3Plan.Boat);
            presenter.Start(snapshot, true, 20d);
            Assert.That(sink.Last.ReducedMotion, Is.True);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Choice));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 20.8d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Prepared));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 22d);
            Assert.That(sink.Last.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Final));
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 26.735d);
            Assert.That(sink.Last.Outcome, Is.EqualTo(snapshot.CurrentOutcome));
            Assert.That(sink.Last.ElapsedSeconds, Is.EqualTo(6.735d).Within(0.000001d));
        }

        [Test]
        public void PauseResumeStaleTokensAndBackwardClockCannotChangeSemanticTime()
        {
            var sink = new Sink(); var presenter = new Stage3OutcomePresenter(sink);
            var snapshot = Snapshot(Stage2River.Low, Stage3Plan.Grain);
            presenter.Start(snapshot, false, 0d);
            var firstPresentationToken = presenter.ActivePresentationTokenId;
            presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 1.2d);
            Assert.That(presenter.Freeze(snapshot.TokenId, snapshot.OutcomeTokenId, 1.2d), Is.True);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 5d), Is.False);
            Assert.That(presenter.Resume(snapshot.TokenId, snapshot.OutcomeTokenId, 10d), Is.True);
            Assert.That(presenter.ActivePresentationTokenId, Is.Not.EqualTo(firstPresentationToken));
            Assert.That(presenter.Advance(Guid.NewGuid(), snapshot.OutcomeTokenId, 11d), Is.False);
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 9d), Is.False);
            Assert.That(sink.Last.ElapsedSeconds, Is.EqualTo(1.2d).Within(0.000001d));
            Assert.That(presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 11d), Is.True);
            Assert.That(sink.Last.ElapsedSeconds, Is.EqualTo(2.2d).Within(0.000001d));
        }

        private static Stage3Snapshot Snapshot(Stage2River river, Stage3Plan plan)
        {
            var benefit = (Stage3Benefit)(int)plan;
            var need = plan == Stage3Plan.Grain ? Stage3Need.WaterRoute
                : plan == Stage3Plan.Channel ? Stage3Need.GrainStorage : Stage3Need.FieldWater;
            return new Stage3Snapshot(Guid.NewGuid(), Stage3State.OutcomePlaying, river, plan,
                new Stage3OutcomeKey(river, plan, benefit, need), Stage3TriedCombinations.None,
                Guid.NewGuid(), Guid.NewGuid(), 0, 0, null, 0, false);
        }

        private sealed class Sink : IStage3OutcomeSink
        {
            internal Stage3OutcomePresentation Last { get; private set; }
            public void Present(Stage3OutcomePresentation presentation) => Last = presentation;
        }
    }
}
