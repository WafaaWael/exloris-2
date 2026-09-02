using System;
using System.Collections.Generic;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using Exloris.Runtime.Layout;
using Exloris.Runtime.Overview;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Exloris.Runtime.Adult
{
    [DisallowMultipleComponent]
    public sealed class AdultGateView : MonoBehaviour, IAdultGateView
    {
        public const string HoldControlId = "adult_gate.hold";
        public const string ConfirmControlId = "adult_gate.confirm";
        public const string CancelControlId = "adult_gate.cancel";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private ChildInputRouter _router;
        private CompletionAdultReportContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private Image _progress;
        private TextMeshProUGUI _instruction;
        private AdultGateState _state;
        private bool _ready;

        public bool ConfirmFocusable => _controls.TryGetValue(ConfirmControlId, out var control)
            && control.Semantic.Enabled;
        public IReadOnlyCollection<string> ControlIds => _controls.Keys;
        public bool ProgressGeometryAligned => _progress != null
            && _controls.TryGetValue(HoldControlId, out var hold)
            && _progress.rectTransform.anchoredPosition == hold.VisibleArt.rectTransform.anchoredPosition
            && _progress.rectTransform.sizeDelta == hold.VisibleArt.rectTransform.sizeDelta;
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public Rect GetControlScreenBounds(string controlId) =>
            JourneyViewFactory.ToScreenRect(_controls[controlId].Target);

        public void Configure(ChildInputRouter router, CompletionAdultReportContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Build();
            if (!TryApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88))
                throw new InvalidOperationException("Default Adult Gate layout is infeasible.");
            UpdateControls();
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels)
        {
            var metrics = CompletionLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi, platformMinimumPixels);
            if (!metrics.Fits(safeWidth, safeHeight)) return false;
            _background.Present(screenWidth, screenHeight);
            var holdSize = Mathf.Min(360f, metrics.ReportViewportHeight * 0.58f);
            var compactHeight = screenHeight < holdSize + metrics.Target + metrics.Gap;
            if (compactHeight) holdSize = metrics.Target;
            var compactOffset = compactHeight ? (metrics.Target + metrics.Gap) * 0.5f : 0f;
            ClosingRouteViewFactory.SetRect(_controls[HoldControlId].Target,
                new Vector2(compactOffset, 40f), new Vector2(holdSize, holdSize));
            ClosingRouteViewFactory.SetRect(_controls[HoldControlId].VisibleArt.rectTransform,
                Vector2.zero, new Vector2(holdSize * 0.78f, holdSize * 0.78f));
            ClosingRouteViewFactory.SetRect(_progress.rectTransform,
                Vector2.zero, _controls[HoldControlId].VisibleArt.rectTransform.sizeDelta);
            ClosingRouteViewFactory.SetControlBottom(_controls[CancelControlId],
                -(metrics.Target + metrics.Gap) * 0.5f, metrics.Target);
            ClosingRouteViewFactory.SetControlBottom(_controls[ConfirmControlId],
                (metrics.Target + metrics.Gap) * 0.5f, metrics.Target);
            ClosingRouteViewFactory.SetRect(_instruction.rectTransform,
                new Vector2(0f, holdSize * 0.72f), new Vector2(Mathf.Min(1100f, safeWidth * 0.72f), 130f));
            return true;
        }

        public void Present(AdultGateSnapshot snapshot)
        {
            _state = snapshot.State;
            var index = Mathf.Clamp(Mathf.RoundToInt((float)snapshot.HoldProgress * 4f), 0, 4);
            _progress.sprite = _content.AdultProgress[index];
            var reset = snapshot.State == AdultGateState.ResetConfirmation;
            _instruction.text = reset
                ? "Restart this adventure?"
                : snapshot.State == AdultGateState.AwaitingConfirm
                    ? "Confirm adult access"
                    : "Grown-ups: hold for 3 seconds";
            _controls[CancelControlId].VisibleArt.sprite = reset ? _content.Stay : _content.Back;
            _controls[ConfirmControlId].VisibleArt.sprite = _content.Confirm;
            UpdateControls();
        }

        public void SetInputReady(bool ready)
        {
            _ready = ready;
            UpdateControls();
        }

        public bool VisibleArtRaycastsDisabled() => ClosingRouteViewFactory.AllGraphicsInert(_canvas);
        public bool AreTouchTargetsTransparent() => ClosingRouteViewFactory.TargetsTransparent(_controls.Values);

        public void SetVisible(bool visible)
        {
            if (_canvas != null) _canvas.SetActive(visible);
        }

        private void Build()
        {
            if (_canvas != null) return;
            _canvas = JourneyViewFactory.CreateCanvas("AdultGateCanvas", transform, 190);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform, _content.ParentBackgrounds[0],
                _content.ParentBackgrounds[1], _content.ParentBackgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            AddControl(HoldControlId, "Hold", _content.AdultHold);
            _progress = JourneyViewFactory.CreateImage(
                "Hold.Progress.VisibleArt", _controls[HoldControlId].VisibleArt.transform,
                Color.white, _content.AdultProgress[0]);
            _progress.preserveAspect = true;
            AddControl(ConfirmControlId, "Confirm", _content.Confirm);
            AddControl(CancelControlId, "Cancel", _content.Back);
            _instruction = JourneyViewFactory.CreateRuntimeText("Instruction", _safeRoot);
            _instruction.fontSize = 48f;
        }

        private void AddControl(string id, string name, Sprite sprite)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id);
            _controls.Add(id, control);
            _router.Register(control.Semantic);
        }

        private void UpdateControls()
        {
            if (_controls.Count == 0) return;
            _controls[HoldControlId].Semantic.Enabled = _ready && _state == AdultGateState.Holding;
            _controls[ConfirmControlId].Semantic.Enabled = _ready
                && (_state == AdultGateState.AwaitingConfirm || _state == AdultGateState.ResetConfirmation);
            _controls[CancelControlId].Semantic.Enabled = _ready
                && (_state == AdultGateState.Holding
                    || _state == AdultGateState.AwaitingConfirm
                    || _state == AdultGateState.ResetConfirmation);
            _controls[ConfirmControlId].VisibleArt.gameObject.SetActive(
                _state == AdultGateState.AwaitingConfirm || _state == AdultGateState.ResetConfirmation);
        }
    }
}
