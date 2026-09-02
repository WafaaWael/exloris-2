using System;
using UnityEngine;

namespace Exloris.Runtime.Avatar
{
    public enum AvatarMotionPhase
    {
        IdleBreathing,
        Blink,
        PreviewTurn,
        PreviewWave,
        PreviewSettle,
        StaticReduced,
    }

    public readonly struct AvatarMotionFrame
    {
        public AvatarMotionFrame(
            AvatarChoice choice,
            AvatarMotionPhase phase,
            Sprite sprite,
            double phaseProgress,
            float breathingOffsetRatio)
        {
            Choice = choice;
            Phase = phase;
            Sprite = sprite;
            PhaseProgress = phaseProgress;
            BreathingOffsetRatio = breathingOffsetRatio;
        }

        public AvatarChoice Choice { get; }
        public AvatarMotionPhase Phase { get; }
        public Sprite Sprite { get; }
        public double PhaseProgress { get; }
        public float BreathingOffsetRatio { get; }
        public Vector2 Pivot => new Vector2(0.5f, 0.08f);
        public bool FeetFixed => true;
        public bool Overshoot => false;
        public double IdleLoopSeconds => AvatarMotionPresenter.IdleLoopSeconds;
        public double BreathingCycleSeconds => AvatarMotionPresenter.BreathingCycleSeconds;
        public double PreviewDurationSeconds => AvatarMotionPresenter.PreviewDurationSeconds;
    }

    public sealed class AvatarMotionPresenter
    {
        public const double IdleLoopSeconds = 24.0d;
        public const double BreathingCycleSeconds = 4.8d;
        public const double PreviewTurnSeconds = 2.0d;
        public const double PreviewWaveSeconds = 1.2d;
        public const double PreviewSettleSeconds = 0.8d;
        public const double PreviewDurationSeconds =
            PreviewTurnSeconds + PreviewWaveSeconds + PreviewSettleSeconds;

        private static readonly double[] BlinkStarts = { 3.2d, 8.9d, 14.7d, 21.4d };
        private const double BlinkDurationSeconds = 0.32d;
        private const float MaximumBreathingOffsetRatio = 0.008f;

        private readonly AvatarContent _content;
        private long _currentToken;
        private bool _currentReduced;
        private bool _completed;
        private AvatarChoice _currentChoice;

        public AvatarMotionPresenter(AvatarContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public AvatarMotionFrame CurrentFrame { get; private set; }

        public AvatarMotionFrame EvaluateSelect(
            AvatarChoice choice,
            double elapsedSeconds,
            bool reducedMotion)
        {
            ValidateChoiceAndTime(choice, elapsedSeconds);
            if (reducedMotion)
            {
                return Frame(choice, AvatarMotionPhase.StaticReduced, 0d, 0f);
            }

            var loopTime = elapsedSeconds % IdleLoopSeconds;
            foreach (var blinkStart in BlinkStarts)
            {
                if (loopTime >= blinkStart && loopTime < blinkStart + BlinkDurationSeconds)
                {
                    return Frame(
                        choice,
                        AvatarMotionPhase.Blink,
                        (loopTime - blinkStart) / BlinkDurationSeconds,
                        0f);
                }
            }

            var breathingProgress = (elapsedSeconds % BreathingCycleSeconds) / BreathingCycleSeconds;
            var breathingOffset = MaximumBreathingOffsetRatio *
                (float)((1d - Math.Cos(breathingProgress * Math.PI * 2d)) * 0.5d);
            return Frame(choice, AvatarMotionPhase.IdleBreathing, breathingProgress, breathingOffset);
        }

        public bool BeginPreview(AvatarChoice choice, long previewToken, bool reducedMotion)
        {
            if (!IsValid(choice) || previewToken <= _currentToken)
            {
                return false;
            }

            _currentChoice = choice;
            _currentToken = previewToken;
            _currentReduced = reducedMotion;
            _completed = false;
            CurrentFrame = reducedMotion
                ? Frame(choice, AvatarMotionPhase.StaticReduced, 0d, 0f)
                : Frame(choice, AvatarMotionPhase.PreviewTurn, 0d, 0f);
            return true;
        }

        public bool TryEvaluatePreview(
            long previewToken,
            double elapsedSeconds,
            out AvatarMotionFrame frame)
        {
            if (previewToken != _currentToken || _completed || elapsedSeconds < 0d)
            {
                frame = default;
                return false;
            }

            if (_currentReduced)
            {
                frame = Frame(_currentChoice, AvatarMotionPhase.StaticReduced, 0d, 0f);
            }
            else if (elapsedSeconds < PreviewTurnSeconds)
            {
                frame = Frame(
                    _currentChoice,
                    AvatarMotionPhase.PreviewTurn,
                    elapsedSeconds / PreviewTurnSeconds,
                    0f);
            }
            else if (elapsedSeconds < PreviewTurnSeconds + PreviewWaveSeconds)
            {
                frame = Frame(
                    _currentChoice,
                    AvatarMotionPhase.PreviewWave,
                    (elapsedSeconds - PreviewTurnSeconds) / PreviewWaveSeconds,
                    0f);
            }
            else
            {
                frame = Frame(
                    _currentChoice,
                    AvatarMotionPhase.PreviewSettle,
                    Math.Min(1d, (elapsedSeconds - PreviewTurnSeconds - PreviewWaveSeconds) /
                        PreviewSettleSeconds),
                    0f);
            }

            CurrentFrame = frame;
            return true;
        }

        public bool TryCompletePreview(long previewToken)
        {
            if (previewToken != _currentToken || _completed)
            {
                return false;
            }

            _completed = true;
            return true;
        }

        private AvatarMotionFrame Frame(
            AvatarChoice choice,
            AvatarMotionPhase phase,
            double phaseProgress,
            float breathingOffsetRatio)
        {
            var sprites = choice == AvatarChoice.Boy
                ? _content.BoyMotionSprites
                : _content.GirlMotionSprites;
            int spriteIndex;
            switch (phase)
            {
                case AvatarMotionPhase.Blink:
                    spriteIndex = 2;
                    break;
                case AvatarMotionPhase.PreviewTurn:
                    spriteIndex = 3;
                    break;
                case AvatarMotionPhase.PreviewWave:
                    spriteIndex = 4;
                    break;
                case AvatarMotionPhase.StaticReduced:
                    spriteIndex = 5;
                    break;
                case AvatarMotionPhase.IdleBreathing:
                    spriteIndex = phaseProgress < 0.5d ? 0 : 1;
                    break;
                default:
                    spriteIndex = 0;
                    break;
            }

            return new AvatarMotionFrame(choice, phase, sprites[spriteIndex], phaseProgress, breathingOffsetRatio);
        }

        private static void ValidateChoiceAndTime(AvatarChoice choice, double elapsedSeconds)
        {
            if (!IsValid(choice)) throw new ArgumentOutOfRangeException(nameof(choice));
            if (elapsedSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        }

        private static bool IsValid(AvatarChoice choice)
        {
            return choice == AvatarChoice.Boy || choice == AvatarChoice.Girl;
        }
    }
}
