using System;

namespace Exloris.Core.Navigation
{
    public enum NavigationRequestFailure
    {
        None,
        Locked,
        RecoveryPending,
        IllegalRoute,
        NoRecovery,
    }

    public enum NavigationCompletion
    {
        Completed,
        RejectedStale,
    }

    public enum NavigationFailureReason
    {
        Timeout,
        Interrupted,
        Cancelled,
    }

    public sealed class NavigationTransaction
    {
        public NavigationTransaction(Guid id, RouteId caller, RouteId destination, double startedAtSeconds)
        {
            Id = id;
            Caller = caller;
            Destination = destination;
            StartedAtSeconds = startedAtSeconds;
        }

        public Guid Id { get; }
        public RouteId Caller { get; }
        public RouteId Destination { get; }
        public double StartedAtSeconds { get; }
    }

    public sealed class NavigationRecovery
    {
        public NavigationRecovery(
            Guid failedTransactionId,
            RouteId caller,
            RouteId destination,
            NavigationFailureReason reason)
        {
            FailedTransactionId = failedTransactionId;
            Caller = caller;
            Destination = destination;
            Reason = reason;
        }

        public Guid FailedTransactionId { get; }
        public RouteId Caller { get; }
        public RouteId Destination { get; }
        public NavigationFailureReason Reason { get; }
    }

    public sealed class NavigationRequestResult
    {
        private NavigationRequestResult(
            bool accepted,
            NavigationRequestFailure failure,
            NavigationTransaction transaction)
        {
            Accepted = accepted;
            Failure = failure;
            Transaction = transaction;
        }

        public bool Accepted { get; }
        public NavigationRequestFailure Failure { get; }
        public NavigationTransaction Transaction { get; }

        public static NavigationRequestResult Accept(NavigationTransaction transaction)
        {
            return new NavigationRequestResult(true, NavigationRequestFailure.None, transaction);
        }

        public static NavigationRequestResult Reject(NavigationRequestFailure failure)
        {
            return new NavigationRequestResult(false, failure, null);
        }
    }
}
