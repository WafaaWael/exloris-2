using System;
using Exloris.Core.Navigation;

namespace Exloris.Runtime.Adult
{
    public enum AdultGatePurpose
    {
        Dashboard,
        Restart,
    }

    public readonly struct AdultGateRequest
    {
        public AdultGateRequest(
            AdultGatePurpose purpose,
            AdultGateRequestOrigin origin,
            RouteId returnRoute,
            Guid returnStateToken,
            string returnPresentationId = null)
        {
            Purpose = purpose;
            Origin = origin;
            ReturnRoute = returnRoute;
            ReturnStateToken = returnStateToken;
            ReturnPresentationId = returnPresentationId;
        }

        public AdultGatePurpose Purpose { get; }
        public AdultGateRequestOrigin Origin { get; }
        public RouteId ReturnRoute { get; }
        public Guid ReturnStateToken { get; }
        public string ReturnPresentationId { get; }
    }

    public interface IAdultGateRequestSink
    {
        bool TryRequest(AdultGateRequest request);
    }

    public enum AdultGateState
    {
        Hidden,
        Holding,
        AwaitingConfirm,
        ResetConfirmation,
        NavigatingReport,
        Returning,
        Resetting,
        Recovery,
    }

    public readonly struct AdultGateSnapshot
    {
        public AdultGateSnapshot(
            Guid tokenId,
            AdultGateState state,
            AdultGateRequest request,
            double holdProgress,
            int activePointerId,
            double holdStartedAtSeconds)
        {
            TokenId = tokenId;
            State = state;
            Request = request;
            HoldProgress = holdProgress;
            ActivePointerId = activePointerId;
            HoldStartedAtSeconds = holdStartedAtSeconds;
        }

        public Guid TokenId { get; }
        public AdultGateState State { get; }
        public AdultGateRequest Request { get; }
        public double HoldProgress { get; }
        public int ActivePointerId { get; }
        public double HoldStartedAtSeconds { get; }
    }

    public interface IAdultGateView
    {
        void Present(AdultGateSnapshot snapshot);
        void SetInputReady(bool ready);
    }

    public interface IAdultGateReturnSink
    {
        bool TryReturn(AdultGateRequest request);
    }

    public interface IConfirmedSessionResetSink
    {
        bool TryReset();
    }
}
