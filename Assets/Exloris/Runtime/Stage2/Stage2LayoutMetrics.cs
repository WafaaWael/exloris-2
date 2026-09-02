using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Stage2
{
    public readonly struct Stage2LayoutMetrics
    {
        private Stage2LayoutMetrics(
            float canvasScale, Rect safeCanvasRect, float target, float gap,
            Vector2[] topCenters, Vector2[] bottomCenters,
            Vector2 choiceSize, Vector2[] choiceCenters,
            Vector2 hotspotSize, Vector2[] hotspotCenters,
            Rect captionRect, Rect outcomeRect, Rect childRect, bool fits)
        {
            CanvasScale = canvasScale;
            SafeCanvasRect = safeCanvasRect;
            Target = target;
            Gap = gap;
            TopCenters = topCenters;
            BottomCenters = bottomCenters;
            ChoiceSize = choiceSize;
            ChoiceCenters = choiceCenters;
            HotspotSize = hotspotSize;
            HotspotCenters = hotspotCenters;
            CaptionRect = captionRect;
            OutcomeRect = outcomeRect;
            ChildRect = childRect;
            Fits = fits;
        }

        public float CanvasScale { get; }
        public Rect SafeCanvasRect { get; }
        public float Target { get; }
        public float Gap { get; }
        public Vector2[] TopCenters { get; }
        public Vector2[] BottomCenters { get; }
        public Vector2 ChoiceSize { get; }
        public Vector2[] ChoiceCenters { get; }
        public Vector2 HotspotSize { get; }
        public Vector2[] HotspotCenters { get; }
        public Rect CaptionRect { get; }
        public Rect OutcomeRect { get; }
        public Rect ChildRect { get; }
        public bool Fits { get; }

        public static Stage2LayoutMetrics Calculate(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeX < 0 || safeY < 0
                || safeWidth <= 0 || safeHeight <= 0 || ppi <= 0 || platformMinimumPixels < 0)
                throw new ArgumentOutOfRangeException();

            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var safe = new Rect(safeX / scale, safeY / scale, safeWidth / scale, safeHeight / scale);
            var physicalTarget = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels);
            var target = Mathf.Max(60f,
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(physicalTarget, scale));
            var gapPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(
                ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var gap = Mathf.Max(18f, ResponsiveLayoutProfile.CalculateRequiredGap(
                target, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(gapPixels, scale)));
            var top = Centers(4, safe.width, safe.height - target * 0.5f, target + gap);
            var bottom = Centers(3, safe.width, target * 0.5f, target + gap);
            var bodyBottom = target + gap;
            var bodyTop = safe.height - target - gap;
            var bodyHeight = Mathf.Max(0f, bodyTop - bodyBottom);
            var captionHeight = Mathf.Min(target, bodyHeight * 0.18f);
            var caption = new Rect(safe.width * 0.15f, bodyTop - captionHeight,
                safe.width * 0.70f, captionHeight);
            var contentTop = caption.yMin - gap;
            var availableHeight = Mathf.Max(0f, contentTop - bodyBottom);
            var optionSide = Mathf.Min(230f,
                Mathf.Min(availableHeight * 0.46f, (safe.width - 4f * gap) / 3f));
            optionSide = Mathf.Max(target, optionSide);
            var optionStep = optionSide + gap;
            var optionY = bodyBottom + availableHeight * 0.52f;
            var choices = Centers(3, safe.width, optionY, optionStep);
            var hotspots = Centers(3, safe.width, optionY, optionStep);
            var childWidth = Mathf.Min(optionSide * 0.78f, safe.width * 0.18f);
            var child = new Rect(gap, bodyBottom, childWidth, availableHeight);
            var outcomeWidth = Mathf.Min(safe.width * 0.50f, availableHeight * 2f);
            var outcome = new Rect(safe.width * 0.5f - outcomeWidth * 0.5f,
                bodyBottom, outcomeWidth, availableHeight);
            var fits = 4f * target + 3f * gap <= safe.width
                && 2f * target + 2f * gap < safe.height
                && availableHeight > target
                && 3f * optionSide + 2f * gap <= safe.width
                && optionSide >= target
                && caption.width > 0 && caption.height > 0
                && child.width > 0 && child.height > 0
                && outcome.width > 0 && outcome.height > 0;
            return new Stage2LayoutMetrics(scale, safe, target, gap, top, bottom,
                Vector2.one * optionSide, choices, Vector2.one * optionSide, hotspots,
                caption, outcome, child, fits);
        }

        private static Vector2[] Centers(int count, float width, float y, float step)
        {
            var result = new Vector2[count];
            var first = width * 0.5f - step * (count - 1) * 0.5f;
            for (var index = 0; index < count; index++) result[index] = new Vector2(first + index * step, y);
            return result;
        }
    }
}
