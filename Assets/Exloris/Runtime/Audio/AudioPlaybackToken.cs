using System;

namespace Exloris.Runtime.Audio
{
    public enum AudioBus
    {
        Voice,
        Music,
        Ambience,
        Sfx
    }

    public sealed class AudioPlaybackToken
    {
        public AudioPlaybackToken(
            Guid id,
            string semanticId,
            AudioBus bus,
            double startedAtSeconds)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Playback token ID cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(semanticId))
            {
                throw new ArgumentException("Semantic audio ID is required.", nameof(semanticId));
            }

            Id = id;
            SemanticId = semanticId;
            Bus = bus;
            StartedAtSeconds = startedAtSeconds;
        }

        public Guid Id { get; }
        public string SemanticId { get; }
        public AudioBus Bus { get; }
        public double StartedAtSeconds { get; }
    }
}
