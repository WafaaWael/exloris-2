using System;
using UnityEngine;

namespace Exloris.Runtime.Layout
{
    public enum TouchTargetFailure
    {
        None,
        BelowMinimumSize,
        VisibleArtOutsideTarget,
        VisibleArtNotCentered,
        Overlap,
        InsufficientSpacing
    }

    public enum TargetPresentationMode
    {
        Review,
        Final
    }

    public readonly struct TouchTargetValidationResult
    {
        public TouchTargetValidationResult(TouchTargetFailure failure, string message)
        {
            Failure = failure;
            Message = message;
        }

        public TouchTargetFailure Failure { get; }
        public string Message { get; }
        public bool IsValid => Failure == TouchTargetFailure.None;
    }

    public static class TouchTargetValidator
    {
        private const float GeometryTolerance = 0.5f;

        public static TouchTargetValidationResult Validate(
            Rect target,
            Rect visibleArt,
            Rect[] otherTargets,
            float minimumTargetCanvasUnits,
            float minimumGapCanvasUnits)
        {
            ValidatePositiveRect(target, nameof(target));
            ValidatePositiveRect(visibleArt, nameof(visibleArt));
            if (minimumTargetCanvasUnits <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumTargetCanvasUnits));
            }

            if (minimumGapCanvasUnits < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minimumGapCanvasUnits));
            }

            if (target.width + GeometryTolerance < minimumTargetCanvasUnits ||
                target.height + GeometryTolerance < minimumTargetCanvasUnits)
            {
                return Failure(
                    TouchTargetFailure.BelowMinimumSize,
                    "Touch target is below the controlling physical or platform floor.");
            }

            if (!ContainsRectInclusive(target, visibleArt))
            {
                return Failure(
                    TouchTargetFailure.VisibleArtOutsideTarget,
                    "Visible interactive art must be fully contained by its intended target.");
            }

            if (Vector2.Distance(target.center, visibleArt.center) > GeometryTolerance)
            {
                return Failure(
                    TouchTargetFailure.VisibleArtNotCentered,
                    "Visible interactive art must be centered in its intended target.");
            }

            if (otherTargets != null)
            {
                foreach (var other in otherTargets)
                {
                    ValidatePositiveRect(other, nameof(otherTargets));
                    if (OverlapsWithPositiveArea(target, other))
                    {
                        return Failure(
                            TouchTargetFailure.Overlap,
                            "Active touch targets cannot overlap.");
                    }

                    if (!HasRequiredSpacing(target, other, minimumGapCanvasUnits))
                    {
                        return Failure(
                            TouchTargetFailure.InsufficientSpacing,
                            "Touch targets do not meet the five-millimetre or quarter-size gap.");
                    }
                }
            }

            return new TouchTargetValidationResult(TouchTargetFailure.None, "PASS");
        }

        public static bool ContainsInclusive(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin - GeometryTolerance &&
                   point.x <= rect.xMax + GeometryTolerance &&
                   point.y >= rect.yMin - GeometryTolerance &&
                   point.y <= rect.yMax + GeometryTolerance;
        }

        public static bool CanRenderDiagnostic(TargetPresentationMode mode)
        {
            return mode == TargetPresentationMode.Review;
        }

        private static bool ContainsRectInclusive(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - GeometryTolerance &&
                   inner.yMin >= outer.yMin - GeometryTolerance &&
                   inner.xMax <= outer.xMax + GeometryTolerance &&
                   inner.yMax <= outer.yMax + GeometryTolerance;
        }

        private static bool OverlapsWithPositiveArea(Rect first, Rect second)
        {
            return first.xMin < second.xMax &&
                   first.xMax > second.xMin &&
                   first.yMin < second.yMax &&
                   first.yMax > second.yMin;
        }

        private static bool HasRequiredSpacing(Rect first, Rect second, float gapFloor)
        {
            var horizontalGap = Mathf.Max(
                second.xMin - first.xMax,
                first.xMin - second.xMax);
            var verticalGap = Mathf.Max(
                second.yMin - first.yMax,
                first.yMin - second.yMax);
            var overlapsVertically = first.yMin < second.yMax && first.yMax > second.yMin;
            var overlapsHorizontally = first.xMin < second.xMax && first.xMax > second.xMin;

            if (overlapsVertically)
            {
                var required = Mathf.Max(
                    gapFloor,
                    0.25f * Mathf.Max(first.width, second.width));
                return horizontalGap + GeometryTolerance >= required;
            }

            if (overlapsHorizontally)
            {
                var required = Mathf.Max(
                    gapFloor,
                    0.25f * Mathf.Max(first.height, second.height));
                return verticalGap + GeometryTolerance >= required;
            }

            var nearestDistance = Mathf.Sqrt(
                horizontalGap * horizontalGap + verticalGap * verticalGap);
            var diagonalRequired = Mathf.Max(
                gapFloor,
                0.25f * Mathf.Max(
                    Mathf.Min(first.width, first.height),
                    Mathf.Min(second.width, second.height)));
            return nearestDistance + GeometryTolerance >= diagonalRequired;
        }

        private static TouchTargetValidationResult Failure(
            TouchTargetFailure failure,
            string message)
        {
            return new TouchTargetValidationResult(failure, message);
        }

        private static void ValidatePositiveRect(Rect rect, string name)
        {
            if (rect.width <= 0f || rect.height <= 0f)
            {
                throw new ArgumentOutOfRangeException(name, "Rectangle must have positive size.");
            }
        }
    }
}
