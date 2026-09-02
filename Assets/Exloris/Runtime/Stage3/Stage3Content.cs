using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Stage3
{
    [CreateAssetMenu(menuName = "Exloris/Stage 3 Content", fileName = "Stage3Content")]
    public sealed class Stage3Content : ScriptableObject
    {
        public const string ResourcePath = "Content/Stage3Content";

        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] plans;
        [SerializeField] private Sprite[] outcomeLayers;
        [SerializeField] private Sprite[] childSprites;
        [SerializeField] private Sprite[] controls;
        [SerializeField] private Sprite[] visualEquivalents;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private AudioClip[] voiceClips;
        [SerializeField] private TextAsset captionTimings;
        [SerializeField] private TextAsset outcomeMotion;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] Plans => plans;
        public Sprite[] OutcomeLayers => outcomeLayers;
        public Sprite[] ChildSprites => childSprites;
        public Sprite[] Controls => controls;
        public Sprite[] VisualEquivalents => visualEquivalents;
        public Sprite[] PresentationSprites => presentationSprites;
        public AudioClip[] VoiceClips => voiceClips;
        public TextAsset CaptionTimings => captionTimings;
        public TextAsset OutcomeMotion => outcomeMotion;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(plans ?? Array.Empty<Sprite>());
                result.AddRange(outcomeLayers ?? Array.Empty<Sprite>());
                result.AddRange(childSprites ?? Array.Empty<Sprite>());
                result.AddRange(controls ?? Array.Empty<Sprite>());
                result.AddRange(visualEquivalents ?? Array.Empty<Sprite>());
                result.AddRange(presentationSprites ?? Array.Empty<Sprite>());
                result.AddRange(voiceClips ?? Array.Empty<AudioClip>());
                result.Add(captionTimings);
                result.Add(outcomeMotion);
                result.Add(music);
                result.Add(ambience);
                return result.ToArray();
            }
        }

        public static Stage3Content LoadApproved()
        {
            var content = Resources.Load<Stage3Content>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Stage 3 content is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite[] approvedBackgrounds,
            Sprite[] approvedPlans,
            Sprite[] approvedOutcomeLayers,
            Sprite[] approvedChildSprites,
            Sprite[] approvedControls,
            Sprite[] approvedVisualEquivalents,
            Sprite[] approvedPresentationSprites,
            AudioClip[] approvedVoiceClips,
            TextAsset approvedCaptionTimings,
            TextAsset approvedOutcomeMotion,
            AudioClip approvedMusic,
            AudioClip approvedAmbience)
        {
            backgrounds = approvedBackgrounds;
            plans = approvedPlans;
            outcomeLayers = approvedOutcomeLayers;
            childSprites = approvedChildSprites;
            controls = approvedControls;
            visualEquivalents = approvedVisualEquivalents;
            presentationSprites = approvedPresentationSprites;
            voiceClips = approvedVoiceClips;
            captionTimings = approvedCaptionTimings;
            outcomeMotion = approvedOutcomeMotion;
            music = approvedMusic;
            ambience = approvedAmbience;
            Validate();
        }

        private void Validate()
        {
            var references = AllReferences;
            if (backgrounds == null || backgrounds.Length != 6
                || plans == null || plans.Length != 3
                || outcomeLayers == null || outcomeLayers.Length != 6
                || childSprites == null || childSprites.Length != 2
                || controls == null || controls.Length != 10
                || visualEquivalents == null || visualEquivalents.Length != 4
                || presentationSprites == null || presentationSprites.Length != 2
                || voiceClips == null || voiceClips.Length != 2
                || captionTimings == null || outcomeMotion == null || music == null || ambience == null
                || references.Length != 39
                || references.Distinct().Count() != 39
                || Array.Exists(references, item => item == null))
                throw new InvalidOperationException("Stage 3 content is incomplete.");
        }
    }
}
