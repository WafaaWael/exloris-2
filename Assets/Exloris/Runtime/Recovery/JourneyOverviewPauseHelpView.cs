using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class JourneyOverviewPauseHelpView : MonoBehaviour, IJourneyOverviewPauseHelpView
    {
        public const string BackControlId = "pause.help.back";

        private readonly List<string> _controlIds = new List<string>();
        private readonly Image[] _displayPictograms = new Image[4];
        private ChildInputRouter _router;
        private JourneyOverviewContent _content;
        private PauseOverlayView _pause;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private Image _scrim;
        private Image _panel;
        private JourneyControlView _back;
        private PauseOverlaySnapshot _snapshot;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public int DisplayPictogramCount => _displayPictograms.Length;
        public int DisplaySemanticControlCount => 0;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public int SortingOrder => _canvas.GetComponent<Canvas>().sortingOrder;

        public void Configure(
            ChildInputRouter router,
            JourneyOverviewContent content,
            PauseOverlayView pause)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            BuildSurface();
            ApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88);
            SetVisible(false);
        }

        public void ShowAbovePause(PauseOverlaySnapshot snapshot)
        {
            _snapshot = snapshot;
            _pause.SetControlsEnabled(false);
            SetVisible(true);
            _router.SetVisibleStateReady(true);
        }

        public void Hide()
        {
            SetVisible(false);
            _pause.SetControlsEnabled(true);
            _router.SetVisibleStateReady(true);
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
            if (metrics.Target > safeWidth || metrics.Target > safeHeight)
                throw new InvalidOperationException("Safe area cannot contain Journey Pause Help.");
            var panelSize = new Vector2(
                Mathf.Min(safeWidth * 0.82f, 1200f),
                Mathf.Min(safeHeight * 0.72f, 720f));
            JourneyViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, panelSize);
            var imageSize = metrics.Target * 0.72f;
            var step = metrics.Target * 0.97f;
            for (var index = 0; index < _displayPictograms.Length; index++)
            {
                var column = index % 2;
                var row = index / 2;
                JourneyViewFactory.SetCentered(
                    _displayPictograms[index].rectTransform,
                    new Vector2((column - 0.5f) * step, (0.5f - row) * step),
                    new Vector2(imageSize, imageSize));
            }
            JourneyViewFactory.SetBottom(
                _back, new Vector2(0f, metrics.Target * 0.5f), metrics.Target);
            _back.CanvasRect = new Rect(
                safeWidth * 0.5f - metrics.Target * 0.5f,
                0f,
                metrics.Target,
                metrics.Target);
        }

        public Sprite GetDisplayPictogram(int index) => _displayPictograms[index].sprite;
        public Rect GetDisplayPictogramScreenBounds(int index) =>
            JourneyViewFactory.ToScreenRect(_displayPictograms[index].rectTransform);
        public Rect GetControlScreenBounds(string controlId)
        {
            if (controlId != BackControlId) throw new KeyNotFoundException(controlId);
            return JourneyViewFactory.ToScreenRect(_back.Target);
        }
        public Sprite GetVisibleSprite(string controlId)
        {
            if (controlId != BackControlId) throw new KeyNotFoundException(controlId);
            return _back.VisibleArt.sprite;
        }
        public bool DisplayArtRaycastsDisabled()
        {
            if (_panel.raycastTarget || _back.VisibleArt.raycastTarget) return false;
            foreach (var display in _displayPictograms)
                if (display.raycastTarget) return false;
            return true;
        }

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("JourneyOverviewPauseHelpCanvas", transform, 300);
            _scrim = JourneyViewFactory.CreateImage(
                "HelpScrim", _canvas.transform, new Color(0f, 0f, 0f, 0.20f), null);
            JourneyViewFactory.Stretch(_scrim.rectTransform);
            _scrim.raycastTarget = true;
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            _panel = JourneyViewFactory.CreateImage(
                "HelpPanel", _safeRoot, Color.white, _content.CaptionBackplate);
            _panel.type = Image.Type.Sliced;
            for (var index = 0; index < _displayPictograms.Length; index++)
            {
                _displayPictograms[index] = JourneyViewFactory.CreateImage(
                    "DisplayOnly." + index,
                    _panel.transform,
                    Color.white,
                    _content.OverviewControls[index]);
                _displayPictograms[index].preserveAspect = true;
            }
            _back = JourneyViewFactory.CreateControl(
                "Back", _safeRoot, _content.LeaveControls[1], BackControlId);
            _controlIds.Add(BackControlId);
            _router.Register(_back.Semantic);
        }

        private void SetVisible(bool visible)
        {
            _back.Semantic.Enabled = visible;
            _canvas.SetActive(visible);
        }
    }
}
