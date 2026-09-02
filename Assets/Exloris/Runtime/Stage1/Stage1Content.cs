using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Stage1
{
    [CreateAssetMenu(menuName = "Exloris/Stage 1 Content", fileName = "Stage1Content")]
    public sealed class Stage1Content : ScriptableObject
    {
        public const string ResourcePath = "Content/Stage1Content";

        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] props;
        [SerializeField] private Sprite[] outcomeLayers;
        [SerializeField] private Sprite[] childSprites;
        [SerializeField] private Sprite[] controls;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private AudioClip[] voiceClips;
        [SerializeField] private TextAsset captionTimings;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;
        [SerializeField] private AudioClip attachSfx;
        [SerializeField] private AudioClip stepSfx;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] Props => props;
        public Sprite[] OutcomeLayers => outcomeLayers;
        public Sprite[] ChildSprites => childSprites;
        public Sprite[] Controls => controls;
        public Sprite[] PresentationSprites => presentationSprites;
        public AudioClip[] VoiceClips => voiceClips;
        public TextAsset CaptionTimings => captionTimings;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;
        public AudioClip AttachSfx => attachSfx;
        public AudioClip StepSfx => stepSfx;

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(props ?? Array.Empty<Sprite>());
                result.AddRange(outcomeLayers ?? Array.Empty<Sprite>());
                result.AddRange(childSprites ?? Array.Empty<Sprite>());
                result.AddRange(controls ?? Array.Empty<Sprite>());
                result.AddRange(presentationSprites ?? Array.Empty<Sprite>());
                result.AddRange(voiceClips ?? Array.Empty<AudioClip>());
                result.Add(captionTimings);
                result.Add(music);
                result.Add(ambience);
                result.Add(attachSfx);
                result.Add(stepSfx);
                return result.ToArray();
            }
        }

        public static Stage1Content LoadApproved()
        {
            var content = Resources.Load<Stage1Content>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Stage 1 content is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite[] approvedBackgrounds,
            Sprite[] approvedProps,
            Sprite[] approvedOutcomeLayers,
            Sprite[] approvedChildSprites,
            Sprite[] approvedControls,
            Sprite[] approvedPresentationSprites,
            AudioClip[] approvedVoiceClips,
            TextAsset approvedCaptionTimings,
            AudioClip approvedMusic,
            AudioClip approvedAmbience,
            AudioClip approvedAttachSfx,
            AudioClip approvedStepSfx)
        {
            backgrounds = approvedBackgrounds;
            props = approvedProps;
            outcomeLayers = approvedOutcomeLayers;
            childSprites = approvedChildSprites;
            controls = approvedControls;
            presentationSprites = approvedPresentationSprites;
            voiceClips = approvedVoiceClips;
            captionTimings = approvedCaptionTimings;
            music = approvedMusic;
            ambience = approvedAmbience;
            attachSfx = approvedAttachSfx;
            stepSfx = approvedStepSfx;
            Validate();
        }

        private void Validate()
        {
            if (backgrounds == null || backgrounds.Length != 9
                || props == null || props.Length != 2
                || outcomeLayers == null || outcomeLayers.Length != 3
                || childSprites == null || childSprites.Length != 2
                || controls == null || controls.Length != 11
                || presentationSprites == null || presentationSprites.Length != 5
                || voiceClips == null || voiceClips.Length != 3
                || captionTimings == null || music == null || ambience == null || attachSfx == null || stepSfx == null
                || AllReferences.Length != 40
                || AllReferences.Distinct().Count() != 40
                || Array.Exists(AllReferences, item => item == null))
                throw new InvalidOperationException("Stage 1 content is incomplete.");
        }
    }
}
