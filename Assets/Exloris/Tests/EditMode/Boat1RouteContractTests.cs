using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat1RouteContractTests
    {
        [Test]
        public void Boat1HasOnlyTheApprovedAdjacentEdges()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.JourneyOverview, RouteId.Boat1), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat1, RouteId.Stage1), Is.True);

            Assert.That(RouteGraph.CanAdvance(RouteId.JourneyOverview, RouteId.Stage1), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat1, RouteId.Boat2), Is.False);
            Assert.That(RouteGraph.CanReturn(RouteId.Boat1, RouteId.JourneyOverview), Is.False);

            foreach (var destination in RouteGraph.FrozenRoute)
            {
                if (destination != RouteId.Stage1)
                {
                    Assert.That(
                        RouteGraph.CanAdvance(RouteId.Boat1, destination),
                        Is.False,
                        $"Boat1 must not advance directly to {destination}.");
                }
            }
        }
    }
}
