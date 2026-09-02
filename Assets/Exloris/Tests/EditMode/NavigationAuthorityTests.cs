using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class NavigationAuthorityTests
    {
        private static readonly RouteId[] ExpectedRoute =
        {
            RouteId.StartupLoading,
            RouteId.IntroVideo,
            RouteId.Welcome,
            RouteId.AvatarSelect,
            RouteId.AvatarPreview,
            RouteId.JourneyOverview,
            RouteId.Boat1,
            RouteId.Stage1,
            RouteId.Boat2,
            RouteId.Stage2,
            RouteId.Boat3,
            RouteId.Stage3,
            RouteId.Completion,
            RouteId.AdultGate,
            RouteId.ParentReport,
        };

        [Test]
        public void FrozenRouteHasExactlyFifteenScreensInOrder()
        {
            CollectionAssert.AreEqual(ExpectedRoute, RouteGraph.FrozenRoute);
        }

        [Test]
        public void FirstRequestWinsAndDuplicateRequestIsRejectedWhileLocked()
        {
            var ids = new Queue<Guid>(new[] { Guid.Parse("00000000-0000-0000-0000-000000000001") });
            var authority = CreateAuthority(ids);

            var first = authority.Request(RouteId.IntroVideo, 1.0);
            var duplicate = authority.Request(RouteId.IntroVideo, 1.01);

            Assert.That(first.Accepted, Is.True);
            Assert.That(first.Transaction.Id, Is.EqualTo(Guid.Parse("00000000-0000-0000-0000-000000000001")));
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.Failure, Is.EqualTo(NavigationRequestFailure.Locked));
            Assert.That(authority.IsLocked, Is.True);
        }

        [Test]
        public void CompletionConsumesTransactionOnceAndAdvancesRoute()
        {
            var ids = new Queue<Guid>(new[] { Guid.Parse("00000000-0000-0000-0000-000000000002") });
            var authority = CreateAuthority(ids);
            var request = authority.Request(RouteId.IntroVideo, 2.0);

            var first = authority.Complete(request.Transaction.Id);
            var duplicate = authority.Complete(request.Transaction.Id);

            Assert.That(first, Is.EqualTo(NavigationCompletion.Completed));
            Assert.That(duplicate, Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(authority.IsLocked, Is.False);
        }

        [Test]
        public void IllegalSkipIsRejectedWithoutCreatingTransaction()
        {
            var authority = CreateAuthority(new Queue<Guid>());

            var result = authority.Request(RouteId.Welcome, 3.0);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Failure, Is.EqualTo(NavigationRequestFailure.IllegalRoute));
            Assert.That(authority.ActiveTransaction, Is.Null);
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
        }

        [Test]
        public void MismatchedCompletionCannotUnlockOrAdvance()
        {
            var ids = new Queue<Guid>(new[] { Guid.Parse("00000000-0000-0000-0000-000000000003") });
            var authority = CreateAuthority(ids);
            var request = authority.Request(RouteId.IntroVideo, 4.0);

            var result = authority.Complete(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));

            Assert.That(result, Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(authority.ActiveTransaction, Is.SameAs(request.Transaction));
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
        }

        [Test]
        public void TenSecondTimeoutShowsRecoveryAndRejectsLateCompletionForever()
        {
            var ids = new Queue<Guid>(new[] { Guid.Parse("00000000-0000-0000-0000-000000000004") });
            var authority = CreateAuthority(ids);
            var request = authority.Request(RouteId.IntroVideo, 5.0);

            Assert.That(authority.CheckTimeout(14.999), Is.False);
            Assert.That(authority.CheckTimeout(15.0), Is.True);

            Assert.That(authority.Recovery, Is.Not.Null);
            Assert.That(authority.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Timeout));
            Assert.That(authority.Recovery.Caller, Is.EqualTo(RouteId.StartupLoading));
            Assert.That(authority.Recovery.Destination, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(authority.Complete(request.Transaction.Id), Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
        }

        [Test]
        public void ExactTimeoutAcceptsFractionalStartWithoutBinarySubtractionDrift()
        {
            const double startedAt = 6.000039998800036d;
            var authority = new NavigationAuthority(RouteId.JourneyOverview);
            authority.Request(RouteId.Boat1, startedAt);

            Assert.That(authority.CheckTimeout(startedAt + NavigationAuthority.TimeoutSeconds),
                Is.True);
            Assert.That(authority.Recovery, Is.Not.Null);
            Assert.That(authority.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Timeout));
        }

        [Test]
        public void RetryCreatesNewTransactionForStoredCallerAndDestination()
        {
            var firstId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var retryId = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var ids = new Queue<Guid>(new[] { firstId, retryId });
            var authority = CreateAuthority(ids);
            authority.Request(RouteId.IntroVideo, 6.0);
            authority.CheckTimeout(16.0);

            var retry = authority.Retry(16.1);

            Assert.That(retry.Accepted, Is.True);
            Assert.That(retry.Transaction.Id, Is.EqualTo(retryId));
            Assert.That(retry.Transaction.Caller, Is.EqualTo(RouteId.StartupLoading));
            Assert.That(retry.Transaction.Destination, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(authority.Recovery, Is.Null);
            Assert.That(authority.Complete(firstId), Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(authority.IsLocked, Is.True);
        }

        [Test]
        public void HomeFromRecoveryUsesNewTransactionAndRoutesToWelcome()
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000007"),
                Guid.Parse("00000000-0000-0000-0000-000000000008"),
            });
            var authority = CreateAuthority(ids);
            authority.Request(RouteId.IntroVideo, 7.0);
            authority.CheckTimeout(17.0);

            var home = authority.Home(17.1);

            Assert.That(home.Accepted, Is.True);
            Assert.That(home.Transaction.Destination, Is.EqualTo(RouteId.Welcome));
            Assert.That(authority.Complete(home.Transaction.Id), Is.EqualTo(NavigationCompletion.Completed));
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.Welcome));
        }

        [Test]
        public void InterruptionCancelsActiveTransactionAndLateCallbackCannotAdvance()
        {
            var id = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var ids = new Queue<Guid>(new[] { id });
            var authority = CreateAuthority(ids);
            authority.Request(RouteId.IntroVideo, 8.0);

            var cancelled = authority.Interrupt();

            Assert.That(cancelled, Is.True);
            Assert.That(authority.Recovery.Reason, Is.EqualTo(NavigationFailureReason.Interrupted));
            Assert.That(authority.Complete(id), Is.EqualTo(NavigationCompletion.RejectedStale));
            Assert.That(authority.CurrentRoute, Is.EqualTo(RouteId.StartupLoading));
        }

        [Test]
        public void SessionResumeCanOnlyStartFromWelcome()
        {
            var authority = new NavigationAuthority(RouteId.AvatarSelect);

            var result = authority.RequestResume(RouteId.Stage2, 18.0);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Failure, Is.EqualTo(NavigationRequestFailure.IllegalRoute));
            Assert.That(authority.ActiveTransaction, Is.Null);
        }

        [Test]
        public void SessionResumeKeepsFirstTransactionAndRejectsDuplicate()
        {
            var firstId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var ids = new Queue<Guid>(new[]
            {
                firstId,
                Guid.Parse("00000000-0000-0000-0000-000000000011"),
            });
            var authority = new NavigationAuthority(RouteId.Welcome, () => ids.Dequeue());
            var first = authority.RequestResume(RouteId.Stage2, 19.0);

            var duplicate = authority.RequestResume(RouteId.Stage2, 19.1);

            Assert.That(first.Accepted, Is.True);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(duplicate.Failure, Is.EqualTo(NavigationRequestFailure.Locked));
            Assert.That(authority.ActiveTransaction, Is.SameAs(first.Transaction));
        }

        [Test]
        public void SessionResumeIsRejectedWhileRecoveryIsPending()
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000012"),
                Guid.Parse("00000000-0000-0000-0000-000000000013"),
            });
            var authority = new NavigationAuthority(RouteId.Welcome, () => ids.Dequeue());
            authority.Request(RouteId.AvatarSelect, 20.0);
            authority.CheckTimeout(30.0);

            var result = authority.RequestResume(RouteId.Stage3, 30.1);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Failure, Is.EqualTo(NavigationRequestFailure.RecoveryPending));
            Assert.That(authority.ActiveTransaction, Is.Null);
            Assert.That(authority.Recovery, Is.Not.Null);
        }

        [Test]
        public void ChooseOtherAllowsOnlyPreviewToSelectReturn()
        {
            var accepted = new NavigationAuthority(
                RouteId.AvatarPreview,
                () => Guid.Parse("00000000-0000-0000-0000-000000000014"));
            var wrongDestination = new NavigationAuthority(RouteId.AvatarPreview);
            var wrongCaller = new NavigationAuthority(RouteId.AvatarSelect);

            var result = accepted.RequestReturn(RouteId.AvatarSelect, 21.0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Transaction.Caller, Is.EqualTo(RouteId.AvatarPreview));
            Assert.That(result.Transaction.Destination, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(wrongDestination.RequestReturn(RouteId.Welcome, 21.1).Failure,
                Is.EqualTo(NavigationRequestFailure.IllegalRoute));
            Assert.That(wrongCaller.RequestReturn(RouteId.Welcome, 21.2).Failure,
                Is.EqualTo(NavigationRequestFailure.IllegalRoute));
        }

        [TestCase(RouteId.AvatarSelect)]
        [TestCase(RouteId.AvatarPreview)]
        [TestCase(RouteId.JourneyOverview)]
        [TestCase(RouteId.Stage2)]
        public void HomeAllowsOnlyApprovedActiveStageCallers(RouteId caller)
        {
            var authority = new NavigationAuthority(
                caller,
                () => Guid.Parse("00000000-0000-0000-0000-000000000015"));

            var result = authority.RequestHome(22.0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Transaction.Caller, Is.EqualTo(caller));
            Assert.That(result.Transaction.Destination, Is.EqualTo(RouteId.Welcome));
        }

        [TestCase(RouteId.Welcome)]
        [TestCase(RouteId.Stage1)]
        public void HomeRejectsEveryUnauthorizedCaller(RouteId caller)
        {
            var authority = new NavigationAuthority(caller);

            var result = authority.RequestHome(23.0);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Failure, Is.EqualTo(NavigationRequestFailure.IllegalRoute));
            Assert.That(authority.ActiveTransaction, Is.Null);
        }

        [Test]
        public void ReturnAndHomeRejectDuplicatesWhileLocked()
        {
            var authority = new NavigationAuthority(
                RouteId.AvatarPreview,
                () => Guid.Parse("00000000-0000-0000-0000-000000000016"));

            var first = authority.RequestReturn(RouteId.AvatarSelect, 24.0);
            var duplicateReturn = authority.RequestReturn(RouteId.AvatarSelect, 24.1);
            var duplicateHome = authority.RequestHome(24.2);

            Assert.That(first.Accepted, Is.True);
            Assert.That(duplicateReturn.Failure, Is.EqualTo(NavigationRequestFailure.Locked));
            Assert.That(duplicateHome.Failure, Is.EqualTo(NavigationRequestFailure.Locked));
        }

        [Test]
        public void ReturnAndHomeRejectRequestsWhileRecoveryIsPending()
        {
            var ids = new Queue<Guid>(new[]
            {
                Guid.Parse("00000000-0000-0000-0000-000000000017"),
            });
            var authority = new NavigationAuthority(RouteId.AvatarSelect, () => ids.Dequeue());
            authority.Request(RouteId.AvatarPreview, 25.0);
            authority.CheckTimeout(35.0);

            Assert.That(authority.RequestReturn(RouteId.AvatarSelect, 35.1).Failure,
                Is.EqualTo(NavigationRequestFailure.RecoveryPending));
            Assert.That(authority.RequestHome(35.2).Failure,
                Is.EqualTo(NavigationRequestFailure.RecoveryPending));
            Assert.That(authority.ActiveTransaction, Is.Null);
            Assert.That(authority.Recovery, Is.Not.Null);
        }

        private static NavigationAuthority CreateAuthority(Queue<Guid> ids)
        {
            return new NavigationAuthority(RouteId.StartupLoading, () => ids.Dequeue());
        }
    }
}
