using System.Linq;
using Exloris.Runtime.Localization;
using NUnit.Framework;
using UnityEditor.Localization;
using UnityEngine.Localization.Pseudo;
using UnityEngine.Localization.Tables;

namespace Exloris.Tests.EditMode
{
    public sealed class LocalizationAssetContractTests
    {
        [Test]
        public void GeneratedEnglishTableContainsEveryStableFoundationEntry()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(
                UnityStringTableResolver.TableName);
            Assert.That(collection, Is.Not.Null);
            var english = collection.GetTable("en") as StringTable;
            Assert.That(english, Is.Not.Null);

            foreach (var pair in FoundationEnglishStrings.ById)
            {
                Assert.That(english.GetEntry(pair.Key), Is.Not.Null, pair.Key);
                Assert.That(english.GetEntry(pair.Key).Value, Is.EqualTo(pair.Value), pair.Key);
            }

            Assert.That(
                english.Values.Select(entry => entry.Key).OrderBy(value => value),
                Is.EqualTo(FoundationStringIds.All.OrderBy(value => value)));
        }

        [Test]
        public void GeneratedPseudoLocaleDoublesLengthsThroughTenThenUsesThirtyFivePercent()
        {
            var pseudo = LocalizationEditorSettings.GetPseudoLocales()
                .Single(locale => locale.Identifier.Code == "qps-ploc");
            var expander = pseudo.Methods.OfType<Expander>().Single();

            Assert.That(expander.ExpansionRules, Has.Count.EqualTo(2));
            Assert.That(expander.ExpansionRules[0].MinCharacters, Is.EqualTo(0));
            Assert.That(expander.ExpansionRules[0].MaxCharacters, Is.EqualTo(11));
            Assert.That(expander.ExpansionRules[0].ExpansionAmount, Is.EqualTo(1f));
            Assert.That(expander.ExpansionRules[1].MinCharacters, Is.EqualTo(11));
            Assert.That(expander.ExpansionRules[1].ExpansionAmount, Is.EqualTo(0.35f));

            Assert.That(pseudo.GetPseudoString("123456789").Length, Is.GreaterThanOrEqualTo(18));
            Assert.That(pseudo.GetPseudoString("1234567890").Length, Is.GreaterThanOrEqualTo(20));
            Assert.That(pseudo.GetPseudoString("12345678901").Length, Is.LessThan(22));
        }

        [Test]
        public void FontPolicyNamesApprovedPrimaryAndSymbolSourcesWithoutClaimingOtherScripts()
        {
            Assert.That(
                FoundationFontFallbackPolicy.PrimaryFontAssetId,
                Is.EqualTo("font-noto-sans-semibold"));
            Assert.That(
                FoundationFontFallbackPolicy.SymbolFontAssetId,
                Is.EqualTo("font-noto-sans-math"));
            Assert.That(FoundationFontFallbackPolicy.RequiresVerifiedScriptFallback("en"), Is.False);
            Assert.That(FoundationFontFallbackPolicy.RequiresVerifiedScriptFallback("ar"), Is.True);
        }
    }
}
