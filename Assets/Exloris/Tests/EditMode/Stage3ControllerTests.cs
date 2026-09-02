using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3ControllerTests
    {
        [Test]
        public void InvalidOrMissingRiverEntryFailsClosedWithoutMutation()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(Guid.NewGuid(), new Stage3EntryContext(Stage2River.Low), 2d, false), Is.False);
            Assert.That(context.Controller.Enter(context.EntryTransactionId, new Stage3EntryContext(Stage2River.None), 2d, false), Is.False);
            Assert.That(context.Controller.Enter(context.EntryTransactionId, new Stage3EntryContext((Stage2River)99), 2d, false), Is.False);
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Boat3));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Boat3));
        }

        [Test]
        public void ValidEntryConsumesCurrentBoat3TransactionExactlyOnceAndRetainsRiver()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(context.EntryTransactionId, new Stage3EntryContext(Stage2River.Middle), 2d, false), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.Intro));
            Assert.That(context.Controller.Snapshot.SelectedRiver, Is.EqualTo(Stage2River.Middle));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Controller.Enter(context.EntryTransactionId, new Stage3EntryContext(Stage2River.Middle), 3d, false), Is.False);
        }

        [Test]
        public void IntroPlayAndRepeatRequireCurrentTokens()
        {
            var context = Entered(Stage2River.Low);
            var token = context.Controller.CurrentTokenId;
            var instruction = context.Controller.CurrentInstructionTokenId;
            Assert.That(context.Controller.HandleRepeat(Guid.NewGuid(), 2.5d), Is.False);
            Assert.That(context.Controller.HandleRepeat(token, 2.5d), Is.True);
            Assert.That(context.Controller.CurrentInstructionTokenId, Is.Not.EqualTo(instruction));
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.ChoosingPlan));
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.False);
        }

        [TestCase(Stage3Plan.Grain)]
        [TestCase(Stage3Plan.Channel)]
        [TestCase(Stage3Plan.Boat)]
        public void PlansAreEqualExplicitChoicesWithOneInteractionOwner(Stage3Plan plan)
        {
            var context = Choosing(Stage2River.Low);
            var interaction = Guid.NewGuid();
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, plan, interaction, 4d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.OutcomePlaying));
            Assert.That(context.Controller.Snapshot.SelectedPlan, Is.EqualTo(plan));
            Assert.That(context.Controller.Snapshot.OutcomeElapsedSeconds, Is.Zero);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, plan, interaction, 4d), Is.False);
        }

        [Test]
        public void AllNineRiverPlanPairsMapToExactBenefitAndRemainingNeed()
        {
            foreach (var river in Rivers)
            foreach (var plan in Plans)
            {
                var context = Choosing(river);
                Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, plan, Guid.NewGuid(), 4d), Is.True, river + "/" + plan);
                var key = context.Controller.Snapshot.CurrentOutcome;
                Assert.That(key.River, Is.EqualTo(river));
                Assert.That(key.Plan, Is.EqualTo(plan));
                Assert.That(key.Benefit, Is.EqualTo(Benefit(plan)));
                Assert.That(key.Need, Is.EqualTo(Need(plan)));
            }
        }

        [Test]
        public void OutcomeUsesExactSemanticDurationAndRejectsStaleOrBackwardTime()
        {
            var context = Playing(Stage2River.Middle, Stage3Plan.Channel, 4d);
            var outcome = context.Controller.CurrentOutcomeTokenId;
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, Guid.NewGuid(), 10.735d), Is.False);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, outcome, 10.734d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.OutcomePlaying));
            Assert.That(context.Controller.Snapshot.OutcomeElapsedSeconds, Is.EqualTo(6.734d).Within(0.000001d));
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, outcome, 9d), Is.False);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, outcome, 10.735d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.OutcomeReady));
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, outcome, 11d), Is.False);
        }

        [Test]
        public void FullAndReducedModesKeepIdenticalSemanticOutcome()
        {
            var full = Playing(Stage2River.High, Stage3Plan.Boat, 4d, false);
            var reduced = Playing(Stage2River.High, Stage3Plan.Boat, 4d, true);
            Complete(full, 10.735d);
            Complete(reduced, 10.735d);
            Assert.That(full.Controller.Snapshot.CurrentOutcome, Is.EqualTo(reduced.Controller.Snapshot.CurrentOutcome));
            Assert.That(full.Controller.Snapshot.OutcomeElapsedSeconds, Is.EqualTo(Stage3Controller.OutcomeSeconds));
            Assert.That(reduced.Controller.Snapshot.OutcomeElapsedSeconds, Is.EqualTo(Stage3Controller.OutcomeSeconds));
            Assert.That(full.View.LastReducedMotion, Is.False);
            Assert.That(reduced.View.LastReducedMotion, Is.True);
        }

        [Test]
        public void ReplayUsesFreshTokenAndNeverRecommitsFact()
        {
            var context = Playing(Stage2River.Low, Stage3Plan.Grain, 4d);
            Complete(context, 10.735d);
            var firstToken = context.Controller.CurrentOutcomeTokenId;
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Stage3Option), Is.EqualTo(1));
            Assert.That(context.Controller.HandleWatchAgain(context.Controller.CurrentTokenId, 11d), Is.True);
            Assert.That(context.Controller.CurrentOutcomeTokenId, Is.Not.EqualTo(firstToken));
            Complete(context, 17.735d);
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Stage3Option), Is.EqualTo(1));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(1));
        }

        [Test]
        public void AnotherPlanKeepsRiverAndChangeRiverRequiresExplicitChoice()
        {
            var context = Playing(Stage2River.Middle, Stage3Plan.Grain, 4d);
            Complete(context, 10.735d);
            Assert.That(context.Controller.HandleAnotherPlan(context.Controller.CurrentTokenId, 11d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.ChoosingPlan));
            Assert.That(context.Controller.Snapshot.SelectedRiver, Is.EqualTo(Stage2River.Middle));
            Assert.That(context.Controller.Snapshot.SelectedPlan, Is.EqualTo(Stage3Plan.None));

            Assert.That(context.Controller.HandleChangeRiver(context.Controller.CurrentTokenId, 12d), Is.False);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, Stage3Plan.Channel, Guid.NewGuid(), 12d), Is.True);
            Complete(context, 18.735d);
            Assert.That(context.Controller.HandleChangeRiver(context.Controller.CurrentTokenId, 19d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.ChoosingRiver));
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.None, Guid.NewGuid(), 20d), Is.False);
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.High, Guid.NewGuid(), 20d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.ChoosingPlan));
            Assert.That(context.Controller.Snapshot.SelectedRiver, Is.EqualTo(Stage2River.High));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(2));
        }

        [Test]
        public void RiverBackRestoresExactCompletedSnapshotWithoutReplayOrNewTokens()
        {
            var context = Playing(Stage2River.Middle, Stage3Plan.Channel, 4d);
            Complete(context, 10.735d);
            Assert.That(context.Controller.TrySetFocus(context.Controller.CurrentTokenId,
                Stage3StageView.AnotherRiverControlId), Is.True);
            var completed = context.Controller.Snapshot;
            var observationCount = context.Session.Observations.Count;

            Assert.That(context.Controller.HandleChangeRiver(context.Controller.CurrentTokenId, 11d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.ChoosingRiver));
            Assert.That(context.Controller.HandleRiverBack(context.Controller.CurrentTokenId), Is.True);

            Assert.That(context.Controller.Snapshot.Equals(completed), Is.True);
            Assert.That(context.Controller.CurrentOutcomeTokenId, Is.EqualTo(completed.OutcomeTokenId));
            Assert.That(context.Controller.CurrentInstructionTokenId, Is.EqualTo(completed.InstructionTokenId));
            Assert.That(context.Session.Observations.Count, Is.EqualTo(observationCount));
            Assert.That(context.Controller.HandleRiverBack(context.Controller.CurrentTokenId), Is.False);
        }

        [Test]
        public void HelpPauseAndLeaveRestoreExactOutcomeBeatAndFocus()
        {
            var context = Playing(Stage2River.Middle, Stage3Plan.Channel, 4d);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, 6d), Is.True);
            Assert.That(context.Controller.TrySetFocus(context.Controller.CurrentTokenId, "CTRL-S3-PAUSE"), Is.True);
            var captured = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHelp(context.Controller.CurrentTokenId), Is.True);
            Assert.That(context.Controller.RestoreFromHelp(captured), Is.True);
            Assert.That(context.Controller.Snapshot.Equals(captured), Is.True);

            Assert.That(context.Controller.HandlePause(6d), Is.True);
            Assert.That(context.Pause.Captured.Equals(captured), Is.True);
            Assert.That(context.Controller.RestoreFromPause(captured, 16d), Is.True);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, 20.734d), Is.True);
            Assert.That(context.Controller.AdvanceOutcome(context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, 20.735d), Is.True);
            var ready = context.Controller.Snapshot;

            Assert.That(context.Controller.HandleHome(19d), Is.True);
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Controller.RestoreFromLeave(ready, 20d), Is.True);
            Assert.That(context.Controller.Snapshot.Equals(ready), Is.True);
        }

        [Test]
        public void FinishRequestsOnlyCompletionOnceAfterOutcomeReady()
        {
            var context = Playing(Stage2River.High, Stage3Plan.Boat, 4d);
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 5d), Is.False);
            Complete(context, 10.735d);
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 11d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage3State.Navigating));
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Completion));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage3));
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Complete), Is.EqualTo(1));
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 12d), Is.False);
        }

        [Test]
        public void InvalidEnumsInteractionsAndClocksFailClosedWithoutConsumingInput()
        {
            var context = Choosing(Stage2River.Low);
            var interaction = Guid.NewGuid();
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, Stage3Plan.None, interaction, 4d), Is.False);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, (Stage3Plan)99, interaction, 4d), Is.False);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, Stage3Plan.Grain, interaction, double.NaN), Is.False);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, Stage3Plan.Grain, interaction, 4d), Is.True);
            Assert.That(context.Session.Observations, Is.Empty);
        }

        private static void Complete(Context context, double now)
        {
            Assert.That(context.Controller.AdvanceOutcome(
                context.Controller.CurrentTokenId, context.Controller.CurrentOutcomeTokenId, now), Is.True);
        }

        private static Context Playing(Stage2River river, Stage3Plan plan, double start, bool reducedMotion = false)
        {
            var context = Choosing(river, reducedMotion);
            Assert.That(context.Controller.HandleSelectPlan(context.Controller.CurrentTokenId, plan, Guid.NewGuid(), start), Is.True);
            return context;
        }

        private static Context Choosing(Stage2River river, bool reducedMotion = false)
        {
            var context = Entered(river, reducedMotion);
            Assert.That(context.Controller.HandlePlay(context.Controller.CurrentTokenId, 3d), Is.True);
            return context;
        }

        private static Context Entered(Stage2River river, bool reducedMotion = false)
        {
            var context = Create();
            Assert.That(context.Controller.Enter(context.EntryTransactionId, new Stage3EntryContext(river), 2d, reducedMotion), Is.True);
            return context;
        }

        private static Context Create()
        {
            var ids = new Queue<Guid>(Enumerable.Range(1, 256).Select(index => Guid.Parse($"30000000-0000-0000-0000-{index:000000000000}")));
            var session = new SessionState();
            session.Begin(RouteId.Boat3);
            var navigation = new NavigationAuthority(RouteId.Boat3, () => ids.Dequeue());
            var request = navigation.Request(RouteId.Stage3, 1d);
            Assert.That(request.Accepted, Is.True);
            var view = new FakeView();
            var pause = new FakePause();
            var leave = new FakeLeave();
            return new Context(session, navigation, request.Transaction.Id, view, pause, leave,
                new Stage3Controller(session, navigation, view, pause, leave, () => ids.Dequeue()));
        }

        private static Stage3Benefit Benefit(Stage3Plan plan)
        {
            switch (plan)
            {
                case Stage3Plan.Grain: return Stage3Benefit.Grain;
                case Stage3Plan.Channel: return Stage3Benefit.Channel;
                case Stage3Plan.Boat: return Stage3Benefit.Boat;
                default: return Stage3Benefit.None;
            }
        }

        private static Stage3Need Need(Stage3Plan plan)
        {
            switch (plan)
            {
                case Stage3Plan.Grain: return Stage3Need.WaterRoute;
                case Stage3Plan.Channel: return Stage3Need.GrainStorage;
                case Stage3Plan.Boat: return Stage3Need.FieldWater;
                default: return Stage3Need.None;
            }
        }

        private static readonly Stage2River[] Rivers = { Stage2River.Low, Stage2River.Middle, Stage2River.High };
        private static readonly Stage3Plan[] Plans = { Stage3Plan.Grain, Stage3Plan.Channel, Stage3Plan.Boat };

        private sealed class Context
        {
            internal Context(SessionState session, NavigationAuthority navigation, Guid entryTransactionId,
                FakeView view, FakePause pause, FakeLeave leave, Stage3Controller controller)
            { Session = session; Navigation = navigation; EntryTransactionId = entryTransactionId; View = view; Pause = pause; Leave = leave; Controller = controller; }
            internal SessionState Session { get; }
            internal NavigationAuthority Navigation { get; }
            internal Guid EntryTransactionId { get; }
            internal FakeView View { get; }
            internal FakePause Pause { get; }
            internal FakeLeave Leave { get; }
            internal Stage3Controller Controller { get; }
        }

        private sealed class FakeView : IStage3View
        {
            internal Stage3Snapshot LastSnapshot;
            internal bool LastReducedMotion;
            internal bool InputReady;
            public void Present(Stage3Snapshot snapshot, bool reducedMotion) { LastSnapshot = snapshot; LastReducedMotion = reducedMotion; }
            public void SetInputReady(bool ready) => InputReady = ready;
        }

        private sealed class FakePause : IStage3PauseSink
        {
            internal Stage3Snapshot Captured;
            public bool TryShow(Stage3Snapshot snapshot) { Captured = snapshot; return true; }
        }

        private sealed class FakeLeave : ILeaveConfirmationSink
        {
            internal RouteId Caller;
            public bool TryShow(RouteId caller) { Caller = caller; return true; }
        }
    }
}
