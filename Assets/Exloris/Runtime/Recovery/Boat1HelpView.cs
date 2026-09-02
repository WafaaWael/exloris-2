using System;
using System.Collections.Generic;
using Exloris.Runtime.Boat;
using Exloris.Runtime.Input;
using Exloris.Runtime.Overview;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class Boat1HelpView : MonoBehaviour, IBoat1HelpView
    {
        public const string BackControlId = "boat_1.help.back";
        private readonly List<string> _ids = new List<string>();
        private ChildInputRouter _router;
        private Boat1Content _content;
        private GameObject _canvas;
        private Image _scrim, _panel, _ibis, _direction;
        private JourneyControlView _back;
        private bool _interruptionBlocked;
        public IReadOnlyList<string> ControlIds => _ids;
        public int DisplaySemanticControlCount => 0;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool BackInputEnabled => _back != null && _back.Semantic.Enabled;
        public void SetInterruptionBlocked(bool blocked)
        {
            _interruptionBlocked = blocked;
            if (_back != null) _back.Semantic.Enabled = IsVisible && !blocked;
        }
        public void SetPauseOverlayPriority(bool priority)
        {
            if (_canvas != null) _canvas.GetComponent<Canvas>().sortingOrder = priority ? 550 : 310;
        }

        public void Configure(ChildInputRouter router, Boat1Content content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            BuildSurface(); ApplyResponsiveLayout(1920, 1080, 1856, 1016, 326, 88); SetVisible(false);
        }
        public void Show(Boat1Snapshot snapshot) { _direction.sprite = _content.Controls[snapshot.IbisDirection == BoatDirection.Right ? 3 : 2]; _ibis.sprite = _content.IbisSprites[6]; SetVisible(true); _router.SetVisibleStateReady(true); }
        public void Hide() { SetVisible(false); _router.SetVisibleStateReady(true); }
        public void ApplyResponsiveLayout(float screenWidth, float screenHeight, float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            var metrics = Boat1LayoutMetrics.Calculate(screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits(safeWidth, safeHeight)) throw new InvalidOperationException("Safe area cannot contain Boat 1 Help.");
            JourneyViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, new Vector2(Mathf.Min(safeWidth * 0.78f, 1000f), Mathf.Min(safeHeight * 0.64f, 640f)));
            var size = metrics.IbisHeight; JourneyViewFactory.SetCentered(_ibis.rectTransform, new Vector2(-size * 0.6f, 0), Vector2.one * size);
            JourneyViewFactory.SetCentered(_direction.rectTransform, new Vector2(size * 0.6f, 0), Vector2.one * size);
            JourneyViewFactory.SetBottom(_back, new Vector2(0, metrics.Target * 0.5f), metrics.Target);
            _back.CanvasRect = new Rect(safeWidth * 0.5f - metrics.Target * 0.5f, 0, metrics.Target, metrics.Target);
        }
        public Rect GetControlScreenBounds(string id) { if (id != BackControlId) throw new KeyNotFoundException(id); return JourneyViewFactory.ToScreenRect(_back.Target); }
        public bool DisplayArtRaycastsDisabled() => !_panel.raycastTarget && !_ibis.raycastTarget && !_direction.raycastTarget && !_back.VisibleArt.raycastTarget;
        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("Boat1HelpCanvas", transform, 310);
            _scrim = JourneyViewFactory.CreateImage("HelpScrim", _canvas.transform, new Color(0, 0, 0, 0.2f), null); JourneyViewFactory.Stretch(_scrim.rectTransform); _scrim.raycastTarget = true;
            var safe = JourneyViewFactory.CreateSafeRoot(_canvas); _panel = JourneyViewFactory.CreateImage("HelpPanel", safe, Color.white, _content.PresentationSprites[0]); _panel.type = Image.Type.Sliced;
            _ibis = JourneyViewFactory.CreateImage("IbisHelp.VisibleArt", _panel.transform, Color.white, _content.IbisSprites[6]); _ibis.preserveAspect = true;
            _direction = JourneyViewFactory.CreateImage("Direction.VisibleArt", _panel.transform, Color.white, _content.Controls[3]); _direction.preserveAspect = true;
            _back = JourneyViewFactory.CreateControl("Back", safe, _content.Controls[2], BackControlId); _ids.Add(BackControlId); _router.Register(_back.Semantic);
        }
        private void SetVisible(bool value)
        {
            _canvas.SetActive(value);
            _back.Semantic.Enabled = value && !_interruptionBlocked;
        }
    }
}
