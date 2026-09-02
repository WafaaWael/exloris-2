using System;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Preferences;

namespace Exloris.Runtime.Welcome
{
    public sealed class WelcomeController
    {
        private readonly SessionState _session;
        private readonly NavigationAuthority _navigation;
        private readonly IWelcomeView _view;
        private readonly IAdultGateRequestSink _adultGateSink;
        private readonly AccessibilityState _accessibility;
        private readonly AccessibilityPreferenceStore _preferenceStore;
        private bool _adultRequested;

        public WelcomeController(
            SessionState session,
            NavigationAuthority navigation,
            IWelcomeView view,
            IAdultGateRequestSink adultGateSink = null,
            AccessibilityState accessibility = null,
            AccessibilityPreferenceStore preferenceStore = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _adultGateSink = adultGateSink;
            _accessibility = accessibility;
            _preferenceStore = preferenceStore;
        }

        public bool HandlePlay(double nowSeconds)
        {
            if (_navigation.IsLocked)
            {
                return false;
            }

            if (_session.IsOpen)
            {
                _view.Show(WelcomePresentationState.SessionChoice);
                return true;
            }

            var request = _navigation.Request(RouteId.AvatarSelect, nowSeconds);
            if (!request.Accepted)
            {
                return false;
            }

            _session.Begin(RouteId.AvatarSelect);
            return true;
        }

        public bool HandleAdult()
        {
            if (_adultRequested || _adultGateSink == null)
            {
                return false;
            }

            var accepted = _adultGateSink.TryRequest(new AdultGateRequest(
                AdultGatePurpose.Dashboard,
                AdultGateRequestOrigin.Welcome,
                RouteId.Welcome,
                Guid.Empty,
                "welcome"));
            if (accepted)
            {
                _adultRequested = true;
            }

            return accepted;
        }

        public void RestoreFromAdultGate() => _adultRequested = false;

        public bool HandleSound()
        {
            _session.SoundEnabled = !_session.SoundEnabled;
            return _session.SoundEnabled;
        }

        public bool HandleCaptions()
        {
            if (_accessibility == null || _preferenceStore == null)
            {
                return false;
            }

            _accessibility.SetCaptionsEnabled(!_accessibility.CaptionsEnabled);
            SaveAccessibility();
            return _accessibility.CaptionsEnabled;
        }

        public bool HandleMotion()
        {
            if (_accessibility == null || _preferenceStore == null)
            {
                return false;
            }

            _accessibility.SetAlwaysReduce(!_accessibility.AlwaysReduce);
            SaveAccessibility();
            return _accessibility.AlwaysReduce;
        }

        private void SaveAccessibility()
        {
            _preferenceStore.Save(new AccessibilityPreferences(
                _accessibility.AlwaysReduce,
                _accessibility.CaptionsEnabled));
        }
    }
}
