using System;

namespace Exloris.Runtime.Overview
{
    public sealed class JourneyOverviewRevealPresenter : IJourneyOverviewView
    {
        public const float FadeSeconds = 0.150f;
        public const float MeasureMaskPixels = 256f;
        public const float ReadMaskPixels = 622f;
        public const float HelpMaskPixels = 1024f;

        private readonly JourneyOverviewContent _content;
        private readonly IJourneyOverviewRevealSink _sink;
        private Guid _currentTokenId;
        private bool _hasFrame;

        public JourneyOverviewRevealPresenter(
            JourneyOverviewContent content,
            IJourneyOverviewRevealSink sink)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public Guid CurrentTokenId => _currentTokenId;

        public bool TryPresent(JourneyOverviewSnapshot snapshot, bool reducedMotion)
        {
            if (snapshot.TokenId == Guid.Empty
                || snapshot.ElapsedSeconds < 0d
                || double.IsNaN(snapshot.ElapsedSeconds)
                || double.IsInfinity(snapshot.ElapsedSeconds))
            {
                return false;
            }

            if (_currentTokenId == Guid.Empty || snapshot.TokenId != _currentTokenId)
            {
                if (snapshot.ElapsedSeconds > 0.000000001d)
                {
                    return false;
                }

                _currentTokenId = snapshot.TokenId;
            }

            var beats = reducedMotion ? 3 : Math.Max(1, Math.Min(3, snapshot.RevealedBeatCount));
            var activeMotion = !reducedMotion
                && snapshot.State != JourneyOverviewRevealState.Paused
                && snapshot.State != JourneyOverviewRevealState.Leaving
                && snapshot.State != JourneyOverviewRevealState.Navigating
                && snapshot.State != JourneyOverviewRevealState.Recovery
                && snapshot.ElapsedSeconds < JourneyOverviewController.CompletionSeconds;
            var frame = new JourneyOverviewRevealFrame(
                snapshot.TokenId,
                beats,
                true,
                beats >= 2,
                beats >= 3,
                beats >= 3 ? HelpMaskPixels : beats >= 2 ? ReadMaskPixels : MeasureMaskPixels,
                activeMotion,
                activeMotion,
                _content.MeasureIcon,
                _content.ReadIcon,
                _content.HelpIcon,
                _content.TimelineConnector,
                _content.CurrentTimelineState,
                _content.LockedTimelineState);
            _sink.PresentReveal(frame);
            _hasFrame = true;
            return true;
        }

        public void Present(JourneyOverviewSnapshot snapshot, bool reducedMotion)
        {
            TryPresent(snapshot, reducedMotion);
        }

        public void SetInputReady(bool ready)
        {
            if (_hasFrame)
            {
                _sink.SetInputReady(ready);
            }
        }
    }
}
