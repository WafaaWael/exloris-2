using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Stage1;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using Exloris.Runtime.Completion;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class PauseOverlayView : MonoBehaviour, IPauseOverlayView
    {
        public const string HelpControlId = "pause.help";
        public const string SoundControlId = "pause.sound";
        public const string CaptionsControlId = "pause.captions";
        public const string ResumeControlId = "pause.resume";
        public const string MotionControlId = "pause.motion";
        public const string HomeControlId = "pause.home";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _controlIds = new List<string>();
        private ChildInputRouter _router;
        private JourneyOverviewContent _content;
        private JourneyOverviewStageView _caller;
        private Boat1StageView _boatCaller;
        private Boat2StageView _boat2Caller;
        private Boat3StageView _boat3Caller;
        private Stage1StageView _stage1Caller;
        private Stage2StageView _stage2Caller;
        private Stage3StageView _stage3Caller;
        private CompletionStageView _completionCaller;
        private bool _helpAvailable = true;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private Image _scrim;
        private Image _panel;
        private Image _pauseSymbol;
        private Vector2 _safeSize;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool ScrimBlocksRaycasts => _scrim != null && _scrim.raycastTarget;
        public PauseOverlaySnapshot Snapshot { get; private set; }

        public void Configure(
            ChildInputRouter router,
            JourneyOverviewContent content,
            JourneyOverviewStageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface();
            ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88);
            SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Boat1StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _boatCaller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Boat2StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _boat2Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Boat3StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _boat3Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Stage1StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _stage1Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Stage2StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _stage2Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, Stage3StageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _stage3Caller = caller ?? throw new ArgumentNullException(nameof(caller));
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
        }

        public void Configure(ChildInputRouter router, JourneyOverviewContent content, CompletionStageView caller)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _completionCaller = caller ?? throw new ArgumentNullException(nameof(caller));
            _helpAvailable = false;
            BuildSurface(); ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88); SetOverlayActive(false);
            _controls[HelpControlId].VisibleArt.gameObject.SetActive(false);
        }

        public void ShowAndBlockCaller(PauseOverlaySnapshot snapshot)
        {
            Snapshot = snapshot;
            SetCallerEnabled(false);
            SetOverlayActive(true);
            _router.SetVisibleStateReady(true);
        }

        public void RestoreCaller(PauseOverlaySnapshot snapshot)
        {
            RequireSnapshot(snapshot);
            SetOverlayActive(false);
            SetCallerEnabled(true);
            _router.SetVisibleStateReady(true);
        }

        public void HoldBlockedForChild(PauseOverlaySnapshot snapshot)
        {
            RequireSnapshot(snapshot);
            SetControlsEnabled(false);
            SetCallerEnabled(false);
            _router.SetVisibleStateReady(true);
        }

        public void RestoreFromChild(PauseOverlaySnapshot snapshot)
        {
            RequireSnapshot(snapshot);
            _canvas.SetActive(true);
            SetControlsEnabled(true);
            SetCallerEnabled(false);
            _router.SetVisibleStateReady(true);
        }

        public void HoldBlockedForTransition(PauseOverlaySnapshot snapshot)
        {
            RequireSnapshot(snapshot);
            SetControlsEnabled(false);
            SetCallerEnabled(false);
            _router.SetVisibleStateReady(false);
        }

        public void ApplySettings(PauseSettings settings)
        {
            if (_content == null) return;
            if (Snapshot.StackToken != Guid.Empty)
                Snapshot = Snapshot.WithSettings(settings);
            _controls[SoundControlId].VisibleArt.sprite =
                settings.SoundEnabled ? _content.PauseControls[2] : _content.PauseControls[3];
            _controls[CaptionsControlId].VisibleArt.sprite =
                settings.CaptionsEnabled ? _content.PauseControls[4] : _content.PauseControls[5];
            _controls[MotionControlId].VisibleArt.sprite =
                settings.ReducedMotion ? _content.PauseControls[6] : _content.PauseControls[7];
        }

        public void SetControlsEnabled(bool enabled)
        {
            foreach (var control in _controls.Values)
                control.Semantic.Enabled = enabled && (_helpAvailable || control.Semantic.Id != HelpControlId);
        }

        private void SetCallerEnabled(bool enabled)
        {
            if (_caller != null) _caller.SetControlsEnabled(enabled);
            else if (_boatCaller != null) _boatCaller.SetInputReady(enabled);
            else if (_boat2Caller != null) _boat2Caller.SetInputReady(enabled);
            else if (_boat3Caller != null) _boat3Caller.SetInputReady(enabled);
            else if (_stage1Caller != null) _stage1Caller.SetInputReady(enabled);
            else if (_stage2Caller != null) _stage2Caller.SetInputReady(enabled);
            else if (_stage3Caller != null) _stage3Caller.SetInputReady(enabled);
            else if (_completionCaller != null) _completionCaller.SetInputReady(enabled);
        }

        public void ApplyResponsiveLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            var metrics = JourneyOverviewLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (3f * metrics.Target + 2f * metrics.Gap > safeWidth
                || 2f * metrics.Target > safeHeight)
                throw new InvalidOperationException("Safe area cannot contain the frozen Pause layout.");
            _safeSize = new Vector2(safeWidth, safeHeight);
            LayoutRow(new[] { HelpControlId, SoundControlId, CaptionsControlId },
                true, metrics, safeWidth, safeHeight);
            LayoutRow(new[] { ResumeControlId, MotionControlId, HomeControlId },
                false, metrics, safeWidth, safeHeight);
            var panelSize = new Vector2(
                Mathf.Min(safeWidth * 0.82f, 1200f),
                Mathf.Min(safeHeight * 0.72f, 720f));
            JourneyViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, panelSize);
            var symbolSize = Mathf.Min(panelSize.x, panelSize.y) * 0.32f;
            JourneyViewFactory.SetCentered(
                _pauseSymbol.rectTransform, Vector2.zero, new Vector2(symbolSize, symbolSize));
        }

        public Sprite GetVisibleSprite(string controlId) => _controls[controlId].VisibleArt.sprite;
        public Rect GetControlCanvasRect(string controlId) => _controls[controlId].CanvasRect;
        public Rect GetControlScreenBounds(string controlId) =>
            JourneyViewFactory.ToScreenRect(_controls[controlId].Target);
        public int SortingOrder => _canvas.GetComponent<Canvas>().sortingOrder;
        public void SetBoatInterruptionPriority(bool priority)
        {
            if (_canvas != null) _canvas.GetComponent<Canvas>().sortingOrder = priority ? 500 : 200;
        }
        public bool AreTouchTargetsTransparent()
        {
            foreach (var control in _controls.Values)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            return true;
        }
        public bool NonScrimArtRaycastsDisabled()
        {
            if (_panel.raycastTarget || _pauseSymbol.raycastTarget) return false;
            foreach (var control in _controls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            return true;
        }
        public bool DoTouchTargetsOverlap() => JourneyViewFactory.TargetsOverlap(_controls.Values);
        public bool AreAllTargetsInsideSafeArea() => JourneyViewFactory.Inside(_controls.Values, _safeSize);

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("PauseOverlayCanvas", transform, 200);
            _scrim = JourneyViewFactory.CreateImage(
                "ModalScrim", _canvas.transform, new Color(0f, 0f, 0f, 0.52f), null);
            JourneyViewFactory.Stretch(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            _panel = JourneyViewFactory.CreateImage(
                "PausePanel", _safeRoot, Color.white, _content.CaptionBackplate);
            _panel.type = Image.Type.Sliced;
            _pauseSymbol = JourneyViewFactory.CreateImage(
                "PauseSymbol.DisplayOnly", _panel.transform, Color.white, _content.OverviewControls[2]);
            _pauseSymbol.preserveAspect = true;

            AddControl(HelpControlId, "Help", _content.PauseControls[0]);
            AddControl(SoundControlId, "Sound", _content.PauseControls[2]);
            AddControl(CaptionsControlId, "Captions", _content.PauseControls[4]);
            AddControl(ResumeControlId, "Resume", _content.PauseControls[1]);
            AddControl(MotionControlId, "Motion", _content.PauseControls[7]);
            AddControl(HomeControlId, "Home", _content.OverviewControls[3]);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutRow(
            string[] ids,
            bool top,
            JourneyOverviewLayoutMetrics metrics,
            float safeWidth,
            float safeHeight)
        {
            for (var index = 0; index < ids.Length; index++)
            {
                var x = safeWidth * 0.5f + (index - 1f) * (metrics.Target + metrics.Gap);
                var y = top ? safeHeight - metrics.Target * 0.5f : metrics.Target * 0.5f;
                var control = _controls[ids[index]];
                if (top)
                    JourneyViewFactory.SetTop(control,
                        new Vector2(x - safeWidth * 0.5f, -metrics.Target * 0.5f), metrics.Target);
                else
                    JourneyViewFactory.SetBottom(control,
                        new Vector2(x - safeWidth * 0.5f, metrics.Target * 0.5f), metrics.Target);
                control.CanvasRect = CenteredRect(x, y, metrics.Target, metrics.Target);
            }
        }

        private void SetOverlayActive(bool active)
        {
            SetControlsEnabled(active);
            _canvas.SetActive(active);
        }

        private void RequireSnapshot(PauseOverlaySnapshot snapshot)
        {
            if (!Snapshot.Equals(snapshot))
                throw new InvalidOperationException("Pause overlay snapshot does not match the active caller.");
        }

        private static Rect CenteredRect(float x, float y, float width, float height) =>
            new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
    }
}
