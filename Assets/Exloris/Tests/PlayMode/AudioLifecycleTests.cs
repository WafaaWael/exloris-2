using System;
using System.Collections.Generic;
using Exloris.Runtime.Media;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class AudioLifecycleTests
    {
        [Test]
        public void InterruptionPausesAllTracksAndNeverAutoResumes()
        {
            var events = new List<string>();
            var clock = new FakeClock(events, 4.25d);
            var tracks = new ISynchronizedMediaTrack[]
            {
                new FakeTrack("voice", events),
                new FakeTrack("caption", events),
                new FakeTrack("animation", events)
            };
            var coordinator = new MediaPlaybackCoordinator(clock, tracks, SequentialIds());
            coordinator.Start("intro");

            coordinator.PauseAndDuckImmediately();

            CollectionAssert.Contains(events, "clock:pause");
            CollectionAssert.Contains(events, "voice:pause");
            CollectionAssert.Contains(events, "caption:pause");
            CollectionAssert.Contains(events, "animation:pause");
            Assert.That(events, Has.None.Contains("resume"));
        }

        [Test]
        public void MappedResumeResynchronizesEveryTrackBeforePlaybackRestarts()
        {
            var events = new List<string>();
            var coordinator = new MediaPlaybackCoordinator(
                new FakeClock(events, 3.5d),
                new ISynchronizedMediaTrack[]
                {
                    new FakeTrack("voice", events),
                    new FakeTrack("caption", events),
                    new FakeTrack("animation", events)
                },
                SequentialIds());
            coordinator.Start("intro");
            coordinator.PauseAndDuckImmediately();
            events.Clear();

            coordinator.ResumeAfterMappedUserAction();

            var firstResume = events.FindIndex(item => item.Contains(":resume"));
            Assert.That(firstResume, Is.GreaterThanOrEqualTo(3));
            Assert.That(events[0], Is.EqualTo("voice:seek:3.500"));
            Assert.That(events[1], Is.EqualTo("caption:seek:3.500"));
            Assert.That(events[2], Is.EqualTo("animation:seek:3.500"));
            Assert.That(events[events.Count - 1], Is.EqualTo("clock:resume"));
        }

        [Test]
        public void OutputRouteChangeReissuesTokenAndRejectsOldCompletion()
        {
            var events = new List<string>();
            var coordinator = new MediaPlaybackCoordinator(
                new FakeClock(events, 2d),
                new ISynchronizedMediaTrack[] { new FakeTrack("voice", events) },
                SequentialIds());
            var original = coordinator.Start("voice_and_caption");

            coordinator.HandleOutputRouteChanged();
            Assert.That(coordinator.Complete(original.Id), Is.False);

            var resumed = coordinator.ResumeAfterMappedUserAction();
            Assert.That(resumed.Id, Is.Not.EqualTo(original.Id));
            Assert.That(resumed.SemanticId, Is.EqualTo(original.SemanticId));
            Assert.That(coordinator.Complete(original.Id), Is.False);
            Assert.That(coordinator.Complete(resumed.Id), Is.True);
            Assert.That(coordinator.Complete(resumed.Id), Is.False);
        }

        [Test]
        public void CaptionSynchronizerRejectsStaleTokenAndMeetsOneHundredMillisecondParity()
        {
            var current = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var stale = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var captions = new CaptionSynchronizer();
            captions.Bind(current);

            Assert.That(captions.Align(current, 1.000d, 1.095d), Is.True);
            Assert.That(captions.LastAlignmentErrorMilliseconds, Is.EqualTo(95d).Within(0.001d));
            Assert.That(captions.Align(current, 1.000d, 1.101d), Is.False);
            Assert.That(captions.Align(stale, 1.000d, 1.000d), Is.False);
        }

        [Test]
        public void StartingNewMediaWhilePlaybackIsActiveIsRejected()
        {
            var events = new List<string>();
            var coordinator = new MediaPlaybackCoordinator(
                new FakeClock(events, 0d),
                new ISynchronizedMediaTrack[0],
                SequentialIds());
            var first = coordinator.Start("first");

            Assert.Throws<InvalidOperationException>(() => coordinator.Start("second"));
            Assert.That(coordinator.Current.Id, Is.EqualTo(first.Id));
            Assert.That(coordinator.Complete(first.Id), Is.True);
        }

        [Test]
        public void UnityAudioConfigurationChangePausesCurrentMediaForResync()
        {
            var events = new List<string>();
            var coordinator = new MediaPlaybackCoordinator(
                new FakeClock(events, 1.5d),
                new ISynchronizedMediaTrack[] { new FakeTrack("voice", events) },
                SequentialIds());
            coordinator.Start("route_sensitive_voice");
            var gameObject = new GameObject("AudioConfigurationMonitorTest");
            var monitor = gameObject.AddComponent<AudioConfigurationMonitor>();
            monitor.Configure(coordinator);

            monitor.HandleConfigurationChanged(deviceWasChanged: true);

            Assert.That(coordinator.ResumePending, Is.True);
            CollectionAssert.Contains(events, "voice:pause");
            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private static Func<Guid> SequentialIds()
        {
            var value = 0;
            return () =>
            {
                value++;
                var bytes = new byte[16];
                bytes[15] = (byte)value;
                return new Guid(bytes);
            };
        }

        private sealed class FakeClock : IMediaPlaybackClock
        {
            private readonly List<string> _events;

            public FakeClock(List<string> events, double timeSeconds)
            {
                _events = events;
                TimeSeconds = timeSeconds;
            }

            public double TimeSeconds { get; }

            public void Pause()
            {
                _events.Add("clock:pause");
            }

            public void Resume()
            {
                _events.Add("clock:resume");
            }
        }

        private sealed class FakeTrack : ISynchronizedMediaTrack
        {
            private readonly string _name;
            private readonly List<string> _events;

            public FakeTrack(string name, List<string> events)
            {
                _name = name;
                _events = events;
            }

            public void Pause()
            {
                _events.Add($"{_name}:pause");
            }

            public void Seek(double timeSeconds)
            {
                _events.Add($"{_name}:seek:{timeSeconds:F3}");
            }

            public void Resume(float gainRampSeconds)
            {
                _events.Add($"{_name}:resume:{gainRampSeconds:F3}");
            }
        }
    }
}
