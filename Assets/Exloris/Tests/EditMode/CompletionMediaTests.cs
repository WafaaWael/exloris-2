using System;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Completion;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class CompletionMediaTests
    {
        [Test]
        public void AudibleStartOwnsOneVoiceAndTwoExactLoopsWithCaptionAndVisualEquivalent()
        {
            var context = Create(sound: true, captions: true);
            var token = Guid.NewGuid();
            var handle = context.Presenter.Start(token, 10d);

            Assert.That(handle.VoiceTokenId.HasValue, Is.True);
            Assert.That(handle.MusicTokenId.HasValue, Is.True);
            Assert.That(handle.AmbienceTokenId.HasValue, Is.True);
            Assert.That(context.Registry.Loops[CompletionMediaPresenter.VoiceId], Is.False);
            Assert.That(context.Registry.Loops[CompletionMediaPresenter.MusicId], Is.True);
            Assert.That(context.Registry.Loops[CompletionMediaPresenter.AmbienceId], Is.True);
            Assert.That(context.Sink.Cue.CaptionId, Is.EqualTo("VO-COMPLETION-C01"));
            Assert.That(context.Sink.Cue.CaptionText, Is.EqualTo("You explored the whole Nile journey."));
            Assert.That(context.Sink.Cue.CaptionVisible, Is.True);
            Assert.That(context.Sink.Cue.VisualEquivalent, Is.SameAs(context.Content.CompletionVisualEquivalent));
        }

        [Test]
        public void MutedStartCreatesNoAudioButKeepsTheNonReadingVisualEquivalent()
        {
            var context = Create(sound: false, captions: false);
            var handle = context.Presenter.Start(Guid.NewGuid(), 1d);

            Assert.That(handle.VoiceTokenId, Is.Null);
            Assert.That(handle.MusicTokenId, Is.Null);
            Assert.That(handle.AmbienceTokenId, Is.Null);
            Assert.That(context.Backend.Started, Is.Empty);
            Assert.That(context.Sink.Cue.VisualEquivalent, Is.Not.Null);
            Assert.That(context.Sink.Cue.CaptionVisible, Is.False);
        }

        [Test]
        public void CaptionAndSemanticCueEndAtExact3646MillisecondsWithoutUnlockAuthority()
        {
            var context = Create(sound: false, captions: true);
            var token = Guid.NewGuid();
            var handle = context.Presenter.Start(token, 10d);
            Assert.That(context.Presenter.Advance(token, handle.MediaTokenId, 13.645999d), Is.True);
            Assert.That(context.Sink.Cue.CaptionVisible, Is.True);
            Assert.That(context.Presenter.Advance(token, handle.MediaTokenId, 13.646d), Is.True);
            Assert.That(context.Sink.Cue.CaptionVisible, Is.False);
            Assert.That(context.Sink.Cue.ElapsedSeconds, Is.EqualTo(3.646d));
        }

        [Test]
        public void InterruptionFreezesThenMappedResumeIssuesFreshTokenAndRejectsStaleCallbacks()
        {
            var context = Create(sound: true, captions: true);
            var completion = Guid.NewGuid();
            var first = context.Presenter.Start(completion, 20d);
            Assert.That(context.Presenter.Pause(completion, first.MediaTokenId, 21d), Is.True);
            Assert.That(context.Backend.PauseCount, Is.EqualTo(1));
            Assert.That(context.Presenter.HandleFocusRestored(), Is.False);
            var resumed = context.Presenter.ResumeAfterMappedUserAction(completion, 30d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId));
            Assert.That(context.Presenter.Advance(completion, first.MediaTokenId, 31d), Is.False);
            Assert.That(context.Backend.ResumeCount, Is.EqualTo(1));
        }

        private static Context Create(bool sound, bool captions)
        {
            var backend = new Backend();
            var ids = new Queue<Guid>();
            for (var index = 1; index <= 20; index++)
                ids.Enqueue(Guid.Parse($"61000000-0000-0000-0000-{index:000000000000}"));
            var director = new AudioDirector(backend, () => ids.Dequeue());
            var registry = new Registry();
            var content = AssetDatabase.LoadAssetAtPath<CompletionAdultReportContent>(
                "Assets/Exloris/Resources/Content/CompletionAdultReportContent.asset");
            var session = new Exloris.Core.Session.SessionState { SoundEnabled = sound };
            session.Begin(Exloris.Core.Navigation.RouteId.Completion);
            var accessibility = new AccessibilityState(alwaysReduce: false, captionsEnabled: captions);
            var sink = new Sink();
            var presenter = new CompletionMediaPresenter(
                director, registry, content, session, accessibility, sink, () => ids.Dequeue());
            return new Context(backend, registry, content, sink, presenter);
        }

        private sealed class Context
        {
            public Context(Backend backend, Registry registry, CompletionAdultReportContent content,
                Sink sink, CompletionMediaPresenter presenter)
            { Backend = backend; Registry = registry; Content = content; Sink = sink; Presenter = presenter; }
            public Backend Backend { get; }
            public Registry Registry { get; }
            public CompletionAdultReportContent Content { get; }
            public Sink Sink { get; }
            public CompletionMediaPresenter Presenter { get; }
        }

        private sealed class Registry : IAudioClipRegistry, IAudioClipRegistryLookup
        {
            public readonly Dictionary<string, bool> Loops = new Dictionary<string, bool>();
            public void RegisterClip(string id, AudioClip clip, bool loop = false) { Assert.That(clip, Is.Not.Null); Loops[id] = loop; }
            public bool IsClipRegistered(string id) => Loops.ContainsKey(id);
        }

        private sealed class Backend : IAudioBackend
        {
            public readonly List<Guid> Started = new List<Guid>();
            public int PauseCount;
            public int ResumeCount;
            public void Start(AudioPlaybackToken token, AudioBus bus) { Started.Add(token.Id); }
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { PauseCount++; }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) { ResumeCount++; }
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }

        private sealed class Sink : ICompletionMediaCueSink
        {
            public CompletionMediaCue Cue;
            public void PresentCompletionMedia(CompletionMediaCue cue) { Cue = cue; }
            public void FreezeCompletionMedia(double elapsedSeconds) { }
            public void ResumeCompletionMedia(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCompletionInputLocked(bool locked) { }
        }
    }
}
