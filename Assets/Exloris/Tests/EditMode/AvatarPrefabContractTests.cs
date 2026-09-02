using System.Linq;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Recovery;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarPrefabContractTests
    {
        [Test]
        public void ExactAvatarPrefabsExistWithoutBakedTextOrSelectableControls()
        {
            AssertPrefab<AvatarSelectView>("Assets/Exloris/UI/Screens/AvatarSelect.prefab");
            AssertPrefab<AvatarPreviewView>("Assets/Exloris/UI/Screens/AvatarPreview.prefab");
            AssertPrefab<LeaveConfirmationView>("Assets/Exloris/UI/Overlays/LeaveConfirmation.prefab");
        }

        [Test]
        public void SelectUsesExactControlsApprovedArtAndInvisibleTargets()
        {
            var content = LoadContent();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/AvatarSelect.prefab");
            var instance = Object.Instantiate(prefab);
            var router = CreateRouter();
            try
            {
                var view = instance.GetComponent<AvatarSelectView>();
                view.Configure(router, content);
                view.Show(AvatarChoice.Boy, AvatarChoice.Girl);

                Assert.That(view.ControlIds, Is.EquivalentTo(new[]
                {
                    AvatarSelectView.BoyControlId,
                    AvatarSelectView.GirlControlId,
                    AvatarSelectView.HomeControlId,
                }));
                Assert.That(view.GetCardFrameSprite(AvatarChoice.Boy), Is.SameAs(content.UiSprites[0]));
                Assert.That(view.GetCardFrameSprite(AvatarChoice.Girl), Is.SameAs(content.UiSprites[0]));
                Assert.That(view.GetAvatarSprite(AvatarChoice.Boy), Is.SameAs(content.BoyMotionSprites[0]));
                Assert.That(view.GetAvatarSprite(AvatarChoice.Girl), Is.SameAs(content.GirlMotionSprites[0]));
                Assert.That(view.GetVisibleSprite(AvatarSelectView.HomeControlId), Is.SameAs(content.UiSprites[2]));
                Assert.That(view.AreTouchTargetsTransparent(), Is.True);
                Assert.That(view.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(view.CaptionBackplateSprite, Is.SameAs(content.UiSprites[7]));
                Assert.That(view.CaptionBackplateOpaque, Is.True);
                Assert.That(view.GetComponentInChildren<SafeAreaController>(true), Is.Not.Null);
                view.ApplyResponsiveLayout(2160f, 1620f, 1598.77f, 1183.08f, 264f, 88);
                Assert.That(view.BackgroundVariant, Is.EqualTo(ResponsiveBackgroundVariant.FourByThree));
                view.ApplyResponsiveLayout(2400f, 1080f, 2038.84f, 825.52f, 429f, 144);
                Assert.That(view.BackgroundVariant, Is.EqualTo(ResponsiveBackgroundVariant.UltraWide));
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PreviewAndLeaveUseOnlyExactApprovedControls()
        {
            var content = LoadContent();
            var previewObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Screens/AvatarPreview.prefab"));
            var leaveObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Exloris/UI/Overlays/LeaveConfirmation.prefab"));
            var previewRouter = CreateRouter();
            var leaveRouter = CreateRouter();
            try
            {
                var preview = previewObject.GetComponent<AvatarPreviewView>();
                preview.Configure(previewRouter, content);
                preview.Show(AvatarChoice.Girl, 7);
                Assert.That(preview.ControlIds, Is.EquivalentTo(new[]
                {
                    AvatarPreviewView.ConfirmControlId,
                    AvatarPreviewView.OtherControlId,
                    AvatarPreviewView.RepeatControlId,
                    AvatarPreviewView.HomeControlId,
                }));
                Assert.That(preview.GetVisibleSprite(AvatarPreviewView.ConfirmControlId), Is.SameAs(content.UiSprites[1]));
                Assert.That(preview.GetVisibleSprite(AvatarPreviewView.HomeControlId), Is.SameAs(content.UiSprites[2]));
                Assert.That(preview.GetVisibleSprite(AvatarPreviewView.RepeatControlId), Is.SameAs(content.UiSprites[3]));
                Assert.That(preview.GetVisibleSprite(AvatarPreviewView.OtherControlId), Is.SameAs(content.UiSprites[4]));
                Assert.That(preview.GetAvatarSprite(), Is.SameAs(content.GirlMotionSprites[0]));
                Assert.That(preview.AreTouchTargetsTransparent(), Is.True);
                Assert.That(preview.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(preview.GetComponentInChildren<SafeAreaController>(true), Is.Not.Null);
                preview.ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
                Assert.That(preview.BackgroundVariant, Is.EqualTo(ResponsiveBackgroundVariant.SixteenByNine));

                var leave = leaveObject.GetComponent<LeaveConfirmationView>();
                leave.Configure(leaveRouter, content);
                Assert.That(leave.ControlIds, Is.EquivalentTo(new[]
                {
                    LeaveConfirmationView.StayControlId,
                    LeaveConfirmationView.CancelControlId,
                    LeaveConfirmationView.LeaveControlId,
                }));
                Assert.That(leave.TechnicalControlIds, Is.EquivalentTo(new[]
                {
                    LeaveConfirmationView.ResumeControlId,
                    LeaveConfirmationView.RetryControlId,
                    LeaveConfirmationView.HomeControlId,
                }));
                Assert.That(leave.GetVisibleSprite(LeaveConfirmationView.StayControlId), Is.SameAs(content.UiSprites[5]));
                Assert.That(leave.GetVisibleSprite(LeaveConfirmationView.CancelControlId), Is.SameAs(content.UiSprites[4]));
                Assert.That(leave.GetVisibleSprite(LeaveConfirmationView.LeaveControlId), Is.SameAs(content.UiSprites[6]));
                Assert.That(leave.GetTechnicalVisibleSprite(LeaveConfirmationView.ResumeControlId),
                    Is.SameAs(content.UiSprites[1]));
                Assert.That(leave.GetTechnicalVisibleSprite(LeaveConfirmationView.RetryControlId),
                    Is.SameAs(content.UiSprites[3]));
                Assert.That(leave.GetTechnicalVisibleSprite(LeaveConfirmationView.HomeControlId),
                    Is.SameAs(content.UiSprites[2]));
                Assert.That(leave.AreTouchTargetsTransparent(), Is.True);
                Assert.That(leave.VisibleArtRaycastsDisabled(), Is.True);
                Assert.That(leave.ScrimBlocksRaycasts, Is.True);
                Assert.That(leave.CaptionBackplateOpaque, Is.True);
                leave.ApplyResponsiveLayout(2400f, 1080f, 2038.84f, 825.52f, 429f, 144);
                Assert.That(leave.DoRecoveryTechnicalTouchTargetsOverlap(), Is.False);
                Assert.That(leave.AreAllTechnicalTargetsInsideSafeArea(), Is.True);
            }
            finally
            {
                previewRouter.Dispose();
                leaveRouter.Dispose();
                Object.DestroyImmediate(previewObject);
                Object.DestroyImmediate(leaveObject);
            }
        }

        [Test]
        public void LeaveBlocksOnlyExactCallerAndStayRestoresItsUnchangedGeometry()
        {
            var content = LoadContent();
            var selectObject = new GameObject("Select");
            var previewObject = new GameObject("Preview");
            var leaveObject = new GameObject("Leave");
            var router = CreateRouter();
            try
            {
                var select = selectObject.AddComponent<AvatarSelectView>();
                var preview = previewObject.AddComponent<AvatarPreviewView>();
                var leave = leaveObject.AddComponent<LeaveConfirmationView>();
                select.Configure(router, content);
                preview.Configure(router, content);
                leave.Configure(router, content);
                select.Show(AvatarChoice.Girl, AvatarChoice.Boy);
                preview.Show(AvatarChoice.Boy, 4);
                leave.BindCallers(select, preview);
                var boyBefore = select.GetAvatarGeometry(AvatarChoice.Boy);

                leave.ShowAndBlockCaller(Exloris.Core.Navigation.RouteId.AvatarSelect);
                Assert.That(select.ControlsEnabled, Is.False);
                Assert.That(preview.ControlsEnabled, Is.True);
                Assert.That(leave.IsVisible, Is.True);

                leave.HideAndRestoreCaller(Exloris.Core.Navigation.RouteId.AvatarSelect);
                Assert.That(select.ControlsEnabled, Is.True);
                Assert.That(select.GetAvatarGeometry(AvatarChoice.Boy).TargetRect,
                    Is.EqualTo(boyBefore.TargetRect));
                Assert.That(leave.IsVisible, Is.False);
            }
            finally
            {
                router.Dispose();
                Object.DestroyImmediate(selectObject);
                Object.DestroyImmediate(previewObject);
                Object.DestroyImmediate(leaveObject);
            }
        }

        private static AvatarContent LoadContent()
        {
            return AssetDatabase.LoadAssetAtPath<AvatarContent>(
                "Assets/Exloris/Resources/Content/AvatarContent.asset");
        }

        private static Exloris.Runtime.Input.ChildInputRouter CreateRouter()
        {
            return new Exloris.Runtime.Input.ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
        }

        private static void AssertPrefab<T>(string path) where T : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(prefab.GetComponent<T>(), Is.Not.Null);
            Assert.That(prefab.GetComponentsInChildren<Selectable>(true), Is.Empty);
            Assert.That(prefab.GetComponentsInChildren<Component>(true).Any(IsTextComponent), Is.False);
        }

        private static bool IsTextComponent(Component component)
        {
            var type = component.GetType();
            return type.Namespace == "TMPro" || type == typeof(Text);
        }
    }
}
