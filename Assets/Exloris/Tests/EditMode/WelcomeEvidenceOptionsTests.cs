using Exloris.Runtime.Diagnostics;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeEvidenceOptionsTests
    {
        [Test]
        public void CaptureRequiresPathAndParsesExactRequestedProofState()
        {
            var arguments = new[]
            {
                "player", "-welcomeCapture", "/tmp/welcome.png",
                "-welcomeScenario", "interruption", "-welcomeMuted",
                "-welcomeCaptions", "-welcomeReducedMotion", "-welcomeCaptureDelay", "5.5"
            };

            Assert.That(WelcomeEvidenceOptions.TryGetCapture(arguments, out var options), Is.True);
            Assert.That(options.Path, Is.EqualTo("/tmp/welcome.png"));
            Assert.That(options.Scenario, Is.EqualTo(WelcomeEvidenceScenario.Interruption));
            Assert.That(options.Muted, Is.True);
            Assert.That(options.CaptionsEnabled, Is.True);
            Assert.That(options.ReducedMotion, Is.True);
            Assert.That(options.DelaySeconds, Is.EqualTo(5.5d));
            Assert.That(WelcomeEvidenceOptions.TryGetCapture(new[] { "player" }, out _), Is.False);
        }

        [TestCase("welcome", WelcomeEvidenceScenario.Welcome)]
        [TestCase("interruption", WelcomeEvidenceScenario.Interruption)]
        [TestCase("sessionChoice", WelcomeEvidenceScenario.SessionChoice)]
        public void OnlyNamedWelcomeOwnedScenariosAreAccepted(
            string value,
            WelcomeEvidenceScenario expected)
        {
            Assert.That(WelcomeEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-welcomeCapture", "/tmp/welcome.png", "-welcomeScenario", value
            }, out var options), Is.True);
            Assert.That(options.Scenario, Is.EqualTo(expected));
            Assert.That(WelcomeEvidenceOptions.TryGetCapture(new[]
            {
                "player", "-welcomeCapture", "/tmp/welcome.png", "-welcomeScenario", "avatar"
            }, out _), Is.False);
        }
    }
}
