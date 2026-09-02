using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Stage2;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage2MediaTests
    {
        [Test]
        public void IntroAndHelpUseExactCaptionsVisualEquivalentsAndSilentGaps()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var intro = context.Presenter.StartIntro(route, 10d);
            AssertCue(context, Stage2MediaPresenter.IntroCaption1Id, true, 10d, 2);
            context.Presenter.Advance(route, intro.MediaTokenId, 12.754d); AssertCue(context, null, false, 12.754d, 2);
            context.Presenter.Advance(route, intro.MediaTokenId, 12.904d); AssertCue(context, Stage2MediaPresenter.IntroCaption2Id, true, 12.904d, 2);
            var help = context.Presenter.StartHelp(route, 20d);
            AssertCue(context, Stage2MediaPresenter.HelpCaption1Id, true, 20d, 3);
            context.Presenter.Advance(route, help.MediaTokenId, 22.437d); AssertCue(context, null, false, 22.437d, 3);
            context.Presenter.Advance(route, help.MediaTokenId, 22.587d); AssertCue(context, Stage2MediaPresenter.HelpCaption2Id, true, 22.587d, 3);
        }

        [Test]
        public void MutedKeepsCaptionsAndVisualMeaningWithoutStartingAudio()
        {
            var context = Create(false, true); var route = Guid.NewGuid();
            var handle = context.Presenter.StartIntro(route, 0d);
            Assert.That(handle.VoiceTokenId, Is.Null);
            Assert.That(handle.MusicTokenId, Is.Null);
            Assert.That(handle.AmbienceTokenId, Is.Null);
            Assert.That(context.Backend.Started, Is.Empty);
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().PresentationSprites[2]));
            Assert.That(context.Sink.Last.CaptionVisible, Is.True);
            Assert.That(context.Presenter.PlayStoneSfx(route, Stage2River.Low, Guid.NewGuid(), 1d), Is.False);
        }

        [Test]
        public void EachStoneHasOneNeutralSfxAndDuplicateActionsAreInert()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            context.Presenter.StartIntro(route, 0d);
            foreach (var river in new[] { Stage2River.Low, Stage2River.Middle, Stage2River.High })
            {
                var action = Guid.NewGuid();
                Assert.That(context.Presenter.PlayStoneSfx(route, river, action, 1d), Is.True);
                Assert.That(context.Presenter.PlayStoneSfx(route, river, action, 1d), Is.False);
            }
            Assert.That(context.Backend.Started.Count(item => item.Bus == AudioBus.Sfx), Is.EqualTo(3));
            Assert.That(context.Backend.Started.Any(item =>
                item.Semantic.IndexOf("correct", StringComparison.OrdinalIgnoreCase) >= 0
                || item.Semantic.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void RepeatPauseResumeAndSoundChangesKeepOnlyCurrentOwners()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var first = context.Presenter.StartIntro(route, 10d);
            var second = context.Presenter.Repeat(route, 11d);
            Assert.That(second.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId));
            Assert.That(context.Backend.Started.Count(item => item.Semantic == Stage2MediaPresenter.MusicId), Is.EqualTo(1));
            context.Presenter.Advance(route, second.MediaTokenId, 13d);
            Assert.That(context.Presenter.Pause(route, second.MediaTokenId, 13d, Stage2PauseReason.AudioFocusLoss), Is.True);
            Assert.That(context.Sink.Locked, Is.True);
            var resumed = context.Presenter.ResumeAfterMappedUserAction(route, 100d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(second.MediaTokenId));
            Assert.That(context.Presenter.ElapsedSeconds, Is.EqualTo(2d));
            context.Session.SoundEnabled = false; context.Presenter.RefreshSettings(101d);
            Assert.That(context.Presenter.HasActiveVoice, Is.False);
            Assert.That(context.Presenter.HasActiveLoops, Is.False);
        }

        private static void AssertCue(Context context, string id, bool visible, double start, int visualIndex)
        {
            Assert.That(context.Sink.Last.CaptionId, Is.EqualTo(id));
            Assert.That(context.Sink.Last.CaptionVisible, Is.EqualTo(visible));
            Assert.That(context.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(start).Within(0.001d));
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().PresentationSprites[visualIndex]));
            Assert.That(context.Sink.Last.RequiresReading, Is.False);
        }

        private static Stage2Content Content() => AssetDatabase.LoadAssetAtPath<Stage2Content>(Stage2ContentGenerator.ContentAssetPath);

        private static Context Create(bool sound, bool captions)
        {
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(1));
            var session = new CoreSessionState { SoundEnabled = sound }; session.Begin(RouteId.Stage2);
            var accessibility = new AccessibilityState(false, captions); accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var sink = new Sink();
            return new Context(backend, session, sink,
                new Stage2MediaPresenter(director, backend, Content(), session, accessibility, sink, Ids(2)));
        }

        private static Func<Guid> Ids(byte prefix)
        {
            var value = 0;
            return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); };
        }

        private sealed class Context
        {
            internal Context(Backend backend, CoreSessionState session, Sink sink, Stage2MediaPresenter presenter)
            { Backend = backend; Session = session; Sink = sink; Presenter = presenter; }
            internal Backend Backend { get; }
            internal CoreSessionState Session { get; }
            internal Sink Sink { get; }
            internal Stage2MediaPresenter Presenter { get; }
        }

        private sealed class Sink : IStage2MediaCueSink
        {
            internal Stage2MediaCue Last { get; private set; }
            internal bool Locked { get; private set; }
            public void PresentMediaCue(Stage2MediaCue cue) => Last = cue;
            public void FreezeMediaCue(double elapsedSeconds) { }
            public void ResumeMediaCue(Guid mediaTokenId, double elapsedSeconds) { }
            public void SetCallerInputLocked(bool locked) => Locked = locked;
            public void ShowPauseOverlay(Stage2PauseReason reason) { }
        }

        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered = new HashSet<string>();
            internal readonly List<StartedItem> Started = new List<StartedItem>();
            public bool IsClipRegistered(string id) => _registered.Contains(id);
            public void RegisterClip(string id, AudioClip clip, bool loop = false) => _registered.Add(id);
            public void Start(AudioPlaybackToken token, AudioBus bus) => Started.Add(new StartedItem(token.SemanticId, bus));
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) { }
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }

        private readonly struct StartedItem
        {
            internal StartedItem(string semantic, AudioBus bus) { Semantic = semantic; Bus = bus; }
            internal string Semantic { get; }
            internal AudioBus Bus { get; }
        }
    }
}
