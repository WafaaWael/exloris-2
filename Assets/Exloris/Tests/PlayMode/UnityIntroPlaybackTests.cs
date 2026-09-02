using System;
using System.IO;
using Exloris.Runtime.Intro;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Video;

namespace Exloris.Tests.PlayMode
{
    public sealed class UnityIntroPlaybackTests
    {
        [Test]
        public void PrepareUsesApprovedStreamingAssetAndDeterministicVideoSettings()
        {
            var owner = new GameObject("UnityIntroPlaybackTest");
            var playback = owner.AddComponent<UnityIntroPlayback>();
            playback.Configure(StartupIntroContent.LoadApproved());
            var token = Guid.Parse("abec63ed-0352-4b76-bc88-ea4245ca859f");
            var variant = IntroVariantSelector.Select(
                1920,
                1080,
                reducedMotion: true,
                preferLowResolution: true);

            playback.Prepare(token, variant);

            var player = owner.GetComponent<VideoPlayer>();
            Assert.That(player, Is.Not.Null);
            Assert.That(player.url, Is.EqualTo(Path.Combine(
                Application.streamingAssetsPath,
                "Exloris/Intro/intro-reduced-16x9-low.mp4")));
            Assert.That(player.playOnAwake, Is.False);
            Assert.That(player.isLooping, Is.False);
            Assert.That(player.skipOnDrop, Is.False);
            Assert.That(player.playbackSpeed, Is.EqualTo(1f));
            Assert.That(player.renderMode, Is.EqualTo(VideoRenderMode.RenderTexture));
            Assert.That(player.targetTexture, Is.Not.Null);
            Assert.That(player.targetTexture.width, Is.EqualTo(1920));
            Assert.That(player.targetTexture.height, Is.EqualTo(1080));
            Assert.That(playback.VideoTexture, Is.SameAs(player.targetTexture));
            Assert.That(player.audioOutputMode, Is.EqualTo(VideoAudioOutputMode.None));
            Assert.That(player.sendFrameReadyEvents, Is.True);
            Assert.That(playback.VoiceClip, Is.SameAs(StartupIntroContent.LoadApproved().Voice));
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }
}
