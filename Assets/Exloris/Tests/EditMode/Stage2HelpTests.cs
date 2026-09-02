using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2HelpTests
    {
        [Test]
        public void HelpMapsNoSelectionAndInspectingToExactNonReadingDemonstrations()
        {
            var c = Create(); var view = new View(); var help = new Stage2HelpController(c, view);
            Assert.That(help.TryShow(), Is.True);
            Assert.That(view.Mode, Is.EqualTo(Stage2HelpMode.ChooseStoneThenPlaces));
            Assert.That(help.HandleBack(), Is.True);
            c.HandlePlay(c.CurrentTokenId, 0.1d);
            c.HandleSelectRiver(c.CurrentTokenId, Stage2River.Low, Guid.NewGuid(), 0.2d);
            Assert.That(help.TryShow(), Is.True);
            Assert.That(view.Mode, Is.EqualTo(Stage2HelpMode.InspectEachPlace));
            var captured = help.CapturedSnapshot;
            Assert.That(help.HandleBack(), Is.True);
            Assert.That(c.Snapshot, Is.EqualTo(captured));
        }

        [Test]
        public void DuplicateHelpAndStaleBackAreInert()
        {
            var c = Create(); var help = new Stage2HelpController(c, new View());
            Assert.That(help.TryShow(), Is.True);
            Assert.That(help.TryShow(), Is.False);
            Assert.That(help.HandleBack(), Is.True);
            Assert.That(help.HandleBack(), Is.False);
        }

        [Test]
        public void RuntimeHarnessResolutionFitsTheHelpSurface()
        {
            var owner = new GameObject("Stage2HelpLayout", typeof(Stage2HelpView));
            var router = new ChildInputRouter(new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.GetComponent<Stage2HelpView>();
                view.Configure(router, Stage2Content.LoadApproved(), JourneyOverviewContent.LoadApproved());
                Assert.That(view.TryApplyResponsiveLayout(1280, 720, 1280, 720, 326, 88), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        private static Stage2Controller Create()
        {
            var session = new SessionState(); session.Begin(RouteId.Boat2);
            var navigation = new NavigationAuthority(RouteId.Boat2);
            var transaction = navigation.Request(RouteId.Stage2, 0d).Transaction;
            var controller = new Stage2Controller(session, navigation, new StageView(), new Pause(), new Leave(), Guid.NewGuid);
            Assert.That(controller.Enter(transaction.Id, 0d, false), Is.True);
            return controller;
        }
        private sealed class View : IStage2HelpView
        { internal Stage2HelpMode Mode; public void Show(Stage2Snapshot s, Stage2HelpMode m) => Mode = m; public void Hide() { } }
        private sealed class StageView : IStage2View { public void Present(Stage2Snapshot s, bool r) { } public void SetInputReady(bool r) { } }
        private sealed class Pause : IStage2PauseSink { public bool TryShow(Stage2Snapshot s) => true; }
        private sealed class Leave : ILeaveConfirmationSink { public bool TryShow(RouteId r) => true; }
    }
}
