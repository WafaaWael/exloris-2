using Exloris.Runtime.Avatar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exloris.Tests.PlayMode
{
    public sealed class AvatarNeutralityTests
    {
        [TestCase(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88, 360f, 520f)]
        [TestCase(1334f, 750f, 1856f, 1016f, 326f, 88, 360f, 224f)]
        [TestCase(2400f, 1080f, 2038.84f, 825.52f, 429f, 144, 360f, 182f)]
        public void BothOrdersHaveIdenticalGeometryProminenceAndContainment(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels, float cardWidth, float cardHeight)
        {
            var router = CreateRouter();
            var owner = new GameObject("AvatarSelectNeutrality");
            try
            {
                var view = owner.AddComponent<AvatarSelectView>();
                view.Configure(router, AvatarContent.LoadApproved());
                view.ApplyResponsiveLayout(screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);

                view.Show(AvatarChoice.Boy, AvatarChoice.Girl);
                var boyLeft = view.GetAvatarGeometry(AvatarChoice.Boy);
                var girlRight = view.GetAvatarGeometry(AvatarChoice.Girl);
                AssertEqualProminence(boyLeft, girlRight, cardWidth, cardHeight);

                view.Show(AvatarChoice.Girl, AvatarChoice.Boy);
                var girlLeft = view.GetAvatarGeometry(AvatarChoice.Girl);
                var boyRight = view.GetAvatarGeometry(AvatarChoice.Boy);
                AssertEqualProminence(girlLeft, boyRight, cardWidth, cardHeight);
                Assert.That(girlLeft.TargetRect, Is.EqualTo(boyLeft.TargetRect));
                Assert.That(boyRight.TargetRect, Is.EqualTo(girlRight.TargetRect));
                Assert.That(view.DoTouchTargetsOverlap(), Is.False);
                Assert.That(view.AreAllTargetsInsideSafeArea(), Is.True);
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        [TestCase(AvatarChoice.Boy)]
        [TestCase(AvatarChoice.Girl)]
        public void PreviewUsesSamePivotBaselineScaleAndMotionContract(AvatarChoice choice)
        {
            var router = CreateRouter();
            var owner = new GameObject("AvatarPreviewNeutrality");
            try
            {
                var view = owner.AddComponent<AvatarPreviewView>();
                view.Configure(router, AvatarContent.LoadApproved());
                view.ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
                view.Show(choice, 11);
                var geometry = view.GetAvatarGeometry();

                Assert.That(geometry.Pivot, Is.EqualTo(new Vector2(0.5f, 0.08f)));
                Assert.That(geometry.Baseline, Is.EqualTo(geometry.ArtRect.yMin).Within(0.01f));
                Assert.That(geometry.Scale, Is.EqualTo(Vector3.one));
                Assert.That(geometry.OutlineWidth, Is.EqualTo(0f));
                Assert.That(geometry.Lighting, Is.EqualTo(Color.white));
                Assert.That(geometry.ShadowOpacity, Is.EqualTo(0f));
                Assert.That(geometry.MotionDurationSeconds, Is.EqualTo(4d));
                Assert.That(geometry.ArtRect.xMin, Is.GreaterThanOrEqualTo(geometry.TargetRect.xMin));
                Assert.That(geometry.ArtRect.xMax, Is.LessThanOrEqualTo(geometry.TargetRect.xMax));
                Assert.That(geometry.ArtRect.yMin, Is.GreaterThanOrEqualTo(geometry.TargetRect.yMin));
                Assert.That(geometry.ArtRect.yMax, Is.LessThanOrEqualTo(geometry.TargetRect.yMax));
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        private static void AssertEqualProminence(
            AvatarGeometry first, AvatarGeometry second, float cardWidth, float cardHeight)
        {
            Assert.That(first.TargetRect.size, Is.EqualTo(new Vector2(cardWidth, cardHeight)).Within(0.01f));
            Assert.That(second.TargetRect.size, Is.EqualTo(first.TargetRect.size));
            Assert.That(second.CardRect.size, Is.EqualTo(first.CardRect.size));
            Assert.That(second.ArtRect.size, Is.EqualTo(first.ArtRect.size));
            Assert.That(second.Pivot, Is.EqualTo(first.Pivot));
            Assert.That(second.Baseline, Is.EqualTo(first.Baseline).Within(0.01f));
            Assert.That(second.Scale, Is.EqualTo(first.Scale));
            Assert.That(second.Lighting, Is.EqualTo(first.Lighting));
            Assert.That(second.ShadowOpacity, Is.EqualTo(first.ShadowOpacity));
            Assert.That(second.OutlineWidth, Is.EqualTo(first.OutlineWidth));
            Assert.That(second.MotionDurationSeconds, Is.EqualTo(first.MotionDurationSeconds));
            Assert.That(first.CardRect.xMin, Is.GreaterThanOrEqualTo(first.TargetRect.xMin));
            Assert.That(first.ArtRect.xMin, Is.GreaterThanOrEqualTo(first.CardRect.xMin));
            Assert.That(first.ArtRect.xMax, Is.LessThanOrEqualTo(first.CardRect.xMax));
        }

        private static Exloris.Runtime.Input.ChildInputRouter CreateRouter()
        {
            return new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
        }
    }
}
