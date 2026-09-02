using Exloris.Runtime.Diagnostics;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class FoundationEvidenceCaptureTests
    {
        [Test]
        public void CapturePathRequiresExplicitArgumentAndRejectsMissingValue()
        {
            Assert.That(
                FoundationEvidenceCapture.TryGetCapturePath(
                    new[] { "player", "-foundationCapture", "/tmp/current.png" },
                    out var path),
                Is.True);
            Assert.That(path, Is.EqualTo("/tmp/current.png"));
            Assert.That(
                FoundationEvidenceCapture.TryGetCapturePath(
                    new[] { "player", "-foundationCapture" },
                    out _),
                Is.False);
        }
    }
}
