using System;
using Exloris.Runtime.Audio;
using Exloris.Runtime.Welcome;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class UnityAudioBackendTests
    {
        [Test]
        public void RegisteredWelcomeSemanticIdsPlayApprovedClipsAndLoopsInsteadOfDiagnosticTone()
        {
            var owner = new GameObject("UnityAudioBackendWelcomeClipTest");
            var backend = new UnityAudioBackend(owner);
            var content = WelcomeContent.LoadApproved();
            backend.RegisterClip("VO-WELCOME-001", content.WelcomeVoice);
            backend.RegisterClip("MUS-MAIN-JOURNEY-LOOP", content.Music, loop: true);
            var director = new AudioDirector(backend, SequentialIds());

            var voice = director.TryStart(AudioBus.Voice, "VO-WELCOME-001", 0d);
            var music = director.TryStart(AudioBus.Music, "MUS-MAIN-JOURNEY-LOOP", 0d);

            Assert.That(backend.GetTokenClip(voice.Id), Is.SameAs(content.WelcomeVoice));
            Assert.That(backend.GetTokenClip(music.Id), Is.SameAs(content.Music));
            Assert.That(backend.GetTokenLoop(music.Id), Is.True);
            Assert.That(backend.GetTokenLoop(voice.Id), Is.False);
            backend.Dispose();
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void VoiceDuckingChangesTheRealSfxBusGainAcrossAttackAndRelease()
        {
            var owner = new GameObject("UnityAudioBackendDuckTest");
            var backend = new UnityAudioBackend(owner);
            var ids = SequentialIds();
            var director = new AudioDirector(backend, ids);
            var sfx = director.TryStart(AudioBus.Sfx, "sfx", 0d);
            var voice = director.TryStart(AudioBus.Voice, "voice", 0d);

            backend.Advance(0.050f);
            Assert.That(backend.GetBusDuckDb(AudioBus.Sfx), Is.EqualTo(-1.5f).Within(0.01f));
            backend.Advance(0.050f);
            Assert.That(backend.GetBusDuckDb(AudioBus.Sfx), Is.EqualTo(-3f).Within(0.01f));
            Assert.That(backend.GetTokenVolume(sfx.Id), Is.LessThan(AudioPolicy.DefaultMasterVolume));

            Assert.That(director.Complete(voice.Id), Is.True);
            backend.Advance(0.300f);
            Assert.That(backend.GetBusDuckDb(AudioBus.Sfx), Is.EqualTo(-1.5f).Within(0.01f));
            backend.Advance(0.300f);
            Assert.That(backend.GetBusDuckDb(AudioBus.Sfx), Is.EqualTo(0f).Within(0.01f));

            backend.Dispose();
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void MappedResumeRestoresMasterGainOverTheRequiredFourHundredEightyMilliseconds()
        {
            var owner = new GameObject("UnityAudioBackendResumeTest");
            var backend = new UnityAudioBackend(owner);
            backend.SetMasterVolume(0.35f);
            backend.PauseAllImmediately();

            backend.ResumeAll(0.35f, AudioPolicy.CalculateGainRiseSeconds(6f));

            Assert.That(backend.MasterRampDurationSeconds, Is.EqualTo(0.480f).Within(0.0001f));
            Assert.That(backend.MasterRampActive, Is.True);
            Assert.That(backend.MasterGain, Is.LessThan(0.35f));
            backend.Advance(0.479f);
            Assert.That(backend.MasterRampActive, Is.True);
            Assert.That(backend.MasterGain, Is.LessThan(0.35f));
            backend.Advance(0.001f);
            Assert.That(backend.MasterRampActive, Is.False);
            Assert.That(backend.MasterGain, Is.EqualTo(0.35f).Within(0.0001f));

            backend.Dispose();
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void OverlappingVoicesDoNotRestartDuckAttackAndActiveVoiceCountIsExact()
        {
            var owner = new GameObject("UnityAudioBackendOverlapTest");
            var backend = new UnityAudioBackend(owner);
            var director = new AudioDirector(backend, SequentialIds());
            director.TryStart(AudioBus.Sfx, "sfx", 0d);
            var first = director.TryStart(AudioBus.Voice, "voice-1", 0d);
            backend.Advance(0.050f);
            var second = director.TryStart(AudioBus.Voice, "voice-2", 0.050d);

            backend.Advance(0.050f);

            Assert.That(backend.GetBusDuckDb(AudioBus.Music), Is.EqualTo(-6f).Within(0.01f));
            Assert.That(director.ActiveVoiceCount, Is.EqualTo(2));
            Assert.That(director.Complete(first.Id), Is.True);
            Assert.That(backend.GetBusDuckDb(AudioBus.Music), Is.EqualTo(-6f).Within(0.01f));
            Assert.That(director.Complete(second.Id), Is.True);

            backend.Dispose();
            UnityEngine.Object.DestroyImmediate(owner);
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
    }
}
