using System.Collections;
using System.Reflection;
using Exloris.Runtime.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Exloris.Tests.PlayMode
{
    public sealed class ReleaseEvidenceInputIsolationTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator EvidenceDriverOwnsDedicatedTouchscreenAndRestoresPhysicalMouse()
        {
            var physical = InputSystem.AddDevice<Mouse>("PhysicalMouse");
            Assert.That(physical.enabled, Is.True);
            var owner = new GameObject("ReleaseEvidenceInputIsolation");

            var driver = owner.AddComponent<ReleaseCandidateEvidenceCapture>();
            driver.InitializeEvidenceInputForTests();
            yield return null;

            Assert.That(physical.enabled, Is.False);
            Assert.That(Touchscreen.current, Is.Not.Null);
            Assert.That(Touchscreen.current.added, Is.True);
            Object.Destroy(owner);
            yield return null;
            Assert.That(physical.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator EvidenceClickProcessesTouchBeganAndEndedInSeparateInputUpdates()
        {
            var physical = InputSystem.AddDevice<Mouse>("PhysicalMouse");
            var owner = new GameObject("ReleaseEvidencePressBoundary");
            var driver = owner.AddComponent<ReleaseCandidateEvidenceCapture>();
            driver.InitializeEvidenceInputForTests();
            var evidenceTouchscreen = Touchscreen.current;
            Assert.That(evidenceTouchscreen, Is.Not.Null);

            var click = typeof(ReleaseCandidateEvidenceCapture).GetMethod(
                "Click", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(click, Is.Not.Null);
            var sequence = (IEnumerator)click.Invoke(driver, new object[] { new Vector2(100f, 100f) });

            currentTime = 100d;
            Assert.That(sequence.MoveNext(), Is.True);
            Assert.That(evidenceTouchscreen.primaryTouch.press.isPressed, Is.True,
                "Touch Began must be processed before the click coroutine yields.");
            Assert.That(sequence.MoveNext(), Is.True);
            Assert.That(evidenceTouchscreen.primaryTouch.press.isPressed, Is.False,
                "Touch Ended must be processed by the next click boundary.");

            Object.Destroy(owner);
            yield return null;
            Assert.That(physical.enabled, Is.True);
        }

    }
}
