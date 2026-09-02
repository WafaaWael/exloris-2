using System;
using System.Collections.Generic;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarMediaTests
    {
        [TestCase(AvatarMediaState.Select, AvatarMediaPresenter.SelectVoiceId, AvatarMediaPresenter.SelectCaption1Id)]
        [TestCase(AvatarMediaState.Preview, AvatarMediaPresenter.PreviewVoiceId, AvatarMediaPresenter.PreviewCaption1Id)]
        [TestCase(AvatarMediaState.Leave, AvatarMediaPresenter.LeaveVoiceId, AvatarMediaPresenter.LeaveCaption1Id)]
        public void StateStartsVoiceCaptionAndVisualWithinOneHundredMilliseconds(
            AvatarMediaState state,
            string expectedVoice,
            string expectedCaption)
        {
            var context = CreateContext();

            var token = context.Presenter.Present(state, true, true, 10.0);

            Assert.That(token.SemanticId, Is.EqualTo(expectedVoice));
            Assert.That(context.Presenter.VoiceIsPlaying, Is.True);
            Assert.That(context.Sink.State, Is.EqualTo(state));
            Assert.That(context.Sink.CaptionId, Is.EqualTo(expectedCaption));
            Assert.That(context.Sink.Visual, Is.SameAs(ExpectedVisual(state)));
            Assert.That(Math.Abs(token.StartedAtSeconds - context.Sink.VisualStart), Is.LessThanOrEqualTo(0.100));
            Assert.That(Math.Abs(token.StartedAtSeconds - context.Sink.CaptionStart), Is.LessThanOrEqualTo(0.100));
            Assert.That(context.Backend.StartedSemantics, Is.EqualTo(new[] { expectedVoice }));
        }

        [TestCase(0.000, AvatarMediaPresenter.PreviewCaption1Id, true, 0.000)]
        [TestCase(2.956, AvatarMediaPresenter.PreviewCaption1Id, true, 0.000)]
        [TestCase(2.957, null, false, 2.957)]
        [TestCase(3.106, AvatarMediaPresenter.PreviewCaption2Id, true, 3.106)]
        [TestCase(5.710, AvatarMediaPresenter.PreviewCaption2Id, true, 3.106)]
        [TestCase(5.711, null, false, 5.711)]
        [TestCase(5.860, AvatarMediaPresenter.PreviewCaption3Id, true, 5.860)]
        [TestCase(8.666, AvatarMediaPresenter.PreviewCaption3Id, true, 5.860)]
        public void PreviewUsesExactThreeCueWindows(
            double elapsed,
            string captionId,
            bool visible,
            double cueStart)
        {
            var context = CreateContext();
            context.Presenter.Present(AvatarMediaState.Preview, true, true, 20.0);

            Assert.That(context.Presenter.Advance(20.0 + elapsed), Is.True);

            Assert.That(context.Sink.CaptionId, Is.EqualTo(captionId));
            Assert.That(context.Sink.CaptionVisible, Is.EqualTo(visible));
            Assert.That(context.Sink.CaptionStart, Is.EqualTo(20.0 + cueStart).Within(0.000001));
        }

        [TestCase(0.000, AvatarMediaPresenter.LeaveCaption1Id, true, 0.000)]
        [TestCase(2.832, AvatarMediaPresenter.LeaveCaption1Id, true, 0.000)]
        [TestCase(2.833, null, false, 2.833)]
        [TestCase(2.982, AvatarMediaPresenter.LeaveCaption2Id, true, 2.982)]
        [TestCase(7.557, AvatarMediaPresenter.LeaveCaption2Id, true, 2.982)]
        public void LeaveUsesExactTwoCueWindows(
            double elapsed,
            string captionId,
            bool visible,
            double cueStart)
        {
            var context = CreateContext();
            context.Presenter.Present(AvatarMediaState.Leave, true, true, 30.0);

            Assert.That(context.Presenter.Advance(30.0 + elapsed), Is.True);

            Assert.That(context.Sink.CaptionId, Is.EqualTo(captionId));
            Assert.That(context.Sink.CaptionVisible, Is.EqualTo(visible));
            Assert.That(context.Sink.CaptionStart, Is.EqualTo(30.0 + cueStart).Within(0.000001));
        }

        [Test]
        public void SelectCueEndsAtExactVoiceDuration()
        {
            var context = CreateContext();
            context.Presenter.Present(AvatarMediaState.Select, true, true, 40.0);

            context.Presenter.Advance(42.474);
            Assert.That(context.Sink.CaptionId, Is.EqualTo(AvatarMediaPresenter.SelectCaption1Id));
            Assert.That(context.Sink.CaptionVisible, Is.True);

            context.Presenter.Advance(42.475);
            Assert.That(context.Sink.CaptionId, Is.Null);
            Assert.That(context.Sink.CaptionVisible, Is.False);
        }

        [TestCase(AvatarMediaState.Select)]
        [TestCase(AvatarMediaState.Preview)]
        [TestCase(AvatarMediaState.Leave)]
        public void MutedModeKeepsNonReadingVisualAndStartsNoAudio(AvatarMediaState state)
        {
            var context = CreateContext();

            var token = context.Presenter.Present(state, false, false, 50.0);

            Assert.That(token, Is.Null);
            Assert.That(context.Presenter.VoiceIsPlaying, Is.False);
            Assert.That(context.Backend.StartedSemantics, Is.Empty);
            Assert.That(context.Sink.Visual, Is.SameAs(ExpectedVisual(state)));
            Assert.That(context.Sink.CaptionVisible, Is.False);
        }

        [Test]
        public void NewStateInvalidatesOldVoiceAndStaleCompletionCannotStopCurrentVoice()
        {
            var context = CreateContext();
            var oldVoice = context.Presenter.Present(AvatarMediaState.Select, true, true, 60.0);

            var currentVoice = context.Presenter.Present(AvatarMediaState.Preview, true, true, 61.0);

            Assert.That(context.Presenter.CompleteVoice(oldVoice.Id), Is.False);
            Assert.That(context.Presenter.CompleteVoice(currentVoice.Id), Is.True);
            Assert.That(context.Presenter.VoiceIsPlaying, Is.False);
            Assert.That(context.Presenter.CompleteVoice(currentVoice.Id), Is.False);
            Assert.That(context.Backend.StoppedIds, Does.Contain(oldVoice.Id));
        }

        [TestCase(AvatarSfxCue.Press, AvatarMediaPresenter.PressSfxId)]
        [TestCase(AvatarSfxCue.Confirm, AvatarMediaPresenter.ConfirmSfxId)]
        [TestCase(AvatarSfxCue.Back, AvatarMediaPresenter.BackSfxId)]
        public void SfxUsesExactTokenAndMutedModeSuppressesIt(
            AvatarSfxCue cue,
            string expectedSemantic)
        {
            var context = CreateContext();

            var token = context.Presenter.PlaySfx(cue, true, 70.0);
            var muted = context.Presenter.PlaySfx(cue, false, 70.1);

            Assert.That(token.SemanticId, Is.EqualTo(expectedSemantic));
            Assert.That(token.Bus, Is.EqualTo(AudioBus.Sfx));
            Assert.That(muted, Is.Null);
            Assert.That(context.Backend.StartedSemantics, Is.EqualTo(new[] { expectedSemantic }));
        }

        private static Sprite ExpectedVisual(AvatarMediaState state)
        {
            return Content().VisualEquivalents[(int)state];
        }

        private static TestContext CreateContext()
        {
            var backend = new FakeBackend();
            var sink = new FakeSink();
            var presenter = new AvatarMediaPresenter(
                new AudioDirector(backend, SequentialIds()),
                backend,
                Content(),
                sink);
            return new TestContext(backend, sink, presenter);
        }

        private static AvatarContent Content()
        {
            return AssetDatabase.LoadAssetAtPath<AvatarContent>(
                "Assets/Exloris/Resources/Content/AvatarContent.asset");
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

        private sealed class TestContext
        {
            public TestContext(FakeBackend backend, FakeSink sink, AvatarMediaPresenter presenter)
            {
                Backend = backend;
                Sink = sink;
                Presenter = presenter;
            }

            public FakeBackend Backend { get; }
            public FakeSink Sink { get; }
            public AvatarMediaPresenter Presenter { get; }
        }

        private sealed class FakeSink : IAvatarMediaCueSink
        {
            public AvatarMediaState State { get; private set; }
            public Sprite Visual { get; private set; }
            public string CaptionId { get; private set; }
            public bool CaptionVisible { get; private set; }
            public double VisualStart { get; private set; }
            public double CaptionStart { get; private set; }

            public void ShowMediaCue(AvatarMediaState state, Sprite visual, string captionId,
                bool captionsVisible, double startedAtSeconds)
            {
                State = state;
                Visual = visual;
                CaptionId = captionId;
                CaptionVisible = captionsVisible;
                VisualStart = startedAtSeconds;
                CaptionStart = startedAtSeconds;
            }

            public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
            {
                CaptionId = captionId;
                CaptionVisible = visible;
                CaptionStart = cueStartedAtSeconds;
            }
        }

        private sealed class FakeBackend : IAudioBackend, IAudioClipRegistry
        {
            public List<string> StartedSemantics { get; } = new List<string>();
            public List<Guid> StoppedIds { get; } = new List<Guid>();

            public void RegisterClip(string semanticId, AudioClip clip, bool loop = false) { }
            public void Start(AudioPlaybackToken token, AudioBus bus) => StartedSemantics.Add(token.SemanticId);
            public void Stop(Guid tokenId) => StoppedIds.Add(tokenId);
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) { }
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
    }
}
