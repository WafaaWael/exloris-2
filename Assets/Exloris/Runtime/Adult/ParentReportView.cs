using System;
using System.Collections.Generic;
using Exloris.Core.Session;
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
    public sealed class ParentReportView : MonoBehaviour, IParentReportView
    {
        public const string CloseControlId = "parent_report.close";
        public const string RestartControlId = "parent_report.restart";
        public const string ScrollControlId = "parent_report.scroll";

        private readonly Dictionary<string, JourneyControlView> _controls =
            new Dictionary<string, JourneyControlView>(StringComparer.Ordinal);
        private readonly string[] _focusOrder =
            { CloseControlId, RestartControlId, ScrollControlId };
        private ChildInputRouter _router;
        private CompletionAdultReportContent _content;
        private GameObject _canvas;
        private RectTransform _safeRoot;
        private ResponsiveBackgroundPresenter _background;
        private ScrollRect _scroll;
        private RectTransform _viewport;
        private RectTransform _cardRoot;
        private CompletionLayoutMetrics _metrics;
        private bool _ready;

        public IReadOnlyList<string> FocusOrder => _focusOrder;
        public IReadOnlyCollection<string> ControlIds => _controls.Keys;
        public int RenderedCardCount { get; private set; }
        public bool ScrollReachable => _cardRoot != null && _viewport != null
            && _cardRoot.rect.height > _viewport.rect.height;
        public ResponsiveBackgroundVariant BackgroundVariant => _background.CurrentVariant;
        public Rect GetControlScreenBounds(string controlId) =>
            JourneyViewFactory.ToScreenRect(_controls[controlId].Target);

        public void Configure(ChildInputRouter router, CompletionAdultReportContent content)
        {
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Build();
            if (!TryApplyResponsiveLayout(1920f, 1080f, 1856f, 1016f, 326f, 88, 1f, 1f))
                throw new InvalidOperationException("Default Parent Report layout is infeasible.");
            UpdateControls();
        }

        public bool TryApplyResponsiveLayout(
            float screenWidth, float screenHeight, float safeWidth, float safeHeight,
            float ppi, int platformMinimumPixels, float textScale, float pseudoExpansion)
        {
            var metrics = CompletionLayoutMetrics.Calculate(
                screenWidth, screenHeight, safeWidth, safeHeight, ppi,
                platformMinimumPixels, textScale, pseudoExpansion);
            if (!metrics.Fits(safeWidth, safeHeight)) return false;
            _metrics = metrics;
            _background.Present(screenWidth, screenHeight);
            ClosingRouteViewFactory.SetControlTop(_controls[CloseControlId],
                -(metrics.Target + metrics.Gap) * 0.5f, metrics.Target);
            ClosingRouteViewFactory.SetControlTop(_controls[RestartControlId],
                (metrics.Target + metrics.Gap) * 0.5f, metrics.Target);
            var viewportCenterY = (metrics.BodyBottom + metrics.BodyTop - safeHeight) * 0.5f;
            ClosingRouteViewFactory.SetRect(_viewport,
                new Vector2(0f, viewportCenterY),
                new Vector2(metrics.ReportContentWidth, metrics.ReportViewportHeight));
            ClosingRouteViewFactory.SetRect(_controls[ScrollControlId].Target,
                _viewport.anchoredPosition, _viewport.sizeDelta);
            ClosingRouteViewFactory.SetRect(_controls[ScrollControlId].VisibleArt.rectTransform,
                Vector2.zero, _viewport.sizeDelta);
            RelayoutCards();
            return true;
        }

        public void Present(ParentReportSnapshot snapshot)
        {
            RebuildCards(snapshot.Cards ?? Array.Empty<ParentReportCard>());
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(snapshot.ScrollNormalized);
            SetInputReady(snapshot.State == ParentReportState.Ready);
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
            _canvas = JourneyViewFactory.CreateCanvas("ParentReportCanvas", transform, 200);
            _background = JourneyViewFactory.CreateBackground(
                _canvas.transform, _content.ParentBackgrounds[0],
                _content.ParentBackgrounds[1], _content.ParentBackgrounds[2]);
            _safeRoot = JourneyViewFactory.CreateSafeRoot(_canvas);
            AddControl(CloseControlId, "Close", _content.Close);
            AddControl(RestartControlId, "Restart", _content.Restart);
            AddControl(ScrollControlId, "ReportScroll", null, true);

            var scrollOwner = new GameObject("ReportScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollOwner.transform.SetParent(_safeRoot, false);
            _scroll = scrollOwner.GetComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.vertical = true;
            _scroll.inertia = false;
            _viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D))
                .GetComponent<RectTransform>();
            _viewport.SetParent(scrollOwner.transform, false);
            _cardRoot = new GameObject("Cards", typeof(RectTransform), typeof(VerticalLayoutGroup))
                .GetComponent<RectTransform>();
            _cardRoot.SetParent(_viewport, false);
            var layout = _cardRoot.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            _scroll.viewport = _viewport;
            _scroll.content = _cardRoot;
            JourneyViewFactory.Stretch(scrollOwner.GetComponent<RectTransform>());
            JourneyViewFactory.Stretch(_viewport);
            _cardRoot.anchorMin = new Vector2(0f, 1f);
            _cardRoot.anchorMax = new Vector2(1f, 1f);
            _cardRoot.pivot = new Vector2(0.5f, 1f);
        }

        private void AddControl(string id, string name, Sprite sprite, bool drag = false)
        {
            var control = JourneyViewFactory.CreateControl(name, _safeRoot, sprite, id, drag);
            _controls.Add(id, control);
            _router.Register(control.Semantic);
        }

        private void RebuildCards(IReadOnlyList<ParentReportCard> cards)
        {
            for (var index = _cardRoot.childCount - 1; index >= 0; index--)
            {
                var child = _cardRoot.GetChild(index).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }

            RenderedCardCount = cards.Count;
            for (var index = 0; index < cards.Count; index++) CreateCard(cards[index], index);
            RelayoutCards();
        }

        private void CreateCard(ParentReportCard card, int index)
        {
            var owner = JourneyViewFactory.CreateImage(
                "Card." + index + ".VisibleArt", _cardRoot, Color.white,
                card.Kind == ParentReportCardKind.Empty ? _content.Frames[1] : _content.Frames[0]);
            owner.type = Image.Type.Sliced;
            var element = owner.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = _metrics.ReportCardHeight > 0f ? _metrics.ReportCardHeight : 220f;
            var text = JourneyViewFactory.CreateRuntimeText("Text." + card.StringId, owner.transform);
            text.text = card.Text;
            text.fontSize = 34f * Mathf.Max(1f, _metrics.TextScale);
            text.margin = new Vector4(64f, 44f, 64f, 44f);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
        }

        private void RelayoutCards()
        {
            if (_cardRoot == null || _viewport == null) return;
            var count = Mathf.Max(0, RenderedCardCount);
            var gap = 24f;
            var height = count == 0 ? 0f : count * Mathf.Max(220f, _metrics.ReportCardHeight) + (count - 1) * gap;
            _cardRoot.sizeDelta = new Vector2(0f, height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_cardRoot);
        }

        private void UpdateControls()
        {
            foreach (var control in _controls.Values) control.Semantic.Enabled = _ready;
        }
    }
}
