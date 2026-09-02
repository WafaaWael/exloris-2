using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Exloris.Runtime.Completion
{
    [CreateAssetMenu(menuName = "Exloris/Completion Adult Report Content", fileName = "CompletionAdultReportContent")]
    public sealed class CompletionAdultReportContent : ScriptableObject
    {
        public const string ResourcePath = "Content/CompletionAdultReportContent";

        [SerializeField] private Sprite[] completionBackgrounds;
        [SerializeField] private Sprite[] parentBackgrounds;
        [SerializeField] private Sprite[] completionAvatars;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite adultHold;
        [SerializeField] private Sprite[] adultProgress;
        [SerializeField] private Sprite[] controls;
        [SerializeField] private TextAsset completionMotion;
        [SerializeField] private AudioClip completionVoice;
        [SerializeField] private Sprite completionVisualEquivalent;
        [SerializeField] private AudioClip completionMusic;
        [SerializeField] private AudioClip villageAmbience;

        public Sprite[] CompletionBackgrounds => completionBackgrounds;
        public Sprite[] ParentBackgrounds => parentBackgrounds;
        public Sprite[] CompletionAvatars => completionAvatars;
        public Sprite[] Frames => frames;
        public Sprite AdultHold => adultHold;
        public Sprite[] AdultProgress => adultProgress;
        public Sprite[] Controls => controls;
        public TextAsset CompletionMotion => completionMotion;
        public AudioClip CompletionVoice => completionVoice;
        public Sprite CompletionVisualEquivalent => completionVisualEquivalent;
        public AudioClip CompletionMusic => completionMusic;
        public AudioClip VillageAmbience => villageAmbience;
        public Sprite Back => controls[0];
        public Sprite Close => controls[1];
        public Sprite Confirm => controls[2];
        public Sprite Home => controls[3];
        public Sprite Restart => controls[4];
        public Sprite Stay => controls[5];

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(completionBackgrounds ?? Array.Empty<Sprite>());
                result.AddRange(parentBackgrounds ?? Array.Empty<Sprite>());
                result.AddRange(completionAvatars ?? Array.Empty<Sprite>());
                result.AddRange(frames ?? Array.Empty<Sprite>());
                result.Add(adultHold);
                result.AddRange(adultProgress ?? Array.Empty<Sprite>());
                result.AddRange(controls ?? Array.Empty<Sprite>());
                result.Add(completionMotion);
                result.Add(completionVoice);
                result.Add(completionVisualEquivalent);
                result.Add(completionMusic);
                result.Add(villageAmbience);
                return result.ToArray();
            }
        }

        public void ConfigureApproved(
            Sprite[] approvedCompletionBackgrounds,
            Sprite[] approvedParentBackgrounds,
            Sprite[] approvedCompletionAvatars,
            Sprite[] approvedFrames,
            Sprite approvedAdultHold,
            Sprite[] approvedAdultProgress,
            Sprite[] approvedControls,
            TextAsset approvedCompletionMotion,
            AudioClip approvedCompletionVoice,
            Sprite approvedVisualEquivalent,
            AudioClip approvedCompletionMusic,
            AudioClip approvedVillageAmbience)
        {
            completionBackgrounds = approvedCompletionBackgrounds;
            parentBackgrounds = approvedParentBackgrounds;
            completionAvatars = approvedCompletionAvatars;
            frames = approvedFrames;
            adultHold = approvedAdultHold;
            adultProgress = approvedAdultProgress;
            controls = approvedControls;
            completionMotion = approvedCompletionMotion;
            completionVoice = approvedCompletionVoice;
            completionVisualEquivalent = approvedVisualEquivalent;
            completionMusic = approvedCompletionMusic;
            villageAmbience = approvedVillageAmbience;
            Validate();
        }

        public static CompletionAdultReportContent LoadApproved()
        {
            var content = Resources.Load<CompletionAdultReportContent>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved closing-route content is missing.");
            content.Validate();
            return content;
        }

        private void Validate()
        {
            if (completionBackgrounds == null || completionBackgrounds.Length != 3
                || parentBackgrounds == null || parentBackgrounds.Length != 3
                || completionAvatars == null || completionAvatars.Length != 2
                || frames == null || frames.Length != 2
                || adultHold == null
                || adultProgress == null || adultProgress.Length != 5
                || controls == null || controls.Length != 6
                || completionMotion == null || completionVoice == null
                || completionVisualEquivalent == null || completionMusic == null
                || villageAmbience == null || AllReferences.Length != 27
                || AllReferences.Any(item => item == null)
                || AllReferences.Distinct().Count() != 27)
                throw new InvalidOperationException("Closing-route content is incomplete.");
        }
    }
}
