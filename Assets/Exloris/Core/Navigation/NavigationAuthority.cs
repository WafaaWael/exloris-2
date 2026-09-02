using System;
using System.Collections.Generic;

namespace Exloris.Core.Navigation
{
    public sealed class NavigationAuthority
    {
        public const double TimeoutSeconds = 10.0;
        private const double ClockEpsilonSeconds = 0.000000001d;

        private readonly Func<Guid> _idFactory;
        private readonly HashSet<Guid> _issuedIds = new HashSet<Guid>();
        private NavigationTransaction _activeTransaction;
        private NavigationRecovery _recovery;

        public NavigationAuthority(RouteId initialRoute, Func<Guid> idFactory = null)
        {
            CurrentRoute = initialRoute;
            _idFactory = idFactory ?? Guid.NewGuid;
        }

        public RouteId CurrentRoute { get; private set; }
        public NavigationTransaction ActiveTransaction => _activeTransaction;
        public NavigationRecovery Recovery => _recovery;
        public bool IsLocked => _activeTransaction != null;

        public NavigationRequestResult Request(RouteId destination, double nowSeconds)
        {
            if (_recovery != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            if (_activeTransaction != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);
            }

            if (!RouteGraph.CanAdvance(CurrentRoute, destination))
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            }

            return Start(CurrentRoute, destination, nowSeconds);
        }

        public NavigationRequestResult RequestResume(RouteId destination, double nowSeconds)
        {
            if (_recovery != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            if (_activeTransaction != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);
            }

            if (CurrentRoute != RouteId.Welcome)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            }

            return Start(CurrentRoute, destination, nowSeconds);
        }

        public NavigationRequestResult RequestReturn(RouteId destination, double nowSeconds)
        {
            if (_recovery != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            if (_activeTransaction != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);
            }

            if (!RouteGraph.CanReturn(CurrentRoute, destination))
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            }

            return Start(CurrentRoute, destination, nowSeconds);
        }

        public NavigationRequestResult RequestAdultDashboard(
            AdultGateRequestOrigin origin, double nowSeconds)
        {
            return RequestAdultGate(origin, false, nowSeconds);
        }

        public NavigationRequestResult RequestAdultRestart(
            AdultGateRequestOrigin origin, double nowSeconds)
        {
            return RequestAdultGate(origin, true, nowSeconds);
        }

        public NavigationRequestResult RequestAdultGateReturn(
            AdultGateRequestOrigin origin, RouteId destination, double nowSeconds)
        {
            if (_recovery != null) return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            if (_activeTransaction != null) return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);
            if (CurrentRoute != RouteId.AdultGate || !RouteGraph.CanReturnFromAdultGate(origin, destination))
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            return Start(CurrentRoute, destination, nowSeconds);
        }

        public bool ConfirmedResetToStartup()
        {
            if (CurrentRoute != RouteId.AdultGate || _activeTransaction != null || _recovery != null) return false;
            CurrentRoute = RouteId.StartupLoading;
            return true;
        }

        public NavigationRequestResult RequestHome(double nowSeconds)
        {
            if (_recovery != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            }

            if (_activeTransaction != null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);
            }

            if (!RouteGraph.CanLeaveToWelcome(CurrentRoute))
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            }

            return Start(CurrentRoute, RouteId.Welcome, nowSeconds);
        }

        public NavigationCompletion Complete(Guid transactionId)
        {
            if (_activeTransaction == null || _activeTransaction.Id != transactionId)
            {
                return NavigationCompletion.RejectedStale;
            }

            CurrentRoute = _activeTransaction.Destination;
            _activeTransaction = null;
            return NavigationCompletion.Completed;
        }

        public bool CheckTimeout(double nowSeconds)
        {
            if (_activeTransaction == null
                || nowSeconds - _activeTransaction.StartedAtSeconds
                    < TimeoutSeconds - ClockEpsilonSeconds)
            {
                return false;
            }

            MoveActiveToRecovery(NavigationFailureReason.Timeout);
            return true;
        }

        public bool Interrupt()
        {
            if (_activeTransaction == null)
            {
                return false;
            }

            MoveActiveToRecovery(NavigationFailureReason.Interrupted);
            return true;
        }

        public bool Cancel()
        {
            if (_activeTransaction == null)
            {
                return false;
            }

            MoveActiveToRecovery(NavigationFailureReason.Cancelled);
            return true;
        }

        public NavigationRequestResult Retry(double nowSeconds)
        {
            if (_recovery == null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.NoRecovery);
            }

            var caller = _recovery.Caller;
            var destination = _recovery.Destination;
            _recovery = null;
            return Start(caller, destination, nowSeconds);
        }

        public NavigationRequestResult Home(double nowSeconds)
        {
            if (_recovery == null)
            {
                return NavigationRequestResult.Reject(NavigationRequestFailure.NoRecovery);
            }

            var caller = _recovery.Caller;
            _recovery = null;
            return Start(caller, RouteId.Welcome, nowSeconds);
        }

        private NavigationRequestResult Start(RouteId caller, RouteId destination, double nowSeconds)
        {
            var transaction = new NavigationTransaction(NextId(), caller, destination, nowSeconds);
            _activeTransaction = transaction;
            return NavigationRequestResult.Accept(transaction);
        }

        private NavigationRequestResult RequestAdultGate(
            AdultGateRequestOrigin origin, bool restart, double nowSeconds)
        {
            if (_recovery != null)
                return NavigationRequestResult.Reject(NavigationRequestFailure.RecoveryPending);
            if (_activeTransaction != null)
                return NavigationRequestResult.Reject(NavigationRequestFailure.Locked);

            var allowed = restart
                ? RouteGraph.CanRequestAdultRestart(origin)
                : RouteGraph.CanRequestAdultDashboard(origin);
            if (!allowed || CurrentRoute != AdultGateCaller(origin))
                return NavigationRequestResult.Reject(NavigationRequestFailure.IllegalRoute);
            return Start(CurrentRoute, RouteId.AdultGate, nowSeconds);
        }

        private static RouteId AdultGateCaller(AdultGateRequestOrigin origin)
        {
            switch (origin)
            {
                case AdultGateRequestOrigin.Welcome:
                case AdultGateRequestOrigin.SessionChoice:
                    return RouteId.Welcome;
                case AdultGateRequestOrigin.Completion:
                    return RouteId.Completion;
                case AdultGateRequestOrigin.ParentReport:
                    return RouteId.ParentReport;
                default:
                    return RouteId.StartupLoading;
            }
        }

        private Guid NextId()
        {
            var id = _idFactory();
            if (id == Guid.Empty || !_issuedIds.Add(id))
            {
                throw new InvalidOperationException("Navigation transaction IDs must be non-empty and unique.");
            }

            return id;
        }

        private void MoveActiveToRecovery(NavigationFailureReason reason)
        {
            var transaction = _activeTransaction;
            _activeTransaction = null;
            _recovery = new NavigationRecovery(
                transaction.Id,
                transaction.Caller,
                transaction.Destination,
                reason);
        }
    }
}
