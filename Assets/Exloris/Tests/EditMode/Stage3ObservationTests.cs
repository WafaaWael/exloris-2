using System;
using System.Linq;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3ObservationTests
    {
        [TestCase("low", "grain", "water_route")]
        [TestCase("low", "channel", "grain_storage")]
        [TestCase("low", "boat", "field_water")]
        [TestCase("middle", "grain", "water_route")]
        [TestCase("middle", "channel", "grain_storage")]
        [TestCase("middle", "boat", "field_water")]
        [TestCase("high", "grain", "water_route")]
        [TestCase("high", "channel", "grain_storage")]
        [TestCase("high", "boat", "field_water")]
        public void AllowlistAcceptsExactStage3RiverPlanBenefitNeedFacts(string river, string plan, string need)
        {
            var pair = ObservationEvent.ChoiceOutcome(
                ObservationEventType.Stage3Option,
                "choice_" + river + "_" + plan,
                "outcome_benefit_" + plan + "_need_" + need);
            Assert.That(pair.ChoiceId, Is.EqualTo("choice_" + river + "_" + plan));
            Assert.That(pair.OutcomeId, Is.EqualTo("outcome_benefit_" + plan + "_need_" + need));
        }

        [Test]
        public void FinishFactsContainOnlyStage3OptionCountAndCompletion()
        {
            var count = ObservationEvent.OptionsTried(ObservationEventType.Stage3Option, "stage_3", 9);
            var complete = ObservationEvent.CompletedStages("stage_3");
            Assert.That(count.SubjectId, Is.EqualTo("stage_3"));
            Assert.That(count.Count, Is.EqualTo(9));
            CollectionAssert.AreEqual(new[] { "stage_3" }, complete.StageIds);
        }

        [TestCase("choice_avatar_boy_grain")]
        [TestCase("choice_fast_boat")]
        [TestCase("choice_score_3_grain")]
        [TestCase("choice_rank_1_channel")]
        [TestCase("choice_diagnosis_boat")]
        [TestCase("choice_recommend_grain")]
        [TestCase("choice_learning_style_channel")]
        public void IdentityTimingAssessmentAndInferenceTokensAreRejected(string token)
        {
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.Stage3Option, token, "outcome_benefit_grain_need_water_route"));
        }

        [Test]
        public void ObservationSchemaHasNoPointerTimingFocusErrorIdentityOrPersistenceFields()
        {
            var properties = typeof(ObservationEvent).GetProperties().Select(property => property.Name).ToArray();
            foreach (var forbidden in new[] { "PointerPath", "ResponseTime", "Focus", "Error", "AvatarId", "ProfileId", "PersistentId" })
                CollectionAssert.DoesNotContain(properties, forbidden);
        }

        [Test]
        public void RiverSelectionBackDoesNotRecordASecondOutcomeObservation()
        {
            var ids = new Queue<Guid>(Enumerable.Range(1, 32)
                .Select(index => Guid.Parse($"31000000-0000-0000-0000-{index:000000000000}")));
            var session = new SessionState(); session.Begin(RouteId.Boat3);
            var navigation = new NavigationAuthority(RouteId.Boat3, () => ids.Dequeue());
            var entry = navigation.Request(RouteId.Stage3, 1d);
            var controller = new Stage3Controller(session, navigation, new View(), new Pause(), new Leave(),
                () => ids.Dequeue());
            Assert.That(controller.Enter(entry.Transaction.Id, new Stage3EntryContext(Stage2River.High), 2d, false), Is.True);
            Assert.That(controller.HandlePlay(controller.CurrentTokenId, 3d), Is.True);
            Assert.That(controller.HandleSelectPlan(controller.CurrentTokenId, Stage3Plan.Boat, Guid.NewGuid(), 4d), Is.True);
            Assert.That(controller.AdvanceOutcome(controller.CurrentTokenId, controller.CurrentOutcomeTokenId, 10.735d), Is.True);
            Assert.That(session.Observations.Count(item => item.Type == ObservationEventType.Stage3Option), Is.EqualTo(1));

            Assert.That(controller.HandleChangeRiver(controller.CurrentTokenId, 11d), Is.True);
            Assert.That(controller.HandleRiverBack(controller.CurrentTokenId), Is.True);

            Assert.That(session.Observations.Count(item => item.Type == ObservationEventType.Stage3Option), Is.EqualTo(1));
            Assert.That(controller.Snapshot.OptionCount, Is.EqualTo(1));
        }

        private sealed class View : IStage3View
        { public void Present(Stage3Snapshot snapshot, bool reducedMotion) { } public void SetInputReady(bool ready) { } }
        private sealed class Pause : IStage3PauseSink
        { public bool TryShow(Stage3Snapshot snapshot) => true; }
        private sealed class Leave : ILeaveConfirmationSink
        { public bool TryShow(RouteId caller) => true; }
    }
}
