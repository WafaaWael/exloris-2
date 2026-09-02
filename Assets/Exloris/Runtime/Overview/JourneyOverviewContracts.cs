using System;
using UnityEngine;

namespace Exloris.Runtime.Overview
{
    public enum JourneyOverviewRevealState
    {
        Hidden,
        Preparing,
        RevealingMeasure,
        RevealingRead,
        RevealingHelp,
        Ready,
        Paused,
        Leaving,
        Navigating,
        Recovery,
    }

    public readonly struct JourneyOverviewSnapshot
    {
        public JourneyOverviewSnapshot(
            Guid tokenId,
            JourneyOverviewRevealState state,
            double elapsedSeconds,
            int revealedBeatCount)
        {
            TokenId = tokenId;
            State = state;
            ElapsedSeconds = elapsedSeconds;
            RevealedBeatCount = revealedBeatCount;
        }

        public Guid TokenId { get; }
        public JourneyOverviewRevealState State { get; }
        public double ElapsedSeconds { get; }
        public int RevealedBeatCount { get; }
    }

    public interface IJourneyOverviewView
    {
        void Present(JourneyOverviewSnapshot snapshot, bool reducedMotion);
        void SetInputReady(bool ready);
    }

    public interface IPauseOverlaySink
    {
        bool TryShow(JourneyOverviewSnapshot snapshot);
    }

    public enum JourneyOverviewTimelineTreatment
    {
        Hidden,
        Current,
        EncouragingNotYet,
    }

    public enum JourneyOverviewPauseReason
    {
        Manual,
        AppBackground,
        AudioFocusLoss,
    }

    public readonly struct JourneyOverviewRevealFrame
    {
        public JourneyOverviewRevealFrame(
            Guid tokenId,
            int revealedBeatCount,
            bool measureVisible,
            bool readVisible,
            bool helpVisible,
            float visualEquivalentMaskPixels,
            bool spatialMotionEnabled,
            bool currentPulseEnabled,
            Sprite measureIcon,
            Sprite readIcon,
            Sprite helpIcon,
            Sprite connector,
            Sprite currentState,
            Sprite lockedState)
        {
            TokenId = tokenId;
            RevealedBeatCount = revealedBeatCount;
            MeasureVisible = measureVisible;
            ReadVisible = readVisible;
            HelpVisible = helpVisible;
            VisualEquivalentMaskPixels = visualEquivalentMaskPixels;
            SpatialMotionEnabled = spatialMotionEnabled;
            CurrentPulseEnabled = currentPulseEnabled;
            MeasureIcon = measureIcon;
            ReadIcon = readIcon;
            HelpIcon = helpIcon;
            Connector = connector;
            CurrentState = currentState;
            LockedState = lockedState;
        }

        public Guid TokenId { get; }
        public int RevealedBeatCount { get; }
        public bool MeasureVisible { get; }
        public bool ReadVisible { get; }
        public bool HelpVisible { get; }
        public float VisualEquivalentMaskPixels { get; }
        public bool SpatialMotionEnabled { get; }
        public bool CurrentPulseEnabled { get; }
        public Sprite MeasureIcon { get; }
        public Sprite ReadIcon { get; }
        public Sprite HelpIcon { get; }
        public Sprite Connector { get; }
        public Sprite CurrentState { get; }
        public Sprite LockedState { get; }
        public JourneyOverviewTimelineTreatment MeasureTreatment =>
            MeasureVisible ? JourneyOverviewTimelineTreatment.Current : JourneyOverviewTimelineTreatment.Hidden;
        public JourneyOverviewTimelineTreatment ReadTreatment =>
            ReadVisible ? JourneyOverviewTimelineTreatment.EncouragingNotYet : JourneyOverviewTimelineTreatment.Hidden;
        public JourneyOverviewTimelineTreatment HelpTreatment =>
            HelpVisible ? JourneyOverviewTimelineTreatment.EncouragingNotYet : JourneyOverviewTimelineTreatment.Hidden;
        public bool HasSemanticTimelineControl => false;
        public bool RequiresReading => false;
        public bool UsesNegativeSfx => false;
    }

    public interface IJourneyOverviewRevealSink
    {
        void PresentReveal(JourneyOverviewRevealFrame frame);
        void SetInputReady(bool ready);
    }

    public readonly struct JourneyOverviewMediaCue
    {
        public JourneyOverviewMediaCue(
            Guid revealTokenId,
            Guid mediaTokenId,
            Sprite visualEquivalent,
            float visualEquivalentMaskPixels,
            string captionId,
            bool captionVisible,
            double captionCueStartSeconds,
            double elapsedSeconds)
        {
            RevealTokenId = revealTokenId;
            MediaTokenId = mediaTokenId;
            VisualEquivalent = visualEquivalent;
            VisualEquivalentMaskPixels = visualEquivalentMaskPixels;
            CaptionId = captionId;
            CaptionVisible = captionVisible;
            CaptionCueStartSeconds = captionCueStartSeconds;
            ElapsedSeconds = elapsedSeconds;
        }

        public Guid RevealTokenId { get; }
        public Guid MediaTokenId { get; }
        public Sprite VisualEquivalent { get; }
        public float VisualEquivalentMaskPixels { get; }
        public string CaptionId { get; }
        public bool CaptionVisible { get; }
        public double CaptionCueStartSeconds { get; }
        public double ElapsedSeconds { get; }
        public bool UsesNegativeSfx => false;
        public bool RequiresReading => false;
    }

    public readonly struct JourneyOverviewMediaHandle
    {
        public JourneyOverviewMediaHandle(
            Guid revealTokenId,
            Guid mediaTokenId,
            Guid? voiceTokenId,
            Guid? musicTokenId,
            Guid? ambienceTokenId)
        {
            RevealTokenId = revealTokenId;
            MediaTokenId = mediaTokenId;
            VoiceTokenId = voiceTokenId;
            MusicTokenId = musicTokenId;
            AmbienceTokenId = ambienceTokenId;
        }

        public Guid RevealTokenId { get; }
        public Guid MediaTokenId { get; }
        public Guid? VoiceTokenId { get; }
        public Guid? MusicTokenId { get; }
        public Guid? AmbienceTokenId { get; }
    }

    public interface IJourneyOverviewMediaCueSink
    {
        void PresentMediaCue(JourneyOverviewMediaCue cue);
        void FreezeMediaCue(double elapsedSeconds);
        void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds);
        void SetCallerInputLocked(bool locked);
        void ShowPauseOverlay(JourneyOverviewPauseReason reason);
    }

    public interface IJourneyOverviewPauseMedia
    {
        Guid CurrentRevealTokenId { get; }
        bool IsPaused { get; }
        JourneyOverviewMediaHandle? ResumeAfterMappedUserAction(
            Guid revealTokenId, double nowSeconds);
        void RefreshSettings(double nowSeconds);
    }
}
