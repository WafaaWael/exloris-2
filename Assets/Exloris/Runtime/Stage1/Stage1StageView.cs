using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Stage1
{
    [DisallowMultipleComponent]
    public sealed class Stage1StageView : MonoBehaviour, IStage1View, IStage1MediaCueSink, IStage1OutcomeSink
    {
        public const string MarkerControlId = "stage_1.marker";
        public const string SocketControlId = "stage_1.socket";
        public const string PlayControlId = "stage_1.play";
        public const string HelpControlId = "stage_1.help";
        public const string RepeatControlId = "stage_1.repeat";
        public const string PauseControlId = "stage_1.pause";
        public const string HomeControlId = "stage_1.home";
        public const string UpControlId = "stage_1.up";
        public const string DownControlId = "stage_1.down";
        public const string ConfirmControlId = "stage_1.confirm";
        public const string ContinueControlId = "stage_1.continue";
        public const string TryAgainControlId = "stage_1.try_again";
        public const string WatchAgainControlId = "stage_1.watch_again";

        private readonly Dictionary<string, JourneyControlView> _controls = new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _ids = new List<string>();
        private readonly List<Image> _displayArt = new List<Image>();
        private ChildInputRouter _router;
        private Stage1Content _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _outcomeLayer, _selectedChild, _captionBackplate, _visualEquivalent, _supportCue, _relationshipHighlight;
        private TextMeshProUGUI _caption;
        private Stage1LayoutMetrics _metrics;
        private Stage1Snapshot _snapshot;
        private Vector2 _safeSize;
        private float _screenWidth = 1920f, _screenHeight = 1080f;
        private bool _inputReady, _mediaLocked, _visible = true, _selectedGirl, _captionsSuppressed;

        public IReadOnlyList<string> ControlIds => _ids;
        public int SemanticControlCount => _ids.Count;
        public int DisplaySemanticControlCount => 0;
        public Color32 CaptionTextColor => _caption == null ? new Color32(0, 0, 0, 0) : _caption.color;
        public Rect SelectedChildCanvasRect { get; private set; }
        public Sprite SelectedChildSprite => _selectedChild?.sprite;
        public IReadOnlyList<string> EnabledControlIds => _controls.Where(pair => pair.Value.Semantic.Enabled).Select(pair => pair.Key).ToArray();
        public Stage1MediaCue LastMediaCue { get; private set; }
        public Stage1OutcomeFrame LastOutcomeFrame { get; private set; }
        public Stage1PauseReason? LastPauseReason { get; private set; }
        public bool SupportCueVisible => _supportCue != null && _supportCue.gameObject.activeSelf;
        public bool RelationshipHighlightVisible => _relationshipHighlight != null && _relationshipHighlight.gameObject.activeSelf;
        public float SupportCueStrength => _supportCue == null ? 0f : _supportCue.color.a;
        public float RelationshipHighlightStrength => _relationshipHighlight == null ? 0f : _relationshipHighlight.color.a;
        public event Action<Stage1PauseReason> PauseOverlayRequested;

        public void Configure(ChildInputRouter router, Stage1Content content, bool selectedGirl)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _selectedGirl = selectedGirl;
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 0, 0, 1856, 1016, 326, 88))
                throw new InvalidOperationException("Default Stage 1 layout is infeasible.");
            _snapshot = new Stage1Snapshot(Guid.Empty, Stage1State.Hidden, false, 0.5d, 0, 0, Stage1OutcomeKind.None,
                Guid.Empty, Guid.Empty, 0, 0, null, false, false);
            Present(_snapshot, false);
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float ppi, int platformMinimumPixels)
        {
            var metrics = Stage1LayoutMetrics.Calculate(
                screenWidth, screenHeight, safeX, safeY, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits) return false;
            _metrics = metrics;
            _screenWidth = screenWidth;
            _screenHeight = screenHeight;
            _safeSize = metrics.SafeCanvasRect.size;
            _background.Present(screenWidth, screenHeight);
            LayoutMarkerTargets();
            LayoutDisplayArt();
            LayoutControlsForState();
            UpdateControls();
            return true;
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels) =>
            TryApplyResponsiveLayout(screenWidth, screenHeight, 0, 0, safeWidth, safeHeight, ppi, platformMinimumPixels);

        public void Present(Stage1Snapshot snapshot, bool reducedMotion)
        {
            _snapshot = snapshot;
            ConfigureOutcomeArt();
            ConfigureSupportArt();
            LayoutMarkerTargets();
            LayoutControlsForState();
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

        public void PresentMediaCue(Stage1MediaCue cue)
        {
            LastMediaCue = cue;
            _visualEquivalent.sprite = cue.VisualEquivalent;
            _visualEquivalent.gameObject.SetActive(true);
            _caption.text = Stage1CaptionStrings.ResolveEnglish(cue.CaptionId);
            var captionVisible = cue.CaptionVisible && !_captionsSuppressed;
            _captionBackplate.gameObject.SetActive(captionVisible);
            _caption.gameObject.SetActive(captionVisible);
        }

        public void FreezeMediaCue(double elapsedSeconds) { }
        public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }

        public void ShowPauseOverlay(Stage1PauseReason reason)
        {
            LastPauseReason = reason;
            PauseOverlayRequested?.Invoke(reason);
        }

        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionsSuppressed = suppressed;
            if (!suppressed) return;
            _captionBackplate.gameObject.SetActive(false);
            _caption.gameObject.SetActive(false);
        }

        public void PresentOutcome(Stage1OutcomeFrame frame)
        {
            LastOutcomeFrame = frame;
            _outcomeLayer.sprite = frame.OutcomeLayer;
            _outcomeLayer.gameObject.SetActive(true);
            _selectedChild.gameObject.SetActive(true);
            if (_metrics.Fits)
                SetControlRect(_controls[MarkerControlId], _metrics.MarkerCenter((float)Math.Max(0d, Math.Min(1d, frame.MarkerPosition))));
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_canvas != null) _canvas.SetActive(visible);
            UpdateControls();
        }

        public Rect GetControlCanvasRect(string id) => _controls[id].CanvasRect;
        public Rect GetControlScreenBounds(string id) => JourneyViewFactory.ToScreenRect(_controls[id].Target);

        public Vector2 GetMarkerScreenPoint(double normalizedPosition)
        {
            if (!_metrics.Fits || double.IsNaN(normalizedPosition) || double.IsInfinity(normalizedPosition)
                || normalizedPosition < 0d || normalizedPosition > 1d) throw new ArgumentOutOfRangeException(nameof(normalizedPosition));
            var center = _metrics.MarkerCenter((float)normalizedPosition);
            var local = new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f);
            return RectTransformUtility.WorldToScreenPoint(null, _safeRoot.TransformPoint(local));
        }

        public bool TryMapScreenPointToMarkerPosition(Vector2 screenPoint, out double normalizedPosition)
        {
            normalizedPosition = 0d;
            if (!_metrics.Fits || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _safeRoot, screenPoint, null, out var local)) return false;
            var canvasY = local.y + _safeSize.y * 0.5f;
            var minimum = _metrics.DragRect.yMin + _metrics.Target * 0.5f;
            var maximum = _metrics.DragRect.yMax - _metrics.Target * 0.5f;
            if (maximum <= minimum) return false;
            normalizedPosition = Math.Max(0d, Math.Min(1d, (canvasY - minimum) / (maximum - minimum)));
            return true;
        }

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

        public bool AreAllEnabledTargetsInsideSafeArea()
        {
            return _controls.Values.Where(control => control.Semantic.Enabled).All(control =>
                control.CanvasRect.xMin >= 0f && control.CanvasRect.yMin >= 0f
                && control.CanvasRect.xMax <= _safeSize.x && control.CanvasRect.yMax <= _safeSize.y);
        }

        public bool VisibleArtContainedByTargets()
        {
            return _controls.Values.Where(control => control.Target.gameObject.activeSelf).All(control =>
                control.VisibleArt.rectTransform.sizeDelta.x <= control.Target.sizeDelta.x + 0.001f
                && control.VisibleArt.rectTransform.sizeDelta.y <= control.Target.sizeDelta.y + 0.001f);
        }

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
            _canvas = JourneyViewFactory.CreateCanvas("Stage1Canvas", transform, 150);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform, _content.Backgrounds[3], _content.Backgrounds[4], _content.Backgrounds[5]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            Add(MarkerControlId, "Marker", _content.Props[0]);
            Add(SocketControlId, "Socket", _content.Props[1]);
            Add(PlayControlId, "Play", _content.Controls[0]);
            Add(HelpControlId, "Help", _content.Controls[1]);
            Add(RepeatControlId, "Repeat", _content.Controls[2]);
            Add(PauseControlId, "Pause", _content.Controls[3]);
            Add(HomeControlId, "Home", _content.Controls[4]);
            Add(UpControlId, "Up", _content.Controls[5]);
            Add(DownControlId, "Down", _content.Controls[6]);
            Add(ConfirmControlId, "Confirm", _content.Controls[7]);
            Add(ContinueControlId, "Continue", _content.Controls[8]);
            Add(TryAgainControlId, "TryAgain", _content.Controls[9]);
            Add(WatchAgainControlId, "WatchAgain", _content.Controls[10]);
            _outcomeLayer = Art("OutcomeLayer", _safeRoot, _content.OutcomeLayers[0]);
            _selectedChild = Art("SelectedChild", _safeRoot, _content.ChildSprites[_selectedGirl ? 1 : 0]);
            _captionBackplate = Art("CaptionBackplate", _safeRoot, _content.PresentationSprites[0]);
            _captionBackplate.type = Image.Type.Sliced;
            _visualEquivalent = Art("VisualEquivalent", _safeRoot, _content.PresentationSprites[2]);
            _relationshipHighlight = Art("RelationshipHighlight", _safeRoot, _content.PresentationSprites[1]);
            _supportCue = Art("SupportCue", _safeRoot, _content.PresentationSprites[1]);
            _relationshipHighlight.transform.SetAsFirstSibling();
            _supportCue.transform.SetAsFirstSibling();
            _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _caption.color = new Color32(61, 43, 29, 255);
            _captionBackplate.gameObject.SetActive(false);
            _visualEquivalent.gameObject.SetActive(false);
            _relationshipHighlight.gameObject.SetActive(false);
            _supportCue.gameObject.SetActive(false);
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
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id, id == MarkerControlId);
            _controls.Add(id, control);
            _ids.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutMarkerTargets()
        {
            if (_controls.Count == 0 || !_metrics.Fits) return;
            var markerCenter = _snapshot.MarkerAttached
                ? _metrics.MarkerCenter((float)Math.Max(0d, Math.Min(1d, _snapshot.MarkerPosition)))
                : _metrics.MarkerTrayCenter;
            SetControlRect(_controls[MarkerControlId], markerCenter);
            SetControlRect(_controls[SocketControlId], _metrics.MarkerCenter(0.5f));
            var socketCenter = _metrics.MarkerCenter(0.5f);
            SetImageRect(_supportCue, CenteredRect(markerCenter, _metrics.Target * 1.08f, _metrics.Target * 1.08f));
            var relationshipCenter = (markerCenter + socketCenter) * 0.5f;
            SetImageRect(_relationshipHighlight, CenteredRect(
                relationshipCenter, _metrics.Target * 0.5f,
                Mathf.Max(_metrics.Target * 0.75f, Mathf.Abs(markerCenter.y - socketCenter.y) + _metrics.Target * 0.45f)));
        }

        private void LayoutDisplayArt()
        {
            SetImageRect(_captionBackplate, _metrics.CaptionRect);
            SetImageRect(_visualEquivalent, new Rect(
                _metrics.CaptionRect.xMin, _metrics.CaptionRect.yMin,
                Mathf.Min(_metrics.CaptionRect.width * 0.32f, _metrics.CaptionRect.height * 2f), _metrics.CaptionRect.height));
            SetImageRect(_outcomeLayer, _metrics.OutcomeRect);
            SetImageRect(_selectedChild, _metrics.ChildRect);
            SelectedChildCanvasRect = _metrics.ChildRect;
        }

        private void LayoutControlsForState()
        {
            if (_controls.Count == 0 || !_metrics.Fits) return;
            var top = _metrics.TopCenters;
            var bottom = _metrics.BottomCenters;
            if (_snapshot.State == Stage1State.Intro || _snapshot.State == Stage1State.Playing || _snapshot.State == Stage1State.Hidden)
            {
                SetControlRect(_controls[HelpControlId], top[0]);
                SetControlRect(_controls[RepeatControlId], top[1]);
                SetControlRect(_controls[PauseControlId], top[2]);
                SetControlRect(_controls[HomeControlId], top[3]);
            }
            else
            {
                SetControlRect(_controls[RepeatControlId], top[0]);
                SetControlRect(_controls[PauseControlId], top[1]);
                SetControlRect(_controls[HomeControlId], top[2]);
            }

            SetControlRect(_controls[PlayControlId], bottom[1]);
            SetControlRect(_controls[UpControlId], bottom[0]);
            SetControlRect(_controls[DownControlId], bottom[1]);
            SetControlRect(_controls[ConfirmControlId], bottom[2]);
            SetControlRect(_controls[ContinueControlId], bottom[0]);
            SetControlRect(_controls[TryAgainControlId], bottom[0]);
            SetControlRect(_controls[WatchAgainControlId], bottom[1]);
            if (_snapshot.State == Stage1State.OutcomeReady && _snapshot.Outcome != Stage1OutcomeKind.InRange)
                SetControlRect(_controls[HelpControlId], bottom[2]);
        }

        private void SetControlRect(JourneyControlView control, Vector2 center)
        {
            JourneyViewFactory.SetCentered(control.Target,
                new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f),
                Vector2.one * _metrics.Target);
            JourneyViewFactory.SetCentered(control.VisibleArt.rectTransform, Vector2.zero, Vector2.one * _metrics.Target * 0.72f);
            control.CanvasRect = CenteredRect(center, _metrics.Target, _metrics.Target);
        }

        private void SetImageRect(Image image, Rect rect)
        {
            JourneyViewFactory.SetCentered(image.rectTransform,
                new Vector2(rect.center.x - _safeSize.x * 0.5f, rect.center.y - _safeSize.y * 0.5f), rect.size);
        }

        private void ConfigureOutcomeArt()
        {
            var outcomeVisible = _snapshot.State == Stage1State.OutcomePlaying || _snapshot.State == Stage1State.OutcomeReady;
            _outcomeLayer.gameObject.SetActive(outcomeVisible);
            _selectedChild.gameObject.SetActive(outcomeVisible);
            if (_snapshot.Outcome == Stage1OutcomeKind.OutsideLow)
            {
                ConfigureBackground(0);
                _outcomeLayer.sprite = _content.OutcomeLayers[1];
            }
            else if (_snapshot.Outcome == Stage1OutcomeKind.OutsideHigh)
            {
                ConfigureBackground(6);
                _outcomeLayer.sprite = _content.OutcomeLayers[2];
            }
            else
            {
                ConfigureBackground(3);
                _outcomeLayer.sprite = _content.OutcomeLayers[0];
            }
        }

        private void ConfigureSupportArt()
        {
            var playing = _snapshot.State == Stage1State.Playing && _snapshot.MarkerAttached;
            var cueVisible = playing && _snapshot.SupportTier >= 1;
            var relationshipVisible = playing && _snapshot.SupportTier >= 2;
            _supportCue.gameObject.SetActive(cueVisible);
            _relationshipHighlight.gameObject.SetActive(relationshipVisible);
            _supportCue.color = new Color(1f, 0.78f, 0.24f, _snapshot.SupportTier >= 3 ? 0.82f : 0.58f);
            _relationshipHighlight.color = new Color(0.96f, 0.68f, 0.18f, _snapshot.SupportTier >= 3 ? 0.62f : 0.42f);
        }

        private void ConfigureBackground(int start)
        {
            _background.Configure(_content.Backgrounds[start], _content.Backgrounds[start + 1], _content.Backgrounds[start + 2]);
            _background.Present(_screenWidth, _screenHeight);
        }

        private void UpdateControls()
        {
            foreach (var pair in _controls)
            {
                var visible = IsVisibleForState(pair.Key);
                pair.Value.Target.gameObject.SetActive(visible);
                pair.Value.Semantic.Enabled = visible && _visible && _inputReady && !_mediaLocked && IsEnabledForState(pair.Key);
            }
        }

        private bool IsVisibleForState(string id)
        {
            switch (_snapshot.State)
            {
                case Stage1State.Intro:
                    return id == PlayControlId || id == HelpControlId || id == RepeatControlId || id == PauseControlId || id == HomeControlId;
                case Stage1State.Playing:
                    return id == MarkerControlId || id == SocketControlId || id == UpControlId || id == DownControlId || id == ConfirmControlId
                        || id == HelpControlId || id == RepeatControlId || id == PauseControlId || id == HomeControlId;
                case Stage1State.OutcomePlaying:
                    return id == MarkerControlId || id == PauseControlId || id == HomeControlId;
                case Stage1State.OutcomeReady:
                    if (_snapshot.Outcome == Stage1OutcomeKind.InRange)
                        return id == MarkerControlId || id == ContinueControlId || id == WatchAgainControlId || id == RepeatControlId || id == PauseControlId || id == HomeControlId;
                    return id == MarkerControlId || id == TryAgainControlId || id == WatchAgainControlId || id == HelpControlId || id == RepeatControlId || id == PauseControlId || id == HomeControlId;
                default:
                    return false;
            }
        }

        private bool IsEnabledForState(string id)
        {
            if (id == ConfirmControlId) return _snapshot.MarkerAttached;
            if (id == SocketControlId) return !_snapshot.MarkerAttached;
            return true;
        }

        private static Rect CenteredRect(Vector2 center, float width, float height) =>
            new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
    }
}
