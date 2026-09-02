using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Overview
{
    [CreateAssetMenu(menuName = "Exloris/Journey Overview Content", fileName = "JourneyOverviewContent")]
    public sealed class JourneyOverviewContent : ScriptableObject
    {
        public const string ResourcePath = "Content/JourneyOverviewContent";

        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] timelineIcons;
        [SerializeField] private Sprite timelineConnector;
        [SerializeField] private Sprite[] timelineStates;
        [SerializeField] private Sprite[] overviewControls;
        [SerializeField] private Sprite[] pauseControls;
        [SerializeField] private Sprite[] leaveControls;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private AudioClip voice;
        [SerializeField] private TextAsset captionTiming;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] TimelineIcons => timelineIcons;
        public Sprite TimelineConnector => timelineConnector;
        public Sprite[] TimelineStates => timelineStates;
        public Sprite[] OverviewControls => overviewControls;
        public Sprite[] PauseControls => pauseControls;
        public Sprite[] LeaveControls => leaveControls;
        public Sprite[] PresentationSprites => presentationSprites;
        public AudioClip Voice => voice;
        public TextAsset CaptionTiming => captionTiming;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;
        public Sprite CaptionBackplate => presentationSprites[0];
        public Sprite PressedOverlay => presentationSprites[1];
        public Sprite OverviewVisualEquivalent => presentationSprites[2];
        public Sprite MeasureIcon => timelineIcons[0];
        public Sprite ReadIcon => timelineIcons[1];
        public Sprite HelpIcon => timelineIcons[2];
        public Sprite CurrentTimelineState => timelineStates[1];
        public Sprite LockedTimelineState => timelineStates[3];

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(timelineIcons ?? Array.Empty<Sprite>());
                result.Add(timelineConnector);
                result.AddRange(timelineStates ?? Array.Empty<Sprite>());
                result.AddRange(overviewControls ?? Array.Empty<Sprite>());
                result.AddRange(pauseControls ?? Array.Empty<Sprite>());
                result.AddRange(leaveControls ?? Array.Empty<Sprite>());
                result.AddRange(presentationSprites ?? Array.Empty<Sprite>());
                result.Add(voice);
                result.Add(captionTiming);
                result.Add(music);
                result.Add(ambience);
                return result.ToArray();
            }
        }

        public static JourneyOverviewContent LoadApproved()
        {
            var content = Resources.Load<JourneyOverviewContent>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Journey Overview content is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite[] approvedBackgrounds,
            Sprite[] approvedTimelineIcons,
            Sprite approvedTimelineConnector,
            Sprite[] approvedTimelineStates,
            Sprite[] approvedOverviewControls,
            Sprite[] approvedPauseControls,
            Sprite[] approvedLeaveControls,
            Sprite[] approvedPresentationSprites,
            AudioClip approvedVoice,
            TextAsset approvedCaptionTiming,
            AudioClip approvedMusic,
            AudioClip approvedAmbience)
        {
            backgrounds = approvedBackgrounds;
            timelineIcons = approvedTimelineIcons;
            timelineConnector = approvedTimelineConnector;
            timelineStates = approvedTimelineStates;
            overviewControls = approvedOverviewControls;
            pauseControls = approvedPauseControls;
            leaveControls = approvedLeaveControls;
            presentationSprites = approvedPresentationSprites;
            voice = approvedVoice;
            captionTiming = approvedCaptionTiming;
            music = approvedMusic;
            ambience = approvedAmbience;
            Validate();
        }

        private void Validate()
        {
            if (backgrounds == null || backgrounds.Length != 3
                || timelineIcons == null || timelineIcons.Length != 3
                || timelineConnector == null
                || timelineStates == null || timelineStates.Length != 4
                || overviewControls == null || overviewControls.Length != 4
                || pauseControls == null || pauseControls.Length != 8
                || leaveControls == null || leaveControls.Length != 3
                || presentationSprites == null || presentationSprites.Length != 3
                || voice == null || captionTiming == null || music == null || ambience == null
                || AllReferences.Length != 33
                || AllReferences.Distinct().Count() != 33
                || Array.Exists(AllReferences, item => item == null))
            {
                throw new InvalidOperationException("Journey Overview content is incomplete.");
            }
        }
    }
}
