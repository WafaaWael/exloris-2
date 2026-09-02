using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Input;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Exloris.Tests.PlayMode
{
    public sealed class AvatarRealInputTests : InputTestFixture
    {
        private Mouse _mouse;
        private readonly List<SemanticInputCommand> _commands = new List<SemanticInputCommand>();

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            _commands.Clear();
            base.TearDown();
        }

        [Test]
        public void SelectAcceptsCentreAndFourEdgesRejectsNearMissAndCoversEveryControl()
        {
            using (var context = CreateSelect())
            {
                ProbeFiveEdges(context.View.GetControlScreenBounds(AvatarSelectView.BoyControlId), 10d);
                Click(new Vector2(
                    context.View.GetControlScreenBounds(AvatarSelectView.BoyControlId).xMin - 1f,
                    context.View.GetControlScreenBounds(AvatarSelectView.BoyControlId).center.y), 11.6d);
                Click(context.View.GetControlScreenBounds(AvatarSelectView.GirlControlId).center, 12d);
                Click(context.View.GetControlScreenBounds(AvatarSelectView.HomeControlId).center, 12.3d);

                Assert.That(_commands, Has.Count.EqualTo(7));
                Assert.That(_commands.GetRange(0, 5), Has.All.Matches<SemanticInputCommand>(item =>
                    item.ControlId == AvatarSelectView.BoyControlId));
                Assert.That(_commands[5].ControlId, Is.EqualTo(AvatarSelectView.GirlControlId));
                Assert.That(_commands[6].ControlId, Is.EqualTo(AvatarSelectView.HomeControlId));
            }
        }

        [Test]
        public void PreviewAndLeaveAcceptEveryExactSemanticControl()
        {
            using (var preview = CreatePreview())
            {
                Click(preview.View.GetControlScreenBounds(AvatarPreviewView.RepeatControlId).center, 20d);
                Click(preview.View.GetControlScreenBounds(AvatarPreviewView.HomeControlId).center, 20.3d);
                Click(preview.View.GetControlScreenBounds(AvatarPreviewView.ConfirmControlId).center, 20.6d);
                Click(preview.View.GetControlScreenBounds(AvatarPreviewView.OtherControlId).center, 20.9d);
                Assert.That(ControlIds(), Is.EqualTo(new[]
                {
                    AvatarPreviewView.RepeatControlId,
                    AvatarPreviewView.HomeControlId,
                    AvatarPreviewView.ConfirmControlId,
                    AvatarPreviewView.OtherControlId,
                }));
            }

            _commands.Clear();
            using (var leave = CreateLeave())
            {
                leave.View.ShowAndBlockCaller(RouteId.AvatarSelect);
                Canvas.ForceUpdateCanvases();
                Click(leave.View.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center, 30d);
                Click(leave.View.GetControlScreenBounds(LeaveConfirmationView.CancelControlId).center, 30.3d);
                Click(leave.View.GetControlScreenBounds(LeaveConfirmationView.LeaveControlId).center, 30.6d);
                Assert.That(ControlIds(), Is.EqualTo(new[]
                {
                    LeaveConfirmationView.StayControlId,
                    LeaveConfirmationView.CancelControlId,
                    LeaveConfirmationView.LeaveControlId,
                }));
            }
        }

        [Test]
        public void LeaveBlocksCallerInputAndStayRestoresExactCallerTarget()
        {
            var routerContext = CreateRouter();
            var selectObject = new GameObject("SelectInput");
            var previewObject = new GameObject("PreviewInput");
            var leaveObject = new GameObject("LeaveInput");
            try
            {
                var content = AvatarContent.LoadApproved();
                var select = selectObject.AddComponent<AvatarSelectView>();
                var preview = previewObject.AddComponent<AvatarPreviewView>();
                var leave = leaveObject.AddComponent<LeaveConfirmationView>();
                select.Configure(routerContext.Router, content);
                preview.Configure(routerContext.Router, content);
                leave.Configure(routerContext.Router, content);
                select.Show(AvatarChoice.Girl, AvatarChoice.Boy);
                preview.SetVisible(false);
                leave.BindCallers(select, preview);
                routerContext.Router.Enable();
                Canvas.ForceUpdateCanvases();
                var callerBounds = select.GetControlScreenBounds(AvatarSelectView.GirlControlId);

                leave.ShowAndBlockCaller(RouteId.AvatarSelect);
                Canvas.ForceUpdateCanvases();
                Click(callerBounds.center, 40d);
                Click(leave.GetControlScreenBounds(LeaveConfirmationView.StayControlId).center, 40.3d);
                Assert.That(ControlIds(), Is.EqualTo(new[] { LeaveConfirmationView.StayControlId }));

                leave.HideAndRestoreCaller(RouteId.AvatarSelect);
                Canvas.ForceUpdateCanvases();
                Click(callerBounds.center, 40.6d);
                Assert.That(_commands[1].ControlId, Is.EqualTo(AvatarSelectView.GirlControlId));
                Assert.That(select.GetControlScreenBounds(AvatarSelectView.GirlControlId), Is.EqualTo(callerBounds));
            }
            finally
            {
                Object.DestroyImmediate(selectObject);
                Object.DestroyImmediate(previewObject);
                Object.DestroyImmediate(leaveObject);
                routerContext.Dispose();
            }
        }

        [Test]
        public void FormulaUsesPhysicalFloorMidpointActualSlotGapAndRejectsImpossibleReflow()
        {
            using (var context = CreateSelect())
            {
                context.View.ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
                var boy = context.View.GetControlCanvasRect(AvatarSelectView.BoyControlId);
                var girl = context.View.GetControlCanvasRect(AvatarSelectView.GirlControlId);
                var home = context.View.GetControlCanvasRect(AvatarSelectView.HomeControlId);
                var before = boy;

                Assert.That(home.width, Is.GreaterThanOrEqualTo(224f));
                Assert.That((boy.center.x + girl.center.x) * 0.5f, Is.EqualTo(1856f * 0.5f).Within(0.01f));
                Assert.That(girl.xMin - boy.xMax,
                    Is.EqualTo(Mathf.Max(94f, boy.width * 0.25f)).Within(0.01f));
                Assert.That(context.View.AreAllTargetsInsideSafeArea(), Is.True);
                Assert.That(context.View.TryApplyResponsiveLayout(
                    1334f, 750f, 360f, 182f, 326f, 88), Is.False);
                Assert.That(context.View.GetControlCanvasRect(AvatarSelectView.BoyControlId), Is.EqualTo(before));
            }
        }

        private SelectContext CreateSelect()
        {
            var router = CreateRouter();
            var owner = new GameObject("AvatarSelectInput");
            var view = owner.AddComponent<AvatarSelectView>();
            view.Configure(router.Router, AvatarContent.LoadApproved());
            view.Show(AvatarChoice.Boy, AvatarChoice.Girl);
            router.Router.Enable();
            Canvas.ForceUpdateCanvases();
            return new SelectContext(owner, router, view);
        }

        private PreviewContext CreatePreview()
        {
            var router = CreateRouter();
            var owner = new GameObject("AvatarPreviewInput");
            var view = owner.AddComponent<AvatarPreviewView>();
            view.Configure(router.Router, AvatarContent.LoadApproved());
            view.Show(AvatarChoice.Boy, 1);
            router.Router.Enable();
            Canvas.ForceUpdateCanvases();
            return new PreviewContext(owner, router, view);
        }

        private LeaveContext CreateLeave()
        {
            var router = CreateRouter();
            var selectObject = new GameObject("LeaveSelectCaller");
            var previewObject = new GameObject("LeavePreviewCaller");
            var owner = new GameObject("LeaveInput");
            var content = AvatarContent.LoadApproved();
            var select = selectObject.AddComponent<AvatarSelectView>();
            var preview = previewObject.AddComponent<AvatarPreviewView>();
            select.Configure(router.Router, content);
            preview.Configure(router.Router, content);
            select.Show(AvatarChoice.Boy, AvatarChoice.Girl);
            preview.SetVisible(false);
            var view = owner.AddComponent<LeaveConfirmationView>();
            view.Configure(router.Router, content);
            view.BindCallers(select, preview);
            router.Router.Enable();
            Canvas.ForceUpdateCanvases();
            return new LeaveContext(owner, selectObject, previewObject, router, view);
        }

        private RouterContext CreateRouter()
        {
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("AvatarTest");
            var point = map.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Mouse>/position");
            var press = map.AddAction("Press", InputActionType.Button, expectedControlLayout: "Button");
            press.AddBinding("<Mouse>/leftButton");
            actions.AddActionMap(map);
            var router = new ChildInputRouter(point, press);
            router.SemanticCommandIssued += _commands.Add;
            return new RouterContext(actions, router);
        }

        private void ProbeFiveEdges(Rect bounds, double startedAt)
        {
            Click(bounds.center, startedAt);
            Click(new Vector2(bounds.xMin + 0.25f, bounds.center.y), startedAt + 0.3d);
            Click(new Vector2(bounds.xMax - 0.25f, bounds.center.y), startedAt + 0.6d);
            Click(new Vector2(bounds.center.x, bounds.yMin + 0.25f), startedAt + 0.9d);
            Click(new Vector2(bounds.center.x, bounds.yMax - 0.25f), startedAt + 1.2d);
        }

        private void Click(Vector2 position, double startedAt)
        {
            currentTime = startedAt;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = position });
            InputSystem.Update();
            Press(_mouse.leftButton);
            currentTime = startedAt + 0.05d;
            Release(_mouse.leftButton);
        }

        private string[] ControlIds()
        {
            var result = new string[_commands.Count];
            for (var index = 0; index < result.Length; index++) result[index] = _commands[index].ControlId;
            return result;
        }

        private sealed class RouterContext : System.IDisposable
        {
            public RouterContext(InputActionAsset actions, ChildInputRouter router)
            {
                Actions = actions;
                Router = router;
            }
            public InputActionAsset Actions { get; }
            public ChildInputRouter Router { get; }
            public void Dispose()
            {
                Router.Dispose();
                Object.DestroyImmediate(Actions);
            }
        }

        private sealed class SelectContext : System.IDisposable
        {
            public SelectContext(GameObject owner, RouterContext router, AvatarSelectView view)
            { Owner = owner; Router = router; View = view; }
            public GameObject Owner { get; }
            public RouterContext Router { get; }
            public AvatarSelectView View { get; }
            public void Dispose() { Object.DestroyImmediate(Owner); Router.Dispose(); }
        }

        private sealed class PreviewContext : System.IDisposable
        {
            public PreviewContext(GameObject owner, RouterContext router, AvatarPreviewView view)
            { Owner = owner; Router = router; View = view; }
            public GameObject Owner { get; }
            public RouterContext Router { get; }
            public AvatarPreviewView View { get; }
            public void Dispose() { Object.DestroyImmediate(Owner); Router.Dispose(); }
        }

        private sealed class LeaveContext : System.IDisposable
        {
            public LeaveContext(GameObject owner, GameObject select, GameObject preview,
                RouterContext router, LeaveConfirmationView view)
            { Owner = owner; Select = select; Preview = preview; Router = router; View = view; }
            public GameObject Owner { get; }
            public GameObject Select { get; }
            public GameObject Preview { get; }
            public RouterContext Router { get; }
            public LeaveConfirmationView View { get; }
            public void Dispose()
            {
                Object.DestroyImmediate(Owner);
                Object.DestroyImmediate(Select);
                Object.DestroyImmediate(Preview);
                Router.Dispose();
            }
        }
    }
}
