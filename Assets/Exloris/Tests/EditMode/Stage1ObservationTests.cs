using System;
using System.Linq;
using Exloris.Core.Session;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1ObservationTests
    {
        [Test]
        public void Stage1AllowlistAcceptsOnlyFactualOptionOutcomeAndCompletionTemplates()
        {
            var option = ObservationEvent.OptionsTried(ObservationEventType.Stage1Option, "stage_1", 3);
            var outcome = ObservationEvent.ChoiceOutcome(ObservationEventType.Stage1Outcome, "choice_marker_position", "outcome_in_range");
            var completion = ObservationEvent.CompletedStages("stage_1");

            Assert.That(option.SubjectId, Is.EqualTo("stage_1"));
            Assert.That(option.Count, Is.EqualTo(3));
            Assert.That(outcome.ChoiceId, Is.EqualTo("choice_marker_position"));
            Assert.That(outcome.OutcomeId, Is.EqualTo("outcome_in_range"));
            CollectionAssert.AreEqual(new[] { "stage_1" }, completion.StageIds);
        }

        [TestCase("choice_avatar_girl")]
        [TestCase("choice_fast")]
        [TestCase("choice_score_3")]
        [TestCase("choice_rank_1")]
        [TestCase("choice_diagnosis")]
        [TestCase("choice_recommend_boat")]
        [TestCase("choice_learning_style")]
        public void BannedIdentityTimingAssessmentAndInferenceTokensAreRejected(string token)
        {
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.Stage1Outcome, token, "outcome_in_range"));
        }

        [Test]
        public void Stage1FactsCannotPopulateUnapprovedTemplates()
        {
            Assert.Throws<ArgumentException>(() => ObservationEvent.OptionsTried(ObservationEventType.BoatRoute, "stage_1", 1));
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(ObservationEventType.Stage1Option, "choice_marker_position", "outcome_in_range"));
            Assert.Throws<ArgumentException>(() => ObservationEvent.CompletedStages(Array.Empty<string>()));
            Assert.That(typeof(ObservationEvent).GetProperties().Select(property => property.Name),
                Does.Not.Contain("PointerPath").And.Not.Contain("ResponseTime").And.Not.Contain("AvatarId"));
        }
    }
}
