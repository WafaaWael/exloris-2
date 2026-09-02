using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Session;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class ParentReportBuilderTests
    {
        private const string Disclosure = "This game keeps only accessibility settings on this device. It does not identify, diagnose, rank, score, compare, or advertise to your child. The parent summary describes only actions from the current play session and disappears when the session is reset or the app data is cleared.";

        [Test]
        public void EmptySnapshotContainsExactDisclosureAndExplicitCurrentSessionEmptyState()
        {
            var report = ParentReportBuilder.Build(Array.Empty<ObservationEvent>());

            Assert.That(report.Cards, Has.Count.EqualTo(2));
            Assert.That(report.Cards[0].StringId, Is.EqualTo("parent_report.privacy_disclosure"));
            Assert.That(report.Cards[0].Text, Is.EqualTo(Disclosure));
            Assert.That(report.Cards[1].StringId, Is.EqualTo("parent_report.empty"));
            Assert.That(report.Cards[1].Text, Is.EqualTo("No recorded journey actions"));
            Assert.That(report.FactualCardCount, Is.Zero);
        }

        [Test]
        public void FiveFrozenFamiliesRenderInFirstSourceOrderWithExactDuplicateReduction()
        {
            var choice = ObservationEvent.ChoiceOutcome(
                ObservationEventType.BoatRoute, "choice_left_bank", "outcome_reeds_move");
            var source = new[]
            {
                choice,
                ObservationEvent.OptionsTried(ObservationEventType.Stage1Option, "stage_1", 2),
                ObservationEvent.BirdGuideUsed(3),
                choice,
                ObservationEvent.Revisited("outcome_reeds_move", 1),
                ObservationEvent.CompletedStages("stage_1", "stage_2", "stage_3"),
            };

            var report = ParentReportBuilder.Build(source);

            Assert.That(report.FactualCardCount, Is.EqualTo(5));
            CollectionAssert.AreEqual(
                new[]
                {
                    "parent_report.privacy_disclosure",
                    "parent_report.choice_outcome",
                    "parent_report.options_tried",
                    "parent_report.bird_guide_use",
                    "parent_report.revisit",
                    "parent_report.completed_stages",
                },
                report.Cards.Select(card => card.StringId));
            CollectionAssert.AreEqual(
                new[]
                {
                    Disclosure,
                    "During this session, your child chose choice_left_bank and saw outcome_reeds_move.",
                    "Your child tried 2 option(s) in stage_1 before continuing.",
                    "Your child used the bird guide 3 time(s) during the boat journeys.",
                    "Your child revisited outcome_reeds_move 1 time(s).",
                    "Your child completed stage_1, stage_2, stage_3 in this session.",
                },
                report.Cards.Select(card => card.Text));
        }

        [Test]
        public void UnsupportedBoatSkipDoesNotCreateAReportFact()
        {
            var report = ParentReportBuilder.Build(new[] { ObservationEvent.BoatSkipped("boat_1", 1) });

            Assert.That(report.FactualCardCount, Is.Zero);
            Assert.That(report.Cards.Last().StringId, Is.EqualTo("parent_report.empty"));
        }

        [Test]
        public void BuilderCopiesOutputAndNeverMutatesOrRetainsTheSourceCollection()
        {
            var source = new List<ObservationEvent> { ObservationEvent.BirdGuideUsed(1) };
            var report = ParentReportBuilder.Build(source);
            source.Clear();
            source.Add(ObservationEvent.BirdGuideUsed(9));

            Assert.That(report.Cards, Has.Count.EqualTo(2));
            Assert.That(report.Cards[1].Text,
                Is.EqualTo("Your child used the bird guide 1 time(s) during the boat journeys."));
            Assert.That(report.Cards, Is.Not.AssignableTo<List<ParentReportCard>>());
        }

        [Test]
        public void NullSnapshotFailsClosed()
        {
            Assert.Throws<ArgumentNullException>(() => ParentReportBuilder.Build(null));
            var report = ParentReportBuilder.Build(new ObservationEvent[] { null });
            Assert.That(report.FactualCardCount, Is.Zero);
            Assert.That(report.Cards.Last().StringId, Is.EqualTo("parent_report.empty"));
        }
    }
}
