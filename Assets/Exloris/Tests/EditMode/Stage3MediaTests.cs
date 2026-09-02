using System;
using System.Collections.Generic;
using System.Linq;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Editor;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreSessionState = Exloris.Core.Session.SessionState;

namespace Exloris.Tests.EditMode
{
    public sealed class Stage3MediaTests
    {
        [Test]
        public void IntroHelpAndOutcomeUseExactCueWindowsAndVisualOwners()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var intro = context.Presenter.StartIntro(route, 10d);
            AssertCue(context, Stage3MediaPresenter.IntroCaption1Id, true, 10d, 0);
            Assert.That(context.Presenter.Advance(route, intro.MediaTokenId, 13.772d), Is.True);
            AssertCue(context, null, false, 13.772d, 0);
            context.Presenter.Advance(route, intro.MediaTokenId, 13.922d);
            AssertCue(context, Stage3MediaPresenter.IntroCaption2Id, true, 13.922d, 0);

            var help = context.Presenter.StartHelp(route, 20d);
            AssertCue(context, Stage3MediaPresenter.IntroCaption1Id, true, 20d, 0);

            foreach (var plan in new[] { Stage3Plan.Grain, Stage3Plan.Channel, Stage3Plan.Boat })
            {
                var outcome = context.Presenter.StartOutcome(route, plan, 30d + (int)plan * 10d);
                AssertCue(context, Stage3MediaPresenter.OutcomeCaption1Id, true, 30d + (int)plan * 10d, (int)plan);
                var start = 30d + (int)plan * 10d;
                context.Presenter.Advance(route, outcome.MediaTokenId, start + 2.850d);
                AssertCue(context, null, false, start + 2.850d, (int)plan);
                context.Presenter.Advance(route, outcome.MediaTokenId, start + 3d);
                AssertCue(context, Stage3MediaPresenter.OutcomeCaption2Id, true, start + 3d, (int)plan);
            }
        }

