using Exloris.Runtime.Localization;
using Exloris.Runtime.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class PseudolocalizationLayoutTests
    {
        [TestCase(1598.77f, 1183.08f)]
        [TestCase(1856f, 1016f)]
        [TestCase(2038.84f, 825.52f)]
        public void ExpandedHarnessStringsFitFrozenCaptionBandAtEveryRatio(
            float safeWidth,
            float safeHeight)
        {
            var captionWidth = System.Math.Min(0.72f * safeWidth, 1200f);
            var captionHeight = System.Math.Min(0.16f * safeHeight, 0.25f * safeHeight);

            foreach (var source in FoundationEnglishStrings.Values)
            {
                var expanded = Pseudolocalizer.Expand(source);
                Assert.That(
                    LocalizationLayoutValidator.Fits(
                        expanded,
                        captionWidth,
                        captionHeight,
                        averageGlyphWidth: 28f,
                        lineHeight: 52f,
                        maximumLines: 3),
                    Is.True,
                    expanded);
            }
        }

        [Test]
        public void ImpossibleTextBoxIsRejectedInsteadOfClipped()
        {
            Assert.That(
                LocalizationLayoutValidator.Fits(
                    Pseudolocalizer.Expand("Tap the picture"),
                    width: 80f,
                    height: 30f,
                    averageGlyphWidth: 28f,
                    lineHeight: 52f,
                    maximumLines: 1),
                Is.False);
        }

        [Test]
        public void ShippedTextUsesApprovedNotoPrimaryAndMathFallback()
        {
            var owner = new GameObject("NotoRuntimeFontTest");
            owner.AddComponent<ScreenHost>();

            var text = owner.GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);

            Assert.That(text, Is.Not.Null);
            Assert.That(text.font.name, Does.Contain("NotoSans-SemiBold"));
            Assert.That(text.font.fallbackFontAssetTable, Is.Not.Null.And.Not.Empty);
            Assert.That(text.font.fallbackFontAssetTable[0].name, Does.Contain("NotoSansMath-Regular"));
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }
}
