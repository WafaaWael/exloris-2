using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Overview;
using Exloris.Runtime.Recovery;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewPauseHelpControllerTests
    {
        [Test]
        public void MatchingPauseOpensOnceAndBackRestoresByteIdenticalSnapshot()
        {
            var pause = Snapshot();
            var parent = new RecordingParent(pause);
            var view = new RecordingView();
            var controller = new JourneyOverviewPauseHelpController(parent, view);

            Assert.That(controller.TryShow(pause), Is.True);
            Assert.That(controller.TryShow(pause), Is.False);
            Assert.That(view.Shown, Is.EqualTo(pause));
            Assert.That(controller.HandleBack(), Is.True);
            Assert.That(controller.HandleBack(), Is.False);
            Assert.That(parent.Restored, Is.EqualTo(pause));
            Assert.That(view.HideCount, Is.EqualTo(1));
        }

        [Test]
        public void WrongOrStalePauseTokenCannotOpenHelp()
        {
            var current = Snapshot();
            var parent = new RecordingParent(current);
            var controller = new JourneyOverviewPauseHelpController(parent, new RecordingView());
            var stale = new PauseOverlaySnapshot(Guid.NewGuid(), current.CallerSnapshot,
                current.Settings);

            Assert.That(controller.TryShow(stale), Is.False);
            Assert.That(controller.HandleBack(), Is.False);
        }

        private static PauseOverlaySnapshot Snapshot()
        {
            return new PauseOverlaySnapshot(
                Guid.NewGuid(),
                new JourneyOverviewSnapshot(Guid.NewGuid(),
                    JourneyOverviewRevealState.RevealingRead, 4d, 2),
                new PauseSettings(true, true, false));
        }

        private sealed class RecordingParent : IPauseOverlayReturnSink
        {
            private readonly PauseOverlaySnapshot _current;
            public RecordingParent(PauseOverlaySnapshot current) => _current = current;
            public PauseOverlaySnapshot Restored { get; private set; }
            public bool IsCurrentPause(PauseOverlaySnapshot snapshot) =>
                snapshot.StackToken == _current.StackToken;
            public bool RestoreFromHelp(PauseOverlaySnapshot snapshot)
            {
                if (!IsCurrentPause(snapshot)) return false;
                Restored = snapshot; return true;
            }
        }

        private sealed class RecordingView : IJourneyOverviewPauseHelpView
        {
            public PauseOverlaySnapshot Shown { get; private set; }
            public int HideCount { get; private set; }
            public void ShowAbovePause(PauseOverlaySnapshot snapshot) => Shown = snapshot;
            public void Hide() => HideCount++;
        }
    }
}
