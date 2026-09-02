using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewRouteContractTests
    {
        [Test]
        public void OverviewHasOnlyTheApprovedAdjacentEdges()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.AvatarPreview, RouteId.JourneyOverview), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.JourneyOverview, RouteId.Boat1), Is.True);

            Assert.That(RouteGraph.CanAdvance(RouteId.AvatarPreview, RouteId.Boat1), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.JourneyOverview, RouteId.Stage1), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.JourneyOverview, RouteId.AvatarPreview), Is.False);
            Assert.That(RouteGraph.CanReturn(RouteId.JourneyOverview, RouteId.AvatarPreview), Is.False);

            foreach (var destination in RouteGraph.FrozenRoute)
            {
                if (destination != RouteId.Boat1)
                {
                    Assert.That(
                        RouteGraph.CanAdvance(RouteId.JourneyOverview, destination),
                        Is.False,
                        $"JourneyOverview must not advance directly to {destination}.");
                }
            }
        }
    }
}
