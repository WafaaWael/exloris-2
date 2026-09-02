using System;
using UnityEngine;

namespace Exloris.Runtime.Layout
{
    public readonly struct EdgeInsets
    {
        public EdgeInsets(float left, float bottom, float right, float top)
        {
            if (left < 0f || bottom < 0f || right < 0f || top < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(left), "Insets cannot be negative.");
            }

            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        public float Left { get; }
        public float Bottom { get; }
        public float Right { get; }
        public float Top { get; }
    }

    public readonly struct NormalizedAnchors
    {
        public NormalizedAnchors(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public Vector2 Min { get; }
        public Vector2 Max { get; }
    }

    [DisallowMultipleComponent]
    public sealed class SafeAreaController : MonoBehaviour
    {
        private const float EdgeTolerance = 0.01f;

        [SerializeField] private RectTransform safeRoot;
        [SerializeField] private Vector4 gestureInsetsPixels;

        private void OnEnable()
        {
            ApplyCurrentScreen();
        }

        public void Configure(RectTransform targetSafeRoot, Vector4 gestureInsets)
        {
            safeRoot = targetSafeRoot ?? throw new ArgumentNullException(nameof(targetSafeRoot));
            gestureInsetsPixels = gestureInsets;
            ApplyCurrentScreen();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCurrentScreen();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                ApplyCurrentScreen();
            }
        }

        public void ApplyCurrentScreen()
        {
            if (safeRoot == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var display = new Rect(0f, 0f, Screen.width, Screen.height);
            var insets = new EdgeInsets(
                gestureInsetsPixels.x,
                gestureInsetsPixels.y,
                gestureInsetsPixels.z,
                gestureInsetsPixels.w);
            var safe = CalculateEffectiveSafeArea(display, Screen.safeArea, Screen.cutouts, insets);
            var anchors = CalculateNormalizedAnchors(display, safe);

            safeRoot.anchorMin = anchors.Min;
            safeRoot.anchorMax = anchors.Max;
            safeRoot.offsetMin = Vector2.zero;
            safeRoot.offsetMax = Vector2.zero;
        }

        public static Rect CalculateEffectiveSafeArea(
            Rect display,
            Rect osSafeArea,
            Rect[] cutouts,
            EdgeInsets gestureInsets)
        {
            ValidatePositiveRect(display, nameof(display));
            ValidatePositiveRect(osSafeArea, nameof(osSafeArea));

            var gestureSafe = Rect.MinMaxRect(
                display.xMin + gestureInsets.Left,
                display.yMin + gestureInsets.Bottom,
                display.xMax - gestureInsets.Right,
                display.yMax - gestureInsets.Top);
            var effective = Intersect(display, osSafeArea);
            effective = Intersect(effective, gestureSafe);

            if (cutouts != null)
            {
                foreach (var cutout in cutouts)
                {
                    if (cutout.width <= 0f || cutout.height <= 0f || !display.Overlaps(cutout))
                    {
                        continue;
                    }

                    effective = ExcludeEdgeCutout(display, effective, cutout);
                }
            }

            if (effective.width <= 0f || effective.height <= 0f)
            {
                throw new InvalidOperationException(
                    "The effective safe area cannot present a valid interactive frame.");
            }

            return effective;
        }

        public static NormalizedAnchors CalculateNormalizedAnchors(Rect display, Rect safeArea)
        {
            ValidatePositiveRect(display, nameof(display));
            ValidatePositiveRect(safeArea, nameof(safeArea));
            var clipped = Intersect(display, safeArea);
            if (clipped.width <= 0f || clipped.height <= 0f)
            {
                throw new InvalidOperationException("Safe area must overlap the display.");
            }

            return new NormalizedAnchors(
                new Vector2(
                    (clipped.xMin - display.xMin) / display.width,
                    (clipped.yMin - display.yMin) / display.height),
                new Vector2(
                    (clipped.xMax - display.xMin) / display.width,
                    (clipped.yMax - display.yMin) / display.height));
        }

        private static Rect ExcludeEdgeCutout(Rect display, Rect effective, Rect cutout)
        {
            var chosenEdge = CutoutEdge.None;
            var chosenFraction = float.PositiveInfinity;

            ConsiderEdge(
                cutout.xMin <= display.xMin + EdgeTolerance,
                (cutout.xMax - display.xMin) / display.width,
                CutoutEdge.Left,
                ref chosenEdge,
                ref chosenFraction);
            ConsiderEdge(
                cutout.xMax >= display.xMax - EdgeTolerance,
                (display.xMax - cutout.xMin) / display.width,
                CutoutEdge.Right,
                ref chosenEdge,
                ref chosenFraction);
            ConsiderEdge(
                cutout.yMin <= display.yMin + EdgeTolerance,
                (cutout.yMax - display.yMin) / display.height,
                CutoutEdge.Bottom,
                ref chosenEdge,
                ref chosenFraction);
            ConsiderEdge(
                cutout.yMax >= display.yMax - EdgeTolerance,
                (display.yMax - cutout.yMin) / display.height,
                CutoutEdge.Top,
                ref chosenEdge,
                ref chosenFraction);

            switch (chosenEdge)
            {
                case CutoutEdge.Left:
                    effective.xMin = Mathf.Max(effective.xMin, cutout.xMax);
                    break;
                case CutoutEdge.Right:
                    effective.xMax = Mathf.Min(effective.xMax, cutout.xMin);
                    break;
                case CutoutEdge.Bottom:
                    effective.yMin = Mathf.Max(effective.yMin, cutout.yMax);
                    break;
                case CutoutEdge.Top:
                    effective.yMax = Mathf.Min(effective.yMax, cutout.yMin);
                    break;
            }

            return effective;
        }

        private static void ConsiderEdge(
            bool touches,
            float fraction,
            CutoutEdge edge,
            ref CutoutEdge chosenEdge,
            ref float chosenFraction)
        {
            if (touches && fraction >= 0f && fraction < chosenFraction)
            {
                chosenEdge = edge;
                chosenFraction = fraction;
            }
        }

        private static Rect Intersect(Rect first, Rect second)
        {
            return Rect.MinMaxRect(
                Mathf.Max(first.xMin, second.xMin),
                Mathf.Max(first.yMin, second.yMin),
                Mathf.Min(first.xMax, second.xMax),
                Mathf.Min(first.yMax, second.yMax));
        }

        private static void ValidatePositiveRect(Rect rect, string name)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Rectangle must have positive size.");
            }
        }

        private enum CutoutEdge
        {
            None,
            Left,
            Right,
            Bottom,
            Top
        }
    }
}
