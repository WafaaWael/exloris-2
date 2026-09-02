using Exloris.Core.Navigation;
using UnityEngine;

namespace Exloris.Runtime.Avatar
{
    public enum AvatarChoice
    {
        Boy,
        Girl,
    }

    public enum AvatarPresentationState
    {
        None,
        Select,
        Preview,
        Transitioning,
    }

    public interface IAvatarChoiceView
    {
        void ShowSelect(AvatarChoice left, AvatarChoice right);
        void ShowPreview(AvatarChoice choice, long previewToken);
    }

    public interface ILeaveConfirmationSink
    {
        bool TryShow(RouteId caller);
    }

    public interface IAvatarOrderSource
    {
        AvatarChoice ChooseInitialLeft();
    }

    public enum AvatarMediaState
    {
        Select,
        Preview,
        Leave,
    }

    public enum AvatarSfxCue
    {
        Press,
        Confirm,
        Back,
    }

    public interface IAvatarMediaCueSink
    {
        void ShowMediaCue(
            AvatarMediaState state,
            Sprite visual,
            string captionId,
            bool captionsVisible,
            double startedAtSeconds);
        void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds);
    }
}
