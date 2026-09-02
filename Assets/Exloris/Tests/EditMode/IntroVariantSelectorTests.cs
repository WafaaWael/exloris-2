using Exloris.Runtime.Intro;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class IntroVariantSelectorTests
    {
        [TestCase(2048, 1536, false, "intro-4x3.mp4", 288, 9.6)]
        [TestCase(1920, 1080, false, "intro-16x9.mp4", 288, 9.6)]
        [TestCase(2592, 1080, false, "intro-24x10.mp4", 288, 9.6)]
        [TestCase(2048, 1536, true, "intro-reduced-4x3.mp4", 120, 4.0)]
        [TestCase(1920, 1080, true, "intro-reduced-16x9.mp4", 120, 4.0)]
        [TestCase(2592, 1080, true, "intro-reduced-24x10.mp4", 120, 4.0)]
        public void SelectsExactApprovedRatioAndMotionVariant(
            int width,
            int height,
            bool reducedMotion,
            string expectedFile,
            int expectedFrames,
            double expectedSeconds)
        {
            var variant = IntroVariantSelector.Select(width, height, reducedMotion, preferLowResolution: false);

            Assert.That(variant.FileName, Is.EqualTo(expectedFile));
            Assert.That(variant.FrameRate, Is.EqualTo(30));
            Assert.That(variant.FrameCount, Is.EqualTo(expectedFrames));
            Assert.That(variant.DurationSeconds, Is.EqualTo(expectedSeconds).Within(0.0001d));
        }

        [TestCase(false, "intro-16x9-low.mp4", 288, 9.6)]
        [TestCase(true, "intro-reduced-16x9-low.mp4", 120, 4.0)]
        public void LowTierNamedDevicesUseApprovedLowTierVariants(
            bool reducedMotion,
            string expectedFile,
            int expectedFrames,
            double expectedSeconds)
        {
            var variant = IntroVariantSelector.Select(1920, 1080, reducedMotion, preferLowResolution: true);

            Assert.That(variant.FileName, Is.EqualTo(expectedFile));
            Assert.That(variant.FrameCount, Is.EqualTo(expectedFrames));
            Assert.That(variant.DurationSeconds, Is.EqualTo(expectedSeconds).Within(0.0001d));
        }

        [TestCase(2048, 1536, false, false, 1920, 1440)]
        [TestCase(1920, 1080, false, false, 2560, 1440)]
        [TestCase(2592, 1080, false, false, 2592, 1080)]
        [TestCase(1920, 1080, false, true, 1920, 1080)]
        public void VariantCarriesExactApprovedRenderDimensions(
            int screenWidth,
            int screenHeight,
            bool reducedMotion,
            bool preferLowResolution,
            int expectedWidth,
            int expectedHeight)
        {
            var variant = IntroVariantSelector.Select(
                screenWidth,
                screenHeight,
                reducedMotion,
                preferLowResolution);

            Assert.That(variant.VideoWidth, Is.EqualTo(expectedWidth));
            Assert.That(variant.VideoHeight, Is.EqualTo(expectedHeight));
        }

        [TestCase(false, false, 64_000_000)]
        [TestCase(true, false, 64_000_000)]
        [TestCase(false, true, 48_000_000)]
        [TestCase(true, true, 48_000_000)]
        public void VariantCarriesItsBindingDecodeBufferCeiling(
            bool reducedMotion,
            bool preferLowResolution,
            long expectedCeiling)
        {
            var variant = IntroVariantSelector.Select(
                1920,
                1080,
                reducedMotion,
                preferLowResolution);

            Assert.That(variant.MaximumDecodeBufferBytes, Is.EqualTo(expectedCeiling));
            Assert.DoesNotThrow(() => variant.EnsureDecodeBufferWithinCeiling(expectedCeiling));
            Assert.Throws<System.InvalidOperationException>(() =>
                variant.EnsureDecodeBufferWithinCeiling(expectedCeiling + 1));
        }

        [Test]
        public void ReducedVariantAllowsOnlyShortOpacityFadeWithoutSpatialMotion()
        {
            var reduced = IntroVariantSelector.Select(
                1920,
                1080,
                reducedMotion: true,
                preferLowResolution: false);

            Assert.That(reduced.TransitionMode, Is.EqualTo(IntroTransitionMode.OpacityFade));
            Assert.That(reduced.MaximumTransitionDurationSeconds, Is.LessThanOrEqualTo(0.150d));
            Assert.That(reduced.HasSpatialMotion, Is.False);
        }
    }
}
