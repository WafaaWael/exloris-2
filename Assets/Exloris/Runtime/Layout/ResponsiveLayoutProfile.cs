using System;
using UnityEngine;

namespace Exloris.Runtime.Layout
{
    public static class ResponsiveLayoutProfile
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float MinimumTargetMillimetres = 12f;
        public const float MinimumGapMillimetres = 5f;
        public const float PreferredGapMillimetres = 8f;

        public static float CalculateCanvasScale(float screenWidth, float screenHeight)
        {
            if (screenWidth <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            }

            if (screenHeight <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(screenHeight));
            }

            return Mathf.Sqrt(
                (screenWidth / ReferenceWidth) *
                (screenHeight / ReferenceHeight));
        }

        public static int MillimetresToPhysicalPixels(float millimetres, float ppi)
        {
            if (millimetres <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(millimetres));
            }

            if (ppi <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(ppi));
            }

            return Mathf.CeilToInt(millimetres * ppi / 25.4f);
        }

        public static int CalculateTargetPhysicalPixels(float ppi, int platformMinimumPixels)
        {
            if (platformMinimumPixels < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(platformMinimumPixels));
            }

            return Mathf.Max(
                MillimetresToPhysicalPixels(MinimumTargetMillimetres, ppi),
                platformMinimumPixels);
        }

        public static int PhysicalPixelsToCanvasCeiling(int physicalPixels, float canvasScale)
        {
            if (physicalPixels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalPixels));
            }

            if (canvasScale <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(canvasScale));
            }

            return Mathf.CeilToInt(physicalPixels / canvasScale);
        }

        public static float CalculateRequiredGap(
            float targetAxisSize,
            float fiveMillimetreCanvasFloor)
        {
            if (targetAxisSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(targetAxisSize));
            }

            if (fiveMillimetreCanvasFloor < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fiveMillimetreCanvasFloor));
            }

            return Mathf.Max(fiveMillimetreCanvasFloor, targetAxisSize * 0.25f);
        }
    }
}
