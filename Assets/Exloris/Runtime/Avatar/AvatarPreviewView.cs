using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Avatar
{
    [DisallowMultipleComponent]
    public sealed class AvatarPreviewView : MonoBehaviour
    {
        public const string ConfirmControlId = "avatar.preview.confirm";
        public const string OtherControlId = "avatar.preview.other";
        public const string RepeatControlId = "avatar.preview.repeat";
        public const string HomeControlId = "avatar.preview.home";

        private readonly Dictionary<string, AvatarControlView> _controls =
            new Dictionary<string, AvatarControlView>();
        private readonly List<string> _controlIds = new List<string>();
        private ChildInputRouter _router;
        private AvatarContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _avatar;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private Image _visualEquivalent;
        private AvatarGeometry _geometry;
        private Vector2 _safeSize;
        private bool _captionRequestedVisible;
        private bool _captionSuppressed;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public bool ControlsEnabled => _controls.Count > 0 && AllControlsEnabled();
        public long PreviewToken { get; private set; }
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeInHierarchy;
        public bool CaptionBackplateVisible => _captionBackplate != null
            && _captionBackplate.gameObject.activeInHierarchy;
        public bool VisualEquivalentVisible =>
            _visualEquivalent != null && _visualEquivalent.gameObject.activeInHierarchy;

        public void Configure(ChildInputRouter router, AvatarContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            BuildSurface();
            ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
        }

        public void Show(AvatarChoice choice, long previewToken)
        {
            if (previewToken <= 0) throw new ArgumentOutOfRangeException(nameof(previewToken));
            _avatar.sprite = choice == AvatarChoice.Boy
                ? _content.BoyMotionSprites[0]
                : _content.GirlMotionSprites[0];
            PreviewToken = previewToken;
            _canvas.SetActive(true);
            SetControlsEnabled(true);
            _router.SetVisibleStateReady(true);
        }

        public void SetControlsEnabled(bool enabled)
        {
            foreach (var control in _controls.Values) control.Semantic.Enabled = enabled;
        }

        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.SetActive(visible);
            SetControlsEnabled(visible);
        }

        public void ApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var metrics = AvatarLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (metrics.PreviewSize.x > safeWidth
                || metrics.PreviewSize.y + 2f * (metrics.Target + metrics.Gap) > safeHeight)
                throw new InvalidOperationException("Safe area cannot contain the frozen Avatar Preview layout.");
            _safeSize = new Vector2(safeWidth, safeHeight);
            _background.Present(screenWidth, screenHeight);
            var center = new Vector2(safeWidth * 0.5f, metrics.PreviewCenterY);
            var target = CenteredRect(center.x, center.y, metrics.PreviewSize.x, metrics.PreviewSize.y);
            var art = AvatarViewFactory.Inset(target, 0.10f);
            AvatarViewFactory.SetCentered(_avatar.rectTransform,
                new Vector2(0f, center.y - safeHeight * 0.5f), art.size);
            _avatar.rectTransform.pivot = new Vector2(0.5f, 0.08f);
            _geometry = new AvatarGeometry(target, target, art, new Vector2(0.5f, 0.08f),
                art.yMin, Vector3.one, Color.white, 0f, 0f, 4d);

            LayoutRow(_controls[RepeatControlId], 0, 2, true, metrics, safeWidth, safeHeight);
            LayoutRow(_controls[HomeControlId], 1, 2, true, metrics, safeWidth, safeHeight);
            LayoutRow(_controls[ConfirmControlId], 0, 2, false, metrics, safeWidth, safeHeight);
            LayoutRow(_controls[OtherControlId], 1, 2, false, metrics, safeWidth, safeHeight);
            LayoutCaption(metrics, safeWidth, safeHeight);
        }

        public AvatarGeometry GetAvatarGeometry() => _geometry;
        public Sprite GetAvatarSprite() => _avatar.sprite;
        public void SetAvatarSprite(Sprite sprite)
        {
            _avatar.sprite = sprite != null ? sprite : throw new ArgumentNullException(nameof(sprite));
        }
        public void ShowMediaCue(Sprite visual, string captionId, bool captionsVisible)
        {
            _visualEquivalent.sprite = visual;
            _visualEquivalent.gameObject.SetActive(true);
            UpdateCaption(captionId, captionsVisible);
        }
        public void UpdateCaption(string captionId, bool visible)
        {
            _caption.text = AvatarCaptionStrings.ResolveEnglish(captionId);
            _captionRequestedVisible = visible;
            ApplyCaptionVisibility();
        }
        public void SetCaptionSuppressed(bool suppressed)
        {
            _captionSuppressed = suppressed;
            ApplyCaptionVisibility();
        }
        public Sprite GetVisibleSprite(string controlId) => _controls[controlId].VisibleArt.sprite;
        public Rect GetControlScreenBounds(string controlId) =>
            AvatarViewFactory.ToScreenRect(_controls[controlId].Target);
        public Rect GetControlCanvasRect(string controlId) => _controls[controlId].CanvasRect;
        public bool DoTouchTargetsOverlap() => AvatarViewFactory.TargetsOverlap(_controls.Values);
        public bool AreAllTargetsInsideSafeArea() => AvatarViewFactory.Inside(_controls.Values, _safeSize);

        public bool AreTouchTargetsTransparent()
        {
            foreach (var control in _controls.Values)
                if (control.TargetImage.color.a != 0f || control.TargetImage.raycastTarget) return false;
            return true;
        }

        public bool VisibleArtRaycastsDisabled()
        {
            if (_avatar != null && _avatar.raycastTarget) return false;
            foreach (var control in _controls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            return true;
        }

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = AvatarViewFactory.CreateCanvas("AvatarPreviewCanvas", transform, 130);
            _background = AvatarViewFactory.CreateBackground(
                _canvas.transform, _content.Backgrounds[0], _content.Backgrounds[1], _content.Backgrounds[2]);
            _safeRoot = AvatarViewFactory.CreateSafeRoot(_canvas);
            _avatar = AvatarViewFactory.CreateImage(
                "SelectedAvatar.VisibleArt", _safeRoot, Color.white, _content.BoyMotionSprites[0]);
            _avatar.preserveAspect = true;
            AddControl(RepeatControlId, "Repeat", _content.UiSprites[3]);
            AddControl(HomeControlId, "Home", _content.UiSprites[2]);
            AddControl(ConfirmControlId, "Confirm", _content.UiSprites[1]);
            AddControl(OtherControlId, "ChooseOther", _content.UiSprites[4]);
            _captionBackplate = AvatarViewFactory.CreateImage(
                "CaptionBackplate", _safeRoot, Color.white, _content.UiSprites[7]);
            _captionBackplate.type = Image.Type.Sliced;
            _caption = AvatarViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _visualEquivalent = AvatarViewFactory.CreateImage(
                "NonTextVisualEquivalent", _safeRoot, Color.white, _content.VisualEquivalents[1]);
            _visualEquivalent.preserveAspect = true;
            _visualEquivalent.gameObject.SetActive(false);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = AvatarViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private static void LayoutRow(
            AvatarControlView control, int index, int count, bool top,
            AvatarLayoutMetrics metrics, float safeWidth, float safeHeight)
        {
            var centerX = safeWidth * 0.5f + (index - (count - 1) * 0.5f) * (metrics.Target + metrics.Gap);
            var centerY = top ? safeHeight - metrics.Target * 0.5f : metrics.Target * 0.5f;
            var localX = centerX - safeWidth * 0.5f;
            if (top) AvatarViewFactory.SetTop(control, new Vector2(localX, -metrics.Target * 0.5f), metrics.Target);
            else AvatarViewFactory.SetBottom(control, new Vector2(localX, metrics.Target * 0.5f), metrics.Target);
            control.CanvasRect = CenteredRect(centerX, centerY, metrics.Target, metrics.Target);
        }

        private void LayoutCaption(AvatarLayoutMetrics metrics, float safeWidth, float safeHeight)
        {
            var bodyBottom = metrics.Target + metrics.Gap;
            var bodyTop = safeHeight - metrics.Target - metrics.Gap;
            var height = Mathf.Min(0.16f * safeHeight, 0.25f * Mathf.Max(0f, bodyTop - bodyBottom));
            var width = Mathf.Min(0.72f * safeWidth, 1200f);
            AvatarViewFactory.SetCentered(_captionBackplate.rectTransform,
                new Vector2(0f, bodyTop - height * 0.5f - safeHeight * 0.5f),
                new Vector2(width, height));
            AvatarViewFactory.SetCentered(_visualEquivalent.rectTransform,
                new Vector2(0f, -safeHeight * 0.18f),
                new Vector2(Mathf.Min(safeWidth * 0.28f, 520f), Mathf.Min(safeHeight * 0.16f, 180f)));
        }

        private bool AllControlsEnabled()
        {
            foreach (var control in _controls.Values)
                if (!control.Semantic.Enabled) return false;
            return true;
        }

        private void ApplyCaptionVisibility()
        {
            var visible = _captionRequestedVisible && !_captionSuppressed;
            _captionBackplate.gameObject.SetActive(visible);
            _caption.gameObject.SetActive(visible);
        }

        private static Rect CenteredRect(float x, float y, float width, float height) =>
            new Rect(x - width * 0.5f, y - height * 0.5f, width, height);
    }
}
