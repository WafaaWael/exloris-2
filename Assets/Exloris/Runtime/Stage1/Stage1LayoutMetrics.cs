using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Stage1
{
    public readonly struct Stage1LayoutMetrics
    {
        private Stage1LayoutMetrics(
            float canvasScale,
            Rect safeCanvasRect,
            float target,
            float gap,
            Vector2[] topCenters,
            Vector2[] bottomCenters,
            Rect bodyRect,
            Rect dragRect,
            Rect captionRect,
            Rect outcomeRect,
            Rect childRect,
            Vector2 markerTrayCenter,
            float minimumMarkerTravel,
            bool fits)
        {
            CanvasScale = canvasScale;
            SafeCanvasRect = safeCanvasRect;
            Target = target;
            Gap = gap;
            TopCenters = topCenters;
            BottomCenters = bottomCenters;
            BodyRect = bodyRect;
            DragRect = dragRect;
            CaptionRect = captionRect;
            OutcomeRect = outcomeRect;
            ChildRect = childRect;
            MarkerTrayCenter = markerTrayCenter;
            MinimumMarkerTravel = minimumMarkerTravel;
            Fits = fits;
        }

        public float CanvasScale { get; }
        public Rect SafeCanvasRect { get; }
        public float Target { get; }
        public float Gap { get; }
        public Vector2[] TopCenters { get; }
        public Vector2[] BottomCenters { get; }
        public Rect BodyRect { get; }
        public Rect DragRect { get; }
        public Rect CaptionRect { get; }
        public Rect OutcomeRect { get; }
        public Rect ChildRect { get; }
        public Vector2 MarkerTrayCenter { get; }
        public float MinimumMarkerTravel { get; }
        public bool Fits { get; }

        public static Stage1LayoutMetrics Calculate(
            float screenWidth,
            float screenHeight,
            float safeX,
            float safeY,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            if (screenWidth <= 0 || screenHeight <= 0 || safeX < 0 || safeY < 0
                || safeWidth <= 0 || safeHeight <= 0 || ppi <= 0 || platformMinimumPixels < 0)
                throw new ArgumentOutOfRangeException();

            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var safe = new Rect(safeX / scale, safeY / scale, safeWidth / scale, safeHeight / scale);
            var physicalTarget = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels);
            var target = Mathf.Max(60f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(physicalTarget, scale));
            var fiveMillimetres = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var gap = Mathf.Max(18f, ResponsiveLayoutProfile.CalculateRequiredGap(
                target, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(fiveMillimetres, scale)));
            var minimumTravelPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(3f, ppi);
            var minimumTravel = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(minimumTravelPixels, scale);
            var top = Centers(4, safe.width, safe.height - target * 0.5f, target + gap);
            var bottom = Centers(3, safe.width, target * 0.5f, target + gap);
            var bodyBottom = target + gap;
            var bodyTop = safe.height - target - gap;
            var body = new Rect(gap, bodyBottom, Mathf.Max(0, safe.width - 2f * gap), Mathf.Max(0, bodyTop - bodyBottom));
            var captionHeight = Mathf.Min(target, body.height * 0.22f);
            var caption = new Rect(safe.width * 0.16f, body.yMax - captionHeight, safe.width * 0.68f, captionHeight);
            var workingTop = caption.yMin - gap;
            var dragWidth = Mathf.Max(target * 1.2f, Mathf.Min(320f, body.width * 0.26f));
            var dragHeight = Mathf.Max(0f, workingTop - body.yMin);
            var dragX = body.x + body.width * 0.43f;
            var drag = new Rect(dragX, body.yMin, dragWidth, dragHeight);
            var childWidth = Mathf.Min(body.width * 0.24f, target * 2.8f);
            var child = new Rect(body.x, body.yMin, childWidth, dragHeight);
            var outcomeX = drag.xMax + gap;
            var outcome = new Rect(outcomeX, body.yMin, Mathf.Max(0, body.xMax - outcomeX), dragHeight);
            var trayX = Mathf.Max(body.x + target * 0.5f, drag.xMin - gap - target * 0.5f);
            var tray = new Vector2(trayX, body.yMin + target * 0.5f);
            var usableMarkerHeight = drag.height - target;
            var fits = 4f * target + 3f * gap <= safe.width
                && 2f * target + 2f * gap < safe.height
                && body.width > 0 && body.height > 0
                && caption.width > 0 && caption.height > 0
                && drag.width >= target && usableMarkerHeight >= 4f * minimumTravel
                && child.width > 0 && child.height > 0
                && outcome.width > 0 && outcome.height > 0
                && trayX + target * 0.5f <= drag.xMin - gap * 0.5f;
            return new Stage1LayoutMetrics(scale, safe, target, gap, top, bottom, body, drag, caption, outcome, child, tray, minimumTravel, fits);
        }

        public Vector2 MarkerCenter(float normalizedPosition)
        {
            if (float.IsNaN(normalizedPosition) || float.IsInfinity(normalizedPosition) || normalizedPosition < 0f || normalizedPosition > 1f)
                throw new ArgumentOutOfRangeException(nameof(normalizedPosition));
            var half = Target * 0.5f;
            return new Vector2(DragRect.center.x, Mathf.Lerp(DragRect.yMin + half, DragRect.yMax - half, normalizedPosition));
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
