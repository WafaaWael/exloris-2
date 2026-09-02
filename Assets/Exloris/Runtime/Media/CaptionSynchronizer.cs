using System;
using Exloris.Runtime.Audio;

namespace Exloris.Runtime.Media
{
    public sealed class CaptionSynchronizer : ISynchronizedMediaTrack, IPlaybackTokenTrack
    {
        private Guid _tokenId;

        public double PositionSeconds { get; private set; }
        public double LastAlignmentErrorMilliseconds { get; private set; }
        public bool IsPaused { get; private set; }

        public void Bind(Guid tokenId)
        {
            if (tokenId == Guid.Empty)
            {
                throw new ArgumentException("Caption token cannot be empty.", nameof(tokenId));
            }

            _tokenId = tokenId;
        }

        public bool Align(Guid tokenId, double referenceSeconds, double captionSeconds)
        {
            if (tokenId == Guid.Empty || tokenId != _tokenId)
            {
                return false;
            }

            LastAlignmentErrorMilliseconds =
                Math.Abs(referenceSeconds - captionSeconds) * 1000d;
            if (LastAlignmentErrorMilliseconds > AudioPolicy.CaptionToleranceSeconds * 1000d)
            {
                return false;
            }

            PositionSeconds = referenceSeconds;
            return true;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Seek(double timeSeconds)
        {
            PositionSeconds = timeSeconds;
        }

        public void Resume(float gainRampSeconds)
        {
            IsPaused = false;
        }
    }
}
