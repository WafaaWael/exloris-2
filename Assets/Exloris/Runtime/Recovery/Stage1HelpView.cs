using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Stage1;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class Stage1HelpView : MonoBehaviour, IStage1HelpView
    {
        public const string BackControlId = "stage_1.help.back";

        private readonly List<string> _ids = new List<string>();
        private ChildInputRouter _router;
        private Stage1Content _stageContent;
        private JourneyOverviewContent _sharedContent;
        private GameObject _canvas;
        private Image _scrim;
        private Image _panel;
        private Image _marker;
        private Image _socket;
        private Image _direction;
        private JourneyControlView _back;
        private bool _interruptionBlocked;

        public IReadOnlyList<string> ControlIds => _ids;
        public int DisplaySemanticControlCount => 0;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool BackInputEnabled => _back != null && _back.Semantic.Enabled;
        public Stage1HelpMode LastMode { get; private set; }

        public void Configure(ChildInputRouter router, Stage1Content stageContent, JourneyOverviewContent sharedContent)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _stageContent = stageContent ?? throw new ArgumentNullException(nameof(stageContent));
            _sharedContent = sharedContent ?? throw new ArgumentNullException(nameof(sharedContent));
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 1 Help layout is infeasible.");
            SetVisible(false);
        }

        public void Show(Stage1Snapshot snapshot, Stage1HelpMode mode)
        {
            LastMode = mode;
            var showSocket = mode == Stage1HelpMode.AttachThenMove || mode == Stage1HelpMode.Attach;
            var showDirection = mode != Stage1HelpMode.Attach;
            _socket.gameObject.SetActive(showSocket);
            _direction.gameObject.SetActive(showDirection);
            _direction.sprite = mode == Stage1HelpMode.AdjustDown
                ? _stageContent.Controls[6]
                : _stageContent.Controls[5];
            SetVisible(true);
            _router.SetVisibleStateReady(true);
        }

        public void Hide()
        {
            SetVisible(false);
            _router.SetVisibleStateReady(true);
        }

        public void SetInterruptionBlocked(bool blocked)
        {
            _interruptionBlocked = blocked;
            if (_back != null) _back.Semantic.Enabled = IsVisible && !blocked;
        }

        public void SetPauseOverlayPriority(bool priority)
        {
            if (_canvas != null) _canvas.GetComponent<Canvas>().sortingOrder = priority ? 550 : 320;
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            if (screenWidth <= 0f || screenHeight <= 0f || safeWidth <= 0f || safeHeight <= 0f
                || ppi <= 0f || platformMinimumPixels < 0) return false;
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var safeCanvasSize = new Vector2(safeWidth / scale, safeHeight / scale);
            var physicalTarget = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels);
            var target = Mathf.Max(60f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(physicalTarget, scale));
            var gapPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(ResponsiveLayoutProfile.MinimumGapMillimetres, ppi);
            var gap = Mathf.Max(18f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(gapPixels, scale));
            if (target + 2f * gap > safeCanvasSize.x || 2f * target + 2f * gap >= safeCanvasSize.y) return false;
            var panelSize = new Vector2(
                Mathf.Min(safeCanvasSize.x * 0.78f, 1000f),
                Mathf.Min(safeCanvasSize.y * 0.64f, 640f));
            JourneyViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, panelSize);
            var artSize = Mathf.Min(panelSize.y * 0.44f, target * 2.2f);
            JourneyViewFactory.SetCentered(_marker.rectTransform, new Vector2(-artSize * 0.7f, 0f), Vector2.one * artSize);
            JourneyViewFactory.SetCentered(_socket.rectTransform, Vector2.zero, Vector2.one * artSize);
            JourneyViewFactory.SetCentered(_direction.rectTransform, new Vector2(artSize * 0.7f, 0f), Vector2.one * artSize);
            JourneyViewFactory.SetBottom(_back, new Vector2(0f, target * 0.5f), target);
            _back.CanvasRect = new Rect(
                safeCanvasSize.x * 0.5f - target * 0.5f,
                0f, target, target);
            return true;
        }

        public bool TouchTargetTransparent() =>
            _back.TargetImage.color.a == 0f && !_back.TargetImage.raycastTarget;

        public Rect GetControlScreenBounds(string controlId)
        {
            if (controlId != BackControlId) throw new KeyNotFoundException(controlId);
            return JourneyViewFactory.ToScreenRect(_back.Target);
        }

        public bool DisplayArtRaycastsDisabled() =>
            !_panel.raycastTarget && !_marker.raycastTarget && !_socket.raycastTarget
            && !_direction.raycastTarget && !_back.VisibleArt.raycastTarget;

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("Stage1HelpCanvas", transform, 320);
            _scrim = JourneyViewFactory.CreateImage("HelpScrim", _canvas.transform, new Color(0f, 0f, 0f, 0.2f), null);
            JourneyViewFactory.Stretch(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            var safe = JourneyViewFactory.CreateSafeRoot(_canvas);
            _panel = JourneyViewFactory.CreateImage("HelpPanel", safe, Color.white, _stageContent.PresentationSprites[0]);
            _panel.type = Image.Type.Sliced;
            _marker = Art("Marker", _panel.transform, _stageContent.Props[0]);
            _socket = Art("Socket", _panel.transform, _stageContent.Props[1]);
            _direction = Art("Direction", _panel.transform, _stageContent.Controls[5]);
            _back = JourneyViewFactory.CreateControl("Back", safe, _sharedContent.LeaveControls[1], BackControlId);
            _ids.Add(BackControlId);
            _router.Register(_back.Semantic);
        }

        private static Image Art(string name, Transform parent, Sprite sprite)
        {
            var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite);
            image.preserveAspect = true;
            return image;
        }

        private void SetVisible(bool visible)
        {
            _canvas.SetActive(visible);
            _back.Semantic.Enabled = visible && !_interruptionBlocked;
        }
    }
}
