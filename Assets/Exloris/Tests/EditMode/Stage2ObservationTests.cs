using System;
using System.Linq;
using Exloris.Core.Session;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2ObservationTests
    {
        [Test]
        public void AllowlistAcceptsOnlyDirectStage2Facts()
        {
            var pair = ObservationEvent.ChoiceOutcome(ObservationEventType.Stage2Option, "choice_middle", "outcome_path");
            var count = ObservationEvent.OptionsTried(ObservationEventType.Stage2Option, "stage_2", 4);
            var complete = ObservationEvent.CompletedStages("stage_2");
            Assert.That(pair.ChoiceId, Is.EqualTo("choice_middle"));
            Assert.That(pair.OutcomeId, Is.EqualTo("outcome_path"));
            Assert.That(count.SubjectId, Is.EqualTo("stage_2"));
            CollectionAssert.AreEqual(new[] { "stage_2" }, complete.StageIds);
        }

        [TestCase("choice_avatar_boy")]
        [TestCase("choice_fast")]
        [TestCase("choice_score_3")]
        [TestCase("choice_rank_1")]
        [TestCase("choice_diagnosis")]
        [TestCase("choice_recommend_field")]
        [TestCase("choice_learning_style")]
        public void IdentityTimingAssessmentAndInferenceTokensAreRejected(string token)
        {
            Assert.Throws<ArgumentException>(() => ObservationEvent.ChoiceOutcome(
                ObservationEventType.Stage2Option, token, "outcome_field"));
        }

        [Test]
        public void ObservationSchemaHasNoPointerTimingFocusOrIdentityFields()
        {
            var properties = typeof(ObservationEvent).GetProperties().Select(property => property.Name).ToArray();
            CollectionAssert.DoesNotContain(properties, "PointerPath");
            CollectionAssert.DoesNotContain(properties, "ResponseTime");
            CollectionAssert.DoesNotContain(properties, "Focus");
            CollectionAssert.DoesNotContain(properties, "AvatarId");
        }
    }
}
