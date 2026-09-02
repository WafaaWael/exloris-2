using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Stage2;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class Stage2HelpView : MonoBehaviour, IStage2HelpView
    {
        public const string BackControlId = "stage_2.help.back";
        private readonly List<string> _ids = new List<string>();
        private ChildInputRouter _router; private Stage2Content _content; private JourneyOverviewContent _shared;
        private GameObject _canvas; private Image _panel; private Image _stone; private readonly Image[] _places = new Image[3];
        private JourneyControlView _back; private bool _blocked;
        public IReadOnlyList<string> ControlIds => _ids;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool BackInputEnabled => _back != null && _back.Semantic.Enabled;
        public Stage2HelpMode LastMode { get; private set; }

        public void Configure(ChildInputRouter router, Stage2Content content, JourneyOverviewContent shared)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _shared = shared ?? throw new ArgumentNullException(nameof(shared));
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 2 Help layout is infeasible.");
            SetVisible(false);
        }

        public void Show(Stage2Snapshot snapshot, Stage2HelpMode mode)
        {
            LastMode = mode;
            _stone.sprite = _content.Stones[snapshot.SelectedRiver == Stage2River.Low ? 0
                : snapshot.SelectedRiver == Stage2River.High ? 2 : 1];
            SetVisible(true); _router.SetVisibleStateReady(true);
        }
        public void Hide() { SetVisible(false); _router.SetVisibleStateReady(true); }
        public void SetInterruptionBlocked(bool blocked) { _blocked = blocked; if (_back != null) _back.Semantic.Enabled = IsVisible && !blocked; }
        public void SetPauseOverlayPriority(bool priority) { if (_canvas != null) _canvas.GetComponent<Canvas>().sortingOrder = priority ? 550 : 330; }

        public bool TryApplyResponsiveLayout(float sw, float sh, float safeW, float safeH, float ppi, int minimum)
        {
            if (sw <= 0 || sh <= 0 || safeW <= 0 || safeH <= 0 || ppi <= 0 || minimum < 0) return false;
            var scale = ResponsiveLayoutProfile.CalculateCanvasScale(sw, sh);
            var safe = new Vector2(safeW / scale, safeH / scale);
            var target = Mathf.Max(60f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, minimum), scale));
            var gap = Mathf.Max(18f, ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(
                ResponsiveLayoutProfile.MillimetresToPhysicalPixels(ResponsiveLayoutProfile.MinimumGapMillimetres, ppi), scale));
            if (4f * target + 3f * gap > safe.x || 2f * target + 2f * gap >= safe.y) return false;
            var panel = new Vector2(Mathf.Min(safe.x * 0.84f, 1150f), Mathf.Min(safe.y * 0.68f, 680f));
            JourneyViewFactory.SetCentered(_panel.rectTransform, Vector2.zero, panel);
            var side = Mathf.Min(target * 1.8f, panel.y * 0.42f); var step = side + gap;
            JourneyViewFactory.SetCentered(_stone.rectTransform, new Vector2(-1.5f * step, 0), Vector2.one * side);
            for (var index = 0; index < 3; index++)
                JourneyViewFactory.SetCentered(_places[index].rectTransform, new Vector2((index - 0.25f) * step, 0), Vector2.one * side);
            JourneyViewFactory.SetBottom(_back, new Vector2(0, target * 0.5f), target);
            _back.CanvasRect = new Rect(safe.x * 0.5f - target * 0.5f, 0, target, target);
            return true;
        }

        public Rect GetControlScreenBounds(string id)
        { if (id != BackControlId) throw new KeyNotFoundException(id); return JourneyViewFactory.ToScreenRect(_back.Target); }

        private void BuildSurface()
        {
            _canvas = JourneyViewFactory.CreateCanvas("Stage2HelpCanvas", transform, 330);
            var scrim = JourneyViewFactory.CreateImage("HelpScrim", _canvas.transform, new Color(0, 0, 0, 0.2f), null);
            JourneyViewFactory.Stretch(scrim.rectTransform); scrim.raycastTarget = true;
            var safe = JourneyViewFactory.CreateSafeRoot(_canvas);
            _panel = JourneyViewFactory.CreateImage("HelpPanel", safe, Color.white, _content.PresentationSprites[0]); _panel.type = Image.Type.Sliced;
            _stone = Art("Stone", _panel.transform, _content.Stones[1]);
            for (var index = 0; index < 3; index++) _places[index] = Art("Place" + index, _panel.transform, _content.Hotspots[index]);
            _back = JourneyViewFactory.CreateControl("Back", safe, _shared.LeaveControls[1], BackControlId);
            _ids.Add(BackControlId); _router.Register(_back.Semantic);
        }
        private static Image Art(string name, Transform parent, Sprite sprite)
        { var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite); image.preserveAspect = true; return image; }
        private void SetVisible(bool visible) { _canvas.SetActive(visible); _back.Semantic.Enabled = visible && !_blocked; }
    }
}
