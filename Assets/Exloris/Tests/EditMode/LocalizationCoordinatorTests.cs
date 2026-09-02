using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Localization;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class LocalizationCoordinatorTests
    {
        [Test]
        public void FoundationStringIdsAreStableUniqueAndLowercase()
        {
            var ids = FoundationStringIds.All;

            Assert.That(ids, Is.Not.Empty);
            Assert.That(ids.Distinct().Count(), Is.EqualTo(ids.Count));
            foreach (var id in ids)
            {
                Assert.That(id, Is.EqualTo(id.ToLowerInvariant()));
                Assert.That(id, Does.StartWith("foundation."));
                Assert.That(id, Does.Not.Contain(" "));
            }
        }

        [Test]
        public void EveryHarnessStringResolvesOnlyThroughStableId()
        {
            var values = FoundationStringIds.All.ToDictionary(id => id, id => $"value:{id}");
            var coordinator = new LocalizationCoordinator(new DictionaryStringResolver(values));

            foreach (var id in FoundationStringIds.All)
            {
                Assert.That(coordinator.Resolve(id), Is.EqualTo($"value:{id}"));
            }

            Assert.Throws<KeyNotFoundException>(() => coordinator.Resolve("baked literal"));
        }

        [Test]
        public void IntroCaptionUsesApprovedStableIdInsteadOfBakedViewText()
        {
            const string expectedId = "foundation.intro.caption.vo_intro_001";

            Assert.That(FoundationStringIds.All, Does.Contain(expectedId));
            Assert.That(
                FoundationEnglishStrings.ById[expectedId],
                Is.EqualTo("Come explore the Nile with me."));
        }

        [TestCase("Play", 2.0f)]
        [TestCase("123456789", 2.0f)]
        [TestCase("1234567890", 2.0f)]
        [TestCase("Tap the picture", 1.35f)]
        public void PseudolocalizationMeetsFrozenExpansionFloor(string source, float multiplier)
        {
            var expanded = Pseudolocalizer.Expand(source);

            Assert.That(expanded.Length, Is.GreaterThanOrEqualTo((int)Math.Ceiling(source.Length * multiplier)));
        }

        [TestCase("en", TextFlowDirection.LeftToRight)]
        [TestCase("en-GB", TextFlowDirection.LeftToRight)]
        [TestCase("ar", TextFlowDirection.RightToLeft)]
        [TestCase("he-IL", TextFlowDirection.RightToLeft)]
        [TestCase("fa", TextFlowDirection.RightToLeft)]
        public void TextDirectionIsStructurallyReady(string localeCode, TextFlowDirection expected)
        {
            Assert.That(LocalizationCoordinator.GetDirection(localeCode), Is.EqualTo(expected));
        }

        [Test]
        public void UntranslatedLocaleCannotBeActivatedOrClaimed()
        {
            var coordinator = new LocalizationCoordinator(
                new DictionaryStringResolver(new Dictionary<string, string>()));

            Assert.That(coordinator.CanActivateLocale("en"), Is.True);
            Assert.That(coordinator.CanActivateLocale("ar"), Is.False);
            Assert.That(coordinator.CanActivateLocale("fr"), Is.False);
        }

        [Test]
        public void SpokenInstructionRequiresNonTextVisualEquivalent()
        {
            Assert.Throws<ArgumentException>(() =>
                new LocalizedInstruction(FoundationStringIds.AudioTest, ""));
            Assert.DoesNotThrow(() =>
                new LocalizedInstruction(FoundationStringIds.AudioTest, "visual.listen"));
        }
    }
}
