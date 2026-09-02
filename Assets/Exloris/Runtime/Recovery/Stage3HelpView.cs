using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Stage3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Recovery
{
    [DisallowMultipleComponent]
    public sealed class Stage3HelpView : MonoBehaviour, IStage3HelpView
    {
        public const string BackControlId = "stage_3.help.back";
        private readonly List<string> _ids = new List<string>();
        private ChildInputRouter _router; private Stage3Content _content;
        private GameObject _canvas; private Image _panel; private readonly Image[] _plans = new Image[3];
        private readonly TextMeshProUGUI[] _riverLabels = new TextMeshProUGUI[3];
        private JourneyControlView _back; private bool _blocked;
        private Stage3Snapshot _caller;
        public IReadOnlyList<string> ControlIds => _ids;
        public bool IsVisible => _canvas != null && _canvas.activeSelf;
        public bool BackInputEnabled => _back != null && _back.Semantic.Enabled;
        public Stage3HelpMode LastMode { get; private set; }
        public int VisibleRiverLabelCount => _riverLabels.Count(label => label != null && label.gameObject.activeSelf);

        public void Configure(ChildInputRouter router, Stage3Content content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 3 Help layout is infeasible.");
            SetVisible(false);
        }
        public void Present(Stage3HelpMode mode, Stage3Snapshot caller, bool reducedMotion)
        {
            LastMode = mode; _caller = caller; ConfigureGuides(); SetVisible(true);
        }
        public bool UsesOutcome(Stage3OutcomeKey outcome) => LastMode == Stage3HelpMode.OutcomeFacts
            && outcome.Benefit >= Stage3Benefit.Grain && outcome.Benefit <= Stage3Benefit.Boat
            && outcome.Need >= Stage3Need.WaterRoute && outcome.Need <= Stage3Need.FieldWater
            && _plans[0].sprite == _content.OutcomeLayers[(int)outcome.Benefit - 1]
            && _plans[1].sprite == _content.OutcomeLayers[3 + (int)outcome.Need - 1]
            && !_plans[2].gameObject.activeSelf;
        public void SetVisible(bool visible)
        { if (_canvas != null) _canvas.SetActive(visible); if (_back != null) _back.Semantic.Enabled = visible && !_blocked; }
        public void SetInterruptionBlocked(bool blocked) { _blocked = blocked; SetVisible(IsVisible); }
        public void SetPauseOverlayPriority(bool priority)
        { if (_canvas != null) _canvas.GetComponent<Canvas>().sortingOrder = priority ? 550 : 340; }

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
            for (var index = 0; index < 3; index++)
            {
                JourneyViewFactory.SetCentered(_plans[index].rectTransform, new Vector2((index - 1) * step, 0), Vector2.one * side);
                JourneyViewFactory.SetCentered(_riverLabels[index].rectTransform, Vector2.zero, Vector2.one * side * 0.82f);
            }
            JourneyViewFactory.SetBottom(_back, new Vector2(0, target * 0.5f), target);
            _back.CanvasRect = new Rect(safe.x * 0.5f - target * 0.5f, 0, target, target);
            return true;
        }
        public Rect GetControlScreenBounds(string id)
        { if (id != BackControlId) throw new KeyNotFoundException(id); return JourneyViewFactory.ToScreenRect(_back.Target); }

        private void BuildSurface()
        {
            _canvas = JourneyViewFactory.CreateCanvas("Stage3HelpCanvas", transform, 340);
            var scrim = JourneyViewFactory.CreateImage("HelpScrim", _canvas.transform, new Color(0, 0, 0, 0.2f), null);
            JourneyViewFactory.Stretch(scrim.rectTransform); scrim.raycastTarget = true;
            var safe = JourneyViewFactory.CreateSafeRoot(_canvas);
            _panel = JourneyViewFactory.CreateImage("HelpPanel", safe, Color.white, _content.PresentationSprites[0]);
            _panel.type = Image.Type.Sliced;
            var riverNames = new[] { "LOW\nNILE", "MIDDLE\nNILE", "HIGH\nNILE" };
            for (var index = 0; index < 3; index++)
            {
                _plans[index] = Art("Guide" + index, _panel.transform, _content.Plans[index]);
                _riverLabels[index] = JourneyViewFactory.CreateRuntimeText("RiverLabel" + index, _plans[index].transform);
                _riverLabels[index].text = riverNames[index]; _riverLabels[index].fontSize = 28;
                _riverLabels[index].alignment = TextAlignmentOptions.Center;
                _riverLabels[index].color = new Color32(61, 43, 29, 255);
                _riverLabels[index].raycastTarget = false;
            }
            _back = JourneyViewFactory.CreateControl("Back", safe, _content.Controls[9], BackControlId);
            _ids.Add(BackControlId); _router.Register(_back.Semantic);
        }
        private void ConfigureGuides()
        {
            for (var index = 0; index < 3; index++)
            {
                _plans[index].gameObject.SetActive(true); _plans[index].color = Color.white;
                _riverLabels[index].gameObject.SetActive(false);
            }
            if (LastMode == Stage3HelpMode.OutcomeFacts)
            {
                _plans[0].sprite = _content.OutcomeLayers[(int)_caller.CurrentOutcome.Benefit - 1];
                _plans[1].sprite = _content.OutcomeLayers[3 + (int)_caller.CurrentOutcome.Need - 1];
                _plans[2].gameObject.SetActive(false);
                return;
            }
            if (LastMode == Stage3HelpMode.RiverSelection)
            {
                for (var index = 0; index < 3; index++)
                {
                    _plans[index].sprite = _content.Controls[7];
                    _plans[index].color = (int)_caller.SelectedRiver - 1 == index
                        ? Color.white : new Color(0.68f, 0.68f, 0.68f, 1f);
                    _riverLabels[index].gameObject.SetActive(true);
                }
                return;
            }
            for (var index = 0; index < 3; index++) _plans[index].sprite = _content.Plans[index];
        }
        private static Image Art(string name, Transform parent, Sprite sprite)
        { var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite); image.preserveAspect = true; return image; }
    }
}
