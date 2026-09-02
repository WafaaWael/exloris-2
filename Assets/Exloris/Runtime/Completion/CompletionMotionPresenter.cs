using System;

namespace Exloris.Runtime.Completion
{
    public readonly struct CompletionMotionFrame
    {
        public CompletionMotionFrame(
            Guid tokenId, double elapsedSeconds, double opacity,
            float scale, bool spatialMotionEnabled, bool staticEquivalent)
        {
            TokenId = tokenId;
            ElapsedSeconds = elapsedSeconds;
            Opacity = opacity;
            Scale = scale;
            SpatialMotionEnabled = spatialMotionEnabled;
            StaticEquivalent = staticEquivalent;
        }

        public Guid TokenId { get; }
        public double ElapsedSeconds { get; }
        public double Opacity { get; }
        public float Scale { get; }
        public bool SpatialMotionEnabled { get; }
        public bool StaticEquivalent { get; }
    }

    public interface ICompletionMotionSink
    {
        void PresentCompletionMotion(CompletionMotionFrame frame);
    }

    public sealed class CompletionMotionPresenter
    {
        public const double KeyStartSeconds = 0d;
        public const double KeyRevealSeconds = 0.6d;
        public const double KeyHoldSeconds = 1.8d;
        public const double KeySettleSeconds = 2.4d;

        private readonly ICompletionMotionSink _sink;
        private Guid _tokenId;

        public CompletionMotionPresenter(ICompletionMotionSink sink)
        {
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public bool TryPresent(CompletionSnapshot snapshot, bool reducedMotion)
        {
            if (snapshot.TokenId == Guid.Empty
                || double.IsNaN(snapshot.ElapsedSeconds)
                || double.IsInfinity(snapshot.ElapsedSeconds)
                || snapshot.ElapsedSeconds < 0d)
                return false;
            if (_tokenId == Guid.Empty || snapshot.TokenId != _tokenId)
            {
                if (snapshot.ElapsedSeconds > 0.000000001d) return false;
                _tokenId = snapshot.TokenId;
            }

            var elapsed = Math.Min(KeySettleSeconds, snapshot.ElapsedSeconds);
            var opacity = reducedMotion ? 1d : EaseOpacity(elapsed);
            _sink.PresentCompletionMotion(new CompletionMotionFrame(
                snapshot.TokenId, snapshot.ElapsedSeconds, opacity, 1f,
                false, reducedMotion));
            return true;
        }

        private static double EaseOpacity(double elapsed)
        {
            if (elapsed <= KeyStartSeconds) return 0d;
            if (elapsed >= KeySettleSeconds) return 1d;
            var normalized = elapsed / KeySettleSeconds;
            return 1d - Math.Pow(1d - normalized, 2d);
        }
    }
}
