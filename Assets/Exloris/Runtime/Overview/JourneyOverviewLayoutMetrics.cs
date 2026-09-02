using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Overview
{
    public readonly struct JourneyOverviewLayoutMetrics
    {
        private JourneyOverviewLayoutMetrics(
            float target,
            float gap,
            Vector2 cardSize,
            Vector2[] cardCenters,
            float cardGap,
            float captionHeight,
            float captionWidth,
            float captionGap,
            float bodyBottom,
            float bodyTop)
        {
            Target = target;
            Gap = gap;
            CardSize = cardSize;
            CardCenters = cardCenters;
            CardGap = cardGap;
            CaptionHeight = captionHeight;
            CaptionWidth = captionWidth;
            CaptionGap = captionGap;
            BodyBottom = bodyBottom;
            BodyTop = bodyTop;
        }

        public float Target { get; }
        public float Gap { get; }
        public Vector2 CardSize { get; }
        public Vector2[] CardCenters { get; }
        public float CardGap { get; }
        public float CaptionHeight { get; }
        public float CaptionWidth { get; }
        public float CaptionGap { get; }
        public float BodyBottom { get; }
        public float BodyTop { get; }

        public static JourneyOverviewLayoutMetrics Calculate(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            if (screenWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(screenWidth));
            if (screenHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(screenHeight));
            if (safeWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(safeWidth));
            if (safeHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(safeHeight));

            var aspect = safeWidth / safeHeight;
            var fourDistance = Mathf.Abs(aspect - 4f / 3f);
            var sixteenDistance = Mathf.Abs(aspect - 16f / 9f);
            var wideDistance = Mathf.Abs(aspect - 2.4f);
            float boardTarget;
            float boardGap;
            Vector2 cardSize;
            float centerRatio;
            if (fourDistance <= sixteenDistance && fourDistance <= wideDistance)
            {
                boardTarget = 97f;
                boardGap = 41f;
                cardSize = new Vector2(320f, 420f);
                centerRatio = 476.40f / 1183.08f;
            }
            else if (sixteenDistance <= wideDistance)
            {
                boardTarget = 224f;
                boardGap = 94f;
                cardSize = new Vector2(360f, 224f);
                centerRatio = 435.10f / 1016f;
            }
            else
            {
                boardTarget = 182f;
                boardGap = 77f;
                cardSize = new Vector2(360f, 182f);
                centerRatio = 353.68f / 825.52f;
            }

            var canvasScale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var targetPixels = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(
                ppi, platformMinimumPixels);
            var deviceTarget = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                targetPixels, canvasScale);
            var gapPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(
                ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var deviceGap = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                gapPixels, canvasScale);
            var target = Mathf.Max(boardTarget, deviceTarget);
            var gap = ResponsiveLayoutProfile.CalculateRequiredGap(
                target, Mathf.Max(boardGap, deviceGap));
            var cardGap = Mathf.Max(boardGap, cardSize.x * 0.25f);
            var centerStep = cardSize.x + cardGap;
            var center = new Vector2(safeWidth * 0.5f, safeHeight * centerRatio);
            var cardCenters = new[]
            {
                new Vector2(center.x - centerStep, center.y),
                center,
                new Vector2(center.x + centerStep, center.y),
            };
            var bodyBottom = target + gap;
            var bodyTop = safeHeight - target - gap;
            var bodyHeight = Mathf.Max(0f, bodyTop - bodyBottom);
            var captionHeight = Mathf.Min(0.16f * safeHeight, 0.25f * bodyHeight);
            return new JourneyOverviewLayoutMetrics(
                target,
                gap,
                cardSize,
                cardCenters,
                cardGap,
                captionHeight,
                Mathf.Min(0.72f * safeWidth, 1200f),
                Mathf.Min(gap, 0.05f * safeHeight),
                bodyBottom,
                bodyTop);
        }

        public bool Fits(float safeWidth, float safeHeight)
        {
            var cueWidth = 2f * CaptionHeight;
            var remainingCaptionWidth = CaptionWidth - cueWidth - CaptionGap;
            return CardSize.x * 3f + CardGap * 2f <= safeWidth
                && CardSize.y <= BodyTop - BodyBottom
                && 3f * Target + 2f * Gap <= safeWidth
                && Target <= safeHeight
                && CaptionHeight > 0f
                && remainingCaptionWidth > 0f;
        }
    }
}
