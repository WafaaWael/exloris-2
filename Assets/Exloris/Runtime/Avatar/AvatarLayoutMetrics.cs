using System;
using Exloris.Runtime.Layout;
using UnityEngine;

namespace Exloris.Runtime.Avatar
{
    public readonly struct AvatarLayoutMetrics
    {
        private AvatarLayoutMetrics(
            float target, float gap, Vector2 cardSize, float cardCenterY,
            Vector2 previewSize, float previewCenterY)
        {
            Target = target;
            Gap = gap;
            CardSize = cardSize;
            CardCenterY = cardCenterY;
            PreviewSize = previewSize;
            PreviewCenterY = previewCenterY;
        }

        public float Target { get; }
        public float Gap { get; }
        public Vector2 CardSize { get; }
        public float CardCenterY { get; }
        public Vector2 PreviewSize { get; }
        public float PreviewCenterY { get; }

        public static AvatarLayoutMetrics Calculate(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            if (screenWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(screenWidth));
            if (screenHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(screenHeight));
            if (safeWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(safeWidth));
            if (safeHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(safeHeight));

            var aspect = screenWidth / screenHeight;
            var fourDistance = Mathf.Abs(aspect - 4f / 3f);
            var sixteenDistance = Mathf.Abs(aspect - 16f / 9f);
            var wideDistance = Mathf.Abs(aspect - 2.4f);
            float boardTarget;
            float boardGap;
            Vector2 card;
            Vector2 preview;
            float cardCenterRatio;
            if (fourDistance <= sixteenDistance && fourDistance <= wideDistance)
            {
                boardTarget = 97f;
                boardGap = 41f;
                card = new Vector2(360f, 520f);
                preview = new Vector2(720f, 620f);
                cardCenterRatio = 476.40f / 1183.08f;
            }
            else if (sixteenDistance <= wideDistance)
            {
                boardTarget = 224f;
                boardGap = 94f;
                card = new Vector2(360f, 224f);
                preview = new Vector2(760f, 224f);
                cardCenterRatio = 435.10f / 1016f;
            }
            else
            {
                boardTarget = 182f;
                boardGap = 77f;
                card = new Vector2(360f, 182f);
                preview = new Vector2(880f, 182f);
                cardCenterRatio = 353.68f / 825.52f;
            }

            var canvasScale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var targetPixels = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels);
            var deviceTarget = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(targetPixels, canvasScale);
            var gapPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(
                ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var deviceGap = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(gapPixels, canvasScale);
            var target = Mathf.Max(boardTarget, deviceTarget);
            var gap = ResponsiveLayoutProfile.CalculateRequiredGap(target, Mathf.Max(boardGap, deviceGap));
            return new AvatarLayoutMetrics(
                target,
                gap,
                new Vector2(Mathf.Max(card.x, target), Mathf.Max(card.y, target)),
                safeHeight * cardCenterRatio,
                new Vector2(Mathf.Max(preview.x, target), Mathf.Max(preview.y, target)),
                safeHeight * cardCenterRatio);
        }
    }

    public readonly struct AvatarGeometry
    {
        public AvatarGeometry(
            Rect targetRect, Rect cardRect, Rect artRect, Vector2 pivot, float baseline,
            Vector3 scale, Color lighting, float shadowOpacity, float outlineWidth,
            double motionDurationSeconds)
        {
            TargetRect = targetRect;
            CardRect = cardRect;
            ArtRect = artRect;
            Pivot = pivot;
            Baseline = baseline;
            Scale = scale;
            Lighting = lighting;
            ShadowOpacity = shadowOpacity;
            OutlineWidth = outlineWidth;
            MotionDurationSeconds = motionDurationSeconds;
        }

        public Rect TargetRect { get; }
        public Rect CardRect { get; }
        public Rect ArtRect { get; }
        public Vector2 Pivot { get; }
        public float Baseline { get; }
        public Vector3 Scale { get; }
        public Color Lighting { get; }
        public float ShadowOpacity { get; }
        public float OutlineWidth { get; }
        public double MotionDurationSeconds { get; }
    }
}
