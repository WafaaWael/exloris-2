using Exloris.Runtime.Layout;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Tests.PlayMode
{
    public sealed class ResponsiveBackgroundPresenterTests
    {
        [TestCase(1440, 1080, ResponsiveBackgroundVariant.FourByThree)]
        [TestCase(1920, 1080, ResponsiveBackgroundVariant.SixteenByNine)]
        [TestCase(2592, 1080, ResponsiveBackgroundVariant.UltraWide)]
        public void ActualOverlayPresenterSelectsTheRatioSpriteAndForbidsCameraMinification(
            int width,
            int height,
            ResponsiveBackgroundVariant expected)
        {
            var canvasObject = new GameObject("BackgroundCanvas", typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var imageObject = new GameObject(
                "ResponsiveBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ResponsiveBackgroundPresenter));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var presenter = imageObject.GetComponent<ResponsiveBackgroundPresenter>();
            var fourByThree = CreateSprite(4, 3);
            var sixteenByNine = CreateSprite(16, 9);
            var ultraWide = CreateSprite(24, 10);
            presenter.Configure(fourByThree, sixteenByNine, ultraWide);

            presenter.Present(width, height);

            Assert.That(presenter.CurrentVariant, Is.EqualTo(expected));
            Assert.That(presenter.IsPresentationGeometryValid(), Is.True);
            Assert.That(imageObject.GetComponent<RectTransform>().localScale, Is.EqualTo(Vector3.one));
            imageObject.GetComponent<RectTransform>().localScale = new Vector3(0.9f, 0.9f, 1f);
            Assert.Throws<System.InvalidOperationException>(() => presenter.ValidateOrThrow());

            Object.DestroyImmediate(fourByThree.texture);
            Object.DestroyImmediate(sixteenByNine.texture);
            Object.DestroyImmediate(ultraWide.texture);
            Object.DestroyImmediate(canvasObject);
        }

        private static Sprite CreateSprite(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
