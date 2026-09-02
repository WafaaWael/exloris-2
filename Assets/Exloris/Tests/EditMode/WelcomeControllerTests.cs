using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Adult;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.Welcome;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeControllerTests
    {
        [Test]
        public void PlayWithoutOpenSessionBeginsAtAvatarSelectAndRequestsItOnce()
        {
            var session = new SessionState();
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var view = new RecordingWelcomeView();
            var controller = new WelcomeController(session, navigation, view);

            var accepted = controller.HandlePlay(10.0);

            Assert.That(accepted, Is.True);
            Assert.That(session.IsOpen, Is.True);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(navigation.ActiveTransaction, Is.Not.Null);
            Assert.That(navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(view.State, Is.EqualTo(WelcomePresentationState.Welcome));
        }

        [Test]
        public void PlayWithOpenSessionShowsChoiceAndPreservesExactLocation()
        {
            var session = new SessionState();
            session.Begin(RouteId.Stage1);
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var view = new RecordingWelcomeView();
            var controller = new WelcomeController(session, navigation, view);

            var accepted = controller.HandlePlay(12.0);

            Assert.That(accepted, Is.True);
            Assert.That(view.State, Is.EqualTo(WelcomePresentationState.SessionChoice));
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage1));
            Assert.That(navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void DuplicatePlayDuringNavigationIsRejectedWithoutShowingSessionChoice()
        {
            var session = new SessionState();
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var view = new RecordingWelcomeView();
            var controller = new WelcomeController(session, navigation, view);
            Assert.That(controller.HandlePlay(20.0), Is.True);
            var firstTransaction = navigation.ActiveTransaction;

            var duplicate = controller.HandlePlay(20.1);

            Assert.That(duplicate, Is.False);
            Assert.That(navigation.ActiveTransaction, Is.SameAs(firstTransaction));
            Assert.That(view.State, Is.EqualTo(WelcomePresentationState.Welcome));
        }

        [Test]
        public void AdultTapRequestsDashboardGateOnceWithoutHoldState()
        {
            var session = new SessionState();
            var navigation = new NavigationAuthority(RouteId.Welcome);
            var sink = new RecordingAdultGateSink();
            var controller = new WelcomeController(
                session,
                navigation,
                new RecordingWelcomeView(),
                sink);

            Assert.That(controller.HandleAdult(), Is.True);
            Assert.That(controller.HandleAdult(), Is.False);

            Assert.That(sink.Count, Is.EqualTo(1));
            Assert.That(sink.LastRequest.Purpose, Is.EqualTo(AdultGatePurpose.Dashboard));
            Assert.That(sink.LastRequest.Origin, Is.EqualTo(AdultGateRequestOrigin.Welcome));
            Assert.That(sink.LastRequest.ReturnRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(sink.LastRequest.ReturnStateToken, Is.EqualTo(Guid.Empty));
            Assert.That(sink.LastRequest.ReturnPresentationId, Is.EqualTo("welcome"));
            Assert.That(navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void SoundTapChangesOnlyVolatileSessionSound()
        {
            var session = new SessionState();
            var controller = new WelcomeController(
                session,
                new NavigationAuthority(RouteId.Welcome),
                new RecordingWelcomeView());

            var enabled = controller.HandleSound();

            Assert.That(enabled, Is.False);
            Assert.That(session.SoundEnabled, Is.False);
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void CaptionAndMotionTapsPersistOnlyApprovedAccessibilityKeysAndOsStillWins()
        {
            var backend = new RecordingPreferenceBackend();
            var store = new AccessibilityPreferenceStore(backend);
            var accessibility = new AccessibilityState(
                alwaysReduce: false,
                captionsEnabled: false);
            var controller = new WelcomeController(
                new SessionState(),
                new NavigationAuthority(RouteId.Welcome),
                new RecordingWelcomeView(),
                adultGateSink: null,
                accessibility: accessibility,
                preferenceStore: store);

            Assert.That(controller.HandleCaptions(), Is.True);
            Assert.That(controller.HandleMotion(), Is.True);
            accessibility.SetOsState(ReduceMotionOsState.Reduced);
            Assert.That(controller.HandleMotion(), Is.False);

            CollectionAssert.AreEquivalent(
                new[]
                {
                    AccessibilityPreferenceStore.AlwaysReduceKey,
                    AccessibilityPreferenceStore.CaptionsEnabledKey,
                },
                backend.Keys);
            Assert.That(accessibility.CaptionsEnabled, Is.True);
            Assert.That(accessibility.AlwaysReduce, Is.False);
            Assert.That(accessibility.EffectiveReducedMotion, Is.True);
        }

        private sealed class RecordingWelcomeView : IWelcomeView
        {
            public WelcomePresentationState State { get; private set; } =
                WelcomePresentationState.Welcome;

            public void Show(WelcomePresentationState state)
            {
                State = state;
            }
        }

        private sealed class RecordingAdultGateSink : IAdultGateRequestSink
        {
            public int Count { get; private set; }
            public AdultGateRequest LastRequest { get; private set; }

            public bool TryRequest(AdultGateRequest request)
            {
                Count++;
                LastRequest = request;
                return true;
            }
        }

        private sealed class RecordingPreferenceBackend : IPreferenceBackend
        {
            private readonly Dictionary<string, bool> _values =
                new Dictionary<string, bool>();

            public IEnumerable<string> Keys => _values.Keys;

            public bool GetBool(string key, bool defaultValue)
            {
                return _values.TryGetValue(key, out var value) ? value : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
            }

            public void Save()
            {
            }
        }
    }
}
