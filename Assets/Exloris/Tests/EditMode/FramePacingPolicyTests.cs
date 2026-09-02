using Exloris.Runtime.Bootstrap;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class FramePacingPolicyTests
    {
        [Test]
        public void MacPlayerUsesHardwareVsyncInsteadOfSoftwareFramePacing()
        {
            var policy = FramePacingPolicy.For(RuntimePlatform.OSXPlayer);

            Assert.That(policy.VSyncCount, Is.EqualTo(1));
            Assert.That(policy.TargetFrameRate, Is.EqualTo(-1));
        }

        [TestCase(RuntimePlatform.Android)]
        [TestCase(RuntimePlatform.IPhonePlayer)]
        public void MobileKeepsFrozenSixtyFrameTarget(RuntimePlatform platform)
        {
            var policy = FramePacingPolicy.For(platform);

            Assert.That(policy.VSyncCount, Is.Zero);
            Assert.That(policy.TargetFrameRate, Is.EqualTo(60));
        }

        [Test]
        public void EditorRemainsDeterministicForExistingPlayModeContracts()
        {
            var policy = FramePacingPolicy.For(RuntimePlatform.OSXEditor);

            Assert.That(policy.VSyncCount, Is.Zero);
            Assert.That(policy.TargetFrameRate, Is.EqualTo(60));
        }
    }
}
