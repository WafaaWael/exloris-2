using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.UI
{
    public enum SemanticIconKind
    {
        Loading,
        Retry,
        Home
    }

    [DisallowMultipleComponent]
    public sealed class SemanticIconGraphic : MaskableGraphic
    {
        [SerializeField] private SemanticIconKind iconKind;

        public SemanticIconKind IconKind
        {
            get => iconKind;
            set
            {
                iconKind = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            switch (iconKind)
            {
                case SemanticIconKind.Home:
                    DrawHome(vertexHelper);
                    break;
                case SemanticIconKind.Retry:
                    DrawRetry(vertexHelper);
                    break;
                default:
                    DrawLoading(vertexHelper);
                    break;
            }
        }

        private void DrawHome(VertexHelper helper)
        {
            var rect = GetPixelAdjustedRect();
            var unit = Mathf.Min(rect.width, rect.height);
            var center = rect.center;
            AddQuad(
                helper,
                new Rect(center.x - unit * 0.30f, center.y - unit * 0.30f, unit * 0.60f, unit * 0.48f),
                color);
            AddTriangle(
                helper,
                new Vector2(center.x - unit * 0.40f, center.y + unit * 0.10f),
                new Vector2(center.x, center.y + unit * 0.42f),
                new Vector2(center.x + unit * 0.40f, center.y + unit * 0.10f),
                color);
            AddQuad(
                helper,
                new Rect(center.x - unit * 0.075f, center.y - unit * 0.30f, unit * 0.15f, unit * 0.24f),
                new Color32(51, 43, 36, 255));
        }

        private void DrawRetry(VertexHelper helper)
        {
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * 0.34f;
            var thickness = radius * 0.28f;
            var center = rect.center;
            const int segments = 12;
            for (var i = 1; i < segments; i++)
            {
                var start = Mathf.Lerp(25f, 315f, (i - 1f) / segments) * Mathf.Deg2Rad;
                var end = Mathf.Lerp(25f, 315f, i / (float)segments) * Mathf.Deg2Rad;
                AddRingSegment(helper, center, radius, thickness, start, end, color);
            }

            var arrow = center + new Vector2(Mathf.Cos(25f * Mathf.Deg2Rad), Mathf.Sin(25f * Mathf.Deg2Rad)) * radius;
            AddTriangle(
                helper,
                arrow + new Vector2(-thickness * 1.6f, thickness * 1.5f),
                arrow + new Vector2(thickness * 1.3f, thickness * 1.1f),
                arrow + new Vector2(thickness * 0.5f, -thickness * 1.8f),
                color);
        }

        private void DrawLoading(VertexHelper helper)
        {
            var rect = GetPixelAdjustedRect();
            var radius = Mathf.Min(rect.width, rect.height) * 0.34f;
            var thickness = radius * 0.30f;
            var center = rect.center;
            const int segments = 8;
            for (var i = 0; i < segments; i++)
            {
                var start = (i * 45f + 5f) * Mathf.Deg2Rad;
                var end = (i * 45f + 34f) * Mathf.Deg2Rad;
                var segmentColor = color;
                segmentColor.a *= Mathf.Lerp(0.30f, 1f, (i + 1f) / segments);
                AddRingSegment(helper, center, radius, thickness, start, end, segmentColor);
            }
        }

        private static void AddRingSegment(
            VertexHelper helper,
            Vector2 center,
            float outerRadius,
            float thickness,
            float start,
            float end,
            Color segmentColor)
        {
            var innerRadius = outerRadius - thickness;
            var index = helper.currentVertCount;
            helper.AddVert(center + Direction(start) * innerRadius, segmentColor, Vector2.zero);
            helper.AddVert(center + Direction(start) * outerRadius, segmentColor, Vector2.zero);
            helper.AddVert(center + Direction(end) * outerRadius, segmentColor, Vector2.zero);
            helper.AddVert(center + Direction(end) * innerRadius, segmentColor, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
        }

        private static Vector2 Direction(float radians)
        {
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private static void AddQuad(VertexHelper helper, Rect rect, Color quadColor)
        {
            var index = helper.currentVertCount;
            helper.AddVert(new Vector2(rect.xMin, rect.yMin), quadColor, Vector2.zero);
            helper.AddVert(new Vector2(rect.xMin, rect.yMax), quadColor, Vector2.zero);
            helper.AddVert(new Vector2(rect.xMax, rect.yMax), quadColor, Vector2.zero);
            helper.AddVert(new Vector2(rect.xMax, rect.yMin), quadColor, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
            helper.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddTriangle(
            VertexHelper helper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Color triangleColor)
        {
            var index = helper.currentVertCount;
            helper.AddVert(first, triangleColor, Vector2.zero);
            helper.AddVert(second, triangleColor, Vector2.zero);
            helper.AddVert(third, triangleColor, Vector2.zero);
            helper.AddTriangle(index, index + 1, index + 2);
        }
    }
}
