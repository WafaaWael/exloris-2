using System;
using System.Collections.Generic;
using Exloris.Core.Navigation;
using Exloris.Core.Session;

namespace Exloris.Runtime.Adult
{
    public sealed class ParentReportController
    {
        private readonly NavigationAuthority _navigation;
        private readonly IParentReportView _view;
        private readonly IAdultGateRequestSink _adultGateSink;
        private readonly Func<Guid> _tokenSource;
        private readonly HashSet<Guid> _issuedTokens = new HashSet<Guid>();

        private AdultGateRequest _sourceRequest;
        private IReadOnlyList<ParentReportCard> _cards = Array.Empty<ParentReportCard>();
        private float _scrollNormalized = 1f;
        private string _focusedControlId = "parent_report.close";

        public ParentReportController(
            NavigationAuthority navigation,
            IParentReportView view,
            IAdultGateRequestSink adultGateSink,
            Func<Guid> tokenSource)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _adultGateSink = adultGateSink ?? throw new ArgumentNullException(nameof(adultGateSink));
            _tokenSource = tokenSource ?? throw new ArgumentNullException(nameof(tokenSource));
        }

        public ParentReportState State { get; private set; }
        public Guid CurrentTokenId { get; private set; }
        public ParentReportSnapshot Snapshot => new ParentReportSnapshot(
            CurrentTokenId,
            State,
            _cards,
            _scrollNormalized,
            _focusedControlId);

        public bool Enter(
            AdultGateRequest sourceRequest,
            Guid transactionId,
            SessionState session,
            double nowSeconds)
        {
            var transaction = _navigation.ActiveTransaction;
            if (State != ParentReportState.Hidden
                || !IsValidDashboardSource(sourceRequest)
                || session == null
                || transaction == null
                || transaction.Id != transactionId
                || transaction.Caller != RouteId.AdultGate
                || transaction.Destination != RouteId.ParentReport
                || _navigation.CurrentRoute != RouteId.AdultGate
                || _navigation.Recovery != null
                || !IsValidClock(nowSeconds))
                return false;

            var report = ParentReportBuilder.Build(session.Observations);
            var token = NextToken();
            if (_navigation.Complete(transactionId) != NavigationCompletion.Completed) return false;

            _sourceRequest = sourceRequest;
            _cards = report.Cards;
            _scrollNormalized = 1f;
            _focusedControlId = "parent_report.close";
            CurrentTokenId = token;
            State = ParentReportState.Ready;
            Present(true);
            return true;
        }

        public bool HandleClose(Guid tokenId, double nowSeconds)
        {
            if (!CanAct(tokenId) || !IsValidClock(nowSeconds)) return false;
            var result = _navigation.RequestReturn(_sourceRequest.ReturnRoute, nowSeconds);
            if (!result.Accepted) return false;
            State = ParentReportState.NavigatingClose;
            _view.SetInputReady(false);
            return true;
        }

        public bool HandleRestart(
            Guid tokenId,
            float scrollNormalized,
            string focusedControlId)
        {
            if (!CanAct(tokenId)
                || float.IsNaN(scrollNormalized)
                || float.IsInfinity(scrollNormalized)
                || scrollNormalized < 0f
                || scrollNormalized > 1f
                || string.IsNullOrWhiteSpace(focusedControlId))
                return false;

            var request = new AdultGateRequest(
                AdultGatePurpose.Restart,
                AdultGateRequestOrigin.ParentReport,
                RouteId.ParentReport,
                CurrentTokenId);
            if (!_adultGateSink.TryRequest(request)) return false;

            _scrollNormalized = scrollNormalized;
            _focusedControlId = focusedControlId;
            State = ParentReportState.NavigatingRestart;
            _view.SetInputReady(false);
            return true;
        }

        public bool RestoreFromAdultGate(Guid tokenId)
        {
            if (tokenId == Guid.Empty || tokenId != CurrentTokenId
                || State != ParentReportState.NavigatingRestart
                || _navigation.CurrentRoute != RouteId.ParentReport
                || _navigation.IsLocked || _navigation.Recovery != null)
                return false;
            State = ParentReportState.Ready;
            Present(true);
            return true;
        }

        private bool CanAct(Guid tokenId)
        {
            return tokenId != Guid.Empty
                && tokenId == CurrentTokenId
                && State == ParentReportState.Ready
                && _navigation.CurrentRoute == RouteId.ParentReport
                && !_navigation.IsLocked
                && _navigation.Recovery == null;
        }

        private static bool IsValidDashboardSource(AdultGateRequest request)
        {
            if (request.Purpose != AdultGatePurpose.Dashboard
                || !RouteGraph.CanRequestAdultDashboard(request.Origin))
                return false;
            if (request.Origin == AdultGateRequestOrigin.Welcome)
                return request.ReturnRoute == RouteId.Welcome
                    && request.ReturnStateToken == Guid.Empty
                    && string.Equals(request.ReturnPresentationId, "welcome", StringComparison.Ordinal);
            return request.Origin == AdultGateRequestOrigin.Completion
                && request.ReturnRoute == RouteId.Completion
                && request.ReturnStateToken != Guid.Empty
                && string.IsNullOrEmpty(request.ReturnPresentationId);
        }

        private void Present(bool ready)
        {
            _view.Present(Snapshot);
            _view.SetInputReady(ready);
        }

        private Guid NextToken()
        {
            var token = _tokenSource();
            if (token == Guid.Empty || !_issuedTokens.Add(token))
                throw new InvalidOperationException("Parent Report token source produced an empty or duplicate token.");
            return token;
        }

        private static bool IsValidClock(double value)
            => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
    }
}
