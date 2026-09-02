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
    public sealed class AvatarSelectView : MonoBehaviour
    {
        public const string BoyControlId = "avatar.select.boy";
        public const string GirlControlId = "avatar.select.girl";
        public const string HomeControlId = "avatar.select.home";

        private readonly Dictionary<string, AvatarControlView> _controls =
            new Dictionary<string, AvatarControlView>();
        private readonly List<string> _controlIds = new List<string>();
        private readonly Dictionary<AvatarChoice, AvatarCard> _cards =
            new Dictionary<AvatarChoice, AvatarCard>();
        private ChildInputRouter _router;
        private AvatarContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _captionBackplate;
        private TextMeshProUGUI _caption;
        private Image _visualEquivalent;
        private Vector2 _safeSize;
        private AvatarChoice _leftChoice = AvatarChoice.Boy;
        private AvatarChoice _rightChoice = AvatarChoice.Girl;
        private AvatarLayoutMetrics _metrics;
        private bool _captionRequestedVisible;
        private bool _captionSuppressed;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public bool ControlsEnabled => _controls.Count > 0 && AllControlsEnabled();
        public Sprite CaptionBackplateSprite => _captionBackplate == null ? null : _captionBackplate.sprite;
        public bool CaptionBackplateOpaque => _captionBackplate != null && _captionBackplate.color.a >= 1f;
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public bool CaptionVisible => _caption != null && _caption.gameObject.activeInHierarchy;
        public bool CaptionBackplateVisible => _captionBackplate != null
            && _captionBackplate.gameObject.activeInHierarchy;
        public bool VisualEquivalentVisible =>
            _visualEquivalent != null && _visualEquivalent.gameObject.activeInHierarchy;
        public AvatarChoice LeftChoice => _leftChoice;
        public AvatarChoice RightChoice => _rightChoice;

        public void Configure(ChildInputRouter router, AvatarContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            BuildSurface();
            ApplyResponsiveLayout(1334f, 750f, 1856f, 1016f, 326f, 88);
        }

        public void Show(AvatarChoice left, AvatarChoice right)
        {
            if (left == right) throw new ArgumentException("Avatar choices must be different.");
            _leftChoice = left;
            _rightChoice = right;
            _cards[AvatarChoice.Boy].Avatar.sprite = _content.BoyMotionSprites[0];
            _cards[AvatarChoice.Girl].Avatar.sprite = _content.GirlMotionSprites[0];
            LayoutCards();
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
            if (!TryApplyResponsiveLayout(
                    screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels))
                throw new InvalidOperationException("Safe area cannot contain the frozen Avatar Select layout.");
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var candidate = AvatarLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            var candidateGap = Mathf.Max(candidate.Gap, candidate.CardSize.x * 0.25f);
            if (candidate.CardSize.x * 2f + candidateGap > safeWidth
                || candidate.CardSize.y + 2f * (candidate.Target + candidate.Gap) > safeHeight)
                return false;

            _metrics = candidate;
            _safeSize = new Vector2(safeWidth, safeHeight);
            _background.Present(screenWidth, screenHeight);
            LayoutCards();

            var home = _controls[HomeControlId];
            AvatarViewFactory.SetTop(home, new Vector2(0f, -_metrics.Target * 0.5f), _metrics.Target);
            home.CanvasRect = CenteredRect(safeWidth * 0.5f, safeHeight - _metrics.Target * 0.5f,
                _metrics.Target, _metrics.Target);
            LayoutCaption(_metrics, safeWidth, safeHeight);
            return true;
        }

        public AvatarGeometry GetAvatarGeometry(AvatarChoice choice)
        {
            var card = _cards[choice];
            var target = card.Control.CanvasRect;
            var frame = AvatarViewFactory.Inset(target, 0.06f);
            var art = AvatarViewFactory.Inset(frame, 0.04f);
            return new AvatarGeometry(target, frame, art, new Vector2(0.5f, 0.08f), art.yMin,
                Vector3.one, Color.white, 0f, 0f, 24d);
        }

        public Sprite GetCardFrameSprite(AvatarChoice choice) => _cards[choice].Frame.sprite;
        public Sprite GetAvatarSprite(AvatarChoice choice) => _cards[choice].Avatar.sprite;
        public void SetAvatarSprite(AvatarChoice choice, Sprite sprite)
        {
            if (sprite == null) throw new ArgumentNullException(nameof(sprite));
            _cards[choice].Avatar.sprite = sprite;
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
            foreach (var control in _controls.Values)
                if (control.VisibleArt.raycastTarget) return false;
            foreach (var card in _cards.Values)
                if (card.Frame.raycastTarget || card.Avatar.raycastTarget) return false;
            return true;
        }

        private void BuildSurface()
        {
            if (_canvas != null) return;
            _canvas = AvatarViewFactory.CreateCanvas("AvatarSelectCanvas", transform, 130);
            _background = AvatarViewFactory.CreateBackground(
                _canvas.transform, _content.Backgrounds[0], _content.Backgrounds[1], _content.Backgrounds[2]);
            _safeRoot = AvatarViewFactory.CreateSafeRoot(_canvas);
            AddCard(AvatarChoice.Boy);
            AddCard(AvatarChoice.Girl);
            AddControl(HomeControlId, "Home", _content.UiSprites[2]);
            _captionBackplate = AvatarViewFactory.CreateImage(
                "CaptionBackplate", _safeRoot, Color.white, _content.UiSprites[7]);
            _captionBackplate.type = Image.Type.Sliced;
            _caption = AvatarViewFactory.CreateRuntimeText("Caption", _captionBackplate.transform);
            _caption.text = string.Empty;
            _visualEquivalent = AvatarViewFactory.CreateImage(
                "NonTextVisualEquivalent", _safeRoot, Color.white, _content.VisualEquivalents[0]);
            _visualEquivalent.preserveAspect = true;
            _visualEquivalent.gameObject.SetActive(false);
        }

        private void AddCard(AvatarChoice choice)
        {
            var id = choice == AvatarChoice.Boy ? BoyControlId : GirlControlId;
            var control = AvatarViewFactory.CreateControl("AvatarCard" + choice, _safeRoot, null, id);
            var frame = control.VisibleArt;
            frame.name = "CardFrame.VisibleArt";
            frame.sprite = _content.UiSprites[0];
            var avatar = AvatarViewFactory.CreateImage("Avatar.VisibleArt", frame.transform, Color.white, null);
            avatar.preserveAspect = true;
            var card = new AvatarCard(control, frame, avatar);
            _cards.Add(choice, card);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutCards()
        {
            if (_safeSize.x <= 0f) return;
            LayoutCard(_cards[_leftChoice], 0);
            LayoutCard(_cards[_rightChoice], 1);
        }

        private void LayoutCard(AvatarCard card, int slot)
        {
            var cardGap = Mathf.Max(_metrics.Gap, _metrics.CardSize.x * 0.25f);
            var centerX = _safeSize.x * 0.5f + (slot - 0.5f) * (_metrics.CardSize.x + cardGap);
            var position = new Vector2(centerX - _safeSize.x * 0.5f,
                _metrics.CardCenterY - _safeSize.y * 0.5f);
            AvatarViewFactory.SetCentered(card.Control.Target, position, _metrics.CardSize);
            AvatarViewFactory.SetCentered(card.Frame.rectTransform, Vector2.zero, _metrics.CardSize * 0.88f);
            AvatarViewFactory.SetAvatarArt(card.Avatar.rectTransform,
                new Vector2(_metrics.CardSize.x * 0.80f, _metrics.CardSize.y * 0.80f));
            card.Control.CanvasRect = CenteredRect(centerX, _metrics.CardCenterY,
                _metrics.CardSize.x, _metrics.CardSize.y);
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = AvatarViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
            _router.Register(control.Semantic);
        }

        private void LayoutCaption(AvatarLayoutMetrics metrics, float safeWidth, float safeHeight)
        {
            var bodyBottom = metrics.Target + metrics.Gap;
            var bodyTop = safeHeight - metrics.Target - metrics.Gap;
            var bodyHeight = Mathf.Max(0f, bodyTop - bodyBottom);
            var height = Mathf.Min(0.16f * safeHeight, 0.25f * bodyHeight);
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

        private sealed class AvatarCard
        {
            public AvatarCard(AvatarControlView control, Image frame, Image avatar)
            {
                Control = control;
                Frame = frame;
                Avatar = avatar;
            }

            public AvatarControlView Control { get; }
            public Image Frame { get; }
            public Image Avatar { get; }
        }
    }
}
