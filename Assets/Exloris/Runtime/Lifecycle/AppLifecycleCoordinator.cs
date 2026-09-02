using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Recovery;

namespace Exloris.Runtime.Lifecycle
{
    public interface IAppLifecycleParticipant
    {
        void PauseAndDuckImmediately();
        void ResumeAfterMappedUserAction();
    }

    public sealed class AppLifecycleCoordinator
    {
        private readonly NavigationAuthority _navigation;
        private readonly RecoveryPresenter _presenter;
        private readonly IReadOnlyList<IAppLifecycleParticipant> _participants;

        public AppLifecycleCoordinator(
            NavigationAuthority navigation,
            RecoveryPresenter presenter,
            IReadOnlyList<IAppLifecycleParticipant> participants)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _participants = participants ?? throw new ArgumentNullException(nameof(participants));
        }

        public bool Tick(double nowSeconds)
        {
            if (!_navigation.CheckTimeout(nowSeconds))
            {
                return false;
            }

            PauseParticipants();
            _presenter.PrepareRecovery(_navigation.Recovery);
            return true;
        }

        public void HandleInterruption()
        {
            PauseParticipants();
            if (_navigation.Interrupt())
            {
                _presenter.PrepareRecovery(_navigation.Recovery);
            }
            else
            {
                _presenter.PreparePaused();
            }
        }

        public void HandleFocusRestored()
        {
            // A focus event is never permission to resume playback or accept input.
        }

        public bool HandleAssetFailure()
        {
            if (!_navigation.Cancel())
            {
                return false;
            }

            PauseParticipants();
            _presenter.PrepareRecovery(_navigation.Recovery);
            return true;
        }

        public NavigationRequestResult Retry(double nowSeconds)
        {
            if (!_presenter.CanAcceptRecoveryAction)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            var result = _navigation.Retry(nowSeconds);
            if (result.Accepted)
            {
                _presenter.BeginLoading();
                ResumeParticipants();
            }

            return result;
        }

        public NavigationRequestResult Home(double nowSeconds)
        {
            if (!_presenter.CanAcceptRecoveryAction)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            var result = _navigation.Home(nowSeconds);
            if (result.Accepted)
            {
                _presenter.BeginLoading();
                ResumeParticipants();
            }

            return result;
        }

        public bool ResumeCurrentScreenAfterUserAction()
        {
            if (!_presenter.CanAcceptResumeAction)
            {
                return false;
            }

            _presenter.CompleteLoadingAndShowContent();
            ResumeParticipants();
            return true;
        }

        private void PauseParticipants()
        {
            foreach (var participant in _participants)
            {
                participant?.PauseAndDuckImmediately();
            }
        }

        private void ResumeParticipants()
        {
            foreach (var participant in _participants)
            {
                participant?.ResumeAfterMappedUserAction();
            }
        }
    }
}
