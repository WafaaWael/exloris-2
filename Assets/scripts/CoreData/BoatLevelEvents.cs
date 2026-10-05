using BoatGame.CoreData.Messaging;
using UnityEngine;

namespace BoatGame.CoreData
{
    public enum LevelState
    {
        Intro,
        Tutorial,
        Playing,
        Paused,
        Arriving,
        Arrived,
        Complete,
        Lesson,
    }

    public enum SfxId
    {
        Bump,
        Coin,
        StarLost,
        Oar,
        LaneMove,
        Win,
        UiTap,
        Correct,
        Wrong,
        WaterRise,
    }

    public readonly struct LevelStateChangedEvent : IEvent
    {
        public readonly LevelState Previous;
        public readonly LevelState Current;

        public LevelStateChangedEvent(LevelState previous, LevelState current)
        {
            Previous = previous;
            Current = current;
        }
    }

    public readonly struct BoatHitObstacleEvent : IEvent
    {
        public readonly int StarsLeft;
        public readonly bool DidLoseStar;
        public readonly Vector3 ContactPoint;

        public BoatHitObstacleEvent(int starsLeft, bool didLoseStar, Vector3 contactPoint)
        {
            StarsLeft = starsLeft;
            DidLoseStar = didLoseStar;
            ContactPoint = contactPoint;
        }
    }

    public readonly struct CoinCollectedEvent : IEvent
    {
        public readonly int TotalCoins;
        public readonly Vector3 Position;

        public CoinCollectedEvent(int totalCoins, Vector3 position)
        {
            TotalCoins = totalCoins;
            Position = position;
        }
    }

    public readonly struct BoatBlockedEvent : IEvent
    {
        public readonly bool IsBlocked;

        public BoatBlockedEvent(bool isBlocked)
        {
            IsBlocked = isBlocked;
        }
    }

    public readonly struct LevelProgressEvent : IEvent
    {
        public readonly float Progress01;

        public LevelProgressEvent(float progress01)
        {
            Progress01 = progress01;
        }
    }

    public readonly struct PlaySfxEvent : IEvent
    {
        public readonly SfxId Id;

        public PlaySfxEvent(SfxId id)
        {
            Id = id;
        }
    }

    /// <summary>Plays a voice-over clip, stopping any line already playing. A null clip just stops the voice.</summary>
    public readonly struct PlayVoiceEvent : IEvent
    {
        public readonly AudioClip Clip;

        public PlayVoiceEvent(AudioClip clip)
        {
            Clip = clip;
        }
    }

    /// <summary>Raised when a voice line starts and when it ends, so the speaker can switch to a talking pose and back.</summary>
    public readonly struct VoiceActivityEvent : IEvent
    {
        public readonly bool IsSpeaking;

        public VoiceActivityEvent(bool isSpeaking)
        {
            IsSpeaking = isSpeaking;
        }
    }
}
