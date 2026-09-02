using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Runtime.Avatar;
using Exloris.Runtime.Input;
using Exloris.Runtime.Completion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    public enum AvatarTechnicalOverlayState
    {
        Hidden,
        Loading,
        Paused,
        Recovery
    }

    [DisallowMultipleComponent]
    public sealed class LeaveConfirmationView :
        MonoBehaviour,
        ILeaveConfirmationView,
        INestedLeaveConfirmationView
    {
        public const string StayControlId = "leave.stay";
        public const string CancelControlId = "leave.cancel";
        public const string LeaveControlId = "leave.leave";
        public const string ResumeControlId = "avatar.interruption.resume";
        public const string RetryControlId = "avatar.recovery.retry";
        public const string HomeControlId = "avatar.recovery.home";

        private readonly Dictionary<string, AvatarControlView> _controls =
            new Dictionary<string, AvatarControlView>();
        private readonly List<string> _controlIds = new List<string>();
        private readonly Dictionary<string, AvatarControlView> _technicalControls =
            new Dictionary<string, AvatarControlView>();
        private readonly List<string> _technicalControlIds = new List<string>();
        private readonly Dictionary<RouteId, Action<bool>> _callerControlBindings =
            new Dictionary<RouteId, Action<bool>>();
        private readonly Dictionary<RouteId, Action<bool>> _callerCaptionSuppressionBindings =
            new Dictionary<RouteId, Action<bool>>();
        private ChildInputRouter _router;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private Image _scrim;
        private Image _panel;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private Image _visualEquivalent;
        private PauseOverlayView _pause;
        private Vector2 _safeSize;
        private bool _technicalRestoreCaptionBackplate;
        private bool _technicalRestoreCaption;
        private bool _technicalRestoreVisual;
        private Guid _nestedStackToken;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public IReadOnlyList<string> TechnicalControlIds => _technicalControlIds;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool ScrimBlocksRaycasts => _scrim != null && _scrim.raycastTarget;
        public bool CaptionBackplateOpaque => _captionBackplate != null && _captionBackplate.color.a >= 1f;
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeInHierarchy;
        public bool VisualEquivalentVisible =>
            _visualEquivalent != null && _visualEquivalent.gameObject.activeInHierarchy;
        public RouteId? CurrentCaller { get; private set; }
        public AvatarTechnicalOverlayState TechnicalOverlayState { get; private set; }

        public void Configure(ChildInputRouter router, AvatarContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            if (content == null) throw new ArgumentNullException(nameof(content));
            BuildSurface(content);
            ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
            SetOverlayActive(false);
        }

        public void BindCallers(AvatarSelectView select, AvatarPreviewView preview)
        {
            if (select == null) throw new ArgumentNullException(nameof(select));
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            _callerControlBindings[RouteId.AvatarSelect] = select.SetControlsEnabled;
            _callerControlBindings[RouteId.AvatarPreview] = preview.SetControlsEnabled;
            _callerCaptionSuppressionBindings[RouteId.AvatarSelect] = select.SetCaptionSuppressed;
            _callerCaptionSuppressionBindings[RouteId.AvatarPreview] = preview.SetCaptionSuppressed;
        }

        public void BindJourneyCaller(
            Exloris.Runtime.Overview.JourneyOverviewStageView journey,
            PauseOverlayView pause)
        {
            if (journey == null) throw new ArgumentNullException(nameof(journey));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.JourneyOverview] = journey.SetControlsEnabled;
            _callerCaptionSuppressionBindings[RouteId.JourneyOverview] = journey.SetCaptionSuppressed;
        }

        public void BindBoat1Caller(Exloris.Runtime.Boat.Boat1StageView boat, PauseOverlayView pause)
        {
            if (boat == null) throw new ArgumentNullException(nameof(boat));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Boat1] = boat.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Boat1] = boat.SetCaptionSuppressed;
        }

        public void BindBoat2Caller(Exloris.Runtime.Boat.Boat2StageView boat, PauseOverlayView pause)
        {
            if (boat == null) throw new ArgumentNullException(nameof(boat));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Boat2] = boat.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Boat2] = boat.SetCaptionSuppressed;
        }

        public void BindBoat3Caller(Exloris.Runtime.Boat.Boat3StageView boat, PauseOverlayView pause)
        {
            if (boat == null) throw new ArgumentNullException(nameof(boat));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Boat3] = boat.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Boat3] = boat.SetCaptionSuppressed;
        }

        public void BindStage1Caller(Exloris.Runtime.Stage1.Stage1StageView stage1, PauseOverlayView pause)
        {
            if (stage1 == null) throw new ArgumentNullException(nameof(stage1));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Stage1] = stage1.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Stage1] = stage1.SetCaptionSuppressed;
        }

        public void BindStage2Caller(Exloris.Runtime.Stage2.Stage2StageView stage2, PauseOverlayView pause)
        {
            if (stage2 == null) throw new ArgumentNullException(nameof(stage2));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Stage2] = stage2.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Stage2] = stage2.SetCaptionSuppressed;
        }

        public void BindStage3Caller(Exloris.Runtime.Stage3.Stage3StageView stage3, PauseOverlayView pause)
        {
            if (stage3 == null) throw new ArgumentNullException(nameof(stage3));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Stage3] = stage3.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Stage3] = stage3.SetCaptionSuppressed;
        }

        public void BindCompletionCaller(CompletionStageView completion, PauseOverlayView pause)
        {
            if (completion == null) throw new ArgumentNullException(nameof(completion));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _callerControlBindings[RouteId.Completion] = completion.SetInputReady;
            _callerCaptionSuppressionBindings[RouteId.Completion] = completion.SetCaptionSuppressed;
        }

        public void ShowAndBlockPause(LeaveRequest request)
        {
            ValidateNested(request);
            CurrentCaller = request.Caller;
            _nestedStackToken = request.StackToken;
            _pause.SetControlsEnabled(false);
            _canvas.GetComponent<Canvas>().sortingOrder = 600;
            SetOverlayActive(true);
            _router.SetVisibleStateReady(true);
        }

        public void HideAndRestorePause(LeaveRequest request)
        {
            RequireNested(request);
            SetOverlayActive(false);
            _canvas.GetComponent<Canvas>().sortingOrder = 400;
            _pause.SetControlsEnabled(true);
            CurrentCaller = null;
            _nestedStackToken = Guid.Empty;
            _router.SetVisibleStateReady(true);
        }

        public void HoldPauseBlockedForTransition(LeaveRequest request)
        {
            RequireNested(request);
            _pause.SetControlsEnabled(false);
            SetControlsEnabled(false);
            _router.SetVisibleStateReady(false);
        }

        public void ShowAndBlockCaller(RouteId caller)
        {
            ValidateCaller(caller);
            CurrentCaller = caller;
            _canvas.GetComponent<Canvas>().sortingOrder = 400;
            SetCallerEnabled(caller, false);
            SetOverlayActive(true);
            _router.SetVisibleStateReady(true);
        }

        public void SetInterruptionBlocked(bool blocked)
        {
            if (IsVisible && TechnicalOverlayState == AvatarTechnicalOverlayState.Hidden)
                SetControlsEnabled(!blocked);
        }

        public void HideAndRestoreCaller(RouteId caller)
        {
            RequireCurrentCaller(caller);
            SetOverlayActive(false);
            SetCallerEnabled(caller, true);
            CurrentCaller = null;
            _nestedStackToken = Guid.Empty;
            _router.SetVisibleStateReady(true);
        }

        public void HoldBlockedForTransition(RouteId caller)
        {
            RequireCurrentCaller(caller);
            SetCallerEnabled(caller, false);
            SetControlsEnabled(false);
        }

        public void CompleteTransitionAndHide()
        {
            if (CurrentCaller.HasValue) SetCallerCaptionSuppressed(CurrentCaller.Value, false);
            SetTechnicalControlsEnabled(AvatarTechnicalOverlayState.Hidden);
            SetOverlayActive(false);
            CurrentCaller = null;
            _nestedStackToken = Guid.Empty;
            TechnicalOverlayState = AvatarTechnicalOverlayState.Hidden;
        }

        public void ShowTechnicalOverlay(RouteId caller, AvatarTechnicalOverlayState state)
        {
            ValidateCaller(caller);
            if (state == AvatarTechnicalOverlayState.Hidden)
                throw new ArgumentOutOfRangeException(nameof(state));

            if (!CurrentCaller.HasValue) CurrentCaller = caller;
            else if (CurrentCaller.Value != caller)
                throw new InvalidOperationException("Technical overlay caller does not match the captured surface.");

            TechnicalOverlayState = state;
            _technicalRestoreCaptionBackplate = _captionBackplate.gameObject.activeSelf;
            _technicalRestoreCaption = _caption.gameObject.activeSelf;
            _technicalRestoreVisual = _visualEquivalent.gameObject.activeSelf;
            _captionBackplate.gameObject.SetActive(false);
            _caption.gameObject.SetActive(false);
            _visualEquivalent.gameObject.SetActive(false);
            SetCallerCaptionSuppressed(caller, true);
            SetCallerEnabled(caller, false);
            SetControlsEnabled(false);
            SetNormalControlsVisible(false);
            _canvas.SetActive(true);
            SetTechnicalControlsEnabled(state);
            _router.SetVisibleStateReady(state != AvatarTechnicalOverlayState.Loading);
        }

        public void HideTechnicalOverlay(bool restoreLeaveOverlay)
        {
            if (!CurrentCaller.HasValue)
                throw new InvalidOperationException("Technical overlay has no captured caller.");

            var caller = CurrentCaller.Value;
            SetCallerCaptionSuppressed(caller, false);
            TechnicalOverlayState = AvatarTechnicalOverlayState.Hidden;
            SetTechnicalControlsEnabled(AvatarTechnicalOverlayState.Hidden);
            if (restoreLeaveOverlay)
            {
                _captionBackplate.gameObject.SetActive(_technicalRestoreCaptionBackplate);
                _caption.gameObject.SetActive(_technicalRestoreCaption);
                _visualEquivalent.gameObject.SetActive(_technicalRestoreVisual);
                SetNormalControlsVisible(true);
                SetControlsEnabled(true);
                _canvas.SetActive(true);
            }
            else
            {
                SetNormalControlsVisible(true);
                SetOverlayActive(false);
                SetCallerEnabled(caller, true);
                CurrentCaller = null;
            }
            _router.SetVisibleStateReady(true);
        }

        public void ShowMediaCue(Sprite visual, string captionId, bool captionsVisible)
        {
            _visualEquivalent.sprite = visual;
            _visualEquivalent.gameObject.SetActive(true);
            UpdateCaption(captionId, captionsVisible);
        }

        public void UpdateCaption(string captionId, bool visible)
        {
            _caption.text = AvatarCaptionStrings.ResolveEnglish(captionId);
            _captionBackplate.gameObject.SetActive(visible);
            _caption.gameObject.SetActive(visible);
        }

        public void ApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var metrics = AvatarLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (3f * metrics.Target + 2f * metrics.Gap > safeWidth
                || 2f * metrics.Target + metrics.Gap > safeHeight)
                throw new InvalidOperationException("Safe area cannot contain the frozen Leave layout.");
            _safeSize = new Vector2(safeWidth, safeHeight);
            var panelSize = new Vector2(
                Mathf.Min(safeWidth * 0.82f, 1200f),
                Mathf.Min(safeHeight * 0.72f, 720f));
            AvatarViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, panelSize);
            AvatarViewFactory.SetCentered(_captionBackplate.rectTransform,
                new Vector2(0f, panelSize.y * 0.10f),
                new Vector2(panelSize.x * 0.78f, panelSize.y * 0.34f));
            AvatarViewFactory.SetCentered(_visualEquivalent.rectTransform,
                new Vector2(0f, -panelSize.y * 0.10f),
                new Vector2(panelSize.x * 0.42f, panelSize.y * 0.28f));

            var ids = new[] { StayControlId, CancelControlId, LeaveControlId };
            for (var index = 0; index < ids.Length; index++)
            {
                var centerX = safeWidth * 0.5f + (index - 1f) * (metrics.Target + metrics.Gap);
                var control = _controls[ids[index]];
                AvatarViewFactory.SetBottom(control,
                    new Vector2(centerX - safeWidth * 0.5f, metrics.Target * 0.5f), metrics.Target);
                control.CanvasRect = CenteredRect(centerX, metrics.Target * 0.5f,
                    metrics.Target, metrics.Target);
            }

            var resume = _technicalControls[ResumeControlId];
            AvatarViewFactory.SetBottom(resume,
                new Vector2(0f, metrics.Target * 0.5f), metrics.Target);
            resume.CanvasRect = CenteredRect(
                safeWidth * 0.5f, metrics.Target * 0.5f, metrics.Target, metrics.Target);
            var recoveryOffset = (metrics.Target + metrics.Gap) * 0.5f;
            var retry = _technicalControls[RetryControlId];
            var home = _technicalControls[HomeControlId];
            AvatarViewFactory.SetBottom(retry,
                new Vector2(-recoveryOffset, metrics.Target * 0.5f), metrics.Target);
            AvatarViewFactory.SetBottom(home,
                new Vector2(recoveryOffset, metrics.Target * 0.5f), metrics.Target);
            retry.CanvasRect = CenteredRect(
                safeWidth * 0.5f - recoveryOffset, metrics.Target * 0.5f,
                metrics.Target, metrics.Target);
            home.CanvasRect = CenteredRect(
                safeWidth * 0.5f + recoveryOffset, metrics.Target * 0.5f,
                metrics.Target, metrics.Target);
        }

        public Sprite GetVisibleSprite(string controlId) => _controls[controlId].VisibleArt.sprite;
        public Sprite GetTechnicalVisibleSprite(string controlId) =>
            _technicalControls[controlId].VisibleArt.sprite;
        public Rect GetControlScreenBounds(string controlId) =>
            AvatarViewFactory.ToScreenRect(_controls[controlId].Target);
        public Rect GetTechnicalControlScreenBounds(string controlId) =>
            AvatarViewFactory.ToScreenRect(_technicalControls[controlId].Target);
        public Rect GetControlCanvasRect(string controlId) => _controls[controlId].CanvasRect;
        public bool DoTouchTargetsOverlap() => AvatarViewFactory.TargetsOverlap(_controls.Values);
        public bool AreAllTargetsInsideSafeArea() => AvatarViewFactory.Inside(_controls.Values, _safeSize);
        public bool DoRecoveryTechnicalTouchTargetsOverlap() =>
            _technicalControls[RetryControlId].CanvasRect.Overlaps(
                _technicalControls[HomeControlId].CanvasRect);
        public bool AreAllTechnicalTargetsInsideSafeArea() =>
            AvatarViewFactory.Inside(_technicalControls.Values, _safeSize);

        public bool AreTouchTargetsTransparent()
        {
            foreach (var control in _controls.Values)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            foreach (var control in _technicalControls.Values)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            return true;
        }

        public bool VisibleArtRaycastsDisabled()
        {
            foreach (var control in _controls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            foreach (var control in _technicalControls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            return _panel != null && !_panel.raycastTarget
                && _captionBackplate != null && !_captionBackplate.raycastTarget;
        }

        private void BuildSurface(AvatarContent content)
        {
            if (_canvas != null) return;
            _canvas = AvatarViewFactory.CreateCanvas("LeaveConfirmationCanvas", transform, 400);
            _scrim = AvatarViewFactory.CreateImage(
                "ModalScrim", _canvas.transform, new Color(0f, 0f, 0f, 0.52f), null);
            AvatarViewFactory.Stretch(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            _safeRoot = AvatarViewFactory.CreateSafeRoot(_canvas);
            _panel = AvatarViewFactory.CreateImage("LeavePanel", _safeRoot, Color.white, content.UiSprites[0]);
            _panel.type = Image.Type.Sliced;
            _captionBackplate = AvatarViewFactory.CreateImage(
                "CaptionBackplate", _panel.transform, Color.white, content.UiSprites[7]);
            _captionBackplate.type = Image.Type.Sliced;
            _caption = AvatarViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _visualEquivalent = AvatarViewFactory.CreateImage(
                "NonTextVisualEquivalent", _panel.transform, Color.white, content.VisualEquivalents[2]);
            _visualEquivalent.preserveAspect = true;
            AddControl(StayControlId, "Stay", content.UiSprites[5]);
            AddControl(CancelControlId, "Cancel", content.UiSprites[4]);
            AddControl(LeaveControlId, "Leave", content.UiSprites[6]);
            AddTechnicalControl(ResumeControlId, "Resume", content.UiSprites[1]);
            AddTechnicalControl(RetryControlId, "Retry", content.UiSprites[3]);
            AddTechnicalControl(HomeControlId, "RecoveryHome", content.UiSprites[2]);
            SetTechnicalControlsEnabled(AvatarTechnicalOverlayState.Hidden);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = AvatarViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void AddTechnicalControl(string id, string name, Sprite sprite)
        {
            var control = AvatarViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _technicalControls.Add(id, control);
            _technicalControlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void SetOverlayActive(bool active)
        {
            SetControlsEnabled(active);
            _canvas.SetActive(active);
        }

        private void SetControlsEnabled(bool enabled)
        {
            foreach (var control in _controls.Values) control.Semantic.Enabled = enabled;
        }

        private void SetNormalControlsVisible(bool visible)
        {
            foreach (var control in _controls.Values) control.Target.gameObject.SetActive(visible);
        }

        private void SetTechnicalControlsEnabled(AvatarTechnicalOverlayState state)
        {
            foreach (var control in _technicalControls.Values)
            {
                control.Semantic.Enabled = false;
                control.Target.gameObject.SetActive(false);
            }

            if (state == AvatarTechnicalOverlayState.Paused)
            {
                _technicalControls[ResumeControlId].Semantic.Enabled = true;
                _technicalControls[ResumeControlId].Target.gameObject.SetActive(true);
            }
            else if (state == AvatarTechnicalOverlayState.Recovery)
            {
                _technicalControls[RetryControlId].Semantic.Enabled = true;
                _technicalControls[HomeControlId].Semantic.Enabled = true;
                _technicalControls[RetryControlId].Target.gameObject.SetActive(true);
                _technicalControls[HomeControlId].Target.gameObject.SetActive(true);
            }
        }

        private void SetCallerEnabled(RouteId caller, bool enabled)
        {
            if (!_callerControlBindings.TryGetValue(caller, out var setEnabled))
                throw new InvalidOperationException(
                    $"Leave caller '{caller}' has no registered input binding.");
            setEnabled(enabled);
        }

        private void SetCallerCaptionSuppressed(RouteId caller, bool suppressed)
        {
            if (!_callerCaptionSuppressionBindings.TryGetValue(caller, out var setSuppressed))
                throw new InvalidOperationException(
                    $"Leave caller '{caller}' has no registered caption binding.");
            setSuppressed(suppressed);
        }

        private static void ValidateCaller(RouteId caller)
        {
            if (!RouteGraph.CanPresentLeaveConfirmation(caller))
                throw new ArgumentOutOfRangeException(nameof(caller));
        }

        private static void ValidateNested(LeaveRequest request)
        {
            if (!RouteGraph.CanPresentLeaveConfirmation(request.Caller)
                || request.ReturnLayer != LeaveReturnLayer.PauseOverlay
                || request.StackToken == Guid.Empty)
                throw new ArgumentOutOfRangeException(nameof(request));
        }

        private void RequireNested(LeaveRequest request)
        {
            ValidateNested(request);
            if (CurrentCaller != request.Caller || _nestedStackToken != request.StackToken)
                throw new InvalidOperationException("Nested Leave stack does not match the active Pause layer.");
        }

        private void RequireCurrentCaller(RouteId caller)
        {
            ValidateCaller(caller);
            if (!CurrentCaller.HasValue || CurrentCaller.Value != caller)
                throw new InvalidOperationException("Leave caller does not match the captured surface.");
        }

        private static Rect CenteredRect(float x, float y, float width, float height) =>
            new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
    }
}
