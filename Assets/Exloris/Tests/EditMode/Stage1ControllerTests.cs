using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Stage1;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1ControllerTests
    {
        [Test]
        public void InvalidEntryIsRejectedWithoutSessionMutation()
        {
            var context = CreateWithoutEntryTransaction();
            var beforeRoute = context.Navigation.CurrentRoute;
            var beforeLocation = context.Session.CurrentOpenSessionLocation;

            Assert.That(context.Controller.Enter(Guid.NewGuid(), 2d, false), Is.False);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Hidden));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(beforeRoute));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(beforeLocation));

            var request = context.Navigation.Request(RouteId.Stage1, 3d);
            Assert.That(request.Accepted, Is.True);
            Assert.That(context.Controller.Enter(Guid.NewGuid(), 4d, false), Is.False);
            Assert.That(context.Navigation.ActiveTransaction.Id, Is.EqualTo(request.Transaction.Id));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Boat1));
        }

        [Test]
        public void ValidEntryConsumesCurrentBoat1TransactionExactlyOnce()
        {
            var context = Create();

            Assert.That(context.Controller.Enter(context.EntryTransactionId, 2d, false), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Intro));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Controller.Snapshot.MarkerAttached, Is.False);
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(0.5d));
            Assert.That(context.Controller.Enter(context.EntryTransactionId, 3d, false), Is.False);
        }

        [Test]
        public void IntroControlsUseCurrentTokensWithoutDuplication()
        {
            var context = Entered();
            var token = context.Controller.CurrentTokenId;
            var instruction = context.Controller.CurrentInstructionTokenId;

            Assert.That(context.Controller.HandleRepeat(Guid.NewGuid(), 2.5d), Is.False);
            Assert.That(context.Controller.HandleRepeat(token, 2.5d), Is.True);
            Assert.That(context.Controller.CurrentInstructionTokenId, Is.Not.EqualTo(instruction));
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Intro));
            Assert.That(context.Controller.HandlePlay(Guid.NewGuid(), 3d), Is.False);
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Playing));
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.False);
        }

        [Test]
        public void TapAndDragAttachmentHaveParity()
        {
            var tap = Playing();
            var tapInteraction = Guid.NewGuid();
            Assert.That(tap.Controller.BeginTapAttach(tap.Controller.CurrentTokenId, tapInteraction, true, 4d), Is.True);
            Assert.That(tap.Controller.CompleteTapAttach(tap.Controller.CurrentTokenId, tapInteraction, true, 4.1d), Is.True);

            var drag = Playing();
            Assert.That(drag.Controller.HandleDragAttach(drag.Controller.CurrentTokenId, Guid.NewGuid(), true, true, 4.1d), Is.True);

            Assert.That(tap.Controller.Snapshot.MarkerAttached, Is.True);
            Assert.That(drag.Controller.Snapshot.MarkerAttached, Is.True);
            Assert.That(tap.Controller.Snapshot.MarkerPosition, Is.EqualTo(drag.Controller.Snapshot.MarkerPosition));
            Assert.That(tap.Session.Observations, Is.Empty);
            Assert.That(drag.Session.Observations, Is.Empty);
        }

        [Test]
        public void NearMissDuplicateAndSameFrameAttachmentRacesAcceptOneOwner()
        {
            var context = Playing();
            var token = context.Controller.CurrentTokenId;
            var interaction = Guid.NewGuid();
            Assert.That(context.Controller.BeginTapAttach(token, interaction, false, 4d), Is.False);
            Assert.That(context.Controller.CompleteTapAttach(token, interaction, true, 4d), Is.False);
            Assert.That(context.Controller.HandleDragAttach(token, Guid.NewGuid(), true, false, 4d), Is.False);

            var winning = Guid.NewGuid();
            Assert.That(context.Controller.BeginTapAttach(token, winning, true, 5d), Is.True);
            Assert.That(context.Controller.CompleteTapAttach(token, winning, true, 5d), Is.True);
            Assert.That(context.Controller.HandleDragAttach(token, Guid.NewGuid(), true, true, 5d), Is.False);
            Assert.That(context.Controller.CompleteTapAttach(token, winning, true, 5d), Is.False);
            Assert.That(context.Controller.Snapshot.MarkerAttached, Is.True);
        }

        [Test]
        public void UnattachedMovesOnlyRequestAttachCue()
        {
            var context = Playing();
            var token = context.Controller.CurrentTokenId;
            var before = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleArrow(token, 1, 4d), Is.EqualTo(Stage1MoveResult.AttachCue));
            Assert.That(context.Controller.HandleDrag(token, 0.9d, 4d), Is.EqualTo(Stage1MoveResult.AttachCue));
            Assert.That(context.Controller.HandleConfirm(token, 4d), Is.False);
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(before.MarkerPosition));
            Assert.That(context.Session.Observations, Is.Empty);
        }

        [Test]
        public void ArrowsUseFrozenBoundedStep()
        {
            var context = Attached();
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleArrow(token, 1, 4d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(0.75d));
            Assert.That(context.Controller.HandleArrow(token, 1, 4.1d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(1d));
            Assert.That(context.Controller.HandleArrow(token, 1, 4.2d), Is.EqualTo(Stage1MoveResult.AtBoundary));
            Assert.That(context.Controller.HandleArrow(token, 0, 4.3d), Is.EqualTo(Stage1MoveResult.Rejected));
            Assert.That(context.Controller.HandleArrow(Guid.NewGuid(), -1, 4.3d), Is.EqualTo(Stage1MoveResult.Rejected));
        }

        [Test]
        public void DragClampsContinuouslyWithoutSnapping()
        {
            var context = Attached();
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleDrag(token, 0.333d, 4d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(0.333d));
            Assert.That(context.Controller.HandleDrag(token, -4d, 4.1d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.Zero);
            Assert.That(context.Controller.HandleDrag(token, 5d, 4.2d), Is.EqualTo(Stage1MoveResult.Moved));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(1d));
            Assert.That(context.Controller.HandleDrag(token, double.NaN, 4.3d), Is.EqualTo(Stage1MoveResult.Rejected));
        }

        [TestCase(0d, Stage1OutcomeKind.OutsideLow)]
        [TestCase(0.249999d, Stage1OutcomeKind.OutsideLow)]
        [TestCase(0.25d, Stage1OutcomeKind.InRange)]
        [TestCase(0.5d, Stage1OutcomeKind.InRange)]
        [TestCase(0.75d, Stage1OutcomeKind.InRange)]
        [TestCase(0.750001d, Stage1OutcomeKind.OutsideHigh)]
        [TestCase(1d, Stage1OutcomeKind.OutsideHigh)]
        public void MinimumBoundaryMiddleAndMaximumClassificationTable(double position, Stage1OutcomeKind expected)
        {
            Assert.That(Stage1Controller.Classify(position), Is.EqualTo(expected));
        }

        [Test]
        public void ConfirmCreatesOneOutcomeToken()
        {
            var context = Attached();
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleConfirm(token, 5d), Is.True);
            var outcomeToken = context.Controller.CurrentOutcomeTokenId;
            Assert.That(outcomeToken, Is.Not.EqualTo(Guid.Empty));
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomePlaying));
            Assert.That(context.Controller.Snapshot.Outcome, Is.EqualTo(Stage1OutcomeKind.InRange));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(1));
            Assert.That(context.Controller.HandleConfirm(token, 5d), Is.False);
            Assert.That(context.Controller.CurrentOutcomeTokenId, Is.EqualTo(outcomeToken));
        }

        [Test]
        public void OutcomeControlsUnlockOnlyAfterCurrentSemanticBeatCompletes()
        {
            var context = ConfirmedAt(0.5d);
            var token = context.Controller.CurrentTokenId;
            var outcome = context.Controller.CurrentOutcomeTokenId;

            Assert.That(context.Controller.HandleContinue(token, 5.1d), Is.False);
            Assert.That(context.Controller.AdvanceOutcome(token, Guid.NewGuid(), 8.2d), Is.False);
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 8.199d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomePlaying));
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 8.2d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomeReady));
            Assert.That(context.Session.Observations.Count(e => e.Type == ObservationEventType.Stage1Outcome), Is.EqualTo(1));
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 8.3d), Is.False);
        }

        [Test]
        public void TryAgainUsesThreeFrozenSupportTiers()
        {
            var context = CompletedOutcomeAt(0.1d);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleTryAgain(token, 9d), Is.True);
            Assert.That(context.Controller.Snapshot.SupportTier, Is.EqualTo(1));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(0.1d));
            CompleteOutsideAttempt(context, 10d, 13.2d);
            Assert.That(context.Controller.HandleTryAgain(token, 14d), Is.True);
            Assert.That(context.Controller.Snapshot.SupportTier, Is.EqualTo(2));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(0.1d));
            CompleteOutsideAttempt(context, 15d, 18.2d);
            Assert.That(context.Controller.HandleTryAgain(token, 19d), Is.True);
            Assert.That(context.Controller.Snapshot.SupportTier, Is.EqualTo(3));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.Zero);
            Assert.That(context.Controller.Snapshot.MarkerAttached, Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Playing));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void WatchAgainDoesNotDuplicateFacts()
        {
            var context = CompletedOutcomeAt(0.5d);
            var token = context.Controller.CurrentTokenId;
            var before = context.Controller.Snapshot;
            var observations = context.Session.Observations.Count;

            Assert.That(context.Controller.HandleWatchAgain(token, 9d), Is.True);
            var replayToken = context.Controller.CurrentOutcomeTokenId;
            Assert.That(replayToken, Is.Not.EqualTo(before.OutcomeTokenId));
            Assert.That(context.Controller.AdvanceOutcome(token, replayToken, 12.2d), Is.True);
            Assert.That(context.Session.Observations.Count, Is.EqualTo(observations));
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(before.MarkerPosition));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(before.OptionCount));
            Assert.That(context.Controller.Snapshot.SupportTier, Is.EqualTo(before.SupportTier));
        }

        [Test]
        public void HelpRepeatPauseAndLeaveRoundTripExactOutcomeBeat()
        {
            var context = ConfirmedAt(0.5d);
            var token = context.Controller.CurrentTokenId;
            var outcome = context.Controller.CurrentOutcomeTokenId;
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 6d), Is.True);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandlePause(6d), Is.True);
            Assert.That(context.Pause.Captured.Equals(captured), Is.True);
            Assert.That(context.Controller.RestoreFromPause(captured, 16d), Is.True);
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 18.199d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomePlaying));
            Assert.That(context.Controller.AdvanceOutcome(token, outcome, 18.2d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.OutcomeReady));

            var ready = context.Controller.Snapshot;
            Assert.That(context.Controller.HandleHome(19d), Is.True);
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Controller.RestoreFromLeave(ready, 20d), Is.True);
            Assert.That(context.Controller.Snapshot.Equals(ready), Is.True);
        }

        [Test]
        public void ContinueCommitsFactsAndRequestsOnlyBoat2()
        {
            var context = CompletedOutcomeAt(0.5d);
            var token = context.Controller.CurrentTokenId;

            Assert.That(context.Controller.HandleContinue(token, 9d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Navigating));
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Boat2));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage1));
            Assert.That(context.Session.Observations.Count(e => e.Type == ObservationEventType.Stage1Option), Is.EqualTo(1));
            Assert.That(context.Session.Observations.Count(e => e.Type == ObservationEventType.Complete), Is.EqualTo(1));
            Assert.That(context.Controller.HandleContinue(token, 10d), Is.False);
        }

        private static void CompleteOutsideAttempt(Context context, double confirmAt, double completeAt)
        {
            Assert.That(context.Controller.HandleConfirm(context.Controller.CurrentTokenId, confirmAt), Is.True);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, completeAt), Is.True);
        }

        private static Context ConfirmedAt(double position)
        {
            var context = Attached();
            Assert.That(
                context.Controller.HandleDrag(context.Controller.CurrentTokenId, position, 4.5d),
                Is.EqualTo(position.Equals(0.5d) ? Stage1MoveResult.AtBoundary : Stage1MoveResult.Moved));
            Assert.That(context.Controller.HandleConfirm(context.Controller.CurrentTokenId, 5d), Is.True);
            return context;
        }

        private static Context CompletedOutcomeAt(double position)
        {
            var context = ConfirmedAt(position);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, 8.2d), Is.True);
            return context;
        }

        private static Context Attached()
        {
            var context = Playing();
            Assert.That(context.Controller.HandleDragAttach(context.Controller.CurrentTokenId, Guid.NewGuid(), true, true, 3.5d), Is.True);
            return context;
        }

        private static Context Playing()
        {
            var context = Entered();
            Assert.That(context.Controller.HandlePlay(context.Controller.CurrentTokenId, 3d), Is.True);
            return context;
        }

        private static Context Entered()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(context.EntryTransactionId, 2d, false), Is.True);
            return context;
        }

        private static Context Create()
        {
            var context = CreateWithoutEntryTransaction();
            var request = context.Navigation.Request(RouteId.Stage1, 1d);
            Assert.That(request.Accepted, Is.True);
            context.EntryTransactionId = request.Transaction.Id;
            return context;
        }

        private static Context CreateWithoutEntryTransaction()
        {
            var ids = new Queue<Guid>(Enumerable.Range(1, 64).Select(index => Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}")));
            var session = new SessionState();
            session.Begin(RouteId.Boat1);
            var navigation = new NavigationAuthority(RouteId.Boat1, () => ids.Dequeue());
            var view = new FakeView();
            var pause = new FakePause();
            var leave = new FakeLeave();
            return new Context(session, navigation, view, pause, leave,
                new Stage1Controller(session, navigation, view, pause, leave, () => ids.Dequeue()));
        }

        private sealed class Context
        {
            internal Context(SessionState session, NavigationAuthority navigation, FakeView view, FakePause pause, FakeLeave leave, Stage1Controller controller)
            {
                Session = session; Navigation = navigation; View = view; Pause = pause; Leave = leave; Controller = controller;
            }
            internal SessionState Session { get; }
            internal NavigationAuthority Navigation { get; }
            internal FakeView View { get; }
            internal FakePause Pause { get; }
            internal FakeLeave Leave { get; }
            internal Stage1Controller Controller { get; }
            internal Guid EntryTransactionId { get; set; }
        }

        private sealed class FakeView : IStage1View
        {
            internal Stage1Snapshot LastSnapshot;
            internal bool InputReady;
            public void Present(Stage1Snapshot snapshot, bool reducedMotion) => LastSnapshot = snapshot;
            public void SetInputReady(bool ready) => InputReady = ready;
        }

        private sealed class FakePause : IStage1PauseSink
        {
            internal Stage1Snapshot Captured;
            public bool TryShow(Stage1Snapshot snapshot) { Captured = snapshot; return true; }
        }

        private sealed class FakeLeave : ILeaveConfirmationSink
        {
            internal RouteId Caller;
            public bool TryShow(RouteId caller) { Caller = caller; return true; }
        }
    }
}
