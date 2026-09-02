using System.Linq;
using Exloris.Core.Navigation;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3RouteContractTests
    {
        [Test]
        public void Stage3HasOnlyBoat3EntryAndCompletionExit()
        {
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat3, RouteId.Stage3), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage3, RouteId.Completion), Is.True);
            Assert.That(RouteGraph.CanAdvance(RouteId.Boat3, RouteId.Completion), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage3, RouteId.Welcome), Is.False);
            Assert.That(RouteGraph.CanAdvance(RouteId.Stage3, RouteId.AdultGate), Is.False);
            Assert.That(RouteGraph.FrozenRoute.Count(route => route == RouteId.Stage3), Is.EqualTo(1));

            foreach (var destination in RouteGraph.FrozenRoute)
                if (destination != RouteId.Completion)
                    Assert.That(RouteGraph.CanAdvance(RouteId.Stage3, destination), Is.False,
                        $"Stage 3 must not advance directly to {destination}.");
        }

        [Test]
        public void HubProfilesAndBackwardRoutesCannotOwnStage3Navigation()
        {
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "Hub");
            CollectionAssert.DoesNotContain(System.Enum.GetNames(typeof(RouteId)), "ProfilePicker");
            Assert.That(RouteGraph.CanPresentLeaveConfirmation(RouteId.Stage3), Is.True);
            Assert.That(RouteGraph.CanReturn(RouteId.Stage3, RouteId.Boat3), Is.False);
        }
    }
}
