using System;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarSessionContractTests
    {
        [Test]
        public void OpenSessionMovesOnlyToChildSessionRoutes()
        {
            var session = new SessionState();
            session.Begin(RouteId.AvatarSelect);

            Assert.That(session.MoveTo(RouteId.AvatarPreview), Is.True);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.AvatarPreview));
            Assert.Throws<ArgumentOutOfRangeException>(() => session.MoveTo(RouteId.Welcome));
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.AvatarPreview));
        }

        [Test]
        public void ClosedSessionCannotMove()
        {
            var session = new SessionState();

            Assert.That(session.MoveTo(RouteId.AvatarPreview), Is.False);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.StartupLoading));
        }

        [TestCase("avatar_boy")]
        [TestCase("avatar_girl")]
        public void SelectAvatarAcceptsOnlyApprovedDirectTokens(string avatarId)
        {
            var session = new SessionState();

            session.SelectAvatar(avatarId);

            Assert.That(session.SelectedAvatarId, Is.EqualTo(avatarId));
            Assert.That(session.Observations, Is.Empty);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("avatar_")]
        [TestCase("avatar_child")]
        [TestCase("boy")]
        [TestCase("Avatar_Boy")]
        public void SelectAvatarRejectsEveryUnapprovedToken(string avatarId)
        {
            var session = new SessionState();

            Assert.Throws<ArgumentException>(() => session.SelectAvatar(avatarId));
            Assert.That(session.SelectedAvatarId, Is.Null);
            Assert.That(session.Observations, Is.Empty);
        }

        [Test]
        public void ResetAndProcessReconstructionClearAvatarChoice()
        {
            var session = new SessionState();
            session.Begin(RouteId.AvatarSelect);
            session.SelectAvatar("avatar_girl");
            session.MoveTo(RouteId.AvatarPreview);

            session.Reset();
            var reconstructed = new SessionState();

            Assert.That(session.SelectedAvatarId, Is.Null);
            Assert.That(session.IsOpen, Is.False);
            Assert.That(session.CurrentOpenSessionLocation, Is.EqualTo(RouteId.StartupLoading));
            Assert.That(reconstructed.SelectedAvatarId, Is.Null);
            Assert.That(reconstructed.IsOpen, Is.False);
            Assert.That(reconstructed.Observations, Is.Empty);
        }
    }
}
