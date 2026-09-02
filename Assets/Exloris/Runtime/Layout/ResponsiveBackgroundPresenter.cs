using System;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Layout
{
    public enum ResponsiveBackgroundVariant
    {
        FourByThree,
        SixteenByNine,
        UltraWide
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class ResponsiveBackgroundPresenter : MonoBehaviour
    {
        private Sprite _fourByThree;
        private Sprite _sixteenByNine;
        private Sprite _ultraWide;
        private RectTransform _rect;
        private Image _image;

        public ResponsiveBackgroundVariant CurrentVariant { get; private set; }

        private void Awake()
        {
            CacheComponents();
        }

        public void Configure(Sprite fourByThree, Sprite sixteenByNine, Sprite ultraWide)
        {
            _fourByThree = fourByThree != null ? fourByThree : throw new ArgumentNullException(nameof(fourByThree));
            _sixteenByNine = sixteenByNine != null ? sixteenByNine : throw new ArgumentNullException(nameof(sixteenByNine));
            _ultraWide = ultraWide != null ? ultraWide : throw new ArgumentNullException(nameof(ultraWide));
            CacheComponents();
            _image.raycastTarget = false;
            _image.type = Image.Type.Simple;
            _image.preserveAspect = false;
        }

        public void Present(float viewportWidth, float viewportHeight)
        {
            if (viewportWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(viewportWidth));
            if (viewportHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(viewportHeight));
            if (_fourByThree == null || _sixteenByNine == null || _ultraWide == null)
            {
                throw new InvalidOperationException("All three responsive background variants are required.");
            }

            CacheComponents();
            CurrentVariant = SelectVariant(viewportWidth / viewportHeight);
            _image.sprite = CurrentVariant == ResponsiveBackgroundVariant.FourByThree
                ? _fourByThree
                : CurrentVariant == ResponsiveBackgroundVariant.SixteenByNine
                    ? _sixteenByNine
                    : _ultraWide;
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
            _rect.localScale = Vector3.one;
            ValidateOrThrow();
        }

        public bool IsPresentationGeometryValid()
        {
            CacheComponents();
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null &&
                   canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                   Approximately(_rect.anchorMin, Vector2.zero) &&
                   Approximately(_rect.anchorMax, Vector2.one) &&
                   Approximately(_rect.offsetMin, Vector2.zero) &&
                   Approximately(_rect.offsetMax, Vector2.zero) &&
                   Approximately(_rect.localScale, Vector3.one);
        }

        public void ValidateOrThrow()
        {
            if (!IsPresentationGeometryValid())
            {
                throw new InvalidOperationException(
                    "BG3 backgrounds require an unscaled, full-stretch Screen Space Overlay presenter; camera zoom/minification is forbidden.");
            }
        }

        private static ResponsiveBackgroundVariant SelectVariant(float aspect)
        {
            var fourByThreeDistance = Mathf.Abs(aspect - 4f / 3f);
            var sixteenByNineDistance = Mathf.Abs(aspect - 16f / 9f);
            var ultraWideDistance = Mathf.Abs(aspect - 2.4f);
            if (fourByThreeDistance <= sixteenByNineDistance && fourByThreeDistance <= ultraWideDistance)
            {
                return ResponsiveBackgroundVariant.FourByThree;
            }

            return sixteenByNineDistance <= ultraWideDistance
                ? ResponsiveBackgroundVariant.SixteenByNine
                : ResponsiveBackgroundVariant.UltraWide;
        }

        private void CacheComponents()
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();
            if (_image == null) _image = GetComponent<Image>();
        }

        private static bool Approximately(Vector2 left, Vector2 right)
        {
            return Vector2.SqrMagnitude(left - right) <= 0.000001f;
        }

        private static bool Approximately(Vector3 left, Vector3 right)
        {
            return Vector3.SqrMagnitude(left - right) <= 0.000001f;
        }
    }
}
