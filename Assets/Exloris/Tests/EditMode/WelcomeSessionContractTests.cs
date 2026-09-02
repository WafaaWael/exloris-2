using Exloris.Core.Navigation;
using Exloris.Core.Session;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeSessionContractTests
    {
        [Test]
        public void BeginCreatesOneVolatileSessionAtAvatarSelectWithoutObservation()
        {
            var session = new SessionState();

            var started = session.Begin(RouteId.AvatarSelect);

            Assert.That(started, Is.True);
            Assert.That(session.IsOpen, Is.True);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.AvatarSelect));
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void SecondBeginIsRejectedAndPreservesExactOpenLocation()
        {
            var session = new SessionState();
            session.Begin(RouteId.Stage1);

            var second = session.Begin(RouteId.AvatarSelect);

            Assert.That(second, Is.False);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.Stage1));
        }

        [TestCase(RouteId.StartupLoading)]
        [TestCase(RouteId.IntroVideo)]
        [TestCase(RouteId.Welcome)]
        [TestCase(RouteId.AdultGate)]
        [TestCase(RouteId.ParentReport)]
        public void BeginRejectsRoutesThatCannotOwnAnOpenChildSession(RouteId invalidLocation)
        {
            var session = new SessionState();

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => session.Begin(invalidLocation));
            Assert.That(session.IsOpen, Is.False);
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void ResetClosesSessionAndClearsItsLocation()
        {
            var session = new SessionState();
            session.Begin(RouteId.Boat2);

            session.Reset();

            Assert.That(session.IsOpen, Is.False);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.StartupLoading));
        }
    }
}
