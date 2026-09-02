using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;

namespace Exloris.Runtime.Welcome
{
    public sealed class SessionChoiceController
    {
        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IWelcomeView _view;
        private readonly IAdultGateRequestSink _adultGateSink;
        private bool _restartRequested;

        public SessionChoiceController(
            SessionState session,
            NavigationAuthority navigation,
            IWelcomeView view,
            IAdultGateRequestSink adultGateSink = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _adultGateSink = adultGateSink;
        }

        public bool HandleContinue(double nowSeconds)
        {
            if (!_session.IsOpen)
            {
                return false;
            }

            return _navigation
                .RequestResume(_session.CurrentOpenSessionLocation, nowSeconds)
                .Accepted;
        }

        public bool HandleBack()
        {
            _view.Show(WelcomePresentationState.Welcome);
            return true;
        }

        public bool HandleRestart()
        {
            if (_restartRequested || _adultGateSink == null)
            {
                return false;
            }

            var accepted = _adultGateSink.TryRequest(new AdultGateRequest(
                AdultGatePurpose.Restart,
                AdultGateRequestOrigin.SessionChoice,
                RouteId.Welcome,
                Guid.Empty,
                "session_choice"));
            if (accepted)
            {
                _restartRequested = true;
            }

            return accepted;
        }

        public void RestoreFromAdultGate() => _restartRequested = false;
    }
}
