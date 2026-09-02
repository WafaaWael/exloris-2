using System;
using UnityEngine;

namespace Exloris.Runtime.Welcome
{
    [CreateAssetMenu(menuName = "Exloris/Welcome Content", fileName = "WelcomeContent")]
    public sealed class WelcomeContent : ScriptableObject
    {
        public const string ResourcePath = "Content/WelcomeContent";

        [SerializeField] private Sprite background4x3;
        [SerializeField] private Sprite background16x9;
        [SerializeField] private Sprite background24x10;
        [SerializeField] private Sprite play;
        [SerializeField] private Sprite adult;
        [SerializeField] private Sprite continueButton;
        [SerializeField] private Sprite restart;
        [SerializeField] private Sprite back;
        [SerializeField] private Sprite resume;
        [SerializeField] private Sprite retry;
        [SerializeField] private Sprite home;
        [SerializeField] private Sprite soundOn;
        [SerializeField] private Sprite soundOff;
        [SerializeField] private Sprite captionsOn;
        [SerializeField] private Sprite captionsOff;
        [SerializeField] private Sprite reducedMotionOn;
        [SerializeField] private Sprite reducedMotionOff;
        [SerializeField] private Sprite captionBackplate;
        [SerializeField] private Sprite pressedOverlay;
        [SerializeField] private AudioClip welcomeVoice;
        [SerializeField] private AudioClip sessionChoiceVoice;
        [SerializeField] private Sprite welcomeVisualEquivalent;
        [SerializeField] private Sprite sessionChoiceVisualEquivalent;
        [SerializeField] private AudioClip music;
        [SerializeField] private AudioClip ambience;

        public Sprite Background4x3 => background4x3;
        public Sprite Background16x9 => background16x9;
        public Sprite Background24x10 => background24x10;
        public Sprite Play => play;
        public Sprite Adult => adult;
        public Sprite Continue => continueButton;
        public Sprite Restart => restart;
        public Sprite Back => back;
        public Sprite Resume => resume;
        public Sprite Retry => retry;
        public Sprite Home => home;
        public Sprite SoundOn => soundOn;
        public Sprite SoundOff => soundOff;
        public Sprite CaptionsOn => captionsOn;
        public Sprite CaptionsOff => captionsOff;
        public Sprite ReducedMotionOn => reducedMotionOn;
        public Sprite ReducedMotionOff => reducedMotionOff;
        public Sprite CaptionBackplate => captionBackplate;
        public Sprite PressedOverlay => pressedOverlay;
        public AudioClip WelcomeVoice => welcomeVoice;
        public AudioClip SessionChoiceVoice => sessionChoiceVoice;
        public Sprite WelcomeVisualEquivalent => welcomeVisualEquivalent;
        public Sprite SessionChoiceVisualEquivalent => sessionChoiceVisualEquivalent;
        public AudioClip Music => music;
        public AudioClip Ambience => ambience;

        public static WelcomeContent LoadApproved()
        {
            var content = Resources.Load<WelcomeContent>(ResourcePath);
            if (content == null)
            {
                throw new InvalidOperationException("Approved Welcome content set is missing.");
            }

            content.Validate();
            return content;
        }

        public void ConfigureApproved(
            Sprite background4x3Sprite,
            Sprite background16x9Sprite,
            Sprite background24x10Sprite,
            Sprite playSprite,
            Sprite adultSprite,
            Sprite continueSprite,
            Sprite restartSprite,
            Sprite backSprite,
            Sprite resumeSprite,
            Sprite retrySprite,
            Sprite homeSprite,
            Sprite soundOnSprite,
            Sprite soundOffSprite,
            Sprite captionsOnSprite,
            Sprite captionsOffSprite,
            Sprite reducedMotionOnSprite,
            Sprite reducedMotionOffSprite,
            Sprite captionBackplateSprite,
            Sprite pressedOverlaySprite,
            AudioClip welcomeVoiceClip,
            AudioClip sessionChoiceVoiceClip,
            Sprite welcomeVisualEquivalentSprite,
            Sprite sessionChoiceVisualEquivalentSprite,
            AudioClip musicClip,
            AudioClip ambienceClip)
        {
            background4x3 = background4x3Sprite;
            background16x9 = background16x9Sprite;
            background24x10 = background24x10Sprite;
            play = playSprite;
            adult = adultSprite;
            continueButton = continueSprite;
            restart = restartSprite;
            back = backSprite;
            resume = resumeSprite;
            retry = retrySprite;
            home = homeSprite;
            soundOn = soundOnSprite;
            soundOff = soundOffSprite;
            captionsOn = captionsOnSprite;
            captionsOff = captionsOffSprite;
            reducedMotionOn = reducedMotionOnSprite;
            reducedMotionOff = reducedMotionOffSprite;
            captionBackplate = captionBackplateSprite;
            pressedOverlay = pressedOverlaySprite;
            welcomeVoice = welcomeVoiceClip;
            sessionChoiceVoice = sessionChoiceVoiceClip;
            welcomeVisualEquivalent = welcomeVisualEquivalentSprite;
            sessionChoiceVisualEquivalent = sessionChoiceVisualEquivalentSprite;
            music = musicClip;
            ambience = ambienceClip;
            Validate();
        }

        private void Validate()
        {
            if (background4x3 == null || background16x9 == null || background24x10 == null ||
                play == null || adult == null || continueButton == null || restart == null || back == null ||
                resume == null || retry == null || home == null ||
                soundOn == null || soundOff == null || captionsOn == null || captionsOff == null ||
                reducedMotionOn == null || reducedMotionOff == null || captionBackplate == null || pressedOverlay == null ||
                welcomeVoice == null || sessionChoiceVoice == null || welcomeVisualEquivalent == null ||
                sessionChoiceVisualEquivalent == null || music == null || ambience == null)
            {
                throw new InvalidOperationException("Welcome content set contains a missing approved asset.");
            }
        }
    }
}
