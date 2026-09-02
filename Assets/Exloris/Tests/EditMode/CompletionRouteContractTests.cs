using System;
using System.Linq;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionRouteContractTests
    {
        [Test]
        public void ClosingRouteUsesOnlyDirectPublicAdjacencyAndExactReturns()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage3, RouteId.Completion), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Completion, RouteId.AdultGate), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.AdultGate, RouteId.ParentReport), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Completion, RouteId.ParentReport), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.ParentReport, RouteId.Completion), Is.False);
            Assert.That(RouteGraph.CanReturn(RouteId.ParentReport, RouteId.Completion), Is.True);
            Assert.That(RouteGraph.CanReturn(RouteId.ParentReport, RouteId.Welcome), Is.True);
            Assert.That(RouteGraph.CanPresentLeaveConfirmation(RouteId.Completion), Is.True);

            CollectionAssert.DoesNotContain(Enum.GetNames(typeof(RouteId)), "Hub");
            CollectionAssert.DoesNotContain(Enum.GetNames(typeof(RouteId)), "ProfilePicker");
            CollectionAssert.DoesNotContain(Enum.GetNames(typeof(RouteId)), "ResetConfirmation");
        }

        [Test]
        public void AdultGateOriginsAreSeparatedByPurposeAndRejectEveryOtherOrigin()
        {
            Assert.That(RouteGraph.CanRequestAdultDashboard(AdultGateRequestOrigin.Welcome), Is.True);
            Assert.That(RouteGraph.CanRequestAdultDashboard(AdultGateRequestOrigin.Completion), Is.True);
            Assert.That(RouteGraph.CanRequestAdultDashboard(AdultGateRequestOrigin.SessionChoice), Is.False);
            Assert.That(RouteGraph.CanRequestAdultDashboard(AdultGateRequestOrigin.ParentReport), Is.False);

            Assert.That(RouteGraph.CanRequestAdultRestart(AdultGateRequestOrigin.SessionChoice), Is.True);
            Assert.That(RouteGraph.CanRequestAdultRestart(AdultGateRequestOrigin.ParentReport), Is.True);
            Assert.That(RouteGraph.CanRequestAdultRestart(AdultGateRequestOrigin.Welcome), Is.False);
            Assert.That(RouteGraph.CanRequestAdultRestart(AdultGateRequestOrigin.Completion), Is.False);

            var origins = Enum.GetValues(typeof(AdultGateRequestOrigin))
                .Cast<AdultGateRequestOrigin>()
                .ToArray();
            Assert.That(origins, Has.Length.EqualTo(4));
        }

        [Test]
        public void ClosingRoutesNeverLegalizeNonadjacentOrBackwardChildAdvance()
        {
            foreach (var destination in RouteGraph.FrozenRoute)
            {
                if (destination != RouteId.AdultGate)
                    Assert.That(RouteGraph.CanAdvance(RouteId.Completion, destination), Is.False,
                        $"Completion must not advance directly to {destination}.");
                if (destination != RouteId.ParentReport)
                    Assert.That(RouteGraph.CanAdvance(RouteId.AdultGate, destination), Is.False,
                        $"Adult Gate must not advance directly to {destination}.");
                Assert.That(RouteGraph.CanAdvance(RouteId.ParentReport, destination), Is.False,
                    $"Parent Report is the terminal public route, not an advance to {destination}.");
            }
        }
    }
}
