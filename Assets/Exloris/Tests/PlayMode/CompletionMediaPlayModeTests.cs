using System;
using System.Collections;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Completion;
using Exloris.Runtime.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class CompletionMediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator RealBackendBindsExactVoiceLoopsCaptionVisualAndReducedStaticFrame()
        {
            var owner = new GameObject("CompletionMediaPlayMode");
            var backend = new UnityAudioBackend(owner);
            var director = new AudioDirector(backend);
            var router = new ChildInputRouter(
                new InputAction("Point", InputActionType.PassThrough),
                new InputAction("Press", InputActionType.Button));
            try
            {
                var content = CompletionAdultReportContent.LoadApproved();
                var session = new Exloris.Core.Session.SessionState();
                session.Begin(RouteId.Completion);
                var accessibility = new AccessibilityState(alwaysReduce: true, captionsEnabled: true);
                var view = owner.AddComponent<CompletionStageView>();
                view.Configure(router, content, "avatar_boy");
                var motion = new CompletionMotionPresenter(view);
                var media = new CompletionMediaPresenter(
                    director, backend, content, session, accessibility, view);
                var token = Guid.NewGuid();
                var handle = media.Start(token, 10d);
                motion.TryPresent(new CompletionSnapshot(
                    token, CompletionState.Presenting, token, 10d, 0d, "completion.home", true), true);
                yield return null;

                Assert.That(backend.GetTokenClip(handle.VoiceTokenId.Value), Is.SameAs(content.CompletionVoice));
                Assert.That(backend.GetTokenLoop(handle.VoiceTokenId.Value), Is.False);
                Assert.That(backend.GetTokenLoop(handle.MusicTokenId.Value), Is.True);
                Assert.That(backend.GetTokenLoop(handle.AmbienceTokenId.Value), Is.True);
                Assert.That(content.CompletionVoice.length, Is.EqualTo(3.645875f).Within(0.002f));
                Assert.That(content.CompletionMusic.length, Is.EqualTo(26.666667f).Within(0.002f));
                Assert.That(content.VillageAmbience.length, Is.EqualTo(60f).Within(0.002f));
                Assert.That(view.CaptionVisible, Is.True);
                Assert.That(view.VisualEquivalentVisible, Is.True);
                Assert.That(view.SelectedChildOpacity, Is.EqualTo(1f));
            }
            finally
            {
                router.Dispose();
                backend.Dispose();
                UnityEngine.Object.Destroy(owner);
            }
        }
    }
}
