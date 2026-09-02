using System.Linq;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2RouteContractTests
    {
        [Test]
        public void Stage2HasOnlyItsTwoAdjacentRouteEdges()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat2, RouteId.Stage2), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, RouteId.Boat3), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat2, RouteId.Boat3), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, RouteId.Welcome), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, RouteId.Stage3), Is.False);
            Assert.That(RouteGraph.FrozenRoute.Count(route => route == RouteId.Stage2), Is.EqualTo(1));

            foreach (var destination in RouteGraph.FrozenRoute)
                if (destination != RouteId.Boat3)
                    Assert.That(RouteGraph.CanAdvance(RouteId.Stage2, destination), Is.False,
                        $"Stage 2 must not advance directly to {destination}.");
        }

        [Test]
        public void HubProfilesAndNonAdjacentRoutesCannotOwnStage2Navigation()
        {
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "Hub");
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "ProfilePicker");
            Assert.That(RouteGraph.CanLeaveToWelcome(RouteId.Stage2), Is.True);
            Assert.That(RouteGraph.CanReturn(RouteId.Stage2, RouteId.Boat2), Is.False);
        }
    }
}
