using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    public readonly struct Boat3LayoutMetrics
    {
        private Boat3LayoutMetrics(float target, float gap, Vector2[] top, Vector2[] bottom, Rect body, Rect caption, float ibisHeight)
        { Target = target; Gap = gap; TopCenters = top; BottomCenters = bottom; BodyRect = body; CaptionRect = caption; IbisHeight = ibisHeight; }
        public float Target { get; }
        public float Gap { get; }
        public Vector2[] TopCenters { get; }
        public Vector2[] BottomCenters { get; }
        public Rect BodyRect { get; }
        public Rect CaptionRect { get; }
        public float IbisHeight { get; }

        public static Boat3LayoutMetrics Calculate(float screenWidth, float screenHeight, float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeWidth <= 0 || safeHeight <= 0 || ppi <= 0) throw new ArgumentOutOfRangeException();
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var target = Mathf.Max(72f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels), scale));
            var physicalGap = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var gap = ResponsiveLayoutProfile.CalculateRequiredGap(target, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(physicalGap, scale));
            var step = target + gap;
            var top = Centers(4, safeWidth, safeHeight - target * 0.5f, step);
            var bottom = Centers(4, safeWidth, target * 0.5f, step);
            var bodyBottom = target + gap; var bodyTop = safeHeight - target - gap;
            var body = new Rect(gap, bodyBottom, Mathf.Max(0, safeWidth - 2 * gap), Mathf.Max(0, bodyTop - bodyBottom));
            var captionHeight = Mathf.Min(target, body.height * 0.24f);
            var caption = new Rect(safeWidth * 0.15f, body.yMax - captionHeight, safeWidth * 0.70f, captionHeight);
            return new Boat3LayoutMetrics(target, gap, top, bottom, body, caption, safeHeight * 0.25f);
        }

        public bool Fits(float safeWidth, float safeHeight) => 4 * Target + 3 * Gap <= safeWidth
            && 2 * Target + 2 * Gap < safeHeight && BodyRect.width > 0 && BodyRect.height > 0
            && CaptionRect.width > 0 && CaptionRect.height > 0 && IbisHeight >= safeHeight * 0.22f && IbisHeight <= safeHeight * 0.28f;

        private static Vector2[] Centers(int count, float width, float y, float step)
        {
            var result = new Vector2[count]; var first = width * 0.5f - step * (count - 1) * 0.5f;
            for (var i = 0; i < count; i++) result[i] = new Vector2(first + i * step, y);
            return result;
        }
    }
}
