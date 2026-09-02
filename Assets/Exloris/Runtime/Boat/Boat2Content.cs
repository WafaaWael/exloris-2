using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Boat
{
    [CreateAssetMenu(menuName = "Exloris/Boat 2 Content", fileName = "Boat2Content")]
    public sealed class Boat2Content : ScriptableObject
    {
        public const string ResourcePath = "Content/Boat2Content";
        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] boatSprites;
        [SerializeField] private Sprite[] childSprites;
        [SerializeField] private Sprite[] boatkeeperSprites;
        [SerializeField] private Sprite[] ibisSprites;
        [SerializeField] private Sprite[] controls;
        [SerializeField] private Sprite[] presentationSprites;
        [SerializeField] private AudioClip[] voiceClips;
        [SerializeField] private TextAsset captionTimings;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;
        [SerializeField] private AudioClip arrivalSfx;
        [SerializeField] private AudioClip ibisSfx;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] BoatSprites => boatSprites;
        public Sprite[] ChildSprites => childSprites;
        public Sprite[] BoatkeeperSprites => boatkeeperSprites;
        public Sprite[] IbisSprites => ibisSprites;
        public Sprite[] Controls => controls;
        public Sprite[] PresentationSprites => presentationSprites;
        public AudioClip[] VoiceClips => voiceClips;
        public TextAsset CaptionTimings => captionTimings;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;
        public AudioClip ArrivalSfx => arrivalSfx;
        public AudioClip IbisSfx => ibisSfx;
        public BoatDirection InitialIbisDirection => BoatDirection.Left;
        public double RouteDurationSeconds => 6d;
        public double[] ProgressCutSeconds => new[] { 0d, 2d, 4d, 6d };

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(boatSprites ?? Array.Empty<Sprite>());
                result.AddRange(childSprites ?? Array.Empty<Sprite>());
                result.AddRange(boatkeeperSprites ?? Array.Empty<Sprite>());
                result.AddRange(ibisSprites ?? Array.Empty<Sprite>());
                result.AddRange(controls ?? Array.Empty<Sprite>());
                result.AddRange(presentationSprites ?? Array.Empty<Sprite>());
                result.AddRange(voiceClips ?? Array.Empty<AudioClip>());
                result.Add(captionTimings); result.Add(music); result.Add(ambience); result.Add(arrivalSfx); result.Add(ibisSfx);
                return result.ToArray();
            }
        }

        public static Boat2Content LoadApproved()
        {
            var content = Resources.Load<Boat2Content>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Boat 2 content is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(Sprite[] approvedBackgrounds, Sprite[] approvedBoatSprites,
            Sprite[] approvedChildSprites, Sprite[] approvedBoatkeeperSprites, Sprite[] approvedIbisSprites,
            Sprite[] approvedControls, Sprite[] approvedPresentationSprites, AudioClip[] approvedVoiceClips,
            TextAsset approvedCaptionTimings, AudioClip approvedMusic, AudioClip approvedAmbience,
            AudioClip approvedArrivalSfx, AudioClip approvedIbisSfx)
        {
            backgrounds = approvedBackgrounds; boatSprites = approvedBoatSprites; childSprites = approvedChildSprites;
            boatkeeperSprites = approvedBoatkeeperSprites; ibisSprites = approvedIbisSprites; controls = approvedControls;
            presentationSprites = approvedPresentationSprites; voiceClips = approvedVoiceClips;
            captionTimings = approvedCaptionTimings; music = approvedMusic; ambience = approvedAmbience;
            arrivalSfx = approvedArrivalSfx; ibisSfx = approvedIbisSfx;
            Validate();
        }

        private void Validate()
        {
            if (backgrounds == null || backgrounds.Length != 9 || boatSprites == null || boatSprites.Length != 5
                || childSprites == null || childSprites.Length != 2 || boatkeeperSprites == null || boatkeeperSprites.Length != 4
                || ibisSprites == null || ibisSprites.Length != 9 || controls == null || controls.Length != 10
                || presentationSprites == null || presentationSprites.Length != 4 || voiceClips == null || voiceClips.Length != 2
                || captionTimings == null || music == null || ambience == null || arrivalSfx == null || ibisSfx == null
                || AllReferences.Length != 50 || AllReferences.Distinct().Count() != 50 || Array.Exists(AllReferences, item => item == null))
                throw new InvalidOperationException("Boat 2 content is incomplete.");
        }
    }
}
