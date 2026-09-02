using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1HelpTests
    {
        [Test]
        public void HelpShowsOnlyTheNextActionAndRestoresExactPlayingState()
        {
            var context = Create();
            var token = context.Controller.CurrentTokenId;
            Assert.That(context.Controller.HandlePlay(token, 3d), Is.True);
            var before = context.Controller.Snapshot;

            Assert.That(context.Help.TryShow(), Is.True);
            Assert.That(context.View.LastMode, Is.EqualTo(Stage1HelpMode.Attach));
            Assert.That(context.Controller.State, Is.EqualTo(Stage1State.Help));
            Assert.That(context.Help.TryShow(), Is.False);
            Assert.That(context.Help.HandleBack(), Is.True);
            Assert.That(context.Controller.Snapshot, Is.EqualTo(before));
            Assert.That(context.View.HideCount, Is.EqualTo(1));

            Assert.That(context.Controller.HandleDragAttach(token, Guid.NewGuid(), true, true, 4d), Is.True);
            var attached = context.Controller.Snapshot;
            Assert.That(context.Help.TryShow(), Is.True);
            Assert.That(context.View.LastMode, Is.EqualTo(Stage1HelpMode.Move));
            Assert.That(context.Help.HandleBack(), Is.True);
            Assert.That(context.Controller.Snapshot, Is.EqualTo(attached));
        }

        [TestCase(0.1d, Stage1HelpMode.AdjustUp)]
        [TestCase(0.9d, Stage1HelpMode.AdjustDown)]
        public void OutcomeHelpShowsOneAdjustmentWithoutMovingMarkerOrSupport(double position, Stage1HelpMode expected)
        {
            var context = Create();
            var token = context.Controller.CurrentTokenId;
            context.Controller.HandlePlay(token, 3d);
            context.Controller.HandleDragAttach(token, Guid.NewGuid(), true, true, 3.5d);
            context.Controller.HandleDrag(token, position, 4d);
            context.Controller.HandleConfirm(token, 5d);
            context.Controller.AdvanceOutcome(token, context.Controller.CurrentOutcomeTokenId, 8.2d);
            var before = context.Controller.Snapshot;

            Assert.That(context.Help.TryShow(), Is.True);
            Assert.That(context.View.LastMode, Is.EqualTo(expected));
            Assert.That(context.Help.HandleBack(), Is.True);
            Assert.That(context.Controller.Snapshot.MarkerPosition, Is.EqualTo(before.MarkerPosition));
            Assert.That(context.Controller.Snapshot.SupportTier, Is.EqualTo(before.SupportTier));
            Assert.That(context.Controller.Snapshot.OptionCount, Is.EqualTo(before.OptionCount));
        }

        [Test]
        public void HelpViewUsesOneTransparentBackTargetAndRaycastFreeDemonstrationArt()
        {
            var owner = new GameObject("Stage1HelpViewTest", typeof(Stage1HelpView));
            var router = new ChildInputRouter(new InputAction("Point", InputActionType.PassThrough), new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.GetComponent<Stage1HelpView>();
                view.Configure(router, StageContent(), OverviewContent());
                view.Show(default, Stage1HelpMode.AttachThenMove);
                Assert.That(view.ControlIds, Is.EqualTo(new[] { Stage1HelpView.BackControlId }));
                Assert.That(view.DisplaySemanticControlCount, Is.Zero);
                Assert.That(view.BackInputEnabled, Is.True);
                Assert.That(view.TouchTargetTransparent(), Is.True);
                Assert.That(view.DisplayArtRaycastsDisabled(), Is.True);
                Assert.That(view.TryApplyResponsiveLayout(1024, 768, 960, 704, 264, 88), Is.True);
            }
            finally { router.Dispose(); UnityEngine.Object.DestroyImmediate(owner); }
        }

        private static Context Create()
        {
            var session = new CoreSessionState(); session.Begin(RouteId.Boat1);
            var navigation = new NavigationAuthority(RouteId.Boat1, Guid.NewGuid);
            var entry = navigation.Request(RouteId.Stage1, 1d);
            var stageView = new StageView();
            var controller = new Stage1Controller(session, navigation, stageView, new Pause(), new Leave(), Guid.NewGuid);
            Assert.That(controller.Enter(entry.Transaction.Id, 2d, false), Is.True);
            var helpView = new HelpView();
            return new Context(controller, helpView, new Stage1HelpController(controller, helpView));
        }

        private static Stage1Content StageContent() => AssetDatabase.LoadAssetAtPath<Stage1Content>("Assets/Exloris/Resources/Content/Stage1Content.asset");
        private static JourneyOverviewContent OverviewContent() => AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>("Assets/Exloris/Resources/Content/JourneyOverviewContent.asset");

        private readonly struct Context
        {
            internal Context(Stage1Controller controller, HelpView view, Stage1HelpController help) { Controller = controller; View = view; Help = help; }
            internal Stage1Controller Controller { get; }
            internal HelpView View { get; }
            internal Stage1HelpController Help { get; }
        }

        private sealed class StageView : IStage1View { public void Present(Stage1Snapshot snapshot, bool reduced) { } public void SetInputReady(bool ready) { } }
        private sealed class Pause : IStage1PauseSink { public bool TryShow(Stage1Snapshot snapshot) => true; }
        private sealed class Leave : Exloris.Runtime.Avatar.ILeaveConfirmationSink { public bool TryShow(RouteId caller) => true; }
        private sealed class HelpView : IStage1HelpView
        {
            internal Stage1HelpMode LastMode; internal int HideCount;
            public void Show(Stage1Snapshot snapshot, Stage1HelpMode mode) => LastMode = mode;
            public void Hide() => HideCount++;
        }
    }
}
