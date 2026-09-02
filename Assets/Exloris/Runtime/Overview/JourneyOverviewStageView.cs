using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Overview
{
    [DisallowMultipleComponent]
    public sealed class JourneyOverviewStageView : MonoBehaviour,
        IJourneyOverviewRevealSink,
        IJourneyOverviewMediaCueSink
    {
        public const string SkipControlId = "journey_overview.skip";
        public const string RepeatControlId = "journey_overview.repeat";
        public const string PauseControlId = "journey_overview.pause";
        public const string HomeControlId = "journey_overview.home";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _controlIds = new List<string>();
        private readonly Image[] _timelineStates = new Image[3];
        private readonly Image[] _timelineIcons = new Image[3];
        private readonly CanvasGroup[] _medallionGroups = new CanvasGroup[3];
        private readonly Image[] _connectors = new Image[2];
        private readonly CanvasGroup[] _connectorGroups = new CanvasGroup[2];
        private readonly Rect[] _timelineCanvasRects = new Rect[5];

        private ChildInputRouter _router;
        private JourneyOverviewContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private Image _visualEquivalent;
        private Rect _captionCanvasRect;
        private Rect _visualCueCanvasRect;
        private Vector2 _safeSize;
        private bool _visible = true;
        private bool _revealInputReady;
        private bool _mediaInputLocked;
        private bool _motionFrozen;
        private bool _captionRequestedVisible;
        private bool _captionSuppressed;
        private float _pulseElapsed;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public int TimelineSemanticControlCount => 0;
        public bool TimelineHasCollider => false;
        public int VisibleBeatCount { get; private set; }
        public bool CurrentPulseEnabled { get; private set; }
        public bool ControlsEnabled => _controls.Count > 0 && AllControlsEnabled();
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeInHierarchy;
        public bool CaptionBackplateVisible => _captionBackplate != null
            && _captionBackplate.gameObject.activeInHierarchy;
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public JourneyOverviewPauseReason? LastPauseReason { get; private set; }
        public event Action<JourneyOverviewPauseReason> PauseOverlayRequested;

        public void Configure(ChildInputRouter router, JourneyOverviewContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            BuildSurface();
            if (!TryApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88))
                throw new InvalidOperationException("Default Journey Overview layout is infeasible.");
            UpdateControls();
        }

        public void SetVisible(bool visible)
        {
            _visible = visible;
            if (_canvas != null) _canvas.SetActive(visible);
            UpdateControls();
        }

        public void SetControlsEnabled(bool enabled)
        {
            _revealInputReady = enabled;
            UpdateControls();
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            var metrics = JourneyOverviewLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits(safeWidth, safeHeight)) return false;

            _safeSize = new Vector2(safeWidth, safeHeight);
            _background.Present(screenWidth, screenHeight);

            LayoutTopControl(_controls[RepeatControlId], 0, metrics, safeWidth, safeHeight);
            LayoutTopControl(_controls[PauseControlId], 1, metrics, safeWidth, safeHeight);
            LayoutTopControl(_controls[HomeControlId], 2, metrics, safeWidth, safeHeight);
            LayoutBottomControl(_controls[SkipControlId], metrics, safeWidth);
            LayoutTimeline(metrics);
            LayoutCaptionAndCue(metrics, safeWidth, safeHeight);
            return true;
        }

        public void ApplyResponsiveLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            if (!TryApplyResponsiveLayout(
                    screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels))
                throw new InvalidOperationException("Safe area cannot contain the frozen Journey Overview layout.");
        }

        public void PresentReveal(JourneyOverviewRevealFrame frame)
        {
            VisibleBeatCount = frame.RevealedBeatCount;
            CurrentPulseEnabled = frame.CurrentPulseEnabled;
            ApplyMedallion(0, frame.MeasureVisible, frame.SpatialMotionEnabled,
                frame.CurrentState, frame.MeasureIcon);
            ApplyMedallion(1, frame.ReadVisible, frame.SpatialMotionEnabled,
                frame.LockedState, frame.ReadIcon);
            ApplyMedallion(2, frame.HelpVisible, frame.SpatialMotionEnabled,
                frame.LockedState, frame.HelpIcon);
            ApplyConnector(0, frame.ReadVisible, frame.SpatialMotionEnabled, frame.Connector);
            ApplyConnector(1, frame.HelpVisible, frame.SpatialMotionEnabled, frame.Connector);
            _motionFrozen = !frame.SpatialMotionEnabled;
            if (_motionFrozen) _medallionGroups[0].transform.localScale = Vector3.one;
        }

        public void SetInputReady(bool ready)
        {
            _revealInputReady = ready;
            UpdateControls();
        }

        public void PresentMediaCue(JourneyOverviewMediaCue cue)
        {
            _visualEquivalent.sprite = cue.VisualEquivalent;
            _visualEquivalent.fillAmount = Mathf.Clamp01(cue.VisualEquivalentMaskPixels / 1024f);
            _visualEquivalent.gameObject.SetActive(true);
            _caption.text = JourneyOverviewCaptionStrings.ResolveEnglish(cue.CaptionId);
            _captionRequestedVisible = cue.CaptionVisible;
            ApplyCaptionVisibility();
        }

        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionSuppressed = suppressed;
            ApplyCaptionVisibility();
        }

        public void FreezeMediaCue(double elapsedSeconds)
        {
            _motionFrozen = true;
        }

        public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds)
        {
            _motionFrozen = false;
        }

        public void SetCallerInputLocked(bool locked)
        {
            _mediaInputLocked = locked;
            UpdateControls();
        }

        public void ShowPauseOverlay(JourneyOverviewPauseReason reason)
        {
            LastPauseReason = reason;
            PauseOverlayRequested?.Invoke(reason);
        }

        public void AdvanceMotion(float unscaledDeltaSeconds)
        {
            if (_motionFrozen || unscaledDeltaSeconds <= 0f) return;
            foreach (var group in _medallionGroups)
                if (group.gameObject.activeSelf && group.alpha < 1f)
                    group.alpha = Mathf.Min(1f, group.alpha + unscaledDeltaSeconds / JourneyOverviewRevealPresenter.FadeSeconds);
            foreach (var group in _connectorGroups)
                if (group.gameObject.activeSelf && group.alpha < 1f)
                    group.alpha = Mathf.Min(1f, group.alpha + unscaledDeltaSeconds / JourneyOverviewRevealPresenter.FadeSeconds);
            if (CurrentPulseEnabled)
            {
                _pulseElapsed += unscaledDeltaSeconds;
                var phase = Mathf.Sin(_pulseElapsed / 1.2f * Mathf.PI * 2f);
                var scale = 1f + 0.0175f * (phase + 1f);
                _medallionGroups[0].transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public Sprite GetVisibleSprite(string controlId) => _controls[controlId].VisibleArt.sprite;
        public Sprite GetTimelineState(int index) => _timelineStates[index].sprite;
        public Sprite GetTimelineIcon(int index) => _timelineIcons[index].sprite;
        public Rect GetControlCanvasRect(string controlId) => _controls[controlId].CanvasRect;
        public Rect GetControlScreenBounds(string controlId) =>
            JourneyViewFactory.ToScreenRect(_controls[controlId].Target);
        public Rect GetTimelineMedallionScreenBounds(int index) =>
            JourneyViewFactory.ToScreenRect((RectTransform)_medallionGroups[index].transform);
        public Rect GetTimelineConnectorScreenBounds(int index) =>
            JourneyViewFactory.ToScreenRect(_connectors[index].rectTransform);
        public bool DoTouchTargetsOverlap() => JourneyViewFactory.TargetsOverlap(_controls.Values);
        public bool AreAllTargetsInsideSafeArea() => JourneyViewFactory.Inside(_controls.Values, _safeSize);
        public bool CaptionInsideSafeArea() => Inside(_captionCanvasRect) && Inside(_visualCueCanvasRect);
        public bool TimelineInsideSafeArea()
        {
            foreach (var rect in _timelineCanvasRects) if (!Inside(rect)) return false;
            return true;
        }

        public bool AreTouchTargetsTransparent()
        {
            foreach (var control in _controls.Values)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            return true;
        }

        public bool VisibleArtRaycastsDisabled()
        {
            foreach (var control in _controls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            return AllTimelineImagesRaycastDisabled()
                && !_captionBackplate.raycastTarget
                && !_caption.raycastTarget
                && !_visualEquivalent.raycastTarget;
        }

        public bool AllTimelineImagesRaycastDisabled()
        {
            foreach (var item in _timelineStates) if (item.raycastTarget) return false;
            foreach (var item in _timelineIcons) if (item.raycastTarget) return false;
            foreach (var item in _connectors) if (item.raycastTarget) return false;
            return true;
        }

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("JourneyOverviewCanvas", transform, 130);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform,
                _content.Backgrounds[0],
                _content.Backgrounds[1],
                _content.Backgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);

            for (var index = 0; index < _connectors.Length; index++)
            {
                _connectors[index] = JourneyViewFactory.CreateImage(
                    "TimelineConnector" + (index + 1) + ".VisibleArt",
                    _safeRoot,
                    Color.white,
                    _content.TimelineConnector);
                _connectors[index].preserveAspect = true;
                _connectorGroups[index] = _connectors[index].gameObject.AddComponent<CanvasGroup>();
                _connectors[index].gameObject.SetActive(false);
            }

            for (var index = 0; index < 3; index++)
            {
                var root = new GameObject(
                    "TimelineMedallion" + (index + 1),
                    typeof(RectTransform),
                    typeof(CanvasGroup));
                root.transform.SetParent(_safeRoot, false);
                _medallionGroups[index] = root.GetComponent<CanvasGroup>();
                _timelineStates[index] = JourneyViewFactory.CreateImage(
                    "State.VisibleArt", root.transform, Color.white, _content.LockedTimelineState);
                _timelineStates[index].preserveAspect = true;
                _timelineIcons[index] = JourneyViewFactory.CreateImage(
                    "Icon.VisibleArt", root.transform, Color.white, _content.TimelineIcons[index]);
                _timelineIcons[index].preserveAspect = true;
                JourneyViewFactory.Stretch(_timelineStates[index].rectTransform);
                JourneyViewFactory.Stretch(_timelineIcons[index].rectTransform);
                root.SetActive(false);
            }

            _visualEquivalent = JourneyViewFactory.CreateImage(
                "NonTextVisualEquivalent", _safeRoot, Color.white, _content.OverviewVisualEquivalent);
            _visualEquivalent.preserveAspect = true;
            _visualEquivalent.type = Image.Type.Filled;
            _visualEquivalent.fillMethod = Image.FillMethod.Horizontal;
            _visualEquivalent.fillOrigin = (int)Image.OriginHorizontal.Left;
            _visualEquivalent.fillAmount = 0f;
            _captionBackplate = JourneyViewFactory.CreateImage(
                "CaptionBackplate", _safeRoot, Color.white, _content.CaptionBackplate);
            _captionBackplate.type = Image.Type.Sliced;
            _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _captionBackplate.gameObject.SetActive(false);

            AddControl(SkipControlId, "Skip", _content.OverviewControls[0]);
            AddControl(RepeatControlId, "Repeat", _content.OverviewControls[1]);
            AddControl(PauseControlId, "Pause", _content.OverviewControls[2]);
            AddControl(HomeControlId, "Home", _content.OverviewControls[3]);
        }

        private void ApplyCaptionVisibility()
        {
            var visible = _captionRequestedVisible && !_captionSuppressed;
            _captionBackplate.gameObject.SetActive(visible);
            _caption.gameObject.SetActive(visible);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutTopControl(
            JourneyControlView control,
            int index,
            JourneyOverviewLayoutMetrics metrics,
            float safeWidth,
            float safeHeight)
        {
            var x = safeWidth * 0.5f + (index - 1f) * (metrics.Target + metrics.Gap);
            var y = safeHeight - metrics.Target * 0.5f;
            JourneyViewFactory.SetTop(control,
                new Vector2(x - safeWidth * 0.5f, -metrics.Target * 0.5f), metrics.Target);
            control.CanvasRect = CenteredRect(x, y, metrics.Target, metrics.Target);
        }

        private void LayoutBottomControl(
            JourneyControlView control,
            JourneyOverviewLayoutMetrics metrics,
            float safeWidth)
        {
            var x = safeWidth * 0.5f;
            var y = metrics.Target * 0.5f;
            JourneyViewFactory.SetBottom(control, new Vector2(0f, y), metrics.Target);
            control.CanvasRect = CenteredRect(x, y, metrics.Target, metrics.Target);
        }

        private void LayoutTimeline(JourneyOverviewLayoutMetrics metrics)
        {
            var medallionSize = 0.88f * Mathf.Min(metrics.CardSize.x, metrics.CardSize.y);
            for (var index = 0; index < 3; index++)
            {
                var center = metrics.CardCenters[index];
                JourneyViewFactory.SetCentered(
                    (RectTransform)_medallionGroups[index].transform,
                    new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f),
                    new Vector2(medallionSize, medallionSize));
                _timelineCanvasRects[index] = CenteredRect(
                    center.x, center.y, medallionSize, medallionSize);
            }

            var step = metrics.CardCenters[1].x - metrics.CardCenters[0].x;
            var connectorSize = Mathf.Min(step, metrics.CardSize.y * 1024f / 430f);
            for (var index = 0; index < 2; index++)
            {
                var center = (metrics.CardCenters[index] + metrics.CardCenters[index + 1]) * 0.5f;
                JourneyViewFactory.SetCentered(
                    _connectors[index].rectTransform,
                    new Vector2(center.x - _safeSize.x * 0.5f, center.y - _safeSize.y * 0.5f),
                    new Vector2(connectorSize, connectorSize));
                _timelineCanvasRects[3 + index] = CenteredRect(
                    center.x, center.y, connectorSize, connectorSize);
            }
        }

        private void LayoutCaptionAndCue(
            JourneyOverviewLayoutMetrics metrics,
            float safeWidth,
            float safeHeight)
        {
            var totalWidth = metrics.CaptionWidth;
            var cueWidth = 2f * metrics.CaptionHeight;
            var captionWidth = totalWidth - cueWidth - metrics.CaptionGap;
            var left = safeWidth * 0.5f - totalWidth * 0.5f;
            var centerY = metrics.BodyTop - metrics.CaptionHeight * 0.5f;
            var cueCenterX = left + cueWidth * 0.5f;
            var captionCenterX = left + cueWidth + metrics.CaptionGap + captionWidth * 0.5f;
            JourneyViewFactory.SetCentered(_visualEquivalent.rectTransform,
                new Vector2(cueCenterX - safeWidth * 0.5f, centerY - safeHeight * 0.5f),
                new Vector2(cueWidth, metrics.CaptionHeight));
            JourneyViewFactory.SetCentered(_captionBackplate.rectTransform,
                new Vector2(captionCenterX - safeWidth * 0.5f, centerY - safeHeight * 0.5f),
                new Vector2(captionWidth, metrics.CaptionHeight));
            _visualCueCanvasRect = CenteredRect(
                cueCenterX, centerY, cueWidth, metrics.CaptionHeight);
            _captionCanvasRect = CenteredRect(
                captionCenterX, centerY, captionWidth, metrics.CaptionHeight);
        }

        private void ApplyMedallion(
            int index,
            bool visible,
            bool animate,
            Sprite state,
            Sprite icon)
        {
            var group = _medallionGroups[index];
            var wasVisible = group.gameObject.activeSelf;
            _timelineStates[index].sprite = state;
            _timelineIcons[index].sprite = icon;
            group.gameObject.SetActive(visible);
            if (!visible) group.alpha = 0f;
            else if (!wasVisible) group.alpha = animate ? 0f : 1f;
            else if (!animate && !_motionFrozen) group.alpha = 1f;
        }

        private void ApplyConnector(int index, bool visible, bool animate, Sprite sprite)
        {
            var group = _connectorGroups[index];
            var wasVisible = group.gameObject.activeSelf;
            _connectors[index].sprite = sprite;
            group.gameObject.SetActive(visible);
            if (!visible) group.alpha = 0f;
            else if (!wasVisible) group.alpha = animate ? 0f : 1f;
            else if (!animate && !_motionFrozen) group.alpha = 1f;
        }

        private void UpdateControls()
        {
            var enabled = _visible && _revealInputReady && !_mediaInputLocked;
            foreach (var control in _controls.Values) control.Semantic.Enabled = enabled;
        }

        private bool AllControlsEnabled()
        {
            foreach (var control in _controls.Values)
                if (!control.Semantic.Enabled) return false;
            return true;
        }

        private bool Inside(Rect rect)
        {
            return rect.xMin >= 0f && rect.yMin >= 0f
                && rect.xMax <= _safeSize.x && rect.yMax <= _safeSize.y;
        }

        private static Rect CenteredRect(float x, float y, float width, float height) =>
            new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
    }

    internal sealed class JourneyControlView
    {
        public JourneyControlView(Image targetImage, Image visibleArt, SemanticControl semantic)
        {
            TargetImage = targetImage;
            VisibleArt = visibleArt;
            Semantic = semantic;
        }

        public Image TargetImage { get; }
        public RectTransform Target => TargetImage.rectTransform;
        public Image VisibleArt { get; }
        public SemanticControl Semantic { get; }
        public Rect CanvasRect { get; set; }
    }

    internal static class JourneyViewFactory
    {
        public static GameObject CreateCanvas(string name, Transform parent, int sortingOrder)
        {
            var owner = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            owner.transform.SetParent(parent, false);
            var canvas = owner.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = owner.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return owner;
        }

        public static RectTransform CreateSafeRoot(GameObject canvas)
        {
            var root = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas.transform, false);
            Stretch(root);
            canvas.AddComponent<SafeAreaController>().Configure(root, Vector4.zero);
            return root;
        }

        public static ResponsiveBackgroundPresenter CreateBackground(
            Transform parent, Sprite fourByThree, Sprite sixteenByNine, Sprite ultraWide)
        {
            var owner = new GameObject(
                "AspectSpecificBackground", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(ResponsiveBackgroundPresenter));
            owner.transform.SetParent(parent, false);
            Stretch(owner.GetComponent<RectTransform>());
            var presenter = owner.GetComponent<ResponsiveBackgroundPresenter>();
            presenter.Configure(fourByThree, sixteenByNine, ultraWide);
            return presenter;
        }

        public static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            owner.transform.SetParent(parent, false);
            var image = owner.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        public static JourneyControlView CreateControl(
            string name, Transform parent, Sprite sprite, string semanticId, bool supportsDrag = false)
        {
            var target = CreateImage(name + ".TouchArea", parent, Color.clear, null);
            var art = CreateImage(name + ".VisibleArt", target.transform, Color.white, sprite);
            art.preserveAspect = true;
            var semantic = new SemanticControl(semanticId, () => ToScreenRect(target.rectTransform), supportsDrag);
            return new JourneyControlView(target, art, semantic);
        }

        public static TextMeshProUGUI CreateRuntimeText(string name, Transform parent)
        {
            var owner = new GameObject(
                name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            owner.transform.SetParent(parent, false);
            var text = owner.GetComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-SemiBold SDF");
            if (text.font == null) throw new InvalidOperationException("Approved Noto TMP font asset is missing.");
            text.text = string.Empty;
            text.fontSize = 46f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            Stretch(text.rectTransform);
            return text;
        }

        public static void SetCentered(RectTransform rect, Vector2 position, Vector2 size)
        {
            SetAnchored(rect, new Vector2(0.5f, 0.5f), position, size);
        }

        public static void SetTop(JourneyControlView control, Vector2 position, float target)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 1f), position, new Vector2(target, target));
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(target * 0.72f, target * 0.72f));
        }

        public static void SetBottom(JourneyControlView control, Vector2 position, float target)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 0f), position, new Vector2(target, target));
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(target * 0.72f, target * 0.72f));
        }

        public static Rect ToScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static bool TargetsOverlap(IEnumerable<JourneyControlView> controls)
        {
            var list = new List<JourneyControlView>(controls);
            for (var first = 0; first < list.Count; first++)
            for (var second = first + 1; second < list.Count; second++)
                if (list[first].CanvasRect.Overlaps(list[second].CanvasRect)) return true;
            return false;
        }

        public static bool Inside(IEnumerable<JourneyControlView> controls, Vector2 safeSize)
        {
            foreach (var control in controls)
            {
                var rect = control.CanvasRect;
                if (rect.xMin < 0f || rect.yMin < 0f
                    || rect.xMax > safeSize.x || rect.yMax > safeSize.y) return false;
            }
            return true;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchored(
            RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
