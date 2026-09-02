using Exloris.Runtime.Diagnostics;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarEvidenceOptionsTests
    {
        [Test]
        public void CaptureRequiresPathAndParsesExactRequestedProofState()
        {
            var arguments = new[]
            {
                "player", "-avatarCapture", "/tmp/avatar.png",
                "-avatarScenario", "previewGirl", "-avatarMuted",
                "-avatarCaptions", "-avatarReducedMotion", "-avatarCaptureDelay", "5.5"
            };

            Assert.That(AvatarEvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/avatar.png"));
            Assert.That(options.Scenario, Is.EqualTo(AvatarEvidenceScenario.PreviewGirl));
            Assert.That(options.Muted, Is.True);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.DelaySeconds, Is.EqualTo(5.5d));
            Assert.That(AvatarEvidenceOptions.TryGetCapture(new[] { "player" }, out _), Is.False);
        }

        [TestCase("selectBoyLeft", AvatarEvidenceScenario.SelectBoyLeft)]
        [TestCase("selectGirlLeft", AvatarEvidenceScenario.SelectGirlLeft)]
        [TestCase("previewBoy", AvatarEvidenceScenario.PreviewBoy)]
        [TestCase("previewGirl", AvatarEvidenceScenario.PreviewGirl)]
        [TestCase("leaveSelect", AvatarEvidenceScenario.LeaveSelect)]
        [TestCase("leavePreview", AvatarEvidenceScenario.LeavePreview)]
        [TestCase("interruption", AvatarEvidenceScenario.Interruption)]
        [TestCase("recoverySelect", AvatarEvidenceScenario.RecoverySelect)]
        [TestCase("sessionResumePreview", AvatarEvidenceScenario.SessionResumePreview)]
        public void OnlyAvatarOwnedScenariosAreAccepted(string value, AvatarEvidenceScenario expected)
        {
            Assert.That(AvatarEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-avatarCapture", "/tmp/avatar.png", "-avatarScenario", value
            }, out var options), Is.True);
            Assert.That(options.Scenario, Is.EqualTo(expected));
        }

        [TestCase("journeyOverview")]
        [TestCase("thirdAvatar")]
        [TestCase("parentReport")]
        public void LaterRouteAndUnapprovedAvatarScenariosAreRejected(string scenario)
        {
            Assert.That(AvatarEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-avatarCapture", "/tmp/avatar.png", "-avatarScenario", scenario
            }, out _), Is.False);
        }

        [TestCase("-0.1")]
        [TestCase("30.1")]
        [TestCase("not-a-number")]
        public void DelayOutsideFrozenEvidenceRangeIsRejected(string delay)
        {
            Assert.That(AvatarEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-avatarCapture", "/tmp/avatar.png", "-avatarCaptureDelay", delay
            }, out _), Is.False);
        }
    }
}
