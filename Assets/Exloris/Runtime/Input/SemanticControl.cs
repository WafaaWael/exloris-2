using System;
using UnityEngine;

namespace Exloris.Runtime.Input
{
    public sealed class SemanticControl
    {
        private readonly Func<Rect> _boundsProvider;

        public SemanticControl(string id, Rect screenBounds, bool supportsDrag = false)
            : this(id, () => screenBounds, supportsDrag)
        {
        }

        public SemanticControl(
            string id,
            Func<Rect> screenBoundsProvider,
            bool supportsDrag = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Semantic control ID is required.", nameof(id));
            }

            _boundsProvider = screenBoundsProvider ??
                throw new ArgumentNullException(nameof(screenBoundsProvider));
            Id = id;
            SupportsDrag = supportsDrag;
            Enabled = true;
        }

        public string Id { get; }
        public bool SupportsDrag { get; }
        public bool Enabled { get; set; }

        public Rect ScreenBounds
        {
            get
            {
                var bounds = _boundsProvider();
                if (bounds.width <= 0f || bounds.height <= 0f)
                {
                    throw new InvalidOperationException(
                        $"Semantic control '{Id}' has invalid screen bounds.");
                }

                return bounds;
            }
        }
    }

    public enum SemanticInputKind
    {
        Tap,
        Drag
    }

    public readonly struct SemanticInputCommand
    {
        public SemanticInputCommand(
            string controlId,
            SemanticInputKind kind,
            Vector2 startPosition,
            Vector2 endPosition,
            double inputTime,
            double durationSeconds = 0d)
        {
            ControlId = controlId;
            Kind = kind;
            StartPosition = startPosition;
            EndPosition = endPosition;
            InputTime = inputTime;
            DurationSeconds = durationSeconds;
        }

        public string ControlId { get; }
        public SemanticInputKind Kind { get; }
        public Vector2 StartPosition { get; }
        public Vector2 EndPosition { get; }
        public double InputTime { get; }
        public double DurationSeconds { get; }
    }
}
