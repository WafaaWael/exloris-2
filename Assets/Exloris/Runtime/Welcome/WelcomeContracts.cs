using UnityEngine;

namespace Exloris.Runtime.Welcome
{
    public enum WelcomePresentationState
    {
        Welcome,
        SessionChoice,
    }

    public interface IWelcomeView
    {
        void Show(WelcomePresentationState state);
    }

    public interface IWelcomeMediaCueSink
    {
        void ShowMediaCue(
            WelcomePresentationState state,
            Sprite visual,
            string captionId,
            bool captionsVisible,
            double startedAtSeconds);
        void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds);
        void PauseMediaCue();
        void ResumeMediaCue();
    }
}