        [Test]
        public void StageOwnedLoopsHaveExactDurationsAndNoJudgmentSfxOwnerExists()
        {
            var content = Content();
            Assert.That(content.Music.length, Is.EqualTo(48f).Within(0.02f));
            Assert.That(content.Ambience.length, Is.EqualTo(60f).Within(0.02f));
            var context = Create(true, true); var route = Guid.NewGuid();
            context.Presenter.StartIntro(route, 0d);
            Assert.That(context.Backend.Started.Count(item => item.Bus == AudioBus.Music), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Count(item => item.Bus == AudioBus.Ambience), Is.EqualTo(1));
            Assert.That(context.Backend.Started.Any(item => item.Bus == AudioBus.Sfx), Is.False);
            Assert.That(context.Backend.Started.Any(item =>
                item.Semantic.IndexOf("correct", StringComparison.OrdinalIgnoreCase) >= 0
                || item.Semantic.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
        }

        [Test]
        public void MutedAndCaptionsOffKeepVisualMeaningWithoutAudioOrReadingDependency()
        {
            var context = Create(false, false); var route = Guid.NewGuid();
            var handle = context.Presenter.StartOutcome(route, Stage3Plan.Channel, 0d);
            Assert.That(handle.VoiceTokenId, Is.Null);
            Assert.That(handle.MusicTokenId, Is.Null);
            Assert.That(handle.AmbienceTokenId, Is.Null);
            Assert.That(context.Backend.Started, Is.Empty);
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().VisualEquivalents[2]));
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Sink.Last.RequiresReading, Is.False);
        }

        [Test]
        public void RiverBackReadyModeKeepsLoopsWithoutReplayingVoiceCaptionOrVisualCue()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            context.Presenter.StartIntro(route, 1d);
            var voicesBefore = context.Backend.Started.Count(item => item.Bus == AudioBus.Voice);

            var ready = context.Presenter.StartOutcomeReady(route, Stage3Plan.Channel, 2d);

            Assert.That(ready.VoiceTokenId, Is.Null);
            Assert.That(ready.MusicTokenId, Is.Not.Null);
            Assert.That(ready.AmbienceTokenId, Is.Not.Null);
            Assert.That(context.Backend.Started.Count(item => item.Bus == AudioBus.Voice), Is.EqualTo(voicesBefore));
            Assert.That(context.Sink.Last.Kind, Is.EqualTo(Stage3MediaCueKind.None));
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Presenter.CanPause(route, ready.MediaTokenId, 2d), Is.True);
        }

        [Test]
        public void RepeatPauseResumeSettingsAndStaleCallbacksKeepOneCurrentOwner()
        {
            var context = Create(true, true); var route = Guid.NewGuid();
            var first = context.Presenter.StartIntro(route, 10d);
            var second = context.Presenter.Repeat(route, 11d);
            Assert.That(second.MediaTokenId, Is.Not.EqualTo(first.MediaTokenId));
            Assert.That(context.Backend.Started.Count(item => item.Semantic == Stage3MediaPresenter.MusicId), Is.EqualTo(1));
            Assert.That(context.Presenter.Advance(route, first.MediaTokenId, 12d), Is.False);
            Assert.That(context.Presenter.Advance(route, second.MediaTokenId, 13d), Is.True);
            Assert.That(context.Presenter.Advance(route, second.MediaTokenId, 13d), Is.False);
            Assert.That(context.Presenter.Pause(route, second.MediaTokenId, 13d), Is.True);
            Assert.That(context.Presenter.Pause(route, second.MediaTokenId, 13.1d), Is.False);
            Assert.That(context.Presenter.Advance(route, second.MediaTokenId, 14d), Is.False);
            Assert.That(context.Presenter.ResumeAfterMappedUserAction(Guid.NewGuid(), 20d), Is.Null);
            var resumed = context.Presenter.ResumeAfterMappedUserAction(route, 100d);
            Assert.That(resumed.HasValue, Is.True);
            Assert.That(resumed.Value.MediaTokenId, Is.Not.EqualTo(second.MediaTokenId));
            Assert.That(context.Presenter.ElapsedSeconds, Is.EqualTo(2d).Within(0.000001d));
            context.Session.SoundEnabled = false; context.Presenter.RefreshSettings(101d);
            Assert.That(context.Presenter.HasActiveVoice, Is.False);
            Assert.That(context.Presenter.HasActiveLoops, Is.False);
            context.Accessibility.SetCaptionsEnabled(false); context.Presenter.RefreshSettings(102d);
            Assert.That(context.Sink.Last.CaptionVisible, Is.False);
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().VisualEquivalents[0]));
        }

        private static void AssertCue(Context context, string id, bool visible, double start, int visualIndex)
        {
            Assert.That(context.Sink.Last.CaptionId, Is.EqualTo(id));
            Assert.That(context.Sink.Last.CaptionVisible, Is.EqualTo(visible));
            Assert.That(context.Sink.Last.CaptionCueStartSeconds, Is.EqualTo(start).Within(0.001d));
            Assert.That(context.Sink.Last.VisualEquivalent, Is.SameAs(Content().VisualEquivalents[visualIndex]));
            Assert.That(context.Sink.Last.RequiresReading, Is.False);
            Assert.That(context.Sink.Last.UsesEvaluationLanguage, Is.False);
            Assert.That(context.Sink.Last.UsesSuccessOrFailureAudio, Is.False);
        }

        private static Stage3Content Content() =>
            AssetDatabase.LoadAssetAtPath<Stage3Content>(Stage3ContentGenerator.ContentAssetPath);

        private static Context Create(bool sound, bool captions)
        {
            var backend = new Backend(); var director = new AudioDirector(backend, Ids(1));
            var session = new CoreSessionState { SoundEnabled = sound }; session.Begin(RouteId.Stage3);
            var accessibility = new AccessibilityState(false, captions);
            accessibility.SetOsState(ReduceMotionOsState.NotReduced);
            var sink = new Sink();
            return new Context(backend, session, accessibility, sink,
                new Stage3MediaPresenter(director, backend, Content(), session, accessibility, sink, Ids(2)));
        }

        private static Func<Guid> Ids(byte prefix)
        {
            var value = 0;
            return () => { var bytes = new byte[16]; bytes[0] = prefix; bytes[15] = (byte)++value; return new Guid(bytes); };
        }

        private sealed class Context
        {
            internal Context(Backend backend, CoreSessionState session, AccessibilityState accessibility,
                Sink sink, Stage3MediaPresenter presenter)
            { Backend = backend; Session = session; Accessibility = accessibility; Sink = sink; Presenter = presenter; }
            internal Backend Backend { get; }
            internal CoreSessionState Session { get; }
            internal AccessibilityState Accessibility { get; }
            internal Sink Sink { get; }
            internal Stage3MediaPresenter Presenter { get; }
        }

        private sealed class Sink : IStage3MediaCueSink
        {
            internal Stage3MediaCue Last { get; private set; }
            public void Present(Stage3MediaCue cue) => Last = cue;
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
