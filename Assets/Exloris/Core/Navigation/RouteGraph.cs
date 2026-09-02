using System;
using System.Collections.Generic;

namespace Exloris.Core.Navigation
{
    public enum AdultGateRequestOrigin
    {
        Welcome,
        SessionChoice,
        Completion,
        ParentReport,
    }

    public static class RouteGraph
    {
        private static readonly RouteId[] Route =
        {
            RouteId.StartupLoading,
            RouteId.IntroVideo,
            RouteId.Welcome,
            RouteId.AvatarSelect,
            RouteId.AvatarPreview,
            RouteId.JourneyOverview,
            RouteId.Boat1,
            RouteId.Stage1,
            RouteId.Boat2,
            RouteId.Stage2,
            RouteId.Boat3,
            RouteId.Stage3,
            RouteId.Completion,
            RouteId.AdultGate,
            RouteId.ParentReport,
        };

        public static IReadOnlyList<RouteId> FrozenRoute => Route;

        public static bool CanAdvance(RouteId caller, RouteId destination)
        {
            var callerIndex = Array.IndexOf(Route, caller);
            return callerIndex >= 0
                && callerIndex + 1 < Route.Length
                && Route[callerIndex + 1] == destination;
        }

        public static bool CanReturn(RouteId caller, RouteId destination)
        {
            return caller == RouteId.AvatarPreview
                    && destination == RouteId.AvatarSelect
                || caller == RouteId.ParentReport
                    && (destination == RouteId.Welcome || destination == RouteId.Completion);
        }

        public static bool CanRequestAdultDashboard(AdultGateRequestOrigin origin)
        {
            return origin == AdultGateRequestOrigin.Welcome
                || origin == AdultGateRequestOrigin.Completion;
        }

        public static bool CanRequestAdultRestart(AdultGateRequestOrigin origin)
        {
            return origin == AdultGateRequestOrigin.SessionChoice
                || origin == AdultGateRequestOrigin.ParentReport;
        }

        public static bool CanReturnFromAdultGate(AdultGateRequestOrigin origin, RouteId destination)
        {
            return destination == AdultGateCaller(origin);
        }

        private static RouteId AdultGateCaller(AdultGateRequestOrigin origin)
        {
            switch (origin)
            {
                case AdultGateRequestOrigin.Welcome:
                case AdultGateRequestOrigin.SessionChoice: return RouteId.Welcome;
                case AdultGateRequestOrigin.Completion: return RouteId.Completion;
                case AdultGateRequestOrigin.ParentReport: return RouteId.ParentReport;
                default: return RouteId.StartupLoading;
            }
        }

        public static bool CanLeaveToWelcome(RouteId caller)
        {
            return caller == RouteId.AvatarSelect
                || caller == RouteId.AvatarPreview
                || caller == RouteId.JourneyOverview
                || caller == RouteId.Boat1
                || caller == RouteId.Boat2
                || caller == RouteId.Stage2
                || caller == RouteId.Boat3
                || caller == RouteId.Stage3
                || caller == RouteId.Completion;
        }

        public static bool CanPresentLeaveConfirmation(RouteId caller)
        {
            return CanLeaveToWelcome(caller) || caller == RouteId.Stage1;
        }
    }
}
