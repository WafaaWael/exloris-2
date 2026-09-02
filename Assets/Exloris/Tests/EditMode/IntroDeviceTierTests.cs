using Exloris.Runtime.Intro;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class IntroDeviceTierTests
    {
        [TestCase("iPhone14,6", RuntimePlatform.IPhonePlayer, 0, 4096, true)]
        [TestCase("iPhone SE (3rd generation)", RuntimePlatform.IPhonePlayer, 0, 4096, true)]
        [TestCase("Nexus 5X", RuntimePlatform.Android, 27, 2049, true)]
        [TestCase("Nexus 5X", RuntimePlatform.Android, 30, 2049, false)]
        [TestCase("Generic low-memory", RuntimePlatform.Android, 34, 2048, true)]
        [TestCase("Generic boundary-above", RuntimePlatform.Android, 34, 2049, false)]
        [TestCase("Pixel 6a", RuntimePlatform.Android, 34, 6144, false)]
        [TestCase("iPad12,1", RuntimePlatform.IPhonePlayer, 0, 3072, false)]
        public void OnlyNamedLowTierTargetsSelectLowVideo(
            string model,
            RuntimePlatform platform,
            int androidApiLevel,
            int systemMemoryMb,
            bool expected)
        {
            Assert.That(
                IntroDeviceTier.ShouldPreferLowResolution(
                    model,
                    platform,
                    androidApiLevel,
                    systemMemoryMb),
                Is.EqualTo(expected));
        }

        [Test]
        public void LowTierPreferenceNeverInventsUnapprovedNonSixteenByNineFiles()
        {
            Assert.That(
                IntroVariantSelector.Select(1440, 1080, false, true).FileName,
                Is.EqualTo("intro-4x3.mp4"));
            Assert.That(
                IntroVariantSelector.Select(2592, 1080, true, true).FileName,
                Is.EqualTo("intro-reduced-24x10.mp4"));
        }
    }
}
