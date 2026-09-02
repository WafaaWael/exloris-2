using System;
using Exloris.Runtime.Recovery;
using UnityEngine;

namespace Exloris.Runtime.Avatar
{
    public sealed class AvatarStageMediaSink : IAvatarMediaCueSink, IAvatarChoiceView
    {
        private readonly AvatarSelectView _select;
        private readonly AvatarPreviewView _preview;
        private readonly LeaveConfirmationView _leave;
        private AvatarMediaState _state;

        public AvatarStageMediaSink(
            AvatarSelectView select, AvatarPreviewView preview, LeaveConfirmationView leave)
        {
            _select = select ?? throw new ArgumentNullException(nameof(select));
            _preview = preview ?? throw new ArgumentNullException(nameof(preview));
            _leave = leave ?? throw new ArgumentNullException(nameof(leave));
        }

        public void ShowSelect(AvatarChoice left, AvatarChoice right)
        {
            _preview.SetVisible(false);
            _select.Show(left, right);
        }

        public void ShowPreview(AvatarChoice choice, long previewToken)
        {
            _select.SetVisible(false);
            _preview.Show(choice, previewToken);
        }

        public void ShowMediaCue(
            AvatarMediaState state, Sprite visual, string captionId,
            bool captionsVisible, double startedAtSeconds)
        {
            _state = state;
            switch (state)
            {
                case AvatarMediaState.Select:
                    _select.ShowMediaCue(visual, captionId, captionsVisible);
                    break;
                case AvatarMediaState.Preview:
                    _preview.ShowMediaCue(visual, captionId, captionsVisible);
                    break;
                case AvatarMediaState.Leave:
                    _leave.ShowMediaCue(visual, captionId, captionsVisible);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
        {
            switch (_state)
            {
                case AvatarMediaState.Select:
                    _select.UpdateCaption(captionId, visible);
                    break;
                case AvatarMediaState.Preview:
                    _preview.UpdateCaption(captionId, visible);
                    break;
                case AvatarMediaState.Leave:
                    _leave.UpdateCaption(captionId, visible);
                    break;
            }
        }
    }
}
