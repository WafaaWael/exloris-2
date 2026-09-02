using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class TouchTargetGeometryTests
    {
        [Test]
        public void ValidTargetContainsCenteredVisibleArtAndPassesCentreAndEdgeTaps()
        {
            var target = new Rect(100f, 100f, 224f, 224f);
            var art = new Rect(131f, 131f, 162f, 162f);

            var result = TouchTargetValidator.Validate(
                target,
                art,
                new Rect[0],
                minimumTargetCanvasUnits: 224f,
                minimumGapCanvasUnits: 94f);

            Assert.That(result.IsValid, Is.True, result.Message);
            Assert.That(TouchTargetValidator.ContainsInclusive(target, target.center), Is.True);
            Assert.That(TouchTargetValidator.ContainsInclusive(target, target.min), Is.True);
            Assert.That(TouchTargetValidator.ContainsInclusive(target, target.max), Is.True);
            Assert.That(
                TouchTargetValidator.ContainsInclusive(target, target.max + Vector2.one),
                Is.False);
        }

        [Test]
        public void VisibleArtOutsideItsIntendedTargetFails()
        {
            var result = TouchTargetValidator.Validate(
                new Rect(100f, 100f, 224f, 224f),
                new Rect(90f, 120f, 180f, 180f),
                new Rect[0],
                224f,
                94f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TouchTargetFailure.VisibleArtOutsideTarget));
        }

        [Test]
        public void OffCentreVisibleArtFails()
        {
            var result = TouchTargetValidator.Validate(
                new Rect(100f, 100f, 224f, 224f),
                new Rect(131f, 130f, 162f, 162f),
                new Rect[0],
                224f,
                94f);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Failure, Is.EqualTo(TouchTargetFailure.VisibleArtNotCentered));
        }

        [Test]
        public void TargetBelowPhysicalFloorFails()
        {
            var result = TouchTargetValidator.Validate(
                new Rect(0f, 0f, 223f, 224f),
                new Rect(20f, 20f, 183f, 184f),
                new Rect[0],
                224f,
                94f);

            Assert.That(result.Failure, Is.EqualTo(TouchTargetFailure.BelowMinimumSize));
        }

        [Test]
        public void OverlappingTargetFails()
        {
            var result = TouchTargetValidator.Validate(
                new Rect(0f, 0f, 224f, 224f),
                new Rect(31f, 31f, 162f, 162f),
                new[] { new Rect(200f, 0f, 224f, 224f) },
                224f,
                56f);

            Assert.That(result.Failure, Is.EqualTo(TouchTargetFailure.Overlap));
        }

        [Test]
        public void GapBelowQuarterOfActualTargetWidthFails()
        {
            var result = TouchTargetValidator.Validate(
                new Rect(0f, 0f, 360f, 224f),
                new Rect(22f, 14f, 316f, 196f),
                new[] { new Rect(449f, 0f, 360f, 224f) },
                224f,
                77f);

            Assert.That(result.Failure, Is.EqualTo(TouchTargetFailure.InsufficientSpacing));
        }

        [Test]
        public void DiagnosticHitboxRenderingIsNeverAllowedInFinalMode()
        {
            Assert.That(TouchTargetValidator.CanRenderDiagnostic(TargetPresentationMode.Review), Is.True);
            Assert.That(TouchTargetValidator.CanRenderDiagnostic(TargetPresentationMode.Final), Is.False);
        }
    }
}
