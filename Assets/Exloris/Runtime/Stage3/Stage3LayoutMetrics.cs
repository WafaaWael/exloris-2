using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Stage3
{
    public readonly struct Stage3LayoutMetrics
    {
        private Stage3LayoutMetrics(float canvasScale, Rect safeCanvasRect, float target, float gap,
            Vector2[] topCenters, Vector2[] bottomCenters, Vector2 choiceSize,
            Vector2[] planCenters, Vector2[] riverCenters, Rect captionRect,
            Rect outcomeRect, Rect childRect, bool fits)
        {
            CanvasScale = canvasScale; SafeCanvasRect = safeCanvasRect; Target = target; Gap = gap;
            TopCenters = topCenters; BottomCenters = bottomCenters; PlanSize = choiceSize;
            RiverSize = choiceSize; PlanCenters = planCenters; RiverCenters = riverCenters;
            CaptionRect = captionRect; BenefitRect = outcomeRect; NeedRect = outcomeRect;
            ChildRect = childRect; Fits = fits;
        }

        public float CanvasScale { get; }
        public Rect SafeCanvasRect { get; }
        public float Target { get; }
        public float Gap { get; }
        public Vector2[] TopCenters { get; }
        public Vector2[] BottomCenters { get; }
        public Vector2 PlanSize { get; }
        public Vector2 RiverSize { get; }
        public Vector2[] PlanCenters { get; }
        public Vector2[] RiverCenters { get; }
        public Rect CaptionRect { get; }
        public Rect BenefitRect { get; }
        public Rect NeedRect { get; }
        public Rect ChildRect { get; }
        public bool Fits { get; }

        public static Stage3LayoutMetrics Calculate(float screenWidth, float screenHeight,
            float safeX, float safeY, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
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
            var gap = Mathf.Max(18f, ResponsiveLayoutProfile.CalculateRequiredGap(target,
                ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(gapPixels, scale)));
            var top = Centers(4, safe.width, safe.height - target * 0.5f, target + gap);
            var bottom = Centers(4, safe.width, target * 0.5f, target + gap);
            var bodyBottom = target + gap;
            var bodyTop = safe.height - target - gap;
            var bodyHeight = Mathf.Max(0f, bodyTop - bodyBottom);
            var captionHeight = Mathf.Min(target, bodyHeight * 0.18f);
            var caption = new Rect(safe.width * 0.15f, bodyTop - captionHeight,
                safe.width * 0.70f, captionHeight);
            var contentTop = caption.yMin - gap;
            var availableHeight = Mathf.Max(0f, contentTop - bodyBottom);
            var choiceSide = Mathf.Min(230f, Mathf.Min(availableHeight * 0.46f,
                (safe.width - 4f * gap) / 3f));
            choiceSide = Mathf.Max(target, choiceSide);
            var centers = Centers(3, safe.width, bodyBottom + availableHeight * 0.52f, choiceSide + gap);
            var childWidth = Mathf.Min(choiceSide * 0.72f, safe.width * 0.16f);
            var child = new Rect(gap, bodyBottom, childWidth, availableHeight);
            var outcomeSide = Mathf.Min(availableHeight, safe.width * 0.42f);
            var outcome = new Rect(safe.width * 0.5f - outcomeSide * 0.5f,
                bodyBottom, outcomeSide, outcomeSide);
            var fits = 4f * target + 3f * gap <= safe.width
                && 2f * target + 2f * gap < safe.height
                && availableHeight > target
                && 3f * choiceSide + 2f * gap <= safe.width
                && choiceSide >= target && caption.width > 0 && caption.height > 0
                && child.width > 0 && child.height > 0
                && outcome.width > 0 && outcome.height <= availableHeight + 0.001f;
            return new Stage3LayoutMetrics(scale, safe, target, gap, top, bottom,
                Vector2.one * choiceSide, centers, centers, caption, outcome, child, fits);
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
