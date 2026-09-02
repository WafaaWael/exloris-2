using System;
using System.Collections.Generic;
using UnityEngine;

namespace Exloris.Runtime.Avatar
{
    [CreateAssetMenu(menuName = "Exloris/Avatar Content", fileName = "AvatarContent")]
    public sealed class AvatarContent : ScriptableObject
    {
        public const string ResourcePath = "Content/AvatarContent";

        [SerializeField] private Sprite[] backgrounds;
        [SerializeField] private Sprite[] boyMotionSprites;
        [SerializeField] private Sprite[] girlMotionSprites;
        [SerializeField] private TextAsset[] motionContracts;
        [SerializeField] private Sprite[] uiSprites;
        [SerializeField] private AudioClip[] voiceClips;
        [SerializeField] private Sprite[] visualEquivalents;
        [SerializeField] private AudioClip[] sfxClips;

        public Sprite[] Backgrounds => backgrounds;
        public Sprite[] BoyMotionSprites => boyMotionSprites;
        public Sprite[] GirlMotionSprites => girlMotionSprites;
        public TextAsset[] MotionContracts => motionContracts;
        public Sprite[] UiSprites => uiSprites;
        public AudioClip[] VoiceClips => voiceClips;
        public Sprite[] VisualEquivalents => visualEquivalents;
        public AudioClip[] SfxClips => sfxClips;

        public UnityEngine.Object[] AllReferences
        {
            get
            {
                var result = new List<UnityEngine.Object>();
                result.AddRange(backgrounds ?? Array.Empty<Sprite>());
                result.AddRange(boyMotionSprites ?? Array.Empty<Sprite>());
                result.AddRange(girlMotionSprites ?? Array.Empty<Sprite>());
                result.AddRange(motionContracts ?? Array.Empty<TextAsset>());
                result.AddRange(uiSprites ?? Array.Empty<Sprite>());
                result.AddRange(voiceClips ?? Array.Empty<AudioClip>());
                result.AddRange(visualEquivalents ?? Array.Empty<Sprite>());
                result.AddRange(sfxClips ?? Array.Empty<AudioClip>());
                return result.ToArray();
            }
        }

        public static AvatarContent LoadApproved()
        {
            var content = Resources.Load<AvatarContent>(ResourcePath);
            if (content == null) throw new InvalidOperationException("Approved Avatar content set is missing.");
            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite[] approvedBackgrounds,
            Sprite[] approvedBoyMotionSprites,
            Sprite[] approvedGirlMotionSprites,
            TextAsset[] approvedMotionContracts,
            Sprite[] approvedUiSprites,
            AudioClip[] approvedVoiceClips,
            Sprite[] approvedVisualEquivalents,
            AudioClip[] approvedSfxClips)
        {
            backgrounds = approvedBackgrounds;
            boyMotionSprites = approvedBoyMotionSprites;
            girlMotionSprites = approvedGirlMotionSprites;
            motionContracts = approvedMotionContracts;
            uiSprites = approvedUiSprites;
            voiceClips = approvedVoiceClips;
            visualEquivalents = approvedVisualEquivalents;
            sfxClips = approvedSfxClips;
            Validate();
        }

        private void Validate()
        {
            if (backgrounds == null || backgrounds.Length != 3 ||
                boyMotionSprites == null || boyMotionSprites.Length != 6 ||
                girlMotionSprites == null || girlMotionSprites.Length != 6 ||
                motionContracts == null || motionContracts.Length != 4 ||
                uiSprites == null || uiSprites.Length != 9 ||
                voiceClips == null || voiceClips.Length != 3 ||
                visualEquivalents == null || visualEquivalents.Length != 3 ||
                sfxClips == null || sfxClips.Length != 3 ||
                Array.Exists(AllReferences, item => item == null))
            {
                throw new InvalidOperationException("Avatar content set is incomplete.");
            }
        }
    }
}
