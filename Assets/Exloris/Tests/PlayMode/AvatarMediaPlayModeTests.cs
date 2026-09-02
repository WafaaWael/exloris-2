using System.Collections;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Avatar;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class AvatarMediaPlayModeTests
    {
        [UnityTest]
        public IEnumerator ApprovedVoiceVisualCaptionAndSfxResolveInRuntime()
        {
            var owner = new GameObject("AvatarMediaRuntimeTest");
            var backend = new UnityAudioBackend(owner);
            try
            {
                var content = AvatarContent.LoadApproved();
                var sink = new RecordingSink();
                var presenter = new AvatarMediaPresenter(
                    new AudioDirector(backend),
                    backend,
                    content,
                    sink);

                var voice = presenter.Present(AvatarMediaState.Preview, true, true, 10.0);
                yield return null;

                Assert.That(backend.GetTokenClip(voice.Id), Is.SameAs(content.VoiceClips[1]));
                Assert.That(sink.Visual, Is.SameAs(content.VisualEquivalents[1]));
                Assert.That(sink.CaptionId, Is.EqualTo(AvatarMediaPresenter.PreviewCaption1Id));
                Assert.That(sink.CaptionVisible, Is.True);

                var sfx = presenter.PlaySfx(AvatarSfxCue.Confirm, true, 10.1);
                Assert.That(backend.GetTokenClip(sfx.Id), Is.SameAs(content.SfxClips[1]));
            }
            finally
            {
                backend.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        [UnityTest]
        public IEnumerator MutedRuntimeKeepsVisualEquivalentWithoutCreatingAudioToken()
        {
            var owner = new GameObject("AvatarMediaMutedRuntimeTest");
            var backend = new UnityAudioBackend(owner);
            try
            {
                var content = AvatarContent.LoadApproved();
                var sink = new RecordingSink();
                var presenter = new AvatarMediaPresenter(
                    new AudioDirector(backend),
                    backend,
                    content,
                    sink);

                var voice = presenter.Present(AvatarMediaState.Leave, false, false, 20.0);
                yield return null;

                Assert.That(voice, Is.Null);
                Assert.That(sink.Visual, Is.SameAs(content.VisualEquivalents[2]));
                Assert.That(sink.CaptionVisible, Is.False);
            }
            finally
            {
                backend.Dispose();
                Object.DestroyImmediate(owner);
            }
        }

        private sealed class RecordingSink : IAvatarMediaCueSink
        {
            public Sprite Visual { get; private set; }
            public string CaptionId { get; private set; }
            public bool CaptionVisible { get; private set; }

            public void ShowMediaCue(AvatarMediaState state, Sprite visual, string captionId,
                bool captionsVisible, double startedAtSeconds)
            {
                Visual = visual;
                CaptionId = captionId;
                CaptionVisible = captionsVisible;
            }

            public void UpdateCaption(string captionId, bool visible, double cueStartedAtSeconds)
            {
                CaptionId = captionId;
                CaptionVisible = visible;
            }
        }
    }
}
