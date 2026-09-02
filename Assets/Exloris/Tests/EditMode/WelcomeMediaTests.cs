using System;
using System.Collections.Generic;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class WelcomeMediaTests
    {
        [Test]
        public void WelcomeStartsApprovedLoopsVoiceCaptionAndVisualCueOnOneClock()
        {
            var backend = new FakeBackend();
            var director = new AudioDirector(backend, SequentialIds());
            var sink = new FakeCueSink();
            var presenter = new WelcomeMediaPresenter(director, backend, Content(), sink);

            var token = presenter.Present(WelcomePresentationState.Welcome, true, true, 10d);

            Assert.That(token, Is.Not.Null);
            Assert.That(token.SemanticId, Is.EqualTo(WelcomeMediaPresenter.WelcomeVoiceId));
            Assert.That(backend.StartedSemantics, Is.EquivalentTo(new[]
            {
                WelcomeMediaPresenter.MusicId,
                WelcomeMediaPresenter.AmbienceId,
                WelcomeMediaPresenter.WelcomeVoiceId,
            }));
            Assert.That(sink.LastState, Is.EqualTo(WelcomePresentationState.Welcome));
            Assert.That(sink.LastVisual, Is.SameAs(Content().WelcomeVisualEquivalent));
            Assert.That(sink.LastCaptionId, Is.EqualTo(WelcomeMediaPresenter.WelcomeCaption1Id));
            Assert.That(Math.Abs(presenter.LastVoiceStartSeconds.Value - sink.LastVisualStartSeconds), Is.LessThanOrEqualTo(0.100d));
            Assert.That(Math.Abs(presenter.LastVoiceStartSeconds.Value - sink.LastCaptionStartSeconds), Is.LessThanOrEqualTo(0.100d));
        }

        [Test]
        public void MutedPresentationStartsNoAudioButKeepsNonReadingVisualEquivalent()
        {
            var backend = new FakeBackend();
            var sink = new FakeCueSink();
            var presenter = new WelcomeMediaPresenter(
                new AudioDirector(backend, SequentialIds()),
                backend,
                Content(),
                sink);

            var token = presenter.Present(WelcomePresentationState.Welcome, false, false, 20d);

            Assert.That(token, Is.Null);
            Assert.That(backend.StartedSemantics, Is.Empty);
            Assert.That(sink.LastVisual, Is.SameAs(Content().WelcomeVisualEquivalent));
            Assert.That(sink.CaptionsVisible, Is.False);
        }

        [Test]
        public void StateChangeInvalidatesOldVoiceAndStaleCompletionCannotReleaseNewVoice()
        {
            var backend = new FakeBackend();
            var director = new AudioDirector(backend, SequentialIds());
            var sink = new FakeCueSink();
            var presenter = new WelcomeMediaPresenter(director, backend, Content(), sink);
            var oldVoice = presenter.Present(WelcomePresentationState.Welcome, true, true, 30d);

            var currentVoice = presenter.Present(WelcomePresentationState.SessionChoice, true, true, 31d);

            Assert.That(currentVoice.SemanticId, Is.EqualTo(WelcomeMediaPresenter.SessionChoiceVoiceId));
            Assert.That(backend.StoppedIds, Does.Contain(oldVoice.Id));
            Assert.That(presenter.CompleteVoice(oldVoice.Id), Is.False);
            Assert.That(director.ActiveVoiceCount, Is.EqualTo(1));
            Assert.That(presenter.CompleteVoice(currentVoice.Id), Is.True);
            Assert.That(presenter.CompleteVoice(currentVoice.Id), Is.False);
            Assert.That(director.ActiveVoiceCount, Is.Zero);
        }

        [Test]
        public void InterruptionInvalidatesOldVoiceAndMappedResumeResynchronizesWithNewToken()
        {
            var backend = new FakeBackend();
            var sink = new FakeCueSink();
            var presenter = new WelcomeMediaPresenter(
                new AudioDirector(backend, SequentialIds()),
                backend,
                Content(),
                sink);
            var voice = presenter.Present(WelcomePresentationState.Welcome, true, true, 40d);

            Assert.That(presenter.Interrupt(), Is.True);
            Assert.That(backend.PauseCount, Is.EqualTo(1));
            Assert.That(presenter.CompleteVoice(voice.Id), Is.False);
            Assert.That(presenter.ResumeAfterMappedUserAction(41d), Is.True);
            Assert.That(backend.ResumeCount, Is.EqualTo(1));
            Assert.That(presenter.CurrentVoiceTokenId, Is.Not.Null);
            Assert.That(presenter.CurrentVoiceTokenId, Is.Not.EqualTo(voice.Id));
            Assert.That(presenter.CompleteVoice(voice.Id), Is.False);
            Assert.That(sink.LastVisualStartSeconds, Is.EqualTo(41d));
            Assert.That(sink.LastCaptionStartSeconds, Is.EqualTo(41d));
        }

        private static WelcomeContent Content()
        {
            return AssetDatabase.LoadAssetAtPath<WelcomeContent>(
                "Assets/Exloris/Resources/Content/WelcomeContent.asset");
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

        private sealed class FakeCueSink : IWelcomeMediaCueSink
        {
            public WelcomePresentationState LastState { get; private set; }
            public Sprite LastVisual { get; private set; }
            public string LastCaptionId { get; private set; }
            public double LastVisualStartSeconds { get; private set; }
            public double LastCaptionStartSeconds { get; private set; }
            public bool CaptionsVisible { get; private set; }

            public void ShowMediaCue(
                WelcomePresentationState state,
                Sprite visual,
                string captionId,
                bool captionsVisible,
                double startedAtSeconds)
            {
                LastState = state;
                LastVisual = visual;
                LastCaptionId = captionId;
                CaptionsVisible = captionsVisible;
                LastVisualStartSeconds = startedAtSeconds;
                LastCaptionStartSeconds = startedAtSeconds;
            }

            public void PauseMediaCue() { }
            public void ResumeMediaCue() { }

            public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
            {
                LastCaptionId = captionId;
                CaptionsVisible = visible;
                LastCaptionStartSeconds = cueStartedAtSeconds;
            }
        }

        private sealed class FakeBackend : IAudioBackend, IAudioClipRegistry
        {
            public readonly List<string> StartedSemantics = new List<string>();
            public readonly List<Guid> StoppedIds = new List<Guid>();
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void RegisterClip(string semanticId, AudioClip clip, bool loop = false) { }
            public void Start(AudioPlaybackToken token, AudioBus bus) => StartedSemantics.Add(token.SemanticId);
            public void Stop(Guid tokenId) => StoppedIds.Add(tokenId);
            public void PauseAllImmediately() => PauseCount++;
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) => ResumeCount++;
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
    }
}
