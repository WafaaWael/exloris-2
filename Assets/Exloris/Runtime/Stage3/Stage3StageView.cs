using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Stage2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Stage3
{
    [DisallowMultipleComponent]
    public sealed class Stage3StageView : MonoBehaviour, IStage3View, IStage3MediaCueSink, IStage3OutcomeSink
    {
        public const string GrainControlId = "stage_3.plan.grain";
        public const string ChannelControlId = "stage_3.plan.channel";
        public const string BoatPlanControlId = "stage_3.plan.boat";
        public const string LowControlId = "stage_3.river.low";
        public const string MiddleControlId = "stage_3.river.middle";
        public const string HighControlId = "stage_3.river.high";
        public const string PlayControlId = "stage_3.play";
        public const string HelpControlId = "stage_3.help";
        public const string RepeatControlId = "stage_3.repeat";
        public const string PauseControlId = "stage_3.pause";
        public const string HomeControlId = "stage_3.home";
        public const string FinishControlId = "stage_3.finish";
        public const string AnotherPlanControlId = "stage_3.another_preparation";
        public const string AnotherRiverControlId = "stage_3.another_river";
        public const string WatchAgainControlId = "stage_3.watch_again";
        public const string RiverBackControlId = "stage_3.river.back";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _ids = new List<string>();
        private readonly List<Image> _displayArt = new List<Image>();
        private Stage3Content _content;
        private ChildInputRouter _router;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _benefitLayer;
        private Image _needLayer;
        private Image _selectedChild;
        private Image _selectedPlan;
        private Image _captionBackplate;
        private Image _visualEquivalent;
        private TextMeshProUGUI _caption;
        private TextMeshProUGUI _riverLabel;
        private Stage3LayoutMetrics _metrics;
        private Stage3Snapshot _snapshot;
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
        public Rect BenefitCanvasRect { get; private set; }
        public Rect NeedCanvasRect { get; private set; }
        public Rect SelectedChildCanvasRect { get; private set; }
        public Sprite SelectedChildSprite => _selectedChild?.sprite;
        public Sprite SelectedPlanSprite => _selectedPlan?.sprite;
        public string SelectedRiverLabel => _riverLabel?.text;
        public bool OutcomeLayerColorsEqual => _benefitLayer != null && _needLayer != null
            && _benefitLayer.color.Equals(_needLayer.color);
        public float OutcomeLayerOpacity => _benefitLayer == null ? 0f : _benefitLayer.color.a;
        public Stage3MediaCue LastMediaCue { get; private set; }
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeSelf;
        public bool VisualEquivalentVisible => _visualEquivalent != null && _visualEquivalent.gameObject.activeSelf;
        public Stage3OutcomePresentation LastOutcomePresentation { get; private set; }
        public IReadOnlyList<string> EnabledControlIds => _controls
            .Where(pair => pair.Value.Semantic.Enabled).Select(pair => pair.Key).ToArray();

        public void Configure(ChildInputRouter router, Stage3Content content, bool selectedGirl)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _selectedGirl = selectedGirl;
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 0, 0, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 3 layout is infeasible.");
            _snapshot = new Stage3Snapshot(Guid.Empty, Stage3State.Hidden, Stage2River.None,
                Stage3Plan.None, default, Stage3TriedCombinations.None, Guid.Empty, Guid.Empty,
                0, 0, null, 0, false);
            Present(_snapshot, false);
        }

        public bool TryApplyResponsiveLayout(float screenWidth, float screenHeight,
            float safeX, float safeY, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var metrics = Stage3LayoutMetrics.Calculate(screenWidth, screenHeight, safeX, safeY,
                safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits) return false;
            _metrics = metrics; _screenWidth = screenWidth; _screenHeight = screenHeight;
            _safeSize = metrics.SafeCanvasRect.size;
            ConfigureBackground(); LayoutDisplayArt(); LayoutControls(); UpdateControls();
            return true;
        }

        public void Present(Stage3Snapshot snapshot, bool reducedMotion)
        {
            _snapshot = snapshot;
            ConfigureBackground(); ConfigureOutcomeArt(); LayoutDisplayArt(); LayoutControls(); UpdateControls();
        }

        public void Present(Stage3MediaCue cue)
        {
            LastMediaCue = cue;
            _visualEquivalent.sprite = cue.VisualEquivalent;
            _visualEquivalent.gameObject.SetActive(cue.Kind != Stage3MediaCueKind.None);
            _caption.text = Stage3CaptionStrings.ResolveEnglish(cue.CaptionId);
            var captionVisible = cue.CaptionVisible && !_captionsSuppressed;
            _captionBackplate.gameObject.SetActive(captionVisible);
            _caption.gameObject.SetActive(captionVisible);
        }

        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionsSuppressed = suppressed;
            if (!suppressed) return;
            _captionBackplate.gameObject.SetActive(false); _caption.gameObject.SetActive(false);
        }

        public void Present(Stage3OutcomePresentation presentation)
        {
            LastOutcomePresentation = presentation;
            if (!ValidOutcome(presentation.Outcome)) return;
            _benefitLayer.sprite = _content.OutcomeLayers[(int)presentation.Outcome.Benefit - 1];
            _needLayer.sprite = _content.OutcomeLayers[3 + (int)presentation.Outcome.Need - 1];
            var final = presentation.VisualState == Stage3OutcomeVisualState.Final;
            var opacity = !final ? 0f : presentation.ReducedMotion ? 1f
                : EaseOut((float)((presentation.ElapsedSeconds - Stage3OutcomePresenter.FinalKeySeconds)
                    / (Stage3OutcomePresenter.HoldKeySeconds - Stage3OutcomePresenter.FinalKeySeconds)));
            var color = new Color(1f, 1f, 1f, opacity);
            _benefitLayer.color = color; _needLayer.color = color;
            _benefitLayer.gameObject.SetActive(final); _needLayer.gameObject.SetActive(final);
            _selectedPlan.color = presentation.VisualState == Stage3OutcomeVisualState.Choice
                ? new Color(0.82f, 0.82f, 0.82f, 1f) : Color.white;
            _selectedChild.gameObject.SetActive(true);
        }

        public void SetInputReady(bool ready) { _inputReady = ready; UpdateControls(); }
        public void SetCallerInputLocked(bool locked) { _mediaLocked = locked; UpdateControls(); }
        public void SetVisible(bool visible)
        {
            _visible = visible; if (_canvas != null) _canvas.SetActive(visible); UpdateControls();
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
                control.CanvasRect.xMin >= 0 && control.CanvasRect.yMin >= 0
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
            _canvas = JourneyViewFactory.CreateCanvas("Stage3Canvas", transform, 170);
            _background = JourneyViewFactory.CreateBackground(_canvas.transform,
                _content.Backgrounds[0], _content.Backgrounds[1], _content.Backgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            Add(GrainControlId, "GrainPlan", _content.Plans[0]);
            Add(ChannelControlId, "ChannelPlan", _content.Plans[1]);
            Add(BoatPlanControlId, "BoatPlan", _content.Plans[2]);
            AddTextControl(LowControlId, "LowRiver", "LOW NILE");
            AddTextControl(MiddleControlId, "MiddleRiver", "MIDDLE NILE");
            AddTextControl(HighControlId, "HighRiver", "HIGH NILE");
            Add(PlayControlId, "Play", _content.Controls[0]); Add(HelpControlId, "Help", _content.Controls[1]);
            Add(RepeatControlId, "Repeat", _content.Controls[2]); Add(PauseControlId, "Pause", _content.Controls[3]);
            Add(HomeControlId, "Home", _content.Controls[4]); Add(FinishControlId, "Finish", _content.Controls[5]);
            Add(AnotherPlanControlId, "AnotherPreparation", _content.Controls[6]);
            Add(AnotherRiverControlId, "AnotherRiver", _content.Controls[7]);
            Add(WatchAgainControlId, "WatchAgain", _content.Controls[8]);
            Add(RiverBackControlId, "RiverBack", _content.Controls[9]);
            _benefitLayer = Art("BenefitLayer", _safeRoot, _content.OutcomeLayers[0]);
            _needLayer = Art("NeedLayer", _safeRoot, _content.OutcomeLayers[3]);
            _selectedChild = Art("SelectedChild", _safeRoot, _content.ChildSprites[_selectedGirl ? 1 : 0]);
            _selectedPlan = Art("SelectedPlan", _safeRoot, _content.Plans[0]);
            _captionBackplate = Art("CaptionBackplate", _safeRoot, _content.PresentationSprites[0]);
            _captionBackplate.type = Image.Type.Sliced;
            _visualEquivalent = Art("VisualEquivalent", _safeRoot, _content.VisualEquivalents[0]);
            _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _caption.color = new Color32(61, 43, 29, 255);
            _riverLabel = JourneyViewFactory.CreateRuntimeText("SelectedRiver", _safeRoot);
            _riverLabel.color = new Color32(61, 43, 29, 255); _riverLabel.fontSize = 34;
            _benefitLayer.gameObject.SetActive(false); _needLayer.gameObject.SetActive(false);
            _selectedChild.gameObject.SetActive(false); _selectedPlan.gameObject.SetActive(false);
            _riverLabel.gameObject.SetActive(false); _captionBackplate.gameObject.SetActive(false);
            _visualEquivalent.gameObject.SetActive(false);
        }

        private Image Art(string name, Transform parent, Sprite sprite)
        {
            var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite);
            image.preserveAspect = true; _displayArt.Add(image); return image;
        }
        private void Add(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control); _ids.Add(id); _router.Register(control.Semantic);
        }
        private void AddTextControl(string id, string name, string label)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, null, id);
            var text = JourneyViewFactory.CreateRuntimeText(name + ".Label", control.VisibleArt.transform);
            text.text = label; text.fontSize = 30; text.color = new Color32(61, 43, 29, 255);
            _controls.Add(id, control); _ids.Add(id); _router.Register(control.Semantic);
        }

        private void LayoutControls()
        {
            if (_controls.Count == 0 || !_metrics.Fits) return;
            SetControlRect(_controls[HelpControlId], _metrics.TopCenters[0], Vector2.one * _metrics.Target);
            SetControlRect(_controls[RepeatControlId], _metrics.TopCenters[1], Vector2.one * _metrics.Target);
            SetControlRect(_controls[PauseControlId], _metrics.TopCenters[2], Vector2.one * _metrics.Target);
            SetControlRect(_controls[HomeControlId], _metrics.TopCenters[3], Vector2.one * _metrics.Target);
            SetControlRect(_controls[AnotherPlanControlId], _metrics.BottomCenters[0], Vector2.one * _metrics.Target);
            SetControlRect(_controls[AnotherRiverControlId], _metrics.BottomCenters[1], Vector2.one * _metrics.Target);
            SetControlRect(_controls[WatchAgainControlId], _metrics.BottomCenters[2], Vector2.one * _metrics.Target);
            SetControlRect(_controls[FinishControlId], _metrics.BottomCenters[3], Vector2.one * _metrics.Target);
            SetControlRect(_controls[PlayControlId], _metrics.BottomCenters[1], Vector2.one * _metrics.Target);
            SetControlRect(_controls[RiverBackControlId], _metrics.BottomCenters[1], Vector2.one * _metrics.Target);
            SetChoiceRects(new[] { GrainControlId, ChannelControlId, BoatPlanControlId }, _metrics.PlanCenters, _metrics.PlanSize);
            SetChoiceRects(new[] { LowControlId, MiddleControlId, HighControlId }, _metrics.RiverCenters, _metrics.RiverSize);
        }

        private void SetChoiceRects(string[] ids, Vector2[] centers, Vector2 size)
        { for (var index = 0; index < ids.Length; index++) SetControlRect(_controls[ids[index]], centers[index], size); }
        private void SetControlRect(JourneyControlView control, Vector2 center, Vector2 size)
        {
            JourneyViewFactory.SetCentered(control.Target,
                new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f), size);
            JourneyViewFactory.SetCentered(control.VisibleArt.rectTransform, Vector2.zero, size * 0.72f);
            control.CanvasRect = new Rect(center - size * 0.5f, size);
        }

        private void LayoutDisplayArt()
        {
            if (!_metrics.Fits) return;
            SetImageRect(_captionBackplate, _metrics.CaptionRect);
            SetImageRect(_visualEquivalent, new Rect(_metrics.CaptionRect.xMin, _metrics.CaptionRect.yMin,
                Mathf.Min(_metrics.CaptionRect.width * 0.32f, _metrics.CaptionRect.height * 2f), _metrics.CaptionRect.height));
            SetImageRect(_benefitLayer, _metrics.BenefitRect); SetImageRect(_needLayer, _metrics.NeedRect);
            SetImageRect(_selectedChild, _metrics.ChildRect);
            var planRect = new Rect(_safeSize.x - _metrics.ChildRect.xMax,
                _metrics.ChildRect.yMin, _metrics.ChildRect.width, _metrics.ChildRect.height);
            SetImageRect(_selectedPlan, planRect);
            SetImageRect(_riverLabel, new Rect(_metrics.CaptionRect.xMin,
                _metrics.CaptionRect.yMin - _metrics.Target * 0.72f,
                _metrics.CaptionRect.width, _metrics.Target * 0.64f));
            BenefitCanvasRect = _metrics.BenefitRect; NeedCanvasRect = _metrics.NeedRect;
            SelectedChildCanvasRect = _metrics.ChildRect;
        }
        private void SetImageRect(Image image, Rect rect) => JourneyViewFactory.SetCentered(image.rectTransform,
            new Vector2(rect.center.x - _safeSize.x * 0.5f, rect.center.y - _safeSize.y * 0.5f), rect.size);
        private void SetImageRect(TextMeshProUGUI text, Rect rect) => JourneyViewFactory.SetCentered(text.rectTransform,
            new Vector2(rect.center.x - _safeSize.x * 0.5f, rect.center.y - _safeSize.y * 0.5f), rect.size);

        private void ConfigureBackground()
        {
            if (_background == null || _content == null) return;
            var after = _snapshot.State == Stage3State.OutcomePlaying || _snapshot.State == Stage3State.OutcomeReady;
            var start = after ? 3 : 0;
            _background.Configure(_content.Backgrounds[start], _content.Backgrounds[start + 1], _content.Backgrounds[start + 2]);
            _background.Present(_screenWidth, _screenHeight);
        }

        private void ConfigureOutcomeArt()
        {
            if (_benefitLayer == null) return;
            var visible = (_snapshot.State == Stage3State.OutcomePlaying || _snapshot.State == Stage3State.OutcomeReady)
                && ValidOutcome(_snapshot.CurrentOutcome);
            var finalSnapshot = visible && _snapshot.State == Stage3State.OutcomeReady;
            _benefitLayer.gameObject.SetActive(finalSnapshot); _needLayer.gameObject.SetActive(finalSnapshot);
            var baseColor = new Color(1f, 1f, 1f, finalSnapshot ? 1f : 0f);
            _benefitLayer.color = baseColor; _needLayer.color = baseColor;
            _selectedChild.gameObject.SetActive(visible);
            var selectionVisible = _snapshot.SelectedPlan >= Stage3Plan.Grain && _snapshot.SelectedPlan <= Stage3Plan.Boat;
            _selectedPlan.gameObject.SetActive(selectionVisible);
            if (selectionVisible) _selectedPlan.sprite = _content.Plans[(int)_snapshot.SelectedPlan - 1];
            var riverVisible = _snapshot.SelectedRiver >= Stage2River.Low && _snapshot.SelectedRiver <= Stage2River.High;
            _riverLabel.gameObject.SetActive(riverVisible);
            _riverLabel.text = _snapshot.SelectedRiver == Stage2River.Low ? "LOW NILE"
                : _snapshot.SelectedRiver == Stage2River.High ? "HIGH NILE" : "MIDDLE NILE";
            if (!visible) return;
            _benefitLayer.sprite = _content.OutcomeLayers[(int)_snapshot.CurrentOutcome.Benefit - 1];
            _needLayer.sprite = _content.OutcomeLayers[3 + (int)_snapshot.CurrentOutcome.Need - 1];
        }

        private void UpdateControls()
        {
            foreach (var pair in _controls)
            {
                var visible = IsVisibleForState(pair.Key);
                pair.Value.Target.gameObject.SetActive(visible);
                pair.Value.Semantic.Enabled = visible && _visible && _inputReady && !_mediaLocked;
            }
        }
        private bool IsVisibleForState(string id)
        {
            switch (_snapshot.State)
            {
                case Stage3State.Intro: return IsCommon(id) || id == PlayControlId;
                case Stage3State.ChoosingPlan: return IsCommon(id) || IsPlanControl(id);
                case Stage3State.OutcomePlaying: return id == PauseControlId || id == HomeControlId;
                case Stage3State.OutcomeReady: return IsCommon(id) || id == FinishControlId
                    || id == AnotherPlanControlId || id == AnotherRiverControlId || id == WatchAgainControlId;
                case Stage3State.ChoosingRiver: return IsCommon(id) || IsRiverControl(id) || id == RiverBackControlId;
                default: return false;
            }
        }
        private static bool IsCommon(string id) => id == HelpControlId || id == RepeatControlId
            || id == PauseControlId || id == HomeControlId;
        private static bool IsPlanControl(string id) => id == GrainControlId || id == ChannelControlId || id == BoatPlanControlId;
        private static bool IsRiverControl(string id) => id == LowControlId || id == MiddleControlId || id == HighControlId;
        private static bool ValidOutcome(Stage3OutcomeKey key) => key.Benefit >= Stage3Benefit.Grain
            && key.Benefit <= Stage3Benefit.Boat && key.Need >= Stage3Need.WaterRoute && key.Need <= Stage3Need.FieldWater;
        private static float EaseOut(float value)
        {
            var t = Mathf.Clamp01(value); return 1f - (1f - t) * (1f - t);
        }
    }
}
