using UnityEngine;

namespace Exloris.Runtime.Audio
{
    public sealed class DbGainRamp
    {
        private float _startDb;
        private float _elapsedSeconds;

        public float CurrentDb { get; private set; }
        public float TargetDb { get; private set; }
        public float DurationSeconds { get; private set; }
        public bool Active { get; private set; }

        public void SetImmediate(float valueDb)
        {
            CurrentDb = valueDb;
            TargetDb = valueDb;
            DurationSeconds = 0f;
            _elapsedSeconds = 0f;
            Active = false;
        }

        public void Start(float targetDb, float durationSeconds)
        {
            _startDb = CurrentDb;
            TargetDb = targetDb;
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            _elapsedSeconds = 0f;
            Active = DurationSeconds > 0f && !Mathf.Approximately(CurrentDb, TargetDb);
            if (!Active) SetImmediate(targetDb);
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!Active || unscaledDeltaTime <= 0f) return;
            _elapsedSeconds = Mathf.Min(DurationSeconds, _elapsedSeconds + unscaledDeltaTime);
            CurrentDb = Mathf.Lerp(_startDb, TargetDb, _elapsedSeconds / DurationSeconds);
            if (_elapsedSeconds >= DurationSeconds)
            {
                CurrentDb = TargetDb;
                Active = false;
            }
        }
    }
}
