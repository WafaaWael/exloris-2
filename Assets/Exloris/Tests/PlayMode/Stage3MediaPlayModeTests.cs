using System;
using System.Collections;
using System.Collections.Generic;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Core.Session;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Input;
using Exloris.Runtime.Stage2;
using Exloris.Runtime.Stage3;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class Stage3MediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator MutedCaptionlessReducedMotionKeepsTheSameVisiblePlanRiverAndFacts()
        {
            var content = Resources.Load<Stage3Content>(Stage3Content.ResourcePath);
            var owner = new GameObject("Stage3MediaPlayMode", typeof(Stage3StageView));
            var router = new ChildInputRouter(new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var view = owner.GetComponent<Stage3StageView>(); view.Configure(router, content, false);
                var backend = new Backend(); var session = new SessionState { SoundEnabled = false };
                session.Begin(RouteId.Stage3);
                var accessibility = new AccessibilityState(true, false);
                accessibility.SetOsState(ReduceMotionOsState.Reduced);
                var media = new Stage3MediaPresenter(new AudioDirector(backend), backend,
                    content, session, accessibility, view);
                var route = Guid.NewGuid();
                media.StartOutcome(route, Stage3Plan.Grain, 0d);
                Assert.That(view.CaptionVisible, Is.False);
                Assert.That(view.VisualEquivalentVisible, Is.True);
                Assert.That(view.LastMediaCue.VisualEquivalent, Is.SameAs(content.VisualEquivalents[1]));
                Assert.That(backend.Started, Is.Zero);

                var outcome = new Stage3OutcomeKey(Stage2River.Low, Stage3Plan.Grain,
                    Stage3Benefit.Grain, Stage3Need.WaterRoute);
                var snapshot = new Stage3Snapshot(route, Stage3State.OutcomePlaying, Stage2River.Low,
                    Stage3Plan.Grain, outcome, Stage3TriedCombinations.None, Guid.NewGuid(),
                    Guid.NewGuid(), 0, 0, null, 0, false);
                view.Present(snapshot, true);
                var presenter = new Stage3OutcomePresenter(view);
                Assert.That(presenter.Start(snapshot, true, 0d), Is.True);
                presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 0.8d);
                Assert.That(view.LastOutcomePresentation.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Prepared));
                presenter.Advance(snapshot.TokenId, snapshot.OutcomeTokenId, 2d);
                Assert.That(view.LastOutcomePresentation.VisualState, Is.EqualTo(Stage3OutcomeVisualState.Final));
                Assert.That(view.OutcomeLayerOpacity, Is.EqualTo(1f).Within(0.001f));
                Assert.That(view.SelectedPlanSprite, Is.SameAs(content.Plans[0]));
                Assert.That(view.SelectedRiverLabel, Is.EqualTo("LOW NILE"));
                yield return null;
            }
            finally { router.Dispose(); UnityEngine.Object.Destroy(owner); }
        }

        private sealed class Backend : IAudioBackend, IAudioClipRegistry, IAudioClipRegistryLookup
        {
            private readonly HashSet<string> _registered = new HashSet<string>();
            internal int Started { get; private set; }
            public bool IsClipRegistered(string id) => _registered.Contains(id);
            public void RegisterClip(string id, AudioClip clip, bool loop = false) => _registered.Add(id);
            public void Start(AudioPlaybackToken token, AudioBus bus) => Started++;
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }
            public void ResumeAll(float targetMasterVolume, float rampSeconds) { }
            public void SetMasterVolume(float linearVolume) { }
            public void ApplyDuck(AudioDuckState state) { }
        }
    }
}
