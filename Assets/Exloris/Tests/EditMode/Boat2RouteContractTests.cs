using System.Linq;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat2RouteContractTests
    {
        [Test]
        public void Boat2HasOnlyItsTwoAdjacentRouteEdges()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage1, RouteId.Boat2), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat2, RouteId.Stage2), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage1, RouteId.Stage2), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat2, RouteId.Boat3), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat2, RouteId.Welcome), Is.False);
            Assert.That(RouteGraph.FrozenRoute.Count(route => route == RouteId.Boat2), Is.EqualTo(1));
        }

        [Test]
        public void HubAndProfilesCannotOwnAnyBoat2Route()
        {
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "Hub");
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "ProfilePicker");
            Assert.That(RouteGraph.CanLeaveToWelcome(RouteId.Boat2), Is.True,
                "Boat 2 reaches Welcome only through the mapped Leave overlay transaction.");
        }
    }
}
