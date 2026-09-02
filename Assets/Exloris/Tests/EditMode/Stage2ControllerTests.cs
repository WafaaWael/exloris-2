using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Stage2;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2ControllerTests
    {
        [Test]
        public void InvalidEntryIsRejectedWithoutSessionMutation()
        {
            var context = CreateWithoutEntry();
            Assert.That(context.Controller.Enter(Guid.NewGuid(), 2d, false), Is.False);
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Boat2));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Boat2));
        }

        [Test]
        public void ValidEntryConsumesCurrentBoat2TransactionExactlyOnce()
        {
            var context = Create();
            Assert.That(context.Controller.Enter(context.EntryTransactionId, 2d, false), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Intro));
            Assert.That(context.Navigation.CurrentRoute, Is.EqualTo(RouteId.Stage2));
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage2));
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
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Choosing));
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.False);
        }

        [TestCase(Stage2River.Low)]
        [TestCase(Stage2River.Middle)]
        [TestCase(Stage2River.High)]
        public void RiverChoicesAreEqualAndAcceptOneOwner(Stage2River river)
        {
            var context = Choosing();
            var interaction = Guid.NewGuid();
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, river, interaction, 4d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Inspecting));
            Assert.That(context.Controller.Snapshot.SelectedRiver, Is.EqualTo(river));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(1));
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, river, interaction, 4d), Is.False);
        }

        [Test]
        public void AllNineRiverHotspotPairsMapExactly()
        {
            foreach (var river in Rivers)
            foreach (var hotspot in Hotspots)
            {
                var context = Inspecting(river);
                Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, hotspot, Guid.NewGuid(), 5d), Is.True, river + "/" + hotspot);
                Assert.That(context.Controller.Snapshot.CurrentHotspot, Is.EqualTo(hotspot));
                Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, context.Controller.CurrentConsequenceTokenId, 7.4d), Is.True);
                Assert.That(context.Controller.ObservedFor(river), Is.EqualTo(Flag(hotspot)));
                var fact = context.Session.Observations.Single(item => item.Type == ObservationEventType.Stage2Option);
                Assert.That(fact.ChoiceId, Is.EqualTo("choice_" + river.ToString().ToLowerInvariant()));
                Assert.That(fact.OutcomeId, Is.EqualTo("outcome_" + hotspot.ToString().ToLowerInvariant()));
            }
        }

        [Test]
        public void HotspotsCompleteOnceAndSetsStayIsolated()
        {
            var context = Inspecting(Stage2River.Low);
            CompleteHotspot(context, Stage2Hotspot.Field, 5d);
            CompleteHotspot(context, Stage2Hotspot.Field, 8d);
            Assert.That(context.Controller.ObservedFor(Stage2River.Low), Is.EqualTo(Stage2ObservedHotspots.Field));
            Assert.That(context.Controller.ObservedFor(Stage2River.Middle), Is.EqualTo(Stage2ObservedHotspots.None));
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Stage2Option), Is.EqualTo(1));
            Assert.That(context.Controller.NextUnvisitedHotspot, Is.EqualTo(Stage2Hotspot.Path));
        }

        [Test]
        public void CompareRetainsAllPerRiverObservedSets()
        {
            var context = Inspecting(Stage2River.Low);
            CompleteHotspot(context, Stage2Hotspot.Field, 5d);
            Assert.That(context.Controller.HandleCompare(context.Controller.CurrentTokenId, 8d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Choosing));
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.Middle, Guid.NewGuid(), 9d), Is.True);
            CompleteHotspot(context, Stage2Hotspot.Path, 10d);
            Assert.That(context.Controller.ObservedFor(Stage2River.Low), Is.EqualTo(Stage2ObservedHotspots.Field));
            Assert.That(context.Controller.ObservedFor(Stage2River.Middle), Is.EqualTo(Stage2ObservedHotspots.Path));
        }

        [Test]
        public void FinishRejectsEveryIncompleteCurrentRiverMask()
        {
            var context = Inspecting(Stage2River.High);
            Assert.That(context.Controller.CanFinish, Is.False);
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 5d), Is.False);
            CompleteHotspot(context, Stage2Hotspot.Field, 6d);
            CompleteHotspot(context, Stage2Hotspot.Path, 9d);
            Assert.That(context.Controller.CanFinish, Is.False);
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 12d), Is.False);
            Assert.That(context.Navigation.ActiveTransaction, Is.Null);
        }

        [Test]
        public void ConsequenceRequiresCurrentTokenAndFrozenSemanticDuration()
        {
            var context = Inspecting(Stage2River.Middle);
            Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, Stage2Hotspot.Boat, Guid.NewGuid(), 5d), Is.True);
            var consequence = context.Controller.CurrentConsequenceTokenId;
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, Guid.NewGuid(), 7.4d), Is.False);
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, consequence, 7.399d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.ConsequencePlaying));
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, consequence, 7.4d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Inspecting));
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, consequence, 8d), Is.False);
        }

        [Test]
        public void ConsequenceClockCannotMoveBackward()
        {
            var context = Inspecting(Stage2River.Middle);
            Assert.That(context.Controller.HandleHotspot(
                context.Controller.CurrentTokenId, Stage2Hotspot.Path, Guid.NewGuid(), 5d), Is.True);
            var token = context.Controller.CurrentConsequenceTokenId;
            Assert.That(context.Controller.AdvanceConsequence(
                context.Controller.CurrentTokenId, token, 6.8d), Is.True);
            Assert.That(context.Controller.Snapshot.ConsequenceElapsedSeconds, Is.EqualTo(1.8d).Within(0.000001d));

            Assert.That(context.Controller.AdvanceConsequence(
                context.Controller.CurrentTokenId, token, 5.3d), Is.False);
            Assert.That(context.Controller.Snapshot.ConsequenceElapsedSeconds, Is.EqualTo(1.8d).Within(0.000001d));
        }

        [Test]
        public void HelpPauseAndLeaveRoundTripExactConsequenceBeat()
        {
            var context = Inspecting(Stage2River.Middle);
            Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, Stage2Hotspot.Path, Guid.NewGuid(), 5d), Is.True);
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, context.Controller.CurrentConsequenceTokenId, 6d), Is.True);
            var captured = context.Controller.Snapshot;
            Assert.That(context.Controller.HandlePause(6d), Is.True);
            Assert.That(context.Pause.Captured.Equals(captured), Is.True);
            Assert.That(context.Controller.RestoreFromPause(captured, 16d), Is.True);
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, context.Controller.CurrentConsequenceTokenId, 17.399d), Is.True);
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, context.Controller.CurrentConsequenceTokenId, 17.4d), Is.True);
            var ready = context.Controller.Snapshot;
            Assert.That(context.Controller.HandleHome(18d), Is.True);
            Assert.That(context.Leave.Caller, Is.EqualTo(RouteId.Stage2));
            Assert.That(context.Controller.RestoreFromLeave(ready, 19d), Is.True);
            Assert.That(context.Controller.Snapshot.Equals(ready), Is.True);
        }

        [Test]
        public void FinishRecordsFactsAndRequestsOnlyBoat3Once()
        {
            var context = Inspecting(Stage2River.Low);
            CompleteHotspot(context, Stage2Hotspot.Field, 5d);
            CompleteHotspot(context, Stage2Hotspot.Path, 8d);
            CompleteHotspot(context, Stage2Hotspot.Boat, 11d);
            Assert.That(context.Controller.CanFinish, Is.True);
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 14d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Navigating));
            Assert.That(context.Navigation.ActiveTransaction.Caller, Is.EqualTo(RouteId.Stage2));
            Assert.That(context.Navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.Boat3));
            Assert.That(context.Session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage2));
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Stage2Option), Is.EqualTo(4));
            Assert.That(context.Session.Observations.Count(item => item.Type == ObservationEventType.Complete), Is.EqualTo(1));
            Assert.That(context.Controller.HandleFinish(context.Controller.CurrentTokenId, 15d), Is.False);
        }

        [Test]
        public void InvalidEnumsInteractionsAndClocksFailClosed()
        {
            var context = Choosing();
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.None, Guid.NewGuid(), 4d), Is.False);
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, (Stage2River)99, Guid.NewGuid(), 4d), Is.False);
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.Low, Guid.Empty, 4d), Is.False);
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.Low, Guid.NewGuid(), double.NaN), Is.False);
            Assert.That(context.Session.Observations, Is.Empty);
        }

        [Test]
        public void RejectedInputDoesNotConsumeTheInteractionId()
        {
            var context = Choosing();
            var interaction = Guid.NewGuid();
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.Low, interaction, double.NaN), Is.False);
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, Stage2River.Low, interaction, 4d), Is.True);

            var hotspotInteraction = Guid.NewGuid();
            Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, (Stage2Hotspot)99, hotspotInteraction, 5d), Is.False);
            Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, Stage2Hotspot.Field, hotspotInteraction, 5d), Is.True);
        }

        [Test]
        public void HelpRestoresExactIntroChoiceAndInspectSnapshotsWithoutActing()
        {
            var intro = Entered();
            AssertHelpRoundTrip(intro);
            var choice = Choosing();
            AssertHelpRoundTrip(choice);
            var inspect = Inspecting(Stage2River.High);
            var before = inspect.Controller.Snapshot;
            Assert.That(inspect.Controller.NextUnvisitedHotspot, Is.EqualTo(Stage2Hotspot.Field));
            AssertHelpRoundTrip(inspect);
            Assert.That(inspect.Controller.Snapshot.Equals(before), Is.True);
            Assert.That(inspect.Session.Observations, Is.Empty);
        }

        [Test]
        public void RepeatReplaysLastCompletedConsequenceWithoutDuplicatingFacts()
        {
            var context = Inspecting(Stage2River.Middle);
            CompleteHotspot(context, Stage2Hotspot.Boat, 5d);
            var facts = context.Session.Observations.Count;
            Assert.That(context.Controller.HandleRepeat(context.Controller.CurrentTokenId, 8d), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.ConsequencePlaying));
            var replay = context.Controller.CurrentConsequenceTokenId;
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, replay, 10.4d), Is.True);
            Assert.That(context.Session.Observations.Count, Is.EqualTo(facts));
            Assert.That(context.Controller.ObservedFor(Stage2River.Middle), Is.EqualTo(Stage2ObservedHotspots.Boat));
        }

        private static void CompleteHotspot(Context context, Stage2Hotspot hotspot, double start)
        {
            Assert.That(context.Controller.HandleHotspot(context.Controller.CurrentTokenId, hotspot, Guid.NewGuid(), start), Is.True);
            Assert.That(context.Controller.AdvanceConsequence(context.Controller.CurrentTokenId, context.Controller.CurrentConsequenceTokenId, start + Stage2Controller.ConsequenceSeconds), Is.True);
        }

        private static void AssertHelpRoundTrip(Context context)
        {
            var before = context.Controller.Snapshot;
            Assert.That(context.Controller.HandleHelp(context.Controller.CurrentTokenId), Is.True);
            Assert.That(context.Controller.State, Is.EqualTo(Stage2State.Help));
            Assert.That(context.Controller.RestoreFromHelp(before), Is.True);
            Assert.That(context.Controller.Snapshot.Equals(before), Is.True);
        }

        private static Context Inspecting(Stage2River river)
        {
            var context = Choosing();
            Assert.That(context.Controller.HandleSelectRiver(context.Controller.CurrentTokenId, river, Guid.NewGuid(), 4d), Is.True);
            return context;
        }

        private static Context Choosing()
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
            var context = CreateWithoutEntry();
            var request = context.Navigation.Request(RouteId.Stage2, 1d);
            Assert.That(request.Accepted, Is.True);
            context.EntryTransactionId = request.Transaction.Id;
            return context;
        }

        private static Context CreateWithoutEntry()
        {
            var ids = new Queue<Guid>(Enumerable.Range(1, 128).Select(index => Guid.Parse($"10000000-0000-0000-0000-{index:000000000000}")));
            var session = new SessionState();
            session.Begin(RouteId.Boat2);
            var navigation = new NavigationAuthority(RouteId.Boat2, () => ids.Dequeue());
            var view = new FakeView();
            var pause = new FakePause();
            var leave = new FakeLeave();
            return new Context(session, navigation, view, pause, leave,
                new Stage2Controller(session, navigation, view, pause, leave, () => ids.Dequeue()));
        }

        private static Stage2ObservedHotspots Flag(Stage2Hotspot hotspot)
        {
            switch (hotspot)
            {
                case Stage2Hotspot.Field: return Stage2ObservedHotspots.Field;
                case Stage2Hotspot.Path: return Stage2ObservedHotspots.Path;
                case Stage2Hotspot.Boat: return Stage2ObservedHotspots.Boat;
                default: return Stage2ObservedHotspots.None;
            }
        }

        private static readonly Stage2River[] Rivers = { Stage2River.Low, Stage2River.Middle, Stage2River.High };
        private static readonly Stage2Hotspot[] Hotspots = { Stage2Hotspot.Field, Stage2Hotspot.Path, Stage2Hotspot.Boat };

        private sealed class Context
        {
            internal Context(SessionState session, NavigationAuthority navigation, FakeView view, FakePause pause, FakeLeave leave, Stage2Controller controller)
            { Session = session; Navigation = navigation; View = view; Pause = pause; Leave = leave; Controller = controller; }
            internal SessionState Session { get; }
            internal NavigationAuthority Navigation { get; }
            internal FakeView View { get; }
            internal FakePause Pause { get; }
            internal FakeLeave Leave { get; }
            internal Stage2Controller Controller { get; }
            internal Guid EntryTransactionId { get; set; }
        }

        private sealed class FakeView : IStage2View
        {
            internal Stage2Snapshot LastSnapshot;
            internal bool InputReady;
            public void Present(Stage2Snapshot snapshot, bool reducedMotion) => LastSnapshot = snapshot;
            public void SetInputReady(bool ready) => InputReady = ready;
        }

        private sealed class FakePause : IStage2PauseSink
        {
            internal Stage2Snapshot Captured;
            public bool TryShow(Stage2Snapshot snapshot) { Captured = snapshot; return true; }
        }

        private sealed class FakeLeave : ILeaveConfirmationSink
        {
            internal RouteId Caller;
            public bool TryShow(RouteId caller) { Caller = caller; return true; }
        }
    }
}
