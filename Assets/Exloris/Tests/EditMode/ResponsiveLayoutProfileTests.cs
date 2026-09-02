using Exloris.Runtime.Layout;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class ResponsiveLayoutProfileTests
    {
        [TestCase(1440, 1080, 0.8660254f)]
        [TestCase(1920, 1080, 1f)]
        [TestCase(2592, 1080, 1.161895f)]
        public void CanvasScaleMatchesFrozenGeometricMeanFormula(
            int width,
            int height,
            float expected)
        {
            Assert.That(
                ResponsiveLayoutProfile.CalculateCanvasScale(width, height),
                Is.EqualTo(expected).Within(0.000001f));
        }

        [TestCase("iPhone SE 3", 326f, 1334, 750, 88, 155, 224, 65, 94)]
        [TestCase("iPad 9", 264f, 2160, 1620, 88, 125, 97, 52, 41)]
        [TestCase("Pixel 6a", 429f, 2400, 1080, 144, 203, 182, 85, 77)]
        [TestCase("Nexus 5X", 423f, 1920, 1080, 126, 200, 200, 84, 84)]
        public void NamedDevicesMeetPhysicalTargetAndGapFloors(
            string device,
            float ppi,
            int width,
            int height,
            int platformMinimumPixels,
            int expectedTargetPixels,
            int expectedTargetCanvasUnits,
            int expectedGapPixels,
            int expectedGapCanvasUnits)
        {
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(width, height);

            Assert.That(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels),
                Is.EqualTo(expectedTargetPixels),
                device);
            Assert.That(
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(expectedTargetPixels, scale),
                Is.EqualTo(expectedTargetCanvasUnits),
                device);
            Assert.That(
                ResponsiveLayoutProfile.MillimetresToPhysicalPixels(5f, ppi),
                Is.EqualTo(expectedGapPixels),
                device);
            Assert.That(
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(expectedGapPixels, scale),
                Is.EqualTo(expectedGapCanvasUnits),
                device);
        }

        [Test]
        public void PlatformMinimumControlsWhenItIsLargerThanTwelveMillimetres()
        {
            Assert.That(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(160f, 100),
                Is.EqualTo(100));
        }

        [TestCase(97f, 41f, 41f)]
        [TestCase(224f, 94f, 94f)]
        [TestCase(360f, 77f, 90f)]
        public void RequiredGapUsesLargerOfFiveMillimetresAndQuarterTarget(
            float targetAxisSize,
            float fiveMillimetreCanvasFloor,
            float expected)
        {
            Assert.That(
                ResponsiveLayoutProfile.CalculateRequiredGap(
                    targetAxisSize,
                    fiveMillimetreCanvasFloor),
                Is.EqualTo(expected).Within(0.001f));
        }
    }
}
