using System;
using System.Collections;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class JourneyOverviewMediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApprovedMediaPausesAndResumesSameBeatWithFreshMediaOwnership()
        {
            var owner = new GameObject("JourneyOverviewMediaPlayMode");
            var backend = new UnityAudioBackend(owner);
            var director = new AudioDirector(backend, SequentialIds(20));
            var session = new SessionState();
            session.Begin(RouteId.JourneyOverview);
            var accessibility = new AccessibilityState(false, true);
            accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var sink = new RecordingSink();
            var presenter = new JourneyOverviewMediaPresenter(
                director, backend, JourneyOverviewContent.LoadApproved(), session,
                accessibility, sink, SequentialIds(30));
            var reveal = Guid.NewGuid();

            var started = presenter.Start(reveal, 10d);
            yield return null;

            Assert.That(backend.GetTokenClip(started.VoiceTokenId.Value),
                Is.SameAs(JourneyOverviewContent.LoadApproved().Voice));
            Assert.That(backend.GetTokenLoop(started.MusicTokenId.Value), Is.True);
            Assert.That(backend.GetTokenLoop(started.AmbienceTokenId.Value), Is.True);
            presenter.Advance(reveal, started.MediaTokenId, 14.5d);
            Assert.That(presenter.Pause(reveal, started.MediaTokenId, 14.5d,
                JourneyOverviewPauseReason.AppBackground), Is.True);
            Assert.That(backend.IsPaused, Is.True);

            var resumed = presenter.ResumeAfterMappedUserAction(reveal, 100d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(started.MediaTokenId));
            Assert.That(presenter.ElapsedSeconds, Is.EqualTo(4.5d).Within(0.0001d));
            Assert.That(sink.Last.CaptionId, Is.EqualTo(JourneyOverviewMediaPresenter.CaptionReadId));
            Assert.That(backend.IsPaused, Is.False);

            backend.Dispose();
            UnityEngine.Object.Destroy(owner);
        }

        private static Func<Guid> SequentialIds(int prefix)
        {
            var value = 0;
            return () =>
            {
                value++;
                var bytes = new byte[16];
                bytes[0] = (byte)prefix;
                bytes[15] = (byte)value;
                return new Guid(bytes);
            };
        }

        private sealed class RecordingSink : IJourneyOverviewMediaCueSink
        {
            public JourneyOverviewMediaCue Last { get; private set; }
            public void PresentMediaCue(JourneyOverviewMediaCue cue) => Last = cue;
            public void FreezeMediaCue(double elapsedSeconds) { }
            public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCallerInputLocked(bool locked) { }
            public void ShowPauseOverlay(JourneyOverviewPauseReason reason) { }
        }
    }
}
