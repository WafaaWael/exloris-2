using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Stage2
{
    [CreateAssetMenu(menuName = "Exloris/Stage 2 Content", fileName = "Stage2Content")]
    public sealed class Stage2Content : ScriptableObject
    {
        public const string ResourcePath = "Content/Stage2Content";

        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] stones;
        [SerializeField] private Sprite[] hotspots;
        [SerializeField] private Sprite[] outcomeLayers;
        [SerializeField] private Sprite[] childSprites;
        [SerializeField] private Sprite[] controls;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private AudioClip[] voiceClips;
        [SerializeField] private TextAsset captionTimings;
        [SerializeField] private TextAsset outcomeMotion;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;
        [SerializeField] private AudioClip[] stoneSfx;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] Stones => stones;
        public Sprite[] Hotspots => hotspots;
        public Sprite[] OutcomeLayers => outcomeLayers;
        public Sprite[] ChildSprites => childSprites;
        public Sprite[] Controls => controls;
        public Sprite[] PresentationSprites => presentationSprites;
        public AudioClip[] VoiceClips => voiceClips;
        public TextAsset CaptionTimings => captionTimings;
        public TextAsset OutcomeMotion => outcomeMotion;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;
        public AudioClip[] StoneSfx => stoneSfx;

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(stones ?? Array.Empty<Sprite>());
                result.AddRange(hotspots ?? Array.Empty<Sprite>());
                result.AddRange(outcomeLayers ?? Array.Empty<Sprite>());
                result.AddRange(childSprites ?? Array.Empty<Sprite>());
                result.AddRange(controls ?? Array.Empty<Sprite>());
                result.AddRange(presentationSprites ?? Array.Empty<Sprite>());
                result.AddRange(voiceClips ?? Array.Empty<AudioClip>());
                result.Add(captionTimings);
                result.Add(outcomeMotion);
                result.Add(music);
                result.Add(ambience);
                result.AddRange(stoneSfx ?? Array.Empty<AudioClip>());
                return result.ToArray();
            }
        }

        public static Stage2Content LoadApproved()
        {
            var content = Resources.Load<Stage2Content>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Stage 2 content is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite[] approvedBackgrounds,
            Sprite[] approvedStones,
            Sprite[] approvedHotspots,
            Sprite[] approvedOutcomeLayers,
            Sprite[] approvedChildSprites,
            Sprite[] approvedControls,
            Sprite[] approvedPresentationSprites,
            AudioClip[] approvedVoiceClips,
            TextAsset approvedCaptionTimings,
            TextAsset approvedOutcomeMotion,
            AudioClip approvedMusic,
            AudioClip approvedAmbience,
            AudioClip[] approvedStoneSfx)
        {
            backgrounds = approvedBackgrounds;
            stones = approvedStones;
            hotspots = approvedHotspots;
            outcomeLayers = approvedOutcomeLayers;
            childSprites = approvedChildSprites;
            controls = approvedControls;
            presentationSprites = approvedPresentationSprites;
            voiceClips = approvedVoiceClips;
            captionTimings = approvedCaptionTimings;
            outcomeMotion = approvedOutcomeMotion;
            music = approvedMusic;
            ambience = approvedAmbience;
            stoneSfx = approvedStoneSfx;
            Validate();
        }

        private void Validate()
        {
            var references = AllReferences;
            if (backgrounds == null || backgrounds.Length != 9
                || stones == null || stones.Length != 3
                || hotspots == null || hotspots.Length != 3
                || outcomeLayers == null || outcomeLayers.Length != 9
                || childSprites == null || childSprites.Length != 2
                || controls == null || controls.Length != 7
                || presentationSprites == null || presentationSprites.Length != 4
                || voiceClips == null || voiceClips.Length != 2
                || captionTimings == null || outcomeMotion == null || music == null || ambience == null
                || stoneSfx == null || stoneSfx.Length != 3
                || references.Length != 46
                || references.Distinct().Count() != 46
                || Array.Exists(references, item => item == null))
                throw new InvalidOperationException("Stage 2 content is incomplete.");
        }
    }
}
