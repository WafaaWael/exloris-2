using System;
using UnityEngine;

namespace Exloris.Runtime.Intro
{
    [CreateAssetMenu(menuName = "Exloris/Startup Intro Content", fileName = "StartupIntroContent")]
    public sealed class StartupIntroContent : ScriptableObject
    {
        public const string ResourcePath = "Content/StartupIntroContent";

        [SerializeField] private Sprite skip;
        [SerializeField] private Sprite resume;
        [SerializeField] private Sprite captionBackplate;
        [SerializeField] private Sprite ibisWingsOpen;
        [SerializeField] private Sprite ibisHeadLift;
        [SerializeField] private Sprite ibisBeakRight;
        [SerializeField] private Sprite ibisCalmSettle;
        [SerializeField] private Sprite ibisStaticReduced;
        [SerializeField] private Sprite visualEquivalent;
        [SerializeField] private AudioClip voice;

        public Sprite Skip => skip;
        public Sprite Resume => resume;
        public Sprite CaptionBackplate => captionBackplate;
        public Sprite IbisWingsOpen => ibisWingsOpen;
        public Sprite IbisHeadLift => ibisHeadLift;
        public Sprite IbisBeakRight => ibisBeakRight;
        public Sprite IbisCalmSettle => ibisCalmSettle;
        public Sprite IbisStaticReduced => ibisStaticReduced;
        public Sprite VisualEquivalent => visualEquivalent;
        public AudioClip Voice => voice;

        public static StartupIntroContent LoadApproved()
        {
            var content = Resources.Load<StartupIntroContent>(ResourcePath);
            if (content == null)
            {
                throw new InvalidOperationException("Approved Startup + Intro content set is missing.");
            }

            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite skipSprite,
            Sprite resumeSprite,
            Sprite captionBackplateSprite,
            Sprite wingsOpenSprite,
            Sprite headLiftSprite,
            Sprite beakRightSprite,
            Sprite calmSettleSprite,
            Sprite staticReducedSprite,
            Sprite visualEquivalentSprite,
            AudioClip voiceClip)
        {
            skip = skipSprite;
            resume = resumeSprite;
            captionBackplate = captionBackplateSprite;
            ibisWingsOpen = wingsOpenSprite;
            ibisHeadLift = headLiftSprite;
            ibisBeakRight = beakRightSprite;
            ibisCalmSettle = calmSettleSprite;
            ibisStaticReduced = staticReducedSprite;
            visualEquivalent = visualEquivalentSprite;
            voice = voiceClip;
            Validate();
        }

        private void Validate()
        {
            if (skip == null || resume == null || captionBackplate == null || ibisWingsOpen == null ||
                ibisHeadLift == null || ibisBeakRight == null || ibisCalmSettle == null ||
                ibisStaticReduced == null || visualEquivalent == null || voice == null)
            {
                throw new InvalidOperationException("Startup + Intro content set contains a missing approved asset.");
            }
        }
    }
}
