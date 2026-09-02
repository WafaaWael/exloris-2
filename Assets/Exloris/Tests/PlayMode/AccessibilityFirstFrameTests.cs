using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Preferences;
using Exloris.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace Exloris.Tests.PlayMode
{
    public sealed class AccessibilityFirstFrameTests
    {
        [Test]
        public void ShippedScreenHostStartsProvisionallyReducedAndWiresCoordinator()
        {
            PlayerPrefs.DeleteKey(AccessibilityPreferenceStore.AlwaysReduceKey);
            PlayerPrefs.DeleteKey(AccessibilityPreferenceStore.CaptionsEnabledKey);
            var owner = new GameObject("AccessibilityShippedPathTest");

            var host = owner.AddComponent<ScreenHost>();

            Assert.That(host.AccessibilityCoordinatorConnected, Is.True);
            Assert.That(host.ReducedMotionEnabled, Is.True);
            Assert.That(host.ReducedMotionStaticEquivalentId, Is.Not.Empty);
            UnityEngine.Object.DestroyImmediate(owner);
        }

        [Test]
        public void ParticipantIsReducedBeforeAnyOsQueryCompletes()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var adapter = new FakeAdapter(ReduceMotionOsState.NotReduced, delayMilliseconds: 50);
            var coordinator = new AccessibilityCoordinator(state, adapter);
            var participant = new FakeParticipant("intro_static_sequence");

            coordinator.Register(participant);

            CollectionAssert.AreEqual(new[] { true }, participant.AppliedStates);
        }

        [Test]
        public async Task SuccessfulPreflightLatchesModeBeforeIntroStarts()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var adapter = new FakeAdapter(ReduceMotionOsState.NotReduced, delayMilliseconds: 1);
            var coordinator = new AccessibilityCoordinator(state, adapter);
            var participant = new FakeParticipant("intro_static_sequence");
            coordinator.Register(participant);

            var introReduced = await coordinator.PreflightIntroAsync(TimeSpan.FromMilliseconds(250));

            Assert.That(introReduced, Is.False);
            Assert.That(coordinator.IntroModeLatched, Is.True);
            Assert.That(coordinator.IntroReducedMotion, Is.False);
            Assert.That(participant.AppliedStates[participant.AppliedStates.Count - 1], Is.False);
        }

        [Test]
        public async Task PreflightTimeoutRemainsReduced()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var adapter = new FakeAdapter(ReduceMotionOsState.NotReduced, delayMilliseconds: 200);
            var coordinator = new AccessibilityCoordinator(state, adapter);

            var introReduced = await coordinator.PreflightIntroAsync(TimeSpan.FromMilliseconds(20));

            Assert.That(introReduced, Is.True);
            Assert.That(state.OsState, Is.EqualTo(ReduceMotionOsState.Unknown));
        }

        [Test]
        public async Task LateOsChangeCannotSwitchMotionDuringLatchedIntro()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var adapter = new FakeAdapter(ReduceMotionOsState.NotReduced, delayMilliseconds: 1);
            var coordinator = new AccessibilityCoordinator(state, adapter);
            var participant = new FakeParticipant("intro_static_sequence");
            coordinator.Register(participant);
            await coordinator.PreflightIntroAsync(TimeSpan.FromMilliseconds(250));
            var callsBeforeChange = participant.AppliedStates.Count;

            adapter.Raise(ReduceMotionOsState.Reduced);

            Assert.That(coordinator.IntroReducedMotion, Is.False);
            Assert.That(participant.AppliedStates.Count, Is.EqualTo(callsBeforeChange));

            coordinator.ReleaseIntroLatchAtWelcome();

            Assert.That(participant.AppliedStates[participant.AppliedStates.Count - 1], Is.True);
        }

        [Test]
        public async Task EnablingReducedMotionAfterIntroSettlesParticipantsImmediately()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var adapter = new FakeAdapter(ReduceMotionOsState.NotReduced, delayMilliseconds: 1);
            var coordinator = new AccessibilityCoordinator(state, adapter);
            var participant = new FakeParticipant("character_idle_static");
            coordinator.Register(participant);
            await coordinator.PreflightIntroAsync(TimeSpan.FromMilliseconds(250));
            coordinator.ReleaseIntroLatchAtWelcome();

            coordinator.SetAlwaysReduce(true);

            Assert.That(participant.AppliedStates[participant.AppliedStates.Count - 1], Is.True);
        }

        [Test]
        public void AnimatedParticipantWithoutNamedStaticEquivalentIsRejected()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            var coordinator = new AccessibilityCoordinator(
                state,
                new FakeAdapter(ReduceMotionOsState.Unknown, delayMilliseconds: 0));

            Assert.Throws<ArgumentException>(() => coordinator.Register(new FakeParticipant("")));
        }

        [Test]
        public void ApplicationResumeRequeriesTheOsAccessibilityAdapter()
        {
            var owner = new GameObject("AccessibilityResumeQueryTest");
            var host = owner.AddComponent<ScreenHost>();
            var field = typeof(ScreenHost).GetField(
                "_accessibilityCoordinator",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var coordinator = (AccessibilityCoordinator)field.GetValue(host);
            var before = coordinator.OsQueryCount;

            host.SendMessage("OnApplicationPause", false);

            Assert.That(coordinator.OsQueryCount, Is.EqualTo(before + 1));
            UnityEngine.Object.DestroyImmediate(owner);
        }

        private sealed class FakeAdapter : IReduceMotionAdapter
        {
            private readonly int _delayMilliseconds;
            private readonly ReduceMotionOsState _queryResult;

            public FakeAdapter(ReduceMotionOsState queryResult, int delayMilliseconds)
            {
                _queryResult = queryResult;
                _delayMilliseconds = delayMilliseconds;
            }

            public event Action<ReduceMotionOsState> Changed;

            public async Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken)
            {
                if (_delayMilliseconds > 0)
                {
                    await Task.Delay(_delayMilliseconds, cancellationToken);
                }

                return _queryResult;
            }

            public void Raise(ReduceMotionOsState state)
            {
                Changed?.Invoke(state);
            }
        }

        private sealed class FakeParticipant : IReducedMotionParticipant
        {
            public FakeParticipant(string staticEquivalentId)
            {
                StaticEquivalentId = staticEquivalentId;
            }

            public string StaticEquivalentId { get; }
            public List<bool> AppliedStates { get; } = new List<bool>();

            public void ApplyReducedMotion(bool reducedMotion)
            {
                AppliedStates.Add(reducedMotion);
            }
        }
    }
}
