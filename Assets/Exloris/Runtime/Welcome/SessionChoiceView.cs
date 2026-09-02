using System;
using System.Collections.Generic;
using Exloris.Runtime.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Welcome
{
    [DisallowMultipleComponent]
    public sealed class SessionChoiceView : MonoBehaviour
    {
        public const string ContinueControlId = "session_choice.continue";
        public const string RestartControlId = "session_choice.restart";
        public const string BackControlId = "session_choice.back";

        private readonly Dictionary<string, WelcomeControlView> _controls = new Dictionary<string, WelcomeControlView>();
        private readonly List<string> _controlIds = new List<string>();
        private ChildInputRouter _inputRouter;
        private GameObject _canvasObject;
        private RectTransform _safeRoot;
        private Image _captionBand;
        private TextMeshProUGUI _captionText;
        private Image _visualEquivalent;

        public IReadOnlyList<string> ControlIds => _controlIds;
        public float CurrentTargetSize { get; private set; }
        public float CurrentGap { get; private set; }
        public Vector2 CurrentSafeSize { get; private set; }
        public bool IsVisible => _canvasObject != null && _canvasObject.activeSelf;
        public bool VisualEquivalentVisible => _visualEquivalent != null && _visualEquivalent.gameObject.activeSelf;
        public bool CaptionVisible => _captionBand != null && _captionBand.gameObject.activeSelf;
        public string CurrentCaptionId { get; private set; }
        public string CurrentCaptionText => _captionText == null ? string.Empty : _captionText.text;
        public Sprite CurrentVisualSprite => _visualEquivalent == null ? null : _visualEquivalent.sprite;
        public bool MediaCuePaused { get; private set; }

        public void Configure(ChildInputRouter inputRouter, WelcomeContent content)
        {
            _inputRouter = inputRouter ?? throw new ArgumentNullException(nameof(inputRouter));
            if (content == null) throw new ArgumentNullException(nameof(content));
            BuildSurface(content);
            Hide();
        }

        public void Show()
        {
            if (_canvasObject == null) throw new InvalidOperationException("Session Choice view is not configured.");
            _canvasObject.SetActive(true);
            SetControlsEnabled(true);
            _inputRouter.SetVisibleStateReady(true);
        }

        public void Hide()
        {
            if (_canvasObject == null) return;
            SetControlsEnabled(false);
            _canvasObject.SetActive(false);
        }

        public Sprite GetVisibleSprite(string controlId)
        {
            if (!_controls.TryGetValue(controlId, out var control)) throw new KeyNotFoundException(controlId);
            return control.VisibleArt.sprite;
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

        public void ShowMediaCue(Sprite visual, string captionId, bool captionsVisible, double startedAtSeconds)
        {
            _visualEquivalent.sprite = visual;
            _visualEquivalent.gameObject.SetActive(true);
            UpdateCaption(captionId, captionsVisible, startedAtSeconds);
        }

        public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
        {
            CurrentCaptionId = captionId;
            _captionText.text = WelcomeCaptionStrings.ResolveEnglish(captionId);
            _captionBand.gameObject.SetActive(visible);
            _captionText.gameObject.SetActive(visible);
        }

        public void PauseMediaCue()
        {
            MediaCuePaused = true;
        }

        public void ResumeMediaCue()
        {
            MediaCuePaused = false;
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
            for (var index = 0; index < _controlIds.Count; index++)
            {
                var xOffset = (index - 1f) * (metrics.Target + metrics.Gap);
                var control = _controls[_controlIds[index]];
                WelcomeViewFactory.SetBottomControl(
                    control,
                    new Vector2(xOffset, metrics.Target * 0.5f),
                    new Vector2(metrics.Target, metrics.Target),
                    0.72f,
                    0.72f);
                control.CanvasRect = new Rect(
                    safeWidth * 0.5f + xOffset - metrics.Target * 0.5f,
                    0f,
                    metrics.Target,
                    metrics.Target);
            }

            var bodyBottom = metrics.Target + metrics.Gap;
            var bodyTop = safeHeight - metrics.Target - metrics.Gap;
            var bodyHeight = bodyTop - bodyBottom;
            var captionHeight = Mathf.Min(0.16f * safeHeight, 0.25f * bodyHeight);
            var captionWidth = Mathf.Min(0.72f * safeWidth, 1200f);
            var captionCenterY = bodyTop - captionHeight * 0.5f;
            WelcomeViewFactory.SetCentered(
                _captionBand.rectTransform,
                new Vector2(0f, captionCenterY - safeHeight * 0.5f),
                new Vector2(captionWidth, captionHeight));
            var captionGap = Mathf.Min(metrics.Gap, 0.05f * safeHeight);
            var interactiveTop = bodyTop - captionHeight - captionGap;
            var cueHeight = Mathf.Min(0.35f * safeHeight, interactiveTop - bodyBottom);
            var cueWidth = Mathf.Min(0.60f * safeWidth, cueHeight * 2f);
            var cueCenterY = bodyBottom + (interactiveTop - bodyBottom) * 0.5f;
            WelcomeViewFactory.SetCentered(
                _visualEquivalent.rectTransform,
                new Vector2(0f, cueCenterY - safeHeight * 0.5f),
                new Vector2(cueWidth, cueHeight));
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

        public Rect GetControlScreenBounds(string controlId)
        {
            if (!_controls.TryGetValue(controlId, out var control)) throw new KeyNotFoundException(controlId);
            return WelcomeViewFactory.ToScreenRect(control.Target);
        }

        private void BuildSurface(WelcomeContent content)
        {
            if (_canvasObject != null) return;
            _canvasObject = WelcomeViewFactory.CreateCanvas("SessionChoiceCanvas", transform, 120);
            _safeRoot = WelcomeViewFactory.CreateSafeRoot(_canvasObject);
            _captionBand = WelcomeViewFactory.CreatePresentationImage(
                "CaptionBand",
                _safeRoot,
                content.CaptionBackplate);
            _captionText = WelcomeViewFactory.CreateRuntimeText("Caption", _captionBand.transform);
            _captionBand.gameObject.SetActive(false);
            _visualEquivalent = WelcomeViewFactory.CreatePresentationImage(
                "NonTextVisualEquivalent",
                _safeRoot,
                content.SessionChoiceVisualEquivalent);
            _visualEquivalent.preserveAspect = true;
            _visualEquivalent.gameObject.SetActive(false);
            AddControl(ContinueControlId, "Continue", content.Continue);
            AddControl(RestartControlId, "Restart", content.Restart);
            AddControl(BackControlId, "Back", content.Back);
            ApplyDefaultLayout();
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = WelcomeViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _controlIds.Add(id);
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
    }
}
