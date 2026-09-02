using System;
using Exloris.Runtime.Audio;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AudioPolicyTests
    {
        [Test]
        public void FrozenMixPolicyValuesAreExact()
        {
            Assert.That(AudioPolicy.DefaultMasterVolume, Is.EqualTo(0.60f));
            Assert.That(AudioPolicy.MaximumConcurrentVoices, Is.EqualTo(16));
            Assert.That(AudioPolicy.AssembledIntegratedLufs, Is.EqualTo(-23f));
            Assert.That(AudioPolicy.MaximumShortTermLufs, Is.EqualTo(-18f));
            Assert.That(AudioPolicy.MaximumTruePeakDbtp, Is.EqualTo(-1f));
            Assert.That(AudioPolicy.VoiceTargetLufs, Is.EqualTo(-23f));
            Assert.That(AudioPolicy.MusicTargetLufs, Is.EqualTo(-29f));
            Assert.That(AudioPolicy.AmbienceTargetLufs, Is.EqualTo(-35f));
            Assert.That(AudioPolicy.SfxMaximumShortTermLufs, Is.EqualTo(-20f));
        }

        [TestCase(0f, 0.250f)]
        [TestCase(1f, 0.250f)]
        [TestCase(6f, 0.480f)]
        [TestCase(10f, 0.800f)]
        public void GainRiseRampUsesFrozenFormula(float gainDeltaLu, float expectedSeconds)
        {
            Assert.That(
                AudioPolicy.CalculateGainRiseSeconds(gainDeltaLu),
                Is.EqualTo(expectedSeconds).Within(0.0001f));
        }

        [Test]
        public void InteractiveOnsetP95UsesOneHundredFiftyMillisecondCeiling()
        {
            var passing = new[] { 0.08, 0.10, 0.12, 0.14, 0.15 };
            var failing = new[] { 0.08, 0.10, 0.12, 0.14, 0.16 };

            Assert.That(AudioPolicy.PassesInteractiveOnsetP95(passing), Is.True);
            Assert.That(AudioPolicy.PassesInteractiveOnsetP95(failing), Is.False);
        }

        [Test]
        public void VoicePlaybackAppliesAndReleasesExactDucking()
        {
            var backend = new FakeAudioBackend();
            var director = new AudioDirector(backend, SequentialIds());

            var token = director.TryStart(AudioBus.Voice, "voice_line", 0d);

            Assert.That(token, Is.Not.Null);
            Assert.That(backend.LastDuck.MusicDb, Is.EqualTo(-6f));
            Assert.That(backend.LastDuck.AmbienceDb, Is.EqualTo(-3f));
            Assert.That(backend.LastDuck.SfxDb, Is.EqualTo(-3f));
            Assert.That(backend.LastDuck.RampSeconds, Is.EqualTo(0.100f));

            Assert.That(director.Complete(token.Id), Is.True);
            Assert.That(backend.LastDuck, Is.EqualTo(new AudioDuckState(0f, 0f, 0f, 0.600f)));
        }

        [Test]
        public void SeventeenthConcurrentVoiceIsRejected()
        {
            var director = new AudioDirector(new FakeAudioBackend(), SequentialIds());
            for (var i = 0; i < AudioPolicy.MaximumConcurrentVoices; i++)
            {
                Assert.That(director.TryStart(AudioBus.Sfx, $"sfx_{i}", i), Is.Not.Null);
            }

            Assert.That(director.TryStart(AudioBus.Sfx, "sfx_17", 17d), Is.Null);
        }

        [Test]
        public void CompletionIsIdempotentAndUnknownTokenIsRejected()
        {
            var director = new AudioDirector(new FakeAudioBackend(), SequentialIds());
            var token = director.TryStart(AudioBus.Sfx, "press", 0d);

            Assert.That(director.Complete(token.Id), Is.True);
            Assert.That(director.Complete(token.Id), Is.False);
            Assert.That(director.Complete(Guid.NewGuid()), Is.False);
        }

        [Test]
        public void ResumeNeverBoostsAboveCurrentUserVolume()
        {
            var backend = new FakeAudioBackend();
            var director = new AudioDirector(backend, SequentialIds());
            director.SetUserMasterVolume(0.35f);

            director.PauseAndDuckImmediately();
            director.ResumeAfterMappedUserAction();

            Assert.That(backend.ResumeTargetVolume, Is.EqualTo(0.35f));
            Assert.That(director.MasterVolume, Is.EqualTo(0.35f));
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

        private sealed class FakeAudioBackend : IAudioBackend
        {
            public AudioDuckState LastDuck { get; private set; }
            public float ResumeTargetVolume { get; private set; }

            public void Start(AudioPlaybackToken token, AudioBus bus) { }
            public void Stop(Guid tokenId) { }
            public void PauseAllImmediately() { }
            public void SeekAll(double timeSeconds) { }

            public void ResumeAll(float targetMasterVolume, float rampSeconds)
            {
                ResumeTargetVolume = targetMasterVolume;
            }

            public void SetMasterVolume(float linearVolume)
            {
                ResumeTargetVolume = linearVolume;
            }

            public void ApplyDuck(AudioDuckState state)
            {
                LastDuck = state;
            }
        }
    }
}
