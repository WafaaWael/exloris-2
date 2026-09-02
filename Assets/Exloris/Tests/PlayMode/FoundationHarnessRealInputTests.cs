using Exloris.Runtime.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Exloris.Tests.PlayMode
{
    public sealed class FoundationHarnessRealInputTests : InputTestFixture
    {
        private ScreenHost _host;
        private Mouse _mouse;
        private double _nextEvidenceTime;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _nextEvidenceTime = 100d;
            var hostObject = new GameObject("FoundationHarnessRealInputTest");
            _host = hostObject.AddComponent<ScreenHost>();
            Canvas.ForceUpdateCanvases();
            Assert.That(_host, Is.Not.Null);
            _host.RefreshControlGeometry();
            Assert.That(_host.InputsActive, Is.True);
            Assert.That(_host.IsControlEnabled(ScreenHost.InputProbeId), Is.True);
        }

        public override void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host.gameObject);
            _host = null;
            _mouse = null;
            base.TearDown();
        }

        [TestCase(ScreenHost.InputProbeId)]
        [TestCase(ScreenHost.RouteProbeId)]
        [TestCase(ScreenHost.RetryId)]
        [TestCase(ScreenHost.HomeId)]
        [TestCase(ScreenHost.ReducedMotionId)]
        [TestCase(ScreenHost.PseudolocalizationId)]
        [TestCase(ScreenHost.AudioProbeId)]
        [TestCase(ScreenHost.InterruptionId)]
        [TestCase(ScreenHost.ResumeId)]
        public void EveryControlAcceptsCentreAndAllFourEdgesThroughRealInput(string controlId)
        {
            for (var probeIndex = 0; probeIndex < 5; probeIndex++)
            {
                ResetHost();
                PrepareControl(controlId);
                var bounds = _host.GetControlBounds(controlId);
                var probes = new[]
                {
                    bounds.center,
                    new Vector2(bounds.xMin + 0.5f, bounds.center.y),
                    new Vector2(bounds.xMax - 0.5f, bounds.center.y),
                    new Vector2(bounds.center.x, bounds.yMin + 0.5f),
                    new Vector2(bounds.center.x, bounds.yMax - 0.5f)
                };

                Tap(_mouse, probes[probeIndex], NextEvidenceTime());

                Assert.That(_host.LastAcceptedControlId, Is.EqualTo(controlId),
                    controlId + " probe " + probeIndex);
            }
        }

        [TestCase(ScreenHost.InputProbeId)]
        [TestCase(ScreenHost.RouteProbeId)]
        [TestCase(ScreenHost.RetryId)]
        [TestCase(ScreenHost.HomeId)]
        [TestCase(ScreenHost.ReducedMotionId)]
        [TestCase(ScreenHost.PseudolocalizationId)]
        [TestCase(ScreenHost.AudioProbeId)]
        [TestCase(ScreenHost.InterruptionId)]
        [TestCase(ScreenHost.ResumeId)]
        public void EveryControlRejectsNearMissAndAdjacentGapThroughRealInput(string controlId)
        {
            ResetHost();
            PrepareControl(controlId);
            var beforeNearMiss = _host.LastAcceptedControlId;
            var bounds = _host.GetControlBounds(controlId);
            Tap(_mouse, new Vector2(bounds.xMax + 2f, bounds.center.y), NextEvidenceTime());
            Assert.That(_host.LastAcceptedControlId, Is.EqualTo(beforeNearMiss), controlId + " near miss");

            ResetHost();
            PrepareControl(controlId);
            var beforeGap = _host.LastAcceptedControlId;
            bounds = _host.GetControlBounds(controlId);
            var neighbour = FindNearestOtherControl(controlId, bounds);
            var gap = (bounds.center + neighbour.center) * 0.5f;
            Tap(_mouse, gap, NextEvidenceTime());
            Assert.That(_host.LastAcceptedControlId, Is.EqualTo(beforeGap), controlId + " adjacent gap");
        }

        [Test]
        public void RealTouchscreenTapUsesTheSameSemanticControl()
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            var position = _host.GetControlBounds(ScreenHost.InputProbeId).center;
            currentTime = 100d;
            BeginTouch(1, position, screen: touchscreen);
            currentTime = 100.05d;
            EndTouch(1, position, screen: touchscreen);

            Assert.That(_host.AcceptedInputCount, Is.EqualTo(1));
        }

        [Test]
        public void InvisibleTargetsAreNonOverlappingAndContainVisibleArt()
        {
            Assert.That(_host.AreTouchTargetsTransparent(), Is.True);
            Assert.That(_host.DoTouchTargetsOverlap(), Is.False);
            Assert.That(_host.DoesEveryTargetContainVisibleArt(), Is.True);
        }

        [Test]
        public void EveryTargetMeetsLargestFrozenDeviceFloorAndSpacing()
        {
            var ids = new[]
            {
                ScreenHost.InputProbeId, ScreenHost.RouteProbeId, ScreenHost.RetryId,
                ScreenHost.HomeId, ScreenHost.ReducedMotionId, ScreenHost.PseudolocalizationId,
                ScreenHost.AudioProbeId, ScreenHost.InterruptionId, ScreenHost.ResumeId
            };

            foreach (var id in ids)
            {
                var bounds = _host.GetControlCanvasRect(id);
                Assert.That(bounds.width, Is.GreaterThanOrEqualTo(224f), id);
                Assert.That(bounds.height, Is.GreaterThanOrEqualTo(224f), id);
            }

            for (var first = 0; first < ids.Length; first++)
            {
                var a = _host.GetControlCanvasRect(ids[first]);
                for (var second = first + 1; second < ids.Length; second++)
                {
                    var b = _host.GetControlCanvasRect(ids[second]);
                    var horizontalGap = Mathf.Max(0f, Mathf.Max(a.xMin, b.xMin) - Mathf.Min(a.xMax, b.xMax));
                    var verticalGap = Mathf.Max(0f, Mathf.Max(a.yMin, b.yMin) - Mathf.Min(a.yMax, b.yMax));
                    Assert.That(Mathf.Max(horizontalGap, verticalGap), Is.GreaterThanOrEqualTo(94f),
                        $"{ids[first]} and {ids[second]}");
                }
            }
        }

        [Test]
        public void TimeoutRetryRejectsStaleCompletion()
        {
            Tap(_mouse, _host.GetControlBounds(ScreenHost.RouteProbeId).center, 100d);
            var originalId = _host.ActiveNavigationId;
            Assert.That(originalId, Is.Not.Null);

            _host.ForceNavigationTimeoutForTests();
            Assert.That(_host.RecoveryVisible, Is.True);

            Tap(_mouse, _host.GetControlBounds(ScreenHost.RetryId).center, 110.3d);
            Assert.That(_host.ActiveNavigationId, Is.Not.Null.And.Not.EqualTo(originalId));
            Assert.That(_host.TryCompleteNavigation(originalId.Value), Is.False);
        }

        [Test]
        public void AccessibilityPseudoAudioAndInterruptionAreRealInputProbes()
        {
            Tap(_mouse, _host.GetControlBounds(ScreenHost.ReducedMotionId).center, 100d);
            Assert.That(_host.ReducedMotionEnabled, Is.True);

            Tap(_mouse, _host.GetControlBounds(ScreenHost.PseudolocalizationId).center, 100.3d);
            Assert.That(_host.PseudolocalizationEnabled, Is.True);

            Tap(_mouse, _host.GetControlBounds(ScreenHost.AudioProbeId).center, 100.6d);
            Assert.That(_host.AudioProbeStarted, Is.True);

            Tap(_mouse, _host.GetControlBounds(ScreenHost.InterruptionId).center, 100.9d);
            Assert.That(_host.AudioInterrupted, Is.True);
            Assert.That(_host.RecoveryVisible, Is.True);
        }

        [Test]
        public void HomeAndExplicitResumeAreMappedRealInputPaths()
        {
            Tap(_mouse, _host.GetControlBounds(ScreenHost.RouteProbeId).center, 100d);
            _host.ForceNavigationTimeoutForTests();
            Tap(_mouse, _host.GetControlBounds(ScreenHost.HomeId).center, 110.3d);
            Assert.That(_host.ActiveNavigationDestination, Is.EqualTo(Exloris.Core.Navigation.RouteId.Welcome));

            Object.DestroyImmediate(_host.gameObject);
            var hostObject = new GameObject("FoundationHarnessResumeTest");
            _host = hostObject.AddComponent<ScreenHost>();
            Canvas.ForceUpdateCanvases();
            Tap(_mouse, _host.GetControlBounds(ScreenHost.InterruptionId).center, 120d);
            _host.HandleFocusRestoredForTests();
            Assert.That(_host.AudioInterrupted, Is.True);
            Tap(_mouse, _host.GetControlBounds(ScreenHost.ResumeId).center, 120.3d);
            Assert.That(_host.AudioInterrupted, Is.False);
            Assert.That(_host.RecoveryVisible, Is.False);
        }

        [Test]
        public void ShippedHostWiresRouteChangeToMappedMediaRecovery()
        {
            Assert.That(_host.MediaLifecycleConnected, Is.True);
            _host.StartSynchronizedMediaForTests("foundation.media.test");

            _host.HandleAudioRouteChangeForTests();

            Assert.That(_host.SynchronizedMediaResumePending, Is.True);
            Assert.That(_host.RecoveryVisible, Is.True);
            Tap(_mouse, _host.GetControlBounds(ScreenHost.ResumeId).center, 130d);
            Assert.That(_host.SynchronizedMediaResumePending, Is.False);
            Assert.That(_host.AudioResumeRampActive, Is.True);
            Assert.That(_host.AudioResumeRampDurationSeconds, Is.EqualTo(0.480f).Within(0.0001f));
        }

        private void ResetHost()
        {
            if (_host != null) Object.DestroyImmediate(_host.gameObject);
            var hostObject = new GameObject("FoundationHarnessRealInputTest");
            _host = hostObject.AddComponent<ScreenHost>();
            Canvas.ForceUpdateCanvases();
            _host.RefreshControlGeometry();
        }

        private void PrepareControl(string controlId)
        {
            if (controlId == ScreenHost.RetryId || controlId == ScreenHost.HomeId)
            {
                Tap(_mouse, _host.GetControlBounds(ScreenHost.RouteProbeId).center, NextEvidenceTime());
                _host.ForceNavigationTimeoutForTests();
            }
            else if (controlId == ScreenHost.ResumeId)
            {
                Tap(_mouse, _host.GetControlBounds(ScreenHost.InterruptionId).center, NextEvidenceTime());
            }

            Assert.That(_host.IsControlEnabled(controlId), Is.True, controlId);
        }

        private Rect FindNearestOtherControl(string controlId, Rect bounds)
        {
            var ids = new[]
            {
                ScreenHost.InputProbeId, ScreenHost.RouteProbeId, ScreenHost.RetryId,
                ScreenHost.HomeId, ScreenHost.ReducedMotionId, ScreenHost.PseudolocalizationId,
                ScreenHost.AudioProbeId, ScreenHost.InterruptionId, ScreenHost.ResumeId
            };
            var nearestDistance = float.PositiveInfinity;
            var nearest = default(Rect);
            foreach (var id in ids)
            {
                if (id == controlId) continue;
                var candidate = _host.GetControlBounds(id);
                var distance = Vector2.SqrMagnitude(candidate.center - bounds.center);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = candidate;
            }

            return nearest;
        }

        private double NextEvidenceTime()
        {
            var value = _nextEvidenceTime;
            _nextEvidenceTime += 0.30d;
            return value;
        }

        private void Tap(Mouse mouse, Vector2 position, double time)
        {
            currentTime = time;
            Set(mouse.position, position);
            Press(mouse.leftButton);
            currentTime = time + 0.05d;
            Release(mouse.leftButton);
        }
    }
}
