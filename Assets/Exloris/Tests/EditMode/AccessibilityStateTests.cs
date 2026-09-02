using Exloris.Core.Accessibility;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class AccessibilityStateTests
    {
        [Test]
        public void UnknownOsStateStartsProvisionallyReduced()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);

            Assert.That(state.OsState, Is.EqualTo(ReduceMotionOsState.Unknown));
            Assert.That(state.EffectiveReducedMotion, Is.True);
        }

        [Test]
        public void OsReducedMotionAlwaysWins()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);

            state.SetOsState(ReduceMotionOsState.Reduced);

            Assert.That(state.EffectiveReducedMotion, Is.True);
        }

        [Test]
        public void AppAlwaysReduceWinsWhenOsAllowsAnimation()
        {
            var state = new AccessibilityState(alwaysReduce: true, captionsEnabled: false);
            state.SetOsState(ReduceMotionOsState.NotReduced);

            Assert.That(state.EffectiveReducedMotion, Is.True);
        }

        [Test]
        public void FullMotionRequiresKnownOsPermissionAndNoAppOverride()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: true);

            state.SetOsState(ReduceMotionOsState.NotReduced);

            Assert.That(state.EffectiveReducedMotion, Is.False);
            Assert.That(state.CaptionsEnabled, Is.True);
        }

        [Test]
        public void ReturningToUnknownImmediatelyRestoresReducedMode()
        {
            var state = new AccessibilityState(alwaysReduce: false, captionsEnabled: false);
            state.SetOsState(ReduceMotionOsState.NotReduced);
            Assert.That(state.EffectiveReducedMotion, Is.False);

            state.SetOsState(ReduceMotionOsState.Unknown);

            Assert.That(state.EffectiveReducedMotion, Is.True);
        }
    }
}
