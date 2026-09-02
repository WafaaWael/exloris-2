using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Intro
{
    [DisallowMultipleComponent]
    public sealed class IntroStageView : MonoBehaviour, IIntroStageView
    {
        public const string ResumeControlId = "intro.resume";
        private const float TargetSize = 224f;
        private const float VisibleSize = 144f;

        private ChildInputRouter _inputRouter;
        private StartupIntroContent _content;
        private GameObject _canvasObject;
        private RawImage _video;
        private Image _ibis;
        private Image _visualEquivalent;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private RectTransform _skipTarget;
        private RectTransform _skipVisible;
        private Image _skipTargetImage;
        private RectTransform _resumeTarget;
        private Image _resumeVisible;
        private SemanticControl _skipSemantic;
        private SemanticControl _resumeSemantic;
        private readonly List<string> _childFacingControlIds = new List<string>();
        private bool _captionsEnabled;
        private bool _reducedMotion;
        private bool _overlayStarted;
        private Guid _token;

        public IReadOnlyList<string> ChildFacingControlIds => _childFacingControlIds;
        public bool VideoLayerPresent => _video != null;
        public bool IbisOverlayPresent => _ibis != null;
        public bool VisualEquivalentPresent => _visualEquivalent != null;
        public bool CaptionLayerPresent => _caption != null && _captionBackplate != null;
        public bool SkipTouchAreaIsTransparent => _skipTargetImage != null &&
            _skipTargetImage.color.a == 0f && !_skipTargetImage.raycastTarget;
        public bool SkipTargetContainsVisibleArt => Contains(SkipScreenBounds, ToScreenRect(_skipVisible));
        public Rect SkipScreenBounds => ToScreenRect(_skipTarget);
        public Rect CaptionBandScreenBounds => ToScreenRect(_captionBackplate == null
            ? null
            : _captionBackplate.rectTransform);
        public Vector2 SkipAnchorMin => _skipTarget == null ? Vector2.zero : _skipTarget.anchorMin;
        public Vector2 SkipAnchorMax => _skipTarget == null ? Vector2.zero : _skipTarget.anchorMax;
        public Vector2 SkipCanvasAnchoredPosition => _skipTarget == null
            ? Vector2.zero
            : _skipTarget.anchoredPosition;
        public Vector2 SkipCanvasSize => _skipTarget == null ? Vector2.zero : _skipTarget.sizeDelta;
        public Rect ResumeScreenBounds => ToScreenRect(_resumeTarget);
        public bool ResumeVisible => _resumeVisible != null && _resumeVisible.gameObject.activeSelf;
        public bool WelcomeBoundaryReached { get; private set; }
        public double MaximumObservedOverlayDriftSeconds { get; private set; }
        public double LastSynchronizedMediaTimeSeconds { get; private set; }

        public void Configure(
            ChildInputRouter inputRouter,
            StartupIntroContent content,
            string captionText = null)
        {
            _inputRouter = inputRouter ?? throw new ArgumentNullException(nameof(inputRouter));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            var resolvedCaption = captionText ??
                FoundationEnglishStrings.ById[FoundationStringIds.IntroVoiceCaption];
            BuildSurface(resolvedCaption);
        }

        public void ShowIntro(
            Guid tokenId,
            IntroMediaVariant variant,
            IntroFirstFramePresentation presentation)
        {
            if (_inputRouter == null || _content == null)
            {
                throw new InvalidOperationException("Intro view must be configured before presentation.");
            }

            _token = tokenId;
            _reducedMotion = variant.FrameCount == IntroVariantSelector.ReducedFrameCount;
            _overlayStarted = false;
            MaximumObservedOverlayDriftSeconds = 0d;
            LastSynchronizedMediaTimeSeconds = 0d;
            _childFacingControlIds.Clear();
            foreach (var id in presentation.ChildFacingControlIds) _childFacingControlIds.Add(id);
            _skipSemantic.Enabled = true;
            _resumeSemantic.Enabled = false;
            _resumeVisible.gameObject.SetActive(false);
            _skipVisible.gameObject.SetActive(true);
            _canvasObject.SetActive(true);
            WelcomeBoundaryReached = false;
            UpdateSynchronizedLayers(tokenId, 0d);
            _inputRouter.SetVisibleStateReady(true);
        }

        public bool ReplacePlaybackToken(Guid previousTokenId, Guid replacementTokenId)
        {
            if (previousTokenId == Guid.Empty || replacementTokenId == Guid.Empty || _token != previousTokenId)
            {
                return false;
            }

            _token = replacementTokenId;
            return true;
        }

        public void ShowWelcomeBoundary()
        {
            if (_canvasObject != null) _canvasObject.SetActive(false);
            _childFacingControlIds.Clear();
            WelcomeBoundaryReached = true;
        }

        public void ShowInterruptionResume()
        {
            _childFacingControlIds.Clear();
            _childFacingControlIds.Add(ResumeControlId);
            _skipSemantic.Enabled = false;
            _resumeSemantic.Enabled = true;
            _skipVisible.gameObject.SetActive(false);
            _resumeVisible.gameObject.SetActive(true);
            _inputRouter.SetVisibleStateReady(true);
        }

        public void HideInterruptionResume()
        {
            _childFacingControlIds.Clear();
            _childFacingControlIds.Add(IntroFirstFramePresentation.SkipControlId);
            _resumeSemantic.Enabled = false;
            _skipSemantic.Enabled = true;
            _resumeVisible.gameObject.SetActive(false);
            _skipVisible.gameObject.SetActive(true);
            _inputRouter.SetVisibleStateReady(true);
        }

        public void BindVideoTexture(Texture texture)
        {
            if (_video != null) _video.texture = texture;
        }

        public void SetCaptionsEnabled(bool enabled)
        {
            _captionsEnabled = enabled;
        }

        public bool UpdateSynchronizedLayers(Guid tokenId, double mediaTimeSeconds)
        {
            if (_visualEquivalent == null || tokenId != _token) return false;

            var snapshot = IntroSynchronizationContract.Evaluate(_reducedMotion, mediaTimeSeconds);
            LastSynchronizedMediaTimeSeconds = snapshot.MediaTimeSeconds;

            _visualEquivalent.gameObject.SetActive(snapshot.VisualEquivalentVisible);
            _captionBackplate.gameObject.SetActive(_captionsEnabled && snapshot.CaptionVisible);
            _caption.gameObject.SetActive(_captionsEnabled && snapshot.CaptionVisible);

            var showIbis = snapshot.IbisPose != IntroIbisPose.Hidden;
            _ibis.gameObject.SetActive(showIbis);
            if (showIbis && !_overlayStarted)
            {
                _overlayStarted = true;
                var cueTime = _reducedMotion
                    ? IntroSynchronizationContract.ReducedStaticIbisSeconds
                    : IntroSynchronizationContract.FullIbisEntrySeconds;
                MaximumObservedOverlayDriftSeconds = Math.Abs(mediaTimeSeconds - cueTime);
            }

            if (!showIbis) return true;
            switch (snapshot.IbisPose)
            {
                case IntroIbisPose.StaticReduced:
                    _ibis.sprite = _content.IbisStaticReduced;
                    break;
                case IntroIbisPose.WingsOpen:
                    _ibis.sprite = _content.IbisWingsOpen;
                    break;
                case IntroIbisPose.HeadLift:
                    _ibis.sprite = _content.IbisHeadLift;
                    break;
                case IntroIbisPose.BeakDirection:
                    _ibis.sprite = _content.IbisBeakRight;
                    break;
                case IntroIbisPose.CalmSettle:
                    _ibis.sprite = _content.IbisCalmSettle;
                    break;
            }

            return true;
        }

        private void BuildSurface(string captionText)
        {
            if (_canvasObject != null) return;

            _canvasObject = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _canvasObject.transform.SetParent(transform, false);
            var canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ResponsiveLayoutProfile.ReferenceWidth, ResponsiveLayoutProfile.ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _video = CreateRawImage("AspectSpecificVideo", _canvasObject.transform, Color.white);
            Stretch(_video.rectTransform);

            var safeRoot = new GameObject("IntroSafeRoot", typeof(RectTransform)).GetComponent<RectTransform>();
            safeRoot.SetParent(_canvasObject.transform, false);
            Stretch(safeRoot);
            var safeArea = _canvasObject.AddComponent<SafeAreaController>();
            safeArea.Configure(safeRoot, Vector4.zero);

            _visualEquivalent = CreateImage("NonTextVisualEquivalent", safeRoot, Color.white, _content.VisualEquivalent);
            SetRect(_visualEquivalent.rectTransform, new Vector2(0f, -100f), new Vector2(720f, 360f));

            _ibis = CreateImage("SeparateIbisOverlay", safeRoot, Color.white, _content.IbisWingsOpen);
            _ibis.preserveAspect = true;
            SetRect(_ibis.rectTransform, new Vector2(300f, 80f), new Vector2(380f, 380f));

            _captionBackplate = CreateImage("CaptionBackplate", safeRoot, Color.white, _content.CaptionBackplate);
            SetTopCenter(_captionBackplate.rectTransform, new Vector2(0f, -190f), new Vector2(1200f, 112f));
            _caption = CreateText("Caption", _captionBackplate.transform, captionText);
            Stretch(_caption.rectTransform);

            _skipTargetImage = CreateImage("Skip.TouchArea", safeRoot, Color.clear, null);
            _skipTargetImage.raycastTarget = false;
            _skipTarget = _skipTargetImage.rectTransform;
            SetBottomCenter(_skipTarget, new Vector2(0f, TargetSize * 0.5f), new Vector2(TargetSize, TargetSize));
            var visible = CreateImage("Skip.VisibleArt", _skipTarget, Color.white, _content.Skip);
            visible.raycastTarget = false;
            visible.preserveAspect = true;
            _skipVisible = visible.rectTransform;
            SetRect(_skipVisible, Vector2.zero, new Vector2(VisibleSize, VisibleSize));

            _skipSemantic = new SemanticControl(
                IntroFirstFramePresentation.SkipControlId,
                () => SkipScreenBounds);
            _inputRouter.Register(_skipSemantic);

            var resumeTargetImage = CreateImage("Resume.TouchArea", safeRoot, Color.clear, null);
            resumeTargetImage.raycastTarget = false;
            _resumeTarget = resumeTargetImage.rectTransform;
            SetBottomCenter(_resumeTarget, new Vector2(0f, TargetSize * 0.5f), new Vector2(TargetSize, TargetSize));
            _resumeVisible = CreateImage("Resume.VisibleArt", _resumeTarget, Color.white, _content.Resume);
            _resumeVisible.raycastTarget = false;
            _resumeVisible.preserveAspect = true;
            SetRect(_resumeVisible.rectTransform, Vector2.zero, new Vector2(VisibleSize, VisibleSize));
            _resumeSemantic = new SemanticControl(ResumeControlId, () => ResumeScreenBounds) { Enabled = false };
            _inputRouter.Register(_resumeSemantic);
            _resumeVisible.gameObject.SetActive(false);
            _canvasObject.SetActive(false);
        }

        private static Image CreateImage(string name, Transform parent, Color color, Sprite sprite)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            owner.transform.SetParent(parent, false);
            var image = owner.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage CreateRawImage(string name, Transform parent, Color color)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            owner.transform.SetParent(parent, false);
            var image = owner.GetComponent<RawImage>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value)
        {
            var owner = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            owner.transform.SetParent(parent, false);
            var text = owner.GetComponent<TextMeshProUGUI>();
            text.font = Resources.Load<TMP_FontAsset>("Fonts/NotoSans-SemiBold SDF");
            if (text.font == null) throw new InvalidOperationException("Approved Noto TMP font asset is missing.");
            text.text = value;
            text.fontSize = 46f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void SetBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
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

        private static Rect ToScreenRect(RectTransform rect)
        {
            if (rect == null) return Rect.zero;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var min = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            var max = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin && inner.xMax <= outer.xMax &&
                   inner.yMin >= outer.yMin && inner.yMax <= outer.yMax;
        }
    }
}
