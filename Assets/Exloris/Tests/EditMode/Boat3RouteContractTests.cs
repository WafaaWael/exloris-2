using System.Linq;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Boat3RouteContractTests
    {
        [Test]
        public void Boat3HasOnlyItsTwoAdjacentRouteEdges()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, RouteId.Boat3), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat3, RouteId.Stage3), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, RouteId.Stage3), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat3, RouteId.Completion), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat3, RouteId.Welcome), Is.False);
            Assert.That(RouteGraph.FrozenRoute.Count(route => route == RouteId.Boat3), Is.EqualTo(1));
        }

        [Test]
        public void HubAndProfilesCannotOwnAnyBoat3Route()
        {
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "Hub");
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "ProfilePicker");
            Assert.That(RouteGraph.CanLeaveToWelcome(RouteId.Boat3), Is.True,
                "Boat 3 reaches Welcome only through the mapped Leave overlay transaction.");
        }
    }
}
