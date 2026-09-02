using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Stage2
{
    [DisallowMultipleComponent]
    public sealed class Stage2StageView : MonoBehaviour, IStage2View, IStage2MediaCueSink, IStage2OutcomeSink
    {
        public const string LowControlId = "stage_2.low";
        public const string MiddleControlId = "stage_2.middle";
        public const string HighControlId = "stage_2.high";
        public const string FieldControlId = "stage_2.field";
        public const string PathControlId = "stage_2.path";
        public const string BoatControlId = "stage_2.boat";
        public const string PlayControlId = "stage_2.play";
        public const string HelpControlId = "stage_2.help";
        public const string RepeatControlId = "stage_2.repeat";
        public const string PauseControlId = "stage_2.pause";
        public const string HomeControlId = "stage_2.home";
        public const string CompareControlId = "stage_2.compare";
        public const string FinishControlId = "stage_2.finish";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _ids = new List<string>();
        private readonly List<Image> _displayArt = new List<Image>();
        private Stage2Content _content;
        private ChildInputRouter _router;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _outcomeLayer;
        private Image _selectedChild;
        private Image _captionBackplate;
        private Image _visualEquivalent;
        private TextMeshProUGUI _caption;
        private Stage2LayoutMetrics _metrics;
        private Stage2Snapshot _snapshot;
        private Vector2 _safeSize;
        private float _screenWidth = 1920f;
        private float _screenHeight = 1080f;
        private bool _inputReady;
        private bool _visible = true;
        private bool _mediaLocked;
        private bool _selectedGirl;
        private bool _captionsSuppressed;

        public IReadOnlyList<string> ControlIds => _ids;
        public int SemanticControlCount => _ids.Count;
        public int DisplaySemanticControlCount => 0;
        public Color32 CaptionTextColor => _caption == null ? new Color32(0, 0, 0, 0) : _caption.color;
        public Rect SelectedChildCanvasRect { get; private set; }
        public Sprite SelectedChildSprite => _selectedChild?.sprite;
        public Stage2MediaCue LastMediaCue { get; private set; }
        public Stage2OutcomeFrame LastOutcomeFrame { get; private set; }
        public Stage2PauseReason? LastPauseReason { get; private set; }
        public event Action<Stage2PauseReason> PauseOverlayRequested;
        public IReadOnlyList<string> EnabledControlIds => _controls
            .Where(pair => pair.Value.Semantic.Enabled).Select(pair => pair.Key).ToArray();

        public void Configure(ChildInputRouter router, Stage2Content content, bool selectedGirl)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _selectedGirl = selectedGirl;
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 0, 0, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 2 layout is infeasible.");
            _snapshot = new Stage2Snapshot(Guid.Empty, Stage2State.Hidden, Stage2River.None,
                Stage2ObservedHotspots.None, Stage2ObservedHotspots.None, Stage2ObservedHotspots.None,
                Stage2Hotspot.None, Guid.Empty, Guid.Empty, 0, 0, null, 0, false);
            Present(_snapshot, false);
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            var metrics = Stage2LayoutMetrics.Calculate(screenWidth, screenHeight, safeX, safeY,
                safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits) return false;
            _metrics = metrics;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _safeSize = metrics.SafeCanvasRect.size;
            ConfigureBackground();
            LayoutDisplayArt();
            LayoutControls();
            UpdateControls();
            return true;
        }

        public void Present(Stage2Snapshot snapshot, bool reducedMotion)
        {
            _snapshot = snapshot;
            ConfigureBackground();
            ConfigureOutcomeArt();
            LayoutDisplayArt();
            LayoutControls();
            UpdateControls();
        }

        public void SetInputReady(bool ready)
        {
            _inputReady = ready;
            UpdateControls();
        }

        public void SetCallerInputLocked(bool locked)
        {
            _mediaLocked = locked;
            UpdateControls();
        }

        public void PresentMediaCue(Stage2MediaCue cue)
        {
            LastMediaCue = cue;
            _visualEquivalent.sprite = cue.VisualEquivalent;
            _visualEquivalent.gameObject.SetActive(true);
            _caption.text = Stage2CaptionStrings.ResolveEnglish(cue.CaptionId);
            var captionVisible = cue.CaptionVisible && !_captionsSuppressed;
            _captionBackplate.gameObject.SetActive(captionVisible);
            _caption.gameObject.SetActive(captionVisible);
        }

        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionsSuppressed = suppressed;
            if (!suppressed) return;
            _captionBackplate.gameObject.SetActive(false);
            _caption.gameObject.SetActive(false);
        }

        public void FreezeMediaCue(double elapsedSeconds) { }
        public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }

        public void ShowPauseOverlay(Stage2PauseReason reason)
        {
            LastPauseReason = reason;
            PauseOverlayRequested?.Invoke(reason);
        }

        public void PresentOutcome(Stage2OutcomeFrame frame)
        {
            LastOutcomeFrame = frame;
            _outcomeLayer.sprite = frame.OutcomeLayer;
            _outcomeLayer.gameObject.SetActive(true);
            _selectedChild.gameObject.SetActive(true);
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_canvas != null) _canvas.SetActive(visible);
            UpdateControls();
        }

        public Rect GetControlCanvasRect(string id) => _controls[id].CanvasRect;
        public Rect GetControlScreenBounds(string id) => JourneyViewFactory.ToScreenRect(_controls[id].Target);

        public bool AreTouchTargetsTransparent() => _controls.Values.All(control =>
            control.TargetImage.color.a == 0f && !control.TargetImage.raycastTarget);

        public bool VisibleArtRaycastsDisabled() => _controls.Values.All(control => !control.VisibleArt.raycastTarget)
            && _displayArt.All(image => !image.raycastTarget) && !_caption.raycastTarget;

        public bool DoEnabledTouchTargetsOverlap()
        {
            var enabled = _controls.Values.Where(control => control.Semantic.Enabled).ToArray();
            for (var first = 0; first < enabled.Length; first++)
            for (var second = first + 1; second < enabled.Length; second++)
                if (enabled[first].CanvasRect.Overlaps(enabled[second].CanvasRect)) return true;
            return false;
        }

        public bool AreAllEnabledTargetsInsideSafeArea() => _controls.Values
            .Where(control => control.Semantic.Enabled).All(control =>
                control.CanvasRect.xMin >= 0f && control.CanvasRect.yMin >= 0f
                && control.CanvasRect.xMax <= _safeSize.x && control.CanvasRect.yMax <= _safeSize.y);

        public bool VisibleArtContainedByTargets() => _controls.Values
            .Where(control => control.Target.gameObject.activeSelf).All(control =>
                control.VisibleArt.rectTransform.sizeDelta.x <= control.Target.sizeDelta.x + 0.001f
                && control.VisibleArt.rectTransform.sizeDelta.y <= control.Target.sizeDelta.y + 0.001f);

        public static int SelectRatioIndex(float width, float height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException();
            var aspect = width / height;
            if (aspect < 1.5f) return 0;
            if (aspect < 2.05f) return 1;
            return 2;
        }

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("Stage2Canvas", transform, 160);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform, _content.Backgrounds[3], _content.Backgrounds[4], _content.Backgrounds[5]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            Add(LowControlId, "Low", _content.Stones[0]);
            Add(MiddleControlId, "Middle", _content.Stones[1]);
            Add(HighControlId, "High", _content.Stones[2]);
            Add(FieldControlId, "Field", _content.Hotspots[0]);
            Add(PathControlId, "Path", _content.Hotspots[1]);
            Add(BoatControlId, "Boat", _content.Hotspots[2]);
            Add(PlayControlId, "Play", _content.Controls[0]);
            Add(HelpControlId, "Help", _content.Controls[1]);
            Add(RepeatControlId, "Repeat", _content.Controls[2]);
            Add(PauseControlId, "Pause", _content.Controls[3]);
            Add(HomeControlId, "Home", _content.Controls[4]);
            Add(CompareControlId, "Compare", _content.Controls[5]);
            Add(FinishControlId, "Finish", _content.Controls[6]);
            _outcomeLayer = Art("OutcomeLayer", _safeRoot, _content.OutcomeLayers[0]);
            _selectedChild = Art("SelectedChild", _safeRoot, _content.ChildSprites[_selectedGirl ? 1 : 0]);
            _captionBackplate = Art("CaptionBackplate", _safeRoot, _content.PresentationSprites[0]);
            _captionBackplate.type = Image.Type.Sliced;
            _visualEquivalent = Art("VisualEquivalent", _safeRoot, _content.PresentationSprites[2]);
            _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _caption.color = new Color32(61, 43, 29, 255);
            _captionBackplate.gameObject.SetActive(false);
            _visualEquivalent.gameObject.SetActive(false);
        }

        private Image Art(string name, Transform parent, Sprite sprite)
        {
            var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite);
            image.preserveAspect = true;
            _displayArt.Add(image);
            return image;
        }

        private void Add(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _ids.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutControls()
        {
            if (_controls.Count == 0 || !_metrics.Fits) return;
            var top = _metrics.TopCenters;
            SetControlRect(_controls[HelpControlId], top[0], Vector2.one * _metrics.Target);
            SetControlRect(_controls[RepeatControlId], top[1], Vector2.one * _metrics.Target);
            SetControlRect(_controls[PauseControlId], top[2], Vector2.one * _metrics.Target);
            SetControlRect(_controls[HomeControlId], top[3], Vector2.one * _metrics.Target);
            SetControlRect(_controls[PlayControlId], _metrics.BottomCenters[1], Vector2.one * _metrics.Target);
            SetControlRect(_controls[CompareControlId], _metrics.BottomCenters[0], Vector2.one * _metrics.Target);
            SetControlRect(_controls[FinishControlId], _metrics.BottomCenters[2], Vector2.one * _metrics.Target);
            SetControlRect(_controls[LowControlId], _metrics.ChoiceCenters[0], _metrics.ChoiceSize);
            SetControlRect(_controls[MiddleControlId], _metrics.ChoiceCenters[1], _metrics.ChoiceSize);
            SetControlRect(_controls[HighControlId], _metrics.ChoiceCenters[2], _metrics.ChoiceSize);
            SetControlRect(_controls[FieldControlId], _metrics.HotspotCenters[0], _metrics.HotspotSize);
            SetControlRect(_controls[PathControlId], _metrics.HotspotCenters[1], _metrics.HotspotSize);
            SetControlRect(_controls[BoatControlId], _metrics.HotspotCenters[2], _metrics.HotspotSize);
        }

        private void LayoutDisplayArt()
        {
            if (!_metrics.Fits) return;
            SetImageRect(_captionBackplate, _metrics.CaptionRect);
            SetImageRect(_visualEquivalent, new Rect(_metrics.CaptionRect.xMin, _metrics.CaptionRect.yMin,
                Mathf.Min(_metrics.CaptionRect.width * 0.32f, _metrics.CaptionRect.height * 2f),
                _metrics.CaptionRect.height));
            SetImageRect(_outcomeLayer, _metrics.OutcomeRect);
            SetImageRect(_selectedChild, _metrics.ChildRect);
            SelectedChildCanvasRect = _metrics.ChildRect;
        }

        private void SetControlRect(JourneyControlView control, Vector2 center, Vector2 size)
        {
            JourneyViewFactory.SetCentered(control.Target,
                new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f), size);
            JourneyViewFactory.SetCentered(control.VisibleArt.rectTransform, Vector2.zero, size * 0.72f);
            control.CanvasRect = new Rect(center - size * 0.5f, size);
        }

        private void SetImageRect(Image image, Rect rect)
        {
            JourneyViewFactory.SetCentered(image.rectTransform,
                new Vector2(rect.center.x - _safeSize.x * 0.5f, rect.center.y - _safeSize.y * 0.5f), rect.size);
        }

        private void ConfigureBackground()
        {
            if (_background == null || _content == null) return;
            var start = _snapshot.SelectedRiver == Stage2River.Low ? 0
                : _snapshot.SelectedRiver == Stage2River.High ? 6 : 3;
            _background.Configure(_content.Backgrounds[start], _content.Backgrounds[start + 1], _content.Backgrounds[start + 2]);
            _background.Present(_screenWidth, _screenHeight);
        }

        private void ConfigureOutcomeArt()
        {
            if (_outcomeLayer == null) return;
            var visible = (_snapshot.State == Stage2State.ConsequencePlaying || _snapshot.State == Stage2State.Inspecting)
                && _snapshot.SelectedRiver != Stage2River.None && _snapshot.CurrentHotspot != Stage2Hotspot.None;
            _outcomeLayer.gameObject.SetActive(visible);
            _selectedChild.gameObject.SetActive(visible);
            if (!visible) return;
            var riverIndex = (int)_snapshot.SelectedRiver - 1;
            var hotspotIndex = (int)_snapshot.CurrentHotspot - 1;
            _outcomeLayer.sprite = _content.OutcomeLayers[riverIndex * 3 + hotspotIndex];
        }

        private void UpdateControls()
        {
            foreach (var pair in _controls)
            {
                var visible = IsVisibleForState(pair.Key);
                pair.Value.Target.gameObject.SetActive(visible);
                pair.Value.Semantic.Enabled = visible && _visible && _inputReady && !_mediaLocked
                    && IsEnabledForState(pair.Key);
            }
        }

        private bool IsVisibleForState(string id)
        {
            switch (_snapshot.State)
            {
                case Stage2State.Intro:
                    return IsCommon(id) || id == PlayControlId;
                case Stage2State.Choosing:
                    return IsCommon(id) || IsRiverControl(id);
                case Stage2State.Inspecting:
                    return IsCommon(id) || IsHotspotControl(id) || id == CompareControlId || id == FinishControlId;
                case Stage2State.ConsequencePlaying:
                    return IsCommon(id) || IsHotspotControl(id);
                default:
                    return false;
            }
        }

        private bool IsEnabledForState(string id)
        {
            if (_snapshot.State == Stage2State.ConsequencePlaying)
                return id == PauseControlId || id == HomeControlId;
            if (id == FinishControlId) return ObservedForSelectedRiver() == Stage2ObservedHotspots.All;
            return true;
        }

        private Stage2ObservedHotspots ObservedForSelectedRiver()
        {
            switch (_snapshot.SelectedRiver)
            {
                case Stage2River.Low: return _snapshot.LowObserved;
                case Stage2River.Middle: return _snapshot.MiddleObserved;
                case Stage2River.High: return _snapshot.HighObserved;
                default: return Stage2ObservedHotspots.None;
            }
        }

        private static bool IsCommon(string id) => id == HelpControlId || id == RepeatControlId
            || id == PauseControlId || id == HomeControlId;
        private static bool IsRiverControl(string id) => id == LowControlId || id == MiddleControlId || id == HighControlId;
        private static bool IsHotspotControl(string id) => id == FieldControlId || id == PathControlId || id == BoatControlId;
    }
}
