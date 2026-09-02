using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Welcome
{
    public enum WelcomeTechnicalOverlayState
    {
        Hidden,
        Loading,
        Paused,
        Recovery
    }

    [DisallowMultipleComponent]
    public sealed class WelcomeStageView : MonoBehaviour, IWelcomeView, IWelcomeMediaCueSink
    {
        public const string AdultControlId = "welcome.adult";
        public const string SoundControlId = "welcome.sound";
        public const string CaptionsControlId = "welcome.captions";
        public const string MotionControlId = "welcome.motion";
        public const string PlayControlId = "welcome.play";
        public const string ResumeControlId = "welcome.interruption.resume";
        public const string RetryControlId = "welcome.recovery.retry";
        public const string HomeControlId = "welcome.recovery.home";

        private readonly Dictionary<string, WelcomeControlView> _controls = new Dictionary<string, WelcomeControlView>();
        private readonly List<string> _controlIds = new List<string>();
        private readonly Dictionary<string, WelcomeControlView> _technicalControls = new Dictionary<string, WelcomeControlView>();
        private readonly List<string> _technicalControlIds = new List<string>();
        private ChildInputRouter _inputRouter;
        private WelcomeContent _content;
        private GameObject _canvasObject;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private SessionChoiceView _sessionChoice;
        private Image _captionBand;
        private TextMeshProUGUI _captionText;
        private Image _visualEquivalent;
        private GameObject _technicalCanvas;
        private RectTransform _technicalSafeRoot;
        private Image _technicalBackplate;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public IReadOnlyList<string> TechnicalControlIds => _technicalControlIds;
        public float CurrentTargetSize { get; private set; }
        public float CurrentGap { get; private set; }
        public Vector2 CurrentSafeSize { get; private set; }
        public Rect CaptionBandCanvasRect { get; private set; }
        public Rect VisualEquivalentCanvasRect { get; private set; }
        public bool VisualEquivalentVisible => _visualEquivalent != null && _visualEquivalent.gameObject.activeSelf;
        public bool CaptionVisible => _captionBand != null && _captionBand.gameObject.activeSelf;
        public string CurrentCaptionId { get; private set; }
        public string CurrentCaptionText => _captionText == null ? string.Empty : _captionText.text;
        public Sprite CurrentVisualSprite => _visualEquivalent == null ? null : _visualEquivalent.sprite;
        public bool MediaCuePaused { get; private set; }
        public WelcomeTechnicalOverlayState TechnicalOverlayState { get; private set; }
        public bool NormalControlsEnabled
        {
            get
            {
                foreach (var control in _controls.Values)
                {
                    if (!control.Semantic.Enabled) return false;
                }

                return _controls.Count > 0;
            }
        }
        public bool NormalControlsVisible
        {
            get
            {
                foreach (var control in _controls.Values)
                {
                    if (!control.Target.gameObject.activeSelf) return false;
                }

                return _controls.Count > 0;
            }
        }
        public ResponsiveBackgroundVariant BackgroundVariant => _background == null
            ? ResponsiveBackgroundVariant.SixteenByNine
            : _background.CurrentVariant;

        public void Configure(ChildInputRouter inputRouter, WelcomeContent content, SessionChoiceView sessionChoice = null)
        {
            _inputRouter = inputRouter ?? throw new ArgumentNullException(nameof(inputRouter));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _sessionChoice = sessionChoice;
            BuildSurface();
            Show(WelcomePresentationState.Welcome);
        }

        public void Show(WelcomePresentationState state)
        {
            if (_canvasObject == null) throw new InvalidOperationException("Welcome view is not configured.");
            var showWelcome = state == WelcomePresentationState.Welcome;
            _canvasObject.SetActive(showWelcome);
            SetControlsEnabled(showWelcome);
            SetNormalControlsVisible(showWelcome);
            if (_sessionChoice != null)
            {
                if (showWelcome) _sessionChoice.Hide();
                else _sessionChoice.Show();
            }

            if (showWelcome) _inputRouter.SetVisibleStateReady(true);
        }

        public void SuspendForAdultGate()
        {
            if (_canvasObject == null) return;
            SetControlsEnabled(false);
            _sessionChoice?.Hide();
            gameObject.SetActive(false);
        }

        public void RestoreFromAdultGate(WelcomePresentationState state)
        {
            gameObject.SetActive(true);
            Show(state);
        }

        public Sprite GetVisibleSprite(string controlId)
        {
            return RequireControl(controlId).VisibleArt.sprite;
        }

        public bool AreTouchTargetsTransparent()
        {
            foreach (var control in _controls.Values)
            {
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            }

            return true;
        }

        public bool VisibleArtRaycastsDisabled()
        {
            foreach (var control in _controls.Values)
            {
                if (control.VisibleArt.raycastTarget) return false;
            }

            return true;
        }

        public void SetSoundEnabled(bool enabled)
        {
            RequireControl(SoundControlId).VisibleArt.sprite = enabled ? _content.SoundOn : _content.SoundOff;
        }

        public void SetCaptionsEnabled(bool enabled)
        {
            RequireControl(CaptionsControlId).VisibleArt.sprite = enabled ? _content.CaptionsOn : _content.CaptionsOff;
            if (_captionBand != null) _captionBand.gameObject.SetActive(enabled);
        }

        public void SetReducedMotionEnabled(bool enabled)
        {
            RequireControl(MotionControlId).VisibleArt.sprite = enabled
                ? _content.ReducedMotionOn
                : _content.ReducedMotionOff;
        }

        public void ShowMediaCue(
            WelcomePresentationState state,
            Sprite visual,
            string captionId,
            bool captionsVisible,
            double startedAtSeconds)
        {
            if (state == WelcomePresentationState.SessionChoice)
            {
                if (_sessionChoice == null) throw new InvalidOperationException("Session Choice media view is missing.");
                _sessionChoice.ShowMediaCue(visual, captionId, captionsVisible, startedAtSeconds);
                return;
            }

            _visualEquivalent.sprite = visual;
            _visualEquivalent.gameObject.SetActive(true);
            UpdateCaption(captionId, captionsVisible, startedAtSeconds);
        }

        public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
        {
            if (_sessionChoice != null && _sessionChoice.IsVisible)
            {
                _sessionChoice.UpdateCaption(captionId, visible, cueStartedAtSeconds);
                return;
            }

            CurrentCaptionId = captionId;
            _captionText.text = WelcomeCaptionStrings.ResolveEnglish(captionId);
            _captionBand.gameObject.SetActive(visible);
            _captionText.gameObject.SetActive(visible);
        }

        public void PauseMediaCue()
        {
            MediaCuePaused = true;
            _sessionChoice?.PauseMediaCue();
        }

        public void ResumeMediaCue()
        {
            MediaCuePaused = false;
            _sessionChoice?.ResumeMediaCue();
        }

        public void ShowTechnicalOverlay(WelcomeTechnicalOverlayState state)
        {
            if (state == WelcomeTechnicalOverlayState.Hidden)
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            TechnicalOverlayState = state;
            SetControlsEnabled(false);
            SetNormalControlsVisible(false);
            _visualEquivalent.gameObject.SetActive(false);
            _sessionChoice?.Hide();
            _technicalCanvas.SetActive(true);
            SetTechnicalControlsEnabled(state);
            _inputRouter.SetVisibleStateReady(state != WelcomeTechnicalOverlayState.Loading);
        }

        public void HideTechnicalOverlay(WelcomePresentationState presentation)
        {
            TechnicalOverlayState = WelcomeTechnicalOverlayState.Hidden;
            SetTechnicalControlsEnabled(WelcomeTechnicalOverlayState.Hidden);
            _technicalCanvas.SetActive(false);
            Show(presentation);
        }

        public bool IsTechnicalControlEnabled(string controlId)
        {
            if (!_technicalControls.TryGetValue(controlId, out var control)) throw new KeyNotFoundException(controlId);
            return control.Semantic.Enabled;
        }

        public bool IsTechnicalControlVisible(string controlId)
        {
            if (!_technicalControls.TryGetValue(controlId, out var control)) throw new KeyNotFoundException(controlId);
            return control.Target.gameObject.activeSelf;
        }

        public void ApplyResponsiveLayout(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            var metrics = WelcomeLayoutMetrics.Calculate(
                screenWidth,
                screenHeight,
                safeWidth,
                safeHeight,
                ppi,
                platformMinimumPixels);
            CurrentTargetSize = metrics.Target;
            CurrentGap = metrics.Gap;
            CurrentSafeSize = new Vector2(safeWidth, safeHeight);
            for (var index = 0; index < 4; index++)
            {
                var xOffset = (index - 1.5f) * (metrics.Target + metrics.Gap);
                var control = _controls[_controlIds[index]];
                WelcomeViewFactory.SetTopControl(
                    control,
                    new Vector2(xOffset, -metrics.Target * 0.5f),
                    new Vector2(metrics.Target, metrics.Target),
                    0.72f,
                    0.72f);
                control.CanvasRect = CenteredRect(
                    safeWidth * 0.5f + xOffset,
                    safeHeight - metrics.Target * 0.5f,
                    metrics.Target,
                    metrics.Target);
            }

            var playSize = new Vector2(2f * metrics.Target + metrics.Gap, metrics.Target);
            var play = _controls[PlayControlId];
            WelcomeViewFactory.SetBottomControl(
                play,
                new Vector2(0f, metrics.Target * 0.5f),
                playSize,
                0.88f,
                0.72f);
            play.CanvasRect = CenteredRect(safeWidth * 0.5f, metrics.Target * 0.5f, playSize.x, playSize.y);

            var resume = _technicalControls[ResumeControlId];
            WelcomeViewFactory.SetBottomControl(
                resume,
                new Vector2(0f, metrics.Target * 0.5f),
                new Vector2(metrics.Target, metrics.Target),
                0.72f,
                0.72f);
            resume.CanvasRect = CenteredRect(
                safeWidth * 0.5f,
                metrics.Target * 0.5f,
                metrics.Target,
                metrics.Target);
            var recoveryOffset = (metrics.Target + metrics.Gap) * 0.5f;
            var retry = _technicalControls[RetryControlId];
            var home = _technicalControls[HomeControlId];
            WelcomeViewFactory.SetBottomControl(
                retry,
                new Vector2(-recoveryOffset, metrics.Target * 0.5f),
                new Vector2(metrics.Target, metrics.Target),
                0.72f,
                0.72f);
            WelcomeViewFactory.SetBottomControl(
                home,
                new Vector2(recoveryOffset, metrics.Target * 0.5f),
                new Vector2(metrics.Target, metrics.Target),
                0.72f,
                0.72f);
            retry.CanvasRect = CenteredRect(
                safeWidth * 0.5f - recoveryOffset,
                metrics.Target * 0.5f,
                metrics.Target,
                metrics.Target);
            home.CanvasRect = CenteredRect(
                safeWidth * 0.5f + recoveryOffset,
                metrics.Target * 0.5f,
                metrics.Target,
                metrics.Target);
            var bodyBottom = metrics.Target + metrics.Gap;
            var bodyTop = safeHeight - metrics.Target - metrics.Gap;
            var bodyHeight = bodyTop - bodyBottom;
            var captionHeight = Mathf.Min(0.16f * safeHeight, 0.25f * bodyHeight);
            var captionWidth = Mathf.Min(0.72f * safeWidth, 1200f);
            var captionCenterY = bodyTop - captionHeight * 0.5f;
            CaptionBandCanvasRect = CenteredRect(safeWidth * 0.5f, captionCenterY, captionWidth, captionHeight);
            WelcomeViewFactory.SetCentered(
                _captionBand.rectTransform,
                new Vector2(0f, captionCenterY - safeHeight * 0.5f),
                new Vector2(captionWidth, captionHeight));
            VisualEquivalentCanvasRect = play.CanvasRect;
            WelcomeViewFactory.SetCentered(
                _visualEquivalent.rectTransform,
                new Vector2(0f, metrics.Target * 0.5f - safeHeight * 0.5f),
                playSize);
            _background.Present(screenWidth, screenHeight);
        }

        public Rect GetControlCanvasRect(string controlId)
        {
            return RequireControl(controlId).CanvasRect;
        }

        public Rect GetControlScreenBounds(string controlId)
        {
            return WelcomeViewFactory.ToScreenRect(RequireControl(controlId).Target);
        }

        public Rect GetTechnicalControlScreenBounds(string controlId)
        {
            if (!_technicalControls.TryGetValue(controlId, out var control)) throw new KeyNotFoundException(controlId);
            return WelcomeViewFactory.ToScreenRect(control.Target);
        }

        public bool DoTouchTargetsOverlap()
        {
            var values = new List<WelcomeControlView>(_controls.Values);
            for (var first = 0; first < values.Count; first++)
            {
                for (var second = first + 1; second < values.Count; second++)
                {
                    if (values[first].CanvasRect.Overlaps(values[second].CanvasRect)) return true;
                }
            }

            return false;
        }

        public bool AreAllTargetsInsideSafeArea()
        {
            foreach (var control in _controls.Values)
            {
                var rect = control.CanvasRect;
                if (rect.xMin < 0f || rect.yMin < 0f || rect.xMax > CurrentSafeSize.x || rect.yMax > CurrentSafeSize.y)
                {
                    return false;
                }
            }

            return true;
        }

        public bool DoesEveryTargetContainVisibleArt()
        {
            foreach (var control in _controls.Values)
            {
                var visible = control.VisibleArt.rectTransform.sizeDelta;
                var target = control.Target.sizeDelta;
                if (visible.x > target.x || visible.y > target.y) return false;
            }

            return true;
        }

        private void BuildSurface()
        {
            if (_canvasObject != null) return;
            _canvasObject = WelcomeViewFactory.CreateCanvas("WelcomeCanvas", transform, 110);
            _background = WelcomeViewFactory.CreateBackground(
                _canvasObject.transform,
                _content.Background4x3,
                _content.Background16x9,
                _content.Background24x10);
            _safeRoot = WelcomeViewFactory.CreateSafeRoot(_canvasObject);
            _captionBand = WelcomeViewFactory.CreatePresentationImage(
                "CaptionBand",
                _safeRoot,
                _content.CaptionBackplate);
            _captionText = WelcomeViewFactory.CreateRuntimeText("Caption", _captionBand.transform);
            _captionBand.gameObject.SetActive(false);
            _visualEquivalent = WelcomeViewFactory.CreatePresentationImage(
                "NonTextVisualEquivalent",
                _safeRoot,
                _content.WelcomeVisualEquivalent);
            _visualEquivalent.preserveAspect = true;
            _visualEquivalent.gameObject.SetActive(false);
            AddControl(AdultControlId, "Adult", _content.Adult);
            AddControl(SoundControlId, "Sound", _content.SoundOn);
            AddControl(CaptionsControlId, "Captions", _content.CaptionsOn);
            AddControl(MotionControlId, "Motion", _content.ReducedMotionOn);
            AddControl(PlayControlId, "Play", _content.Play);
            BuildTechnicalOverlay();
            ApplyDefaultLayout();
        }

        private void BuildTechnicalOverlay()
        {
            _technicalCanvas = WelcomeViewFactory.CreateCanvas("WelcomeTechnicalOverlay", transform, 130);
            _technicalSafeRoot = WelcomeViewFactory.CreateSafeRoot(_technicalCanvas);
            _technicalBackplate = WelcomeViewFactory.CreatePresentationImage(
                "TechnicalOverlayBackplate",
                _technicalSafeRoot,
                null);
            _technicalBackplate.color = new Color(0f, 0f, 0f, 0.38f);
            WelcomeViewFactory.StretchToParent(_technicalBackplate.rectTransform);
            AddTechnicalControl(ResumeControlId, "Resume", _content.Resume);
            AddTechnicalControl(RetryControlId, "Retry", _content.Retry);
            AddTechnicalControl(HomeControlId, "Home", _content.Home);
            SetTechnicalControlsEnabled(WelcomeTechnicalOverlayState.Hidden);
            _technicalCanvas.SetActive(false);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = WelcomeViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _inputRouter.Register(control.Semantic);
        }

        private void AddTechnicalControl(string id, string name, Sprite sprite)
        {
            var control = WelcomeViewFactory.CreateControl(name, _technicalSafeRoot, sprite, id);
            _technicalControls.Add(id, control);
            _technicalControlIds.Add(id);
            _inputRouter.Register(control.Semantic);
        }

        private void ApplyDefaultLayout()
        {
            ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
        }

        private void SetControlsEnabled(bool enabled)
        {
            foreach (var control in _controls.Values) control.Semantic.Enabled = enabled;
        }

        private void SetNormalControlsVisible(bool visible)
        {
            foreach (var control in _controls.Values) control.Target.gameObject.SetActive(visible);
        }

        private void SetTechnicalControlsEnabled(WelcomeTechnicalOverlayState state)
        {
            foreach (var control in _technicalControls.Values)
            {
                control.Semantic.Enabled = false;
                control.Target.gameObject.SetActive(false);
            }
            if (state == WelcomeTechnicalOverlayState.Paused)
            {
                _technicalControls[ResumeControlId].Semantic.Enabled = true;
                _technicalControls[ResumeControlId].Target.gameObject.SetActive(true);
            }
            else if (state == WelcomeTechnicalOverlayState.Recovery)
            {
                _technicalControls[RetryControlId].Semantic.Enabled = true;
                _technicalControls[HomeControlId].Semantic.Enabled = true;
                _technicalControls[RetryControlId].Target.gameObject.SetActive(true);
                _technicalControls[HomeControlId].Target.gameObject.SetActive(true);
            }
        }

        private WelcomeControlView RequireControl(string id)
        {
            if (_controls.TryGetValue(id, out var control)) return control;
            if (_technicalControls.TryGetValue(id, out control)) return control;
            throw new KeyNotFoundException(id);
        }

        private static Rect CenteredRect(float centerX, float centerY, float width, float height)
        {
            return new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);
        }
    }

    internal sealed class WelcomeControlView
    {
        public WelcomeControlView(Image targetImage, Image visibleArt, SemanticControl semantic)
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

    internal readonly struct WelcomeLayoutMetrics
    {
        public WelcomeLayoutMetrics(float target, float gap)
        {
            Target = target;
            Gap = gap;
        }

        public float Target { get; }
        public float Gap { get; }

        public static WelcomeLayoutMetrics Calculate(
            float screenWidth,
            float screenHeight,
            float safeWidth,
            float safeHeight,
            float ppi,
            int platformMinimumPixels)
        {
            if (safeWidth <= 0f) throw new ArgumentOutOfRangeException(nameof(safeWidth));
            if (safeHeight <= 0f) throw new ArgumentOutOfRangeException(nameof(safeHeight));
            var aspect = screenWidth / screenHeight;
            var fourDistance = Mathf.Abs(aspect - 4f / 3f);
            var sixteenDistance = Mathf.Abs(aspect - 16f / 9f);
            var wideDistance = Mathf.Abs(aspect - 2.4f);
            float boardTarget;
            float boardGap;
            if (fourDistance <= sixteenDistance && fourDistance <= wideDistance)
            {
                boardTarget = 97f;
                boardGap = 41f;
            }
            else if (sixteenDistance <= wideDistance)
            {
                boardTarget = 224f;
                boardGap = 94f;
            }
            else
            {
                boardTarget = 182f;
                boardGap = 77f;
            }

            var canvasScale = ResponsiveLayoutProfile.CalculateCanvasScale(screenWidth, screenHeight);
            var targetPixels = ResponsiveLayoutProfile.CalculateTargetPhysicalPixels(ppi, platformMinimumPixels);
            var deviceTarget = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(targetPixels, canvasScale);
            var gapPixels = ResponsiveLayoutProfile.MillimetresToPhysicalPixels(
                ResponsiveLayoutProfile.MinimumGapMillimetres,
                ppi);
            var deviceGap = ResponsiveLayoutProfile.PhysicalPixelsToCanvasCeiling(gapPixels, canvasScale);
            var target = Mathf.Max(boardTarget, deviceTarget);
            var millimetreGap = Mathf.Max(boardGap, deviceGap);
            return new WelcomeLayoutMetrics(
                target,
                ResponsiveLayoutProfile.CalculateRequiredGap(target, millimetreGap));
        }
    }

    internal static class WelcomeViewFactory
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
            scaler.referenceResolution = new Vector2(ResponsiveLayoutProfile.ReferenceWidth, ResponsiveLayoutProfile.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return owner;
        }

        public static ResponsiveBackgroundPresenter CreateBackground(
            Transform parent,
            Sprite fourByThree,
            Sprite sixteenByNine,
            Sprite ultraWide)
        {
            var owner = new GameObject(
                "AspectSpecificBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(ResponsiveBackgroundPresenter));
            owner.transform.SetParent(parent, false);
            var presenter = owner.GetComponent<ResponsiveBackgroundPresenter>();
            Stretch(owner.GetComponent<RectTransform>());
            presenter.Configure(fourByThree, sixteenByNine, ultraWide);
            return presenter;
        }

        public static RectTransform CreateSafeRoot(GameObject canvasObject)
        {
            var safeRoot = new GameObject("SafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(canvasObject.transform, false);
            Stretch(safeRoot);
            canvasObject.AddComponent<SafeAreaController>().Configure(safeRoot, Vector4.zero);
            return safeRoot;
        }

        public static WelcomeControlView CreateControl(string name, Transform parent, Sprite sprite, string semanticId)
        {
            var target = CreateImage(name + ".TouchArea", parent, Color.clear, null);
            target.raycastTarget = false;
            var visible = CreateImage(name + ".VisibleArt", target.transform, Color.white, sprite);
            visible.raycastTarget = false;
            visible.preserveAspect = true;
            var semantic = new SemanticControl(semanticId, () => ToScreenRect(target.rectTransform));
            return new WelcomeControlView(target, visible, semantic);
        }

        public static Image CreatePresentationImage(string name, Transform parent, Sprite sprite)
        {
            var image = CreateImage(name, parent, Color.white, sprite);
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        public static TextMeshProUGUI CreateRuntimeText(string name, Transform parent)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
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

        public static void StretchToParent(RectTransform rect)
        {
            Stretch(rect);
        }

        public static void SetTopControl(
            WelcomeControlView control,
            Vector2 position,
            Vector2 size,
            float artWidthFraction,
            float artHeightFraction)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 1f), position, size);
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size.x * artWidthFraction, size.y * artHeightFraction));
        }

        public static void SetBottomControl(
            WelcomeControlView control,
            Vector2 position,
            Vector2 size,
            float artWidthFraction,
            float artHeightFraction)
        {
            SetAnchored(control.Target, new Vector2(0.5f, 0f), position, size);
            SetAnchored(control.VisibleArt.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(size.x * artWidthFraction, size.y * artHeightFraction));
        }

        public static Rect ToScreenRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            owner.transform.SetParent(parent, false);
            var image = owner.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
