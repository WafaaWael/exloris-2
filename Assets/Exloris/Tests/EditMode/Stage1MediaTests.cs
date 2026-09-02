using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Stage1;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage1MediaTests
    {
        [Test]
        public void ExactVoiceCaptionIntervalsAndSilentGaps()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var intro = context.Presenter.StartIntro(route, 10d);
            AssertCue(context, Stage1MediaPresenter.IntroCaption1Id, true, 10d);
            context.Presenter.Advance(route, intro.MediaTokenId, 13.744d); AssertCue(context, null, false, 13.744d);
            context.Presenter.Advance(route, intro.MediaTokenId, 13.894d); AssertCue(context, Stage1MediaPresenter.IntroCaption2Id, true, 13.894d);
            context.Presenter.Advance(route, intro.MediaTokenId, 15.554d); AssertCue(context, null, false, 15.554d);
            context.Presenter.Advance(route, intro.MediaTokenId, 15.704d); AssertCue(context, Stage1MediaPresenter.IntroCaption3Id, true, 15.704d);

            var attach = context.Presenter.StartAttachCue(route, 30d);
            AssertCue(context, Stage1MediaPresenter.AttachCaption1Id, true, 30d);
            context.Presenter.Advance(route, attach.MediaTokenId, 33.966d); AssertCue(context, null, false, 33.966d);

            var outside = context.Presenter.StartOutside(route, 40d);
            AssertCue(context, Stage1MediaPresenter.OutsideCaption1Id, true, 40d);
            context.Presenter.Advance(route, outside.MediaTokenId, 43.249d); AssertCue(context, null, false, 43.249d);
            context.Presenter.Advance(route, outside.MediaTokenId, 43.399d); AssertCue(context, Stage1MediaPresenter.OutsideCaption2Id, true, 43.399d);
            context.Presenter.Advance(route, outside.MediaTokenId, 45.983d); AssertCue(context, null, false, 45.983d);
            context.Presenter.Advance(route, outside.MediaTokenId, 46.133d); AssertCue(context, Stage1MediaPresenter.OutsideCaption3Id, true, 46.133d);
            Assert.That(context.Backend.Ducks.Any(value => value.MusicDb == -6f), Is.True);
        }

        [Test]
        public void MutedStartsNoVoiceLoopOrSemanticSfxAndKeepsVisualMeaning()
        {
            var context = Create(false, true); var route = Guid.NewGuid();
            var handle = context.Presenter.StartIntro(route, 0d);
            Assert.That(handle.VoiceTokenId, Is.Null);
            Assert.That(handle.MusicTokenId, Is.Null);
            Assert.That(handle.AmbienceTokenId, Is.Null);
            Assert.That(context.Backend.Started, Is.Empty);
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().PresentationSprites[2]));
            Assert.That(context.Sink.Last.CaptionVisible, Is.True);
            Assert.That(context.Presenter.PlayAttachSfx(route, Guid.NewGuid(), 1d), Is.False);
            Assert.That(context.Presenter.PlayStepSfx(route, Guid.NewGuid(), 1d), Is.False);
            Assert.That(context.Backend.Started, Is.Empty);
        }

        [Test]
        public void RepeatReplacesTokensWithoutDuplicateMusicOrAmbienceOwners()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var first = context.Presenter.StartIntro(route, 1d);
            var second = context.Presenter.Repeat(route, 2d);
            Assert.That(second.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId));
            Assert.That(second.VoiceTokenId, Is.Not.EqualTo(first.VoiceTokenId));
            Assert.That(context.Backend.Started.Count(item => item.semantic == Stage1MediaPresenter.MusicId), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Count(item => item.semantic == Stage1MediaPresenter.AmbienceId), Is.EqualTo(1));
            Assert.That(context.Presenter.Advance(route, first.MediaTokenId, 3d), Is.False);
        }

        [Test]
        public void AttachAndStepSfxMapOnlyAcceptedUniqueActions()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            context.Presenter.StartIntro(route, 0d);
            var attach = Guid.NewGuid(); var step = Guid.NewGuid();
            Assert.That(context.Presenter.PlayAttachSfx(route, attach, 1d), Is.True);
            Assert.That(context.Presenter.PlayAttachSfx(route, attach, 1d), Is.False);
            Assert.That(context.Presenter.PlayStepSfx(route, step, 1.1d), Is.True);
            Assert.That(context.Presenter.PlayStepSfx(route, step, 1.1d), Is.False);
            Assert.That(context.Presenter.PlayStepSfx(Guid.NewGuid(), Guid.NewGuid(), 1.2d), Is.False);
            Assert.That(context.Backend.Started.Count(item => item.semantic == Stage1MediaPresenter.AttachSfxId), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Count(item => item.semantic == Stage1MediaPresenter.StepSfxId), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Any(item => item.semantic.IndexOf("correct", StringComparison.OrdinalIgnoreCase) >= 0
                || item.semantic.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void PauseAndMappedResumeReanchorVoiceCaptionAndSfxToExactRemainingTime()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var handle = context.Presenter.StartIntro(route, 10d);
            Assert.That(context.Presenter.PlayStepSfx(route, Guid.NewGuid(), 11d), Is.True);
            Assert.That(context.Presenter.Advance(route, handle.MediaTokenId, 13d), Is.True);
            Assert.That(context.Presenter.Pause(route, handle.MediaTokenId, 13d, Stage1PauseReason.AudioFocusLoss), Is.True);
            Assert.That(context.Sink.FrozenSeconds, Is.EqualTo(3d));
            Assert.That(context.Sink.Locked, Is.True);
            Assert.That(context.Presenter.Advance(route, handle.MediaTokenId, 14d), Is.False);
            var resumed = context.Presenter.ResumeAfterMappedUserAction(route, 100d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(handle.MediaTokenId));
            Assert.That(context.Presenter.ElapsedSeconds, Is.EqualTo(3d));
            Assert.That(context.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(97d).Within(0.100d));
            Assert.That(context.Backend.ResumeCount, Is.EqualTo(1));
        }

        [Test]
        public void SoundRouteChangeStopsAndRestartsOnlyCurrentSemanticOwners()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            context.Presenter.StartOutside(route, 0d);
            context.Session.SoundEnabled = false;
            context.Presenter.RefreshSettings(1d);
            Assert.That(context.Presenter.HasActiveVoice, Is.False);
            Assert.That(context.Presenter.HasActiveLoops, Is.False);
            context.Session.SoundEnabled = true;
            context.Presenter.RefreshSettings(2d);
            Assert.That(context.Presenter.HasActiveVoice, Is.True);
            Assert.That(context.Presenter.HasActiveLoops, Is.True);
            Assert.That(context.Backend.Started.Count(item => item.semantic == Stage1MediaPresenter.OutsideVoiceId), Is.EqualTo(2));
        }

        [Test]
        public void SilencedNarrationAndCaptionDoNotReturnOnTheNextHostAdvance()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var handle = context.Presenter.StartIntro(route, 10d);
            Assert.That(context.Presenter.SilenceNarration(route), Is.True);
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Presenter.Advance(route, handle.MediaTokenId, 10.1d), Is.True);
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Sink.Last.CaptionId, Is.Null);
            Assert.That(context.Presenter.HasActiveVoice, Is.False);
        }

        private static void AssertCue(Context context, string captionId, bool visible, double start)
        {
            Assert.That(context.Sink.Last.CaptionId, Is.EqualTo(captionId));
            Assert.That(context.Sink.Last.CaptionVisible, Is.EqualTo(visible));
            Assert.That(context.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(start).Within(0.100d));
        }

        private static Stage1Content Content() => AssetDatabase.LoadAssetAtPath<Stage1Content>(Stage1ContentGenerator.ContentAssetPath);

        private static Context Create(bool sound, bool captions)
        {
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(1));
            var session = new CoreSessionState { SoundEnabled = sound }; session.Begin(RouteId.Stage1);
            var accessibility = new AccessibilityState(false, captions); accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var sink = new Sink();
            return new Context(backend, session, sink,
                new Stage1MediaPresenter(director, backend, Content(), session, accessibility, sink, Ids(2)));
        }

        private static Func<Guid> Ids(byte prefix)
        {
            var value = 0;
            return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); };
        }

        private sealed class Context
        {
            internal Context(Backend backend, CoreSessionState session, Sink sink, Stage1MediaPresenter presenter)
            { Backend = backend; Session = session; Sink = sink; Presenter = presenter; }
            internal Backend Backend { get; }
            internal CoreSessionState Session { get; }
            internal Sink Sink { get; }
            internal Stage1MediaPresenter Presenter { get; }
        }

        private sealed class Sink : IStage1MediaCueSink
        {
            internal Stage1MediaCue Last { get; private set; }
            internal double FrozenSeconds { get; private set; }
            internal bool Locked { get; private set; }
            public void PresentMediaCue(Stage1MediaCue cue) => Last = cue;
            public void FreezeMediaCue(double elapsedSeconds) => FrozenSeconds = elapsedSeconds;
            public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCallerInputLocked(bool locked) => Locked = locked;
            public void ShowPauseOverlay(Stage1PauseReason reason) { }
        }

        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered = new HashSet<string>();
            internal readonly List<(Guid id, string semantic, AudioBus bus)> Started = new List<(Guid, string, AudioBus)>();
            internal readonly List<AudioDuckState> Ducks = new List<AudioDuckState>();
            internal int ResumeCount { get; private set; }
            public bool IsClipRegistered(string id) => _registered.Contains(id);
            public void RegisterClip(string id, AudioClip clip, bool loop = false) => _registered.Add(id);
            public void Start(AudioPlaybackToken token, AudioBus bus) => Started.Add((token.Id, token.SemanticId, bus));
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) => ResumeCount++;
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) => Ducks.Add(state);
        }
    }
}
