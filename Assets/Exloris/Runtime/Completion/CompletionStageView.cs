using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Completion
{
    [DisallowMultipleComponent]
    public sealed class CompletionStageView : MonoBehaviour, ICompletionView,
        ICompletionMotionSink, ICompletionMediaCueSink
    {
        public const string AdultControlId = "completion.adult";
        public const string PauseControlId = "completion.pause";
        public const string HomeControlId = "completion.home";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly List<string> _controlIds = new List<string>();
        private ChildInputRouter _router;
        private CompletionAdultReportContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _selectedChild;
        private Image _visualEquivalent;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private Vector2 _safeSize;
        private bool _ready;
        private bool _mediaLocked;
        private bool _captionSuppressed;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public int SelectedChildCount => _selectedChild != null && _selectedChild.gameObject.activeSelf ? 1 : 0;
        public Rect SelectedChildRect { get; private set; }
        public float SelectedChildOpacity => _selectedChild == null ? 0f : _selectedChild.color.a;
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeInHierarchy;
        public bool VisualEquivalentVisible => _visualEquivalent != null && _visualEquivalent.gameObject.activeInHierarchy;
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public Rect GetControlScreenBounds(string controlId) =>
            JourneyViewFactory.ToScreenRect(_controls[controlId].Target);

        public void Configure(ChildInputRouter router, CompletionAdultReportContent content, string avatarId)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Build();
            SelectAvatar(avatarId);
            if (!TryApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88))
                throw new InvalidOperationException("Default Completion layout is infeasible.");
            UpdateControls();
        }

        public void SelectAvatar(string avatarId)
        {
            if (_selectedChild == null) throw new InvalidOperationException("Completion view is not configured.");
            _selectedChild.sprite = avatarId == "avatar_boy" ? _content.CompletionAvatars[0]
                : avatarId == "avatar_girl" ? _content.CompletionAvatars[1]
                : throw new ArgumentException("Completion avatar must be avatar_boy or avatar_girl.", nameof(avatarId));
            _selectedChild.gameObject.SetActive(true);
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var metrics = CompletionLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits(safeWidth, safeHeight)) return false;
            _safeSize = new Vector2(safeWidth, safeHeight);
            _background.Present(screenWidth, screenHeight);

            ClosingRouteViewFactory.SetControlTop(_controls[AdultControlId],
                -(metrics.Target + metrics.Gap), metrics.Target);
            ClosingRouteViewFactory.SetControlTop(_controls[PauseControlId], 0f, metrics.Target);
            ClosingRouteViewFactory.SetControlTop(_controls[HomeControlId],
                metrics.Target + metrics.Gap, metrics.Target);
            var childSize = Mathf.Min(520f, (metrics.BodyTop - metrics.BodyBottom) * 0.82f);
            var childCenter = new Vector2(0f, (metrics.BodyBottom + metrics.BodyTop - safeHeight) * 0.5f);
            ClosingRouteViewFactory.SetRect(_selectedChild.rectTransform, childCenter, new Vector2(childSize, childSize));
            SelectedChildRect = ClosingRouteViewFactory.CenteredCanvasRect(
                safeWidth * 0.5f, safeHeight * 0.5f + childCenter.y, childSize, childSize);
            ClosingRouteViewFactory.SetRect(_visualEquivalent.rectTransform,
                new Vector2(0f, metrics.BodyBottom - safeHeight * 0.5f + 78f), new Vector2(360f, 180f));
            ClosingRouteViewFactory.SetRect(_captionBackplate.rectTransform,
                new Vector2(0f, metrics.BodyBottom - safeHeight * 0.5f + 92f),
                new Vector2(Mathf.Min(1100f, safeWidth * 0.7f), 184f));
            return true;
        }

        public void Present(CompletionSnapshot snapshot)
        {
            SetInputReady(snapshot.State == CompletionState.Ready);
        }

        public void SetInputReady(bool ready)
        {
            _ready = ready;
            UpdateControls();
        }

        public void PresentCompletionMotion(CompletionMotionFrame frame)
        {
            var color = _selectedChild.color;
            color.a = Mathf.Clamp01((float)frame.Opacity);
            _selectedChild.color = color;
            _selectedChild.rectTransform.localScale = Vector3.one;
        }

        public void PresentCompletionMedia(CompletionMediaCue cue)
        {
            _visualEquivalent.sprite = cue.VisualEquivalent;
            _visualEquivalent.gameObject.SetActive(true);
            _caption.text = cue.CaptionText;
            var captionVisible = cue.CaptionVisible && !_captionSuppressed;
            _captionBackplate.gameObject.SetActive(captionVisible);
            _caption.gameObject.SetActive(captionVisible);
        }

        public void FreezeCompletionMedia(double elapsedSeconds) { }
        public void ResumeCompletionMedia(Guid mediaTokenId, double elapsedSeconds) { }

        public void SetCompletionInputLocked(bool locked)
        {
            _mediaLocked = locked;
            UpdateControls();
        }

        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionSuppressed = suppressed;
            if (suppressed)
            {
                _captionBackplate.gameObject.SetActive(false);
                _caption.gameObject.SetActive(false);
            }
        }

        public bool AreTouchTargetsTransparent() => ClosingRouteViewFactory.TargetsTransparent(_controls.Values);
        public bool VisibleArtRaycastsDisabled() => ClosingRouteViewFactory.AllGraphicsInert(_canvas);

        private void Build()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("CompletionCanvas", transform, 180);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform, _content.CompletionBackgrounds[0],
                _content.CompletionBackgrounds[1], _content.CompletionBackgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            _selectedChild = JourneyViewFactory.CreateImage(
                "SelectedChild.VisibleArt", _safeRoot, Color.white, _content.CompletionAvatars[0]);
            _selectedChild.preserveAspect = true;
            _visualEquivalent = JourneyViewFactory.CreateImage(
                "CompletionVisualEquivalent", _safeRoot, Color.white, _content.CompletionVisualEquivalent);
            _visualEquivalent.preserveAspect = true;
            _captionBackplate = JourneyViewFactory.CreateImage(
                "CaptionBackplate", _safeRoot, Color.white, _content.Frames[0]);
            _captionBackplate.type = Image.Type.Sliced;
            _caption = JourneyViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _caption.fontSize = 46f;
            _captionBackplate.gameObject.SetActive(false);
            AddControl(AdultControlId, "Adult", _content.AdultHold);
            AddControl(PauseControlId, "Pause", JourneyOverviewContent.LoadApproved().OverviewControls[2]);
            AddControl(HomeControlId, "Home", _content.Home);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void UpdateControls()
        {
            foreach (var control in _controls.Values) control.Semantic.Enabled = _ready && !_mediaLocked;
        }
    }

    internal static class ClosingRouteViewFactory
    {
        public static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        public static void SetControlTop(JourneyControlView control, float x, float target)
        {
            JourneyViewFactory.SetTop(control, new Vector2(x, -target * 0.5f), target);
            control.CanvasRect = CenteredCanvasRect(x, -target * 0.5f, target, target);
        }

        public static void SetControlBottom(JourneyControlView control, float x, float target)
        {
            JourneyViewFactory.SetBottom(control, new Vector2(x, target * 0.5f), target);
            control.CanvasRect = CenteredCanvasRect(x, target * 0.5f, target, target);
        }

        public static Rect CenteredCanvasRect(float x, float y, float width, float height)
            => new Rect(x - width * 0.5f, y - height * 0.5f, width, height);

        public static bool TargetsTransparent(IEnumerable<JourneyControlView> controls)
        {
            foreach (var control in controls)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            return true;
        }

        public static bool AllGraphicsInert(GameObject root)
        {
            if (root == null) return false;
            foreach (var graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget) return false;
            return true;
        }
    }
}
