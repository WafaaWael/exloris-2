using System;
using System.Collections.Generic;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Lifecycle;

namespace Exloris.Runtime.Media
{
    public interface IMediaPlaybackClock
    {
        double TimeSeconds { get; }
        void Pause();
        void Resume();
    }

    public interface ISynchronizedMediaTrack
    {
        void Pause();
        void Seek(double timeSeconds);
        void Resume(float gainRampSeconds);
    }

    public interface IPlaybackTokenTrack
    {
        void Bind(Guid tokenId);
    }

    public sealed class MediaPlaybackToken
    {
        public MediaPlaybackToken(Guid id, string semanticId)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Media playback ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(semanticId))
            {
                throw new ArgumentException("Media semantic ID is required.", nameof(semanticId));
            }

            Id = id;
            SemanticId = semanticId;
        }

        public Guid Id { get; }
        public string SemanticId { get; }
    }

    public sealed class MediaPlaybackCoordinator : IAppLifecycleParticipant
    {
        private readonly IMediaPlaybackClock _clock;
        private readonly IReadOnlyList<ISynchronizedMediaTrack> _tracks;
        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issued = new HashSet<Guid>();
        private MediaPlaybackToken _current;
        private string _pausedSemanticId;
        private double _pausedPosition;
        private bool _resumePending;

        public MediaPlaybackCoordinator(
            IMediaPlaybackClock clock,
            IReadOnlyList<ISynchronizedMediaTrack> tracks,
            Func<Guid> idFactory = null)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _tracks = tracks ?? throw new ArgumentNullException(nameof(tracks));
            _idFactory = idFactory ?? Guid.NewGuid;
        }

        public MediaPlaybackToken Current => _current;
        public bool ResumePending => _resumePending;

        public MediaPlaybackToken Start(string semanticId)
        {
            if (_current != null || _resumePending)
            {
                throw new InvalidOperationException("Active media must complete or be explicitly stopped before new playback starts.");
            }

            _resumePending = false;
            _pausedSemanticId = null;
            _current = CreateToken(semanticId);
            BindTracks(_current.Id);
            return _current;
        }

        public bool Complete(Guid tokenId)
        {
            if (_current == null || _current.Id != tokenId)
            {
                return false;
            }

            _current = null;
            return true;
        }

        public void PauseAndDuckImmediately()
        {
            if (_current == null)
            {
                return;
            }

            _pausedSemanticId = _current.SemanticId;
            _pausedPosition = _clock.TimeSeconds;
            _current = null;
            _resumePending = true;
            _clock.Pause();
            foreach (var track in _tracks)
            {
                track?.Pause();
            }
        }

        public void HandleOutputRouteChanged()
        {
            PauseAndDuckImmediately();
        }

        public MediaPlaybackToken ResumeAfterMappedUserAction()
        {
            if (!_resumePending)
            {
                return _current;
            }

            var replacement = CreateToken(_pausedSemanticId);
            BindTracks(replacement.Id);
            foreach (var track in _tracks)
            {
                track?.Seek(_pausedPosition);
            }

            var ramp = AudioPolicy.CalculateGainRiseSeconds(6f);
            foreach (var track in _tracks)
            {
                track?.Resume(ramp);
            }

            _clock.Resume();
            _current = replacement;
            _pausedSemanticId = null;
            _resumePending = false;
            return replacement;
        }

        void IAppLifecycleParticipant.ResumeAfterMappedUserAction()
        {
            ResumeAfterMappedUserAction();
        }

        private MediaPlaybackToken CreateToken(string semanticId)
        {
            var id = _idFactory();
            if (id == Guid.Empty || !_issued.Add(id))
            {
                throw new InvalidOperationException("Media playback IDs must be non-empty and unique.");
            }

            return new MediaPlaybackToken(id, semanticId);
        }

        private void BindTracks(Guid tokenId)
        {
            foreach (var track in _tracks)
            {
                if (track is IPlaybackTokenTrack tokenTrack)
                {
                    tokenTrack.Bind(tokenId);
                }
            }
        }
    }
}
