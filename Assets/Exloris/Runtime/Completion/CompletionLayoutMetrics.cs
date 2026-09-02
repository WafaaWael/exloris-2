using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Completion
{
    public readonly struct CompletionLayoutMetrics
    {
        private CompletionLayoutMetrics(
            float target, float gap, float bodyBottom, float bodyTop,
            float contentWidth, float reportViewportHeight, float reportCardHeight,
            float textScale, float pseudoExpansion)
        {
            Target = target;
            Gap = gap;
            BodyBottom = bodyBottom;
            BodyTop = bodyTop;
            ReportContentWidth = contentWidth;
            ReportViewportHeight = reportViewportHeight;
            ReportCardHeight = reportCardHeight;
            TextScale = textScale;
            PseudoExpansion = pseudoExpansion;
        }

        public float Target { get; }
        public float Gap { get; }
        public float BodyBottom { get; }
        public float BodyTop { get; }
        public float ReportContentWidth { get; }
        public float ReportViewportHeight { get; }
        public float ReportCardHeight { get; }
        public float TextScale { get; }
        public float PseudoExpansion { get; }

        public static CompletionLayoutMetrics Calculate(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels, float textScale = 1f, float pseudoExpansion = 1f)
        {
            if (screenWidth <= 0f || screenHeight <= 0f || safeWidth <= 0f || safeHeight <= 0f)
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            if (textScale <= 0f || pseudoExpansion < 1f)
                throw new ArgumentOutOfRangeException(nameof(textScale));

            var canvasScale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var target = Mathf.Max(104f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels), canvasScale));
            var physicalGap = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.MillimetresToPhysicalPixels(
                    ResponsiveLayoutProfile.MinimumGapMillimetres, ppi), canvasScale);
            var gap = ResponsiveLayoutProfile.CalculateRequiredGap(target, Mathf.Max(32f, physicalGap));
            var bodyBottom = target + gap;
            var bodyTop = safeHeight - target - gap;
            var viewport = Mathf.Max(0f, bodyTop - bodyBottom);
            var contentWidth = Mathf.Min(1280f, Mathf.Max(0f, safeWidth - 2f * (gap + target * 0.5f)));
            var cardHeight = Mathf.Max(220f, 220f * textScale * Mathf.Min(pseudoExpansion, 1.5f));
            return new CompletionLayoutMetrics(
                target, gap, bodyBottom, bodyTop, contentWidth, viewport,
                cardHeight, textScale, pseudoExpansion);
        }

        public bool Fits(float safeWidth, float safeHeight)
        {
            return safeWidth > 0f && safeHeight > 0f
                && 2f * Target + Gap <= safeWidth
                && BodyTop > BodyBottom
                && ReportViewportHeight > Target
                && ReportContentWidth >= Mathf.Min(720f, safeWidth * 0.7f)
                && ReportCardHeight > 0f;
        }
    }
}
