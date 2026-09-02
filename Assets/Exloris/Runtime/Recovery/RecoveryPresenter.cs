using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Input;

namespace Exloris.Runtime.Recovery
{
    public enum RecoveryPresentationState
    {
        Hidden,
        Loading,
        Preparing,
        Visible,
        PausedPreparing,
        PausedVisible
    }

    public sealed class RecoveryPresenter
    {
        private readonly IInputGate _inputGate;

        public RecoveryPresenter(IInputGate inputGate)
        {
            _inputGate = inputGate ?? throw new ArgumentNullException(nameof(inputGate));
            State = RecoveryPresentationState.Hidden;
        }

        public RecoveryPresentationState State { get; private set; }
        public NavigationRecovery Recovery { get; private set; }
        public bool CanAcceptRecoveryAction => State == RecoveryPresentationState.Visible;
        public bool CanAcceptResumeAction => State == RecoveryPresentationState.PausedVisible;

        public void BeginLoading()
        {
            Recovery = null;
            State = RecoveryPresentationState.Loading;
            _inputGate.SetVisibleStateReady(false);
        }

        public void CompleteLoadingAndShowContent()
        {
            Recovery = null;
            State = RecoveryPresentationState.Hidden;
            _inputGate.SetVisibleStateReady(true);
        }

        public void PrepareRecovery(NavigationRecovery recovery)
        {
            Recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            State = RecoveryPresentationState.Preparing;
            _inputGate.SetVisibleStateReady(false);
        }

        public void PreparePaused()
        {
            Recovery = null;
            State = RecoveryPresentationState.PausedPreparing;
            _inputGate.SetVisibleStateReady(false);
        }

        public void MarkRecoveryVisible()
        {
            if (State == RecoveryPresentationState.Preparing)
            {
                State = RecoveryPresentationState.Visible;
            }
            else if (State == RecoveryPresentationState.PausedPreparing)
            {
                State = RecoveryPresentationState.PausedVisible;
            }
            else
            {
                throw new InvalidOperationException(
                    "Only a prepared recovery or pause surface can become visible.");
            }

            _inputGate.SetVisibleStateReady(true);
        }
    }
}
