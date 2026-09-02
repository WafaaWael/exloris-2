using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Input;
using Exloris.Runtime.Lifecycle;
using Exloris.Runtime.Recovery;
using NUnit.Framework;

namespace Exloris.Tests.PlayMode
{
    public sealed class RecoveryFlowTests
    {
        [Test]
        public void LoadingKeepsInputLockedUntilSurfaceIsActuallyHidden()
        {
            var gate = new FakeInputGate();
            var presenter = new RecoveryPresenter(gate);

            presenter.BeginLoading();
            Assert.That(gate.Ready, Is.False);
            Assert.That(presenter.State, Is.EqualTo(RecoveryPresentationState.Loading));

            presenter.CompleteLoadingAndShowContent();
            Assert.That(gate.Ready, Is.True);
            Assert.That(presenter.State, Is.EqualTo(RecoveryPresentationState.Hidden));
        }

        [Test]
        public void TimeoutPreparesRecoveryAndLocksInputUntilOverlayIsVisible()
        {
            var setup = CreateSetup();
            setup.Navigation.Request(RouteId.IntroVideo, 0d);

            var timedOut = setup.Coordinator.Tick(10d);

            Assert.That(timedOut, Is.True);
            Assert.That(setup.Presenter.State, Is.EqualTo(RecoveryPresentationState.Preparing));
            Assert.That(setup.Gate.Ready, Is.False);
            Assert.That(setup.Presenter.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Timeout));

            setup.Presenter.MarkRecoveryVisible();
            Assert.That(setup.Gate.Ready, Is.True);
        }

        [Test]
        public void RetryRequiresVisibleOverlayAndCreatesNewTransactionId()
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222")
            });
            var setup = CreateSetup(() => ids.Dequeue());
            var original = setup.Navigation.Request(RouteId.IntroVideo, 0d).Transaction;
            setup.Coordinator.Tick(10d);

            var beforeVisible = setup.Coordinator.Retry(10.1d);
            Assert.That(beforeVisible.Accepted, Is.False);
            Assert.That(beforeVisible.Failure, Is.EqualTo(NavigationRequestFailure.RecoveryPending));

            setup.Presenter.MarkRecoveryVisible();
            var retried = setup.Coordinator.Retry(10.2d);

            Assert.That(retried.Accepted, Is.True);
            Assert.That(retried.Transaction.Id, Is.Not.EqualTo(original.Id));
            Assert.That(retried.Transaction.Caller, Is.EqualTo(original.Caller));
            Assert.That(retried.Transaction.Destination, Is.EqualTo(original.Destination));
            Assert.That(
                setup.Navigation.Complete(original.Id),
                Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(setup.Presenter.State, Is.EqualTo(RecoveryPresentationState.Loading));
        }

        [Test]
        public void HomeRecoveryCreatesMappedWelcomeTransaction()
        {
            var setup = CreateSetup();
            setup.Navigation.Request(RouteId.IntroVideo, 0d);
            setup.Coordinator.HandleAssetFailure();
            setup.Presenter.MarkRecoveryVisible();

            var home = setup.Coordinator.Home(1d);

            Assert.That(home.Accepted, Is.True);
            Assert.That(home.Transaction.Destination, Is.EqualTo(RouteId.Welcome));
        }

        [Test]
        public void InterruptionCancelsActiveTransactionAndPausesImmediately()
        {
            var setup = CreateSetup();
            var request = setup.Navigation.Request(RouteId.IntroVideo, 0d);

            setup.Coordinator.HandleInterruption();

            Assert.That(setup.Playback.PauseCalls, Is.EqualTo(1));
            Assert.That(setup.Navigation.ActiveTransaction, Is.Null);
            Assert.That(setup.Navigation.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Interrupted));
            Assert.That(
                setup.Navigation.Complete(request.Transaction.Id),
                Is.EqualTo(NavigationCompletion.RejectedStale));
        }

        [Test]
        public void FocusReturnNeverResumesWithoutMappedUserAction()
        {
            var setup = CreateSetup();
            setup.Navigation.Request(RouteId.IntroVideo, 0d);
            setup.Coordinator.HandleInterruption();

            setup.Coordinator.HandleFocusRestored();
            Assert.That(setup.Playback.ResumeCalls, Is.Zero);

            setup.Presenter.MarkRecoveryVisible();
            setup.Coordinator.Retry(1d);
            Assert.That(setup.Playback.ResumeCalls, Is.EqualTo(1));
        }

        [Test]
        public void InterruptionWithoutTransactionRequiresExplicitResumeAction()
        {
            var setup = CreateSetup();

            setup.Coordinator.HandleInterruption();
            setup.Coordinator.HandleFocusRestored();

            Assert.That(
                setup.Presenter.State,
                Is.EqualTo(RecoveryPresentationState.PausedPreparing));
            Assert.That(setup.Playback.ResumeCalls, Is.Zero);

            setup.Presenter.MarkRecoveryVisible();
            Assert.That(setup.Coordinator.ResumeCurrentScreenAfterUserAction(), Is.True);
            Assert.That(setup.Playback.ResumeCalls, Is.EqualTo(1));
            Assert.That(setup.Gate.Ready, Is.True);
        }

        [Test]
        public void AssetFailureUsesMappedRecoveryAndCannotDoubleCancel()
        {
            var setup = CreateSetup();
            setup.Navigation.Request(RouteId.IntroVideo, 0d);

            Assert.That(setup.Coordinator.HandleAssetFailure(), Is.True);
            Assert.That(setup.Coordinator.HandleAssetFailure(), Is.False);
            Assert.That(setup.Presenter.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Cancelled));
        }

        private static Setup CreateSetup(Func<Guid> idFactory = null)
        {
            var gate = new FakeInputGate();
            var presenter = new RecoveryPresenter(gate);
            var navigation = new NavigationAuthority(RouteId.StartupLoading, idFactory);
            var playback = new FakePlaybackParticipant();
            var coordinator = new AppLifecycleCoordinator(
                navigation,
                presenter,
                new[] { playback });
            return new Setup(gate, presenter, navigation, playback, coordinator);
        }

        private sealed class FakeInputGate : IInputGate
        {
            public bool Ready { get; private set; }

            public void SetVisibleStateReady(bool ready)
            {
                Ready = ready;
            }
        }

        private sealed class FakePlaybackParticipant : IAppLifecycleParticipant
        {
            public int PauseCalls { get; private set; }
            public int ResumeCalls { get; private set; }

            public void PauseAndDuckImmediately()
            {
                PauseCalls++;
            }

            public void ResumeAfterMappedUserAction()
            {
                ResumeCalls++;
            }
        }

        private sealed class Setup
        {
            public Setup(
                FakeInputGate gate,
                RecoveryPresenter presenter,
                NavigationAuthority navigation,
                FakePlaybackParticipant playback,
                AppLifecycleCoordinator coordinator)
            {
                Gate = gate;
                Presenter = presenter;
                Navigation = navigation;
                Playback = playback;
                Coordinator = coordinator;
            }

            public FakeInputGate Gate { get; }
            public RecoveryPresenter Presenter { get; }
            public NavigationAuthority Navigation { get; }
            public FakePlaybackParticipant Playback { get; }
            public AppLifecycleCoordinator Coordinator { get; }
        }
    }
}
