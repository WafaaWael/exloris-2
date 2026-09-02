using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Overview;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class JourneyOverviewMediaTests
    {
        private const string ContentPath =
            "Assets/Exloris/Resources/Content/JourneyOverviewContent.asset";

        [Test]
        public void ExactCaptionIntervalsPreserveBothSilentGapsAndOrderedVisual()
        {
            var context = CreateContext(sound: true, captions: true);
            var reveal = Guid.NewGuid();
            var handle = context.Presenter.Start(reveal, 10d);

            AssertCue(context.Sink.Last, JourneyOverviewMediaPresenter.CaptionMeasureId, true, 256f, 10d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 13.138999d), Is.True);
            AssertCue(context.Sink.Last, JourneyOverviewMediaPresenter.CaptionMeasureId, true, 256f, 10d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 13.139d), Is.True);
            AssertCue(context.Sink.Last, null, false, 256f, 13.139d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 13.289d), Is.True);
            AssertCue(context.Sink.Last, JourneyOverviewMediaPresenter.CaptionReadId, true, 622f, 13.289d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 16.032d), Is.True);
            AssertCue(context.Sink.Last, null, false, 622f, 16.032d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 16.182d), Is.True);
            AssertCue(context.Sink.Last, JourneyOverviewMediaPresenter.CaptionHelpId, true, 1024f, 16.182d);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 18.999d), Is.True);
            Assert.That(context.Presenter.ElapsedSeconds, Is.EqualTo(8.999d).Within(0.000001d));
        }

        [Test]
        public void MutedAndCaptionsOffKeepIdenticalOrderedVisualWithoutAnyAudioToken()
        {
            var context = CreateContext(sound: false, captions: false);
            var reveal = Guid.NewGuid();
            var handle = context.Presenter.Start(reveal, 20d);

            Assert.That(handle.VoiceTokenId, Is.Null);
            Assert.That(handle.MusicTokenId, Is.Null);
            Assert.That(handle.AmbienceTokenId, Is.Null);
            Assert.That(context.Backend.Started, Is.Empty);
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().OverviewVisualEquivalent));
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Presenter.Advance(reveal, handle.MediaTokenId, 23.289d), Is.True);
            Assert.That(context.Sink.Last.VisualEquivalentMaskPixels, Is.EqualTo(622f));
            Assert.That(context.Backend.Started, Is.Empty);
        }

        [Test]
        public void RepeatReplacesVoiceAndMediaTokensWithoutDuplicatingSharedLoops()
        {
            var context = CreateContext(sound: true, captions: true);
            var firstReveal = Guid.NewGuid();
            var first = context.Presenter.Start(firstReveal, 30d);
            var secondReveal = Guid.NewGuid();

            var second = context.Presenter.Start(secondReveal, 31d);

            Assert.That(second.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId));
            Assert.That(second.VoiceTokenId, Is.Not.EqualTo(first.VoiceTokenId));
            Assert.That(context.Backend.Stopped, Does.Contain(first.VoiceTokenId.Value));
            Assert.That(context.Backend.Started.Count(item => item.semantic == JourneyOverviewMediaPresenter.MusicId), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Count(item => item.semantic == JourneyOverviewMediaPresenter.AmbienceId), Is.EqualTo(1));
            Assert.That(context.Presenter.Advance(firstReveal, first.MediaTokenId, 32d), Is.False);
            Assert.That(context.Presenter.CompleteVoice(firstReveal, first.MediaTokenId,
                first.VoiceTokenId.Value), Is.False);
        }

        [Test]
        public void PauseFreezesCueAndMappedResumeUsesExactElapsedWithFreshMediaToken()
        {
            var context = CreateContext(sound: true, captions: true);
            var reveal = Guid.NewGuid();
            var original = context.Presenter.Start(reveal, 40d);
            context.Presenter.Advance(reveal, original.MediaTokenId, 44.5d);

            Assert.That(context.Presenter.Pause(reveal, original.MediaTokenId, 44.5d,
                JourneyOverviewPauseReason.AppBackground), Is.True);
            Assert.That(context.Backend.PauseCount, Is.EqualTo(1));
            Assert.That(context.Sink.InputLocked, Is.True);
            Assert.That(context.Sink.PauseReasons,
                Is.EqualTo(new[] { JourneyOverviewPauseReason.AppBackground }));
            Assert.That(context.Presenter.Advance(reveal, original.MediaTokenId, 45d), Is.False);
            Assert.That(context.Presenter.HandleFocusRestored(), Is.False);
            Assert.That(context.Backend.ResumeCount, Is.Zero);

            var resumed = context.Presenter.ResumeAfterMappedUserAction(reveal, 100d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(original.MediaTokenId));
            Assert.That(resumed.Value.VoiceTokenId, Is.EqualTo(original.VoiceTokenId));
            Assert.That(context.Presenter.ElapsedSeconds, Is.EqualTo(4.5d).Within(0.0001d));
            Assert.That(context.Sink.Last.CaptionId, Is.EqualTo(JourneyOverviewMediaPresenter.CaptionReadId));
            Assert.That(context.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(98.789d).Within(0.0001d));
            Assert.That(context.Backend.ResumeCount, Is.EqualTo(1));
            Assert.That(context.Presenter.CompleteVoice(reveal, original.MediaTokenId,
                original.VoiceTokenId.Value), Is.False);
            Assert.That(context.Presenter.CompleteVoice(reveal, resumed.Value.MediaTokenId,
                resumed.Value.VoiceTokenId.Value), Is.True);
        }

        [Test]
        public void AudioFocusLossUsesSamePauseOverlayAndNeverEmitsNegativeSfx()
        {
            var context = CreateContext(sound: true, captions: true);
            var reveal = Guid.NewGuid();
            var handle = context.Presenter.Start(reveal, 50d);

            Assert.That(context.Presenter.Pause(reveal, handle.MediaTokenId, 51d,
                JourneyOverviewPauseReason.AudioFocusLoss), Is.True);

            Assert.That(context.Sink.PauseReasons,
                Is.EqualTo(new[] { JourneyOverviewPauseReason.AudioFocusLoss }));
            Assert.That(context.Backend.Started.All(item => item.bus != AudioBus.Sfx), Is.True);
            Assert.That(context.Sink.Last.UsesNegativeSfx, Is.False);
            Assert.That(context.Sink.Last.RequiresReading, Is.False);
        }

        private static TestContext CreateContext(bool sound, bool captions)
        {
            var backend = new FakeBackend();
            var director = new AudioDirector(backend, SequentialIds(80));
            var session = new Exloris.Core.Session.SessionState { SoundEnabled = sound };
            session.Begin(Exloris.Core.Navigation.RouteId.JourneyOverview);
            var accessibility = new AccessibilityState(false, captions);
            accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var sink = new RecordingMediaSink();
            var presenter = new JourneyOverviewMediaPresenter(
                director, backend, Content(), session, accessibility, sink, SequentialIds(90));
            return new TestContext(backend, sink, presenter);
        }

        private static JourneyOverviewContent Content()
        {
            var content = AssetDatabase.LoadAssetAtPath<JourneyOverviewContent>(ContentPath);
            Assert.That(content, Is.Not.Null);
            return content;
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

        private static void AssertCue(JourneyOverviewMediaCue cue, string caption,
            bool visible, float maskPixels, double start)
        {
            Assert.That(cue.CaptionId, Is.EqualTo(caption));
            Assert.That(cue.CaptionVisible, Is.EqualTo(visible));
            Assert.That(cue.VisualEquivalentMaskPixels, Is.EqualTo(maskPixels).Within(0.001f));
            Assert.That(cue.CaptionCueStartSeconds, Is.EqualTo(start).Within(AudioPolicy.CaptionToleranceSeconds));
        }

        private sealed class TestContext
        {
            public TestContext(FakeBackend backend, RecordingMediaSink sink,
                JourneyOverviewMediaPresenter presenter)
            {
                Backend = backend;
                Sink = sink;
                Presenter = presenter;
            }

            public FakeBackend Backend { get; }
            public RecordingMediaSink Sink { get; }
            public JourneyOverviewMediaPresenter Presenter { get; }
        }

        private sealed class RecordingMediaSink : IJourneyOverviewMediaCueSink
        {
            public List<JourneyOverviewMediaCue> Cues { get; } =
                new List<JourneyOverviewMediaCue>();
            public List<JourneyOverviewPauseReason> PauseReasons { get; } =
                new List<JourneyOverviewPauseReason>();
            public JourneyOverviewMediaCue Last => Cues[Cues.Count - 1];
            public bool InputLocked { get; private set; }

            public void PresentMediaCue(JourneyOverviewMediaCue cue) => Cues.Add(cue);
            public void FreezeMediaCue(double elapsedSeconds) { }
            public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCallerInputLocked(bool locked) => InputLocked = locked;
            public void ShowPauseOverlay(JourneyOverviewPauseReason reason) => PauseReasons.Add(reason);
        }

        private sealed class FakeBackend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered = new HashSet<string>(StringComparer.Ordinal);
            public List<(Guid id, string semantic, AudioBus bus)> Started { get; } =
                new List<(Guid, string, AudioBus)>();
            public List<Guid> Stopped { get; } = new List<Guid>();
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public bool IsClipRegistered(string semanticId) => _registered.Contains(semanticId);
            public void RegisterClip(string semanticId, AudioClip clip, bool loop = false)
            {
                if (!_registered.Add(semanticId))
                    throw new InvalidOperationException("duplicate registration: " + semanticId);
            }
            public void Start(AudioPlaybackToken token, AudioBus bus) =>
                Started.Add((token.Id, token.SemanticId, bus));
            public void Stop(Guid tokenId) => Stopped.Add(tokenId);
            public void PauseAllImmediately() => PauseCount++;
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) => ResumeCount++;
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
    }
}
