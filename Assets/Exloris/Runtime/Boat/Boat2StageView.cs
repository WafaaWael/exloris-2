using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Boat
{
    [DisallowMultipleComponent]
    public sealed class Boat2StageView : MonoBehaviour, IBoat2View, IBoat2MotionSink, IBoat2MediaCueSink
    {
        public const string FieldBankControlId = "boat_2.field_bank";
        public const string FollowIbisControlId = "boat_2.follow_ibis";
        public const string DockControlId = "boat_2.dock";
        public const string HelpControlId = "boat_2.help";
        public const string RepeatControlId = "boat_2.repeat";
        public const string SkipControlId = "boat_2.skip";
        public const string PauseControlId = "boat_2.pause";
        public const string HomeControlId = "boat_2.home";
        private readonly Dictionary<string, JourneyControlView> _controls = new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _ids = new List<string>();
        private readonly List<Image> _displayArt = new List<Image>();
        private ChildInputRouter _router;
        private Boat2Content _content;
        private GameObject _canvas;
        private RectTransform _safeRoot, _boatRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _wake, _boat, _child, _keeper, _oar, _rail, _ibis, _captionBackplate, _visualEquivalent;
        private TextMeshProUGUI _caption;
        private Vector2 _safeSize, _boatBasePosition;
        private float _screenWidth = 1920, _screenHeight = 1080;
        private bool _inputReady, _mediaLocked, _visible = true, _selectedGirl, _captionsSuppressed;
        public Boat2Snapshot LastSnapshot { get; private set; }
        public bool InputReady { get; private set; }
        public IReadOnlyList<string> ControlIds => _ids;
        public int SemanticControlCount => _ids.Count;
        public int DisplaySemanticControlCount => 0;
        public bool ControlsEnabled => _controls.Count == 8 && _controls.Values.All(control => control.Semantic.Enabled);
        public Boat2PauseReason? LastPauseReason { get; private set; }
        public event Action<Boat2PauseReason> PauseOverlayRequested;
        public Vector2 SelectedChildRenderedSize => _child == null ? Vector2.zero : _child.rectTransform.sizeDelta;
        public Vector2 SelectedChildAnchoredPosition => _child == null ? Vector2.zero : _child.rectTransform.anchoredPosition;

        public void Configure(ChildInputRouter router, Boat2Content content, bool selectedGirl = false)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router)); _content = content ?? throw new ArgumentNullException(nameof(content)); _selectedGirl = selectedGirl;
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920, 1080, 1856, 1016, 326, 88)) throw new InvalidOperationException("Default Boat 2 layout is infeasible.");
            UpdateControls();
        }

        public bool TryApplyResponsiveLayout(float screenWidth, float screenHeight, float safeWidth, float safeHeight, float ppi, int minimum)
        {
            var metrics = Boat2LayoutMetrics.Calculate(screenWidth, screenHeight, safeWidth, safeHeight, ppi, minimum);
            if (!metrics.Fits(safeWidth, safeHeight)) return false;
            _screenWidth = screenWidth; _screenHeight = screenHeight; _safeSize = new Vector2(safeWidth, safeHeight); _background.Present(screenWidth, screenHeight);
            LayoutRow(new[] { HelpControlId, RepeatControlId, PauseControlId, HomeControlId }, metrics.TopCenters, metrics);
            LayoutRow(new[] { FieldBankControlId, FollowIbisControlId, DockControlId, SkipControlId }, metrics.BottomCenters, metrics);
            JourneyViewFactory.SetCentered(_boatRoot, new Vector2(0, metrics.BodyRect.center.y - safeHeight * 0.5f), new Vector2(metrics.BodyRect.width * 0.72f, metrics.BodyRect.height * 0.72f));
            _boatBasePosition = _boatRoot.anchoredPosition;
            JourneyViewFactory.SetCentered(_ibis.rectTransform, new Vector2(metrics.BodyRect.xMax - safeWidth * 0.5f - metrics.IbisHeight * 0.5f, metrics.BodyRect.center.y - safeHeight * 0.5f), Vector2.one * metrics.IbisHeight);
            JourneyViewFactory.SetCentered(_captionBackplate.rectTransform, new Vector2(metrics.CaptionRect.center.x - safeWidth * 0.5f, metrics.CaptionRect.center.y - safeHeight * 0.5f), metrics.CaptionRect.size);
            JourneyViewFactory.SetCentered(_visualEquivalent.rectTransform, new Vector2(metrics.CaptionRect.xMin + metrics.CaptionRect.height - safeWidth * 0.5f, metrics.CaptionRect.center.y - safeHeight * 0.5f), new Vector2(metrics.CaptionRect.height * 2, metrics.CaptionRect.height));
            LayoutBoatLayers(); return true;
        }

        public void Present(Boat2Snapshot snapshot, bool reducedMotion)
        {
            LastSnapshot = snapshot;
            if (snapshot.State == Boat2State.Arriving || snapshot.State == Boat2State.Navigating) ConfigureBackground(6);
            else if (snapshot.State == Boat2State.Traveling) ConfigureBackground(3);
            else ConfigureBackground(0);
        }
        public void SetInputReady(bool ready) { InputReady = ready; _inputReady = ready; UpdateControls(); }
        public void SetVisible(bool visible) { _visible = visible; if (_canvas != null) _canvas.SetActive(visible); UpdateControls(); }
        public void PresentMotion(Boat2MotionFrame frame) { _boat.sprite = frame.BoatSprite; _keeper.sprite = frame.BoatkeeperSprite; _ibis.sprite = frame.IbisSprite; _boatRoot.anchoredPosition = _boatBasePosition + new Vector2(0, frame.BoatYOffsetPixels); }
        public void PresentMediaCue(Boat2MediaCue cue) { _visualEquivalent.sprite = cue.VisualEquivalent; _visualEquivalent.gameObject.SetActive(true); _caption.text = Boat2CaptionStrings.ResolveEnglish(cue.CaptionId); _captionBackplate.gameObject.SetActive(cue.CaptionVisible && !_captionsSuppressed); _caption.gameObject.SetActive(cue.CaptionVisible && !_captionsSuppressed); }
        public void FreezeMediaCue(double elapsedSeconds) { }
        public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }
        public void SetCallerInputLocked(bool locked) { _mediaLocked = locked; UpdateControls(); }
        public void ShowPauseOverlay(Boat2PauseReason reason) { LastPauseReason = reason; PauseOverlayRequested?.Invoke(reason); }
        public void SetCaptionSuppressed(bool suppressed) { _captionsSuppressed = suppressed; if (suppressed) { _captionBackplate.gameObject.SetActive(false); _caption.gameObject.SetActive(false); } }
        public Rect GetControlCanvasRect(string id) => _controls[id].CanvasRect;
        public Rect GetControlScreenBounds(string id) => JourneyViewFactory.ToScreenRect(_controls[id].Target);
        public bool DoTouchTargetsOverlap() => JourneyViewFactory.TargetsOverlap(_controls.Values);
        public bool AreAllTargetsInsideSafeArea() => JourneyViewFactory.Inside(_controls.Values, _safeSize);
        public bool AreTouchTargetsTransparent() => _controls.Values.All(c => c.TargetImage.color.a == 0f && !c.TargetImage.raycastTarget);
        public bool VisibleArtRaycastsDisabled() => _controls.Values.All(c => !c.VisibleArt.raycastTarget) && _displayArt.TrueForAll(image => !image.raycastTarget) && !_caption.raycastTarget;

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("Boat2Canvas", transform, 150);
            _background = JourneyViewFactory.CreateBackground(_canvas.transform, _content.Backgrounds[0], _content.Backgrounds[1], _content.Backgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas); _boatRoot = new GameObject("BoatComposition", typeof(RectTransform)).GetComponent<RectTransform>(); _boatRoot.SetParent(_safeRoot, false);
            _wake = Art("Wake", _boatRoot, _content.BoatSprites[3]); _boat = Art("BoatHull", _boatRoot, _content.BoatSprites[0]); _child = Art("SelectedChild", _boatRoot, _content.ChildSprites[_selectedGirl ? 1 : 0]);
            _keeper = Art("Boatkeeper", _boatRoot, _content.BoatkeeperSprites[0]); _oar = Art("SteeringOar", _boatRoot, _content.BoatSprites[2]); _rail = Art("NearRail", _boatRoot, _content.BoatSprites[1]);
            _ibis = Art("Ibis", _safeRoot, _content.IbisSprites[0]); _visualEquivalent = Art("NonTextVisualEquivalent", _safeRoot, _content.PresentationSprites[2]);
            _captionBackplate = Art("CaptionBackplate", _safeRoot, _content.PresentationSprites[0]); _captionBackplate.type = Image.Type.Sliced; _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform); _caption.color = new Color32(61, 43, 29, 255); _captionBackplate.gameObject.SetActive(false);
            Add(FieldBankControlId, "FieldBank", 0); Add(FollowIbisControlId, "FollowIbis", 4); Add(DockControlId, "Dock", 1);
            Add(HelpControlId, "Help", 5); Add(RepeatControlId, "Repeat", 6); Add(SkipControlId, "Skip", 7); Add(PauseControlId, "Pause", 8); Add(HomeControlId, "Home", 9);
        }
        private Image Art(string name, Transform parent, Sprite sprite) { var image = JourneyViewFactory.CreateImage(name + ".VisibleArt", parent, Color.white, sprite); image.preserveAspect = true; _displayArt.Add(image); return image; }
        private void Add(string id, string name, int sprite) { var control = JourneyViewFactory.CreateControl(name, _safeRoot, _content.Controls[sprite], id); _controls.Add(id, control); _ids.Add(id); _router.Register(control.Semantic); }
        private void LayoutRow(string[] ids, Vector2[] centers, Boat2LayoutMetrics metrics)
        {
            for (var i = 0; i < ids.Length; i++) { var control = _controls[ids[i]]; JourneyViewFactory.SetCentered(control.Target, new Vector2(centers[i].x - _safeSize.x * 0.5f, centers[i].y - _safeSize.y * 0.5f), Vector2.one * metrics.Target); JourneyViewFactory.SetCentered(control.VisibleArt.rectTransform, Vector2.zero, Vector2.one * metrics.Target * 0.72f); control.CanvasRect = new Rect(centers[i].x - metrics.Target * 0.5f, centers[i].y - metrics.Target * 0.5f, metrics.Target, metrics.Target); }
        }
        private void LayoutBoatLayers() { JourneyViewFactory.Stretch(_wake.rectTransform); JourneyViewFactory.Stretch(_boat.rectTransform); JourneyViewFactory.Stretch(_rail.rectTransform); SetNormalized(_child.rectTransform, new Vector2(0.42f, 0.42f), 0.32f); SetNormalized(_keeper.rectTransform, new Vector2(0.66f, 0.44f), 0.36f); SetNormalized(_oar.rectTransform, new Vector2(0.72f, 0.50f), 0.45f); }
        private static void SetNormalized(RectTransform rect, Vector2 position, float size) { rect.anchorMin = rect.anchorMax = position; rect.pivot = Vector2.one * 0.5f; rect.anchoredPosition = Vector2.zero; rect.sizeDelta = Vector2.one * size * 1024f; }
        private void ConfigureBackground(int index) { _background.Configure(_content.Backgrounds[index], _content.Backgrounds[index + 1], _content.Backgrounds[index + 2]); _background.Present(_screenWidth, _screenHeight); }
        private void UpdateControls() { var enabled = _visible && _inputReady && !_mediaLocked; foreach (var control in _controls.Values) control.Semantic.Enabled = enabled; }
    }
}
