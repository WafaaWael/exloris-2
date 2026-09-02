using Exloris.Runtime.Avatar;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Exloris.Tests.EditMode
{
    public sealed class AvatarMotionTests
    {
        [TestCase(0.0, AvatarMotionPhase.IdleBreathing)]
        [TestCase(3.2, AvatarMotionPhase.Blink)]
        [TestCase(3.51, AvatarMotionPhase.Blink)]
        [TestCase(3.53, AvatarMotionPhase.IdleBreathing)]
        [TestCase(8.9, AvatarMotionPhase.Blink)]
        [TestCase(14.7, AvatarMotionPhase.Blink)]
        [TestCase(21.4, AvatarMotionPhase.Blink)]
        [TestCase(24.0, AvatarMotionPhase.IdleBreathing)]
        public void BothAvatarsShareExactTwentyFourSecondIdleAndBlinkWindows(
            double elapsed,
            AvatarMotionPhase expectedPhase)
        {
            var presenter = Presenter();

            var boy = presenter.EvaluateSelect(AvatarChoice.Boy, elapsed, false);
            var girl = presenter.EvaluateSelect(AvatarChoice.Girl, elapsed, false);

            Assert.That(boy.Phase, Is.EqualTo(expectedPhase));
            Assert.That(girl.Phase, Is.EqualTo(expectedPhase));
            Assert.That(boy.IdleLoopSeconds, Is.EqualTo(24.0d));
            Assert.That(girl.IdleLoopSeconds, Is.EqualTo(24.0d));
            Assert.That(boy.BreathingCycleSeconds, Is.EqualTo(4.8d));
            Assert.That(girl.BreathingCycleSeconds, Is.EqualTo(4.8d));
            AssertParity(boy, girl);
        }

        [TestCase(0.0, AvatarMotionPhase.PreviewTurn)]
        [TestCase(1.999, AvatarMotionPhase.PreviewTurn)]
        [TestCase(2.0, AvatarMotionPhase.PreviewWave)]
        [TestCase(3.199, AvatarMotionPhase.PreviewWave)]
        [TestCase(3.2, AvatarMotionPhase.PreviewSettle)]
        [TestCase(3.999, AvatarMotionPhase.PreviewSettle)]
        public void BothAvatarsShareExactPreviewSequenceWithoutOvershoot(
            double elapsed,
            AvatarMotionPhase expectedPhase)
        {
            var presenter = Presenter();
            Assert.That(presenter.BeginPreview(AvatarChoice.Boy, 10, false), Is.True);
            Assert.That(presenter.TryEvaluatePreview(10, elapsed, out var boy), Is.True);
            Assert.That(presenter.BeginPreview(AvatarChoice.Girl, 11, false), Is.True);
            Assert.That(presenter.TryEvaluatePreview(11, elapsed, out var girl), Is.True);

            Assert.That(boy.Phase, Is.EqualTo(expectedPhase));
            Assert.That(girl.Phase, Is.EqualTo(expectedPhase));
            Assert.That(boy.PreviewDurationSeconds, Is.EqualTo(4.0d));
            Assert.That(girl.PreviewDurationSeconds, Is.EqualTo(4.0d));
            Assert.That(boy.Overshoot, Is.False);
            Assert.That(girl.Overshoot, Is.False);
            AssertParity(boy, girl);
        }

        [TestCase(AvatarChoice.Boy)]
        [TestCase(AvatarChoice.Girl)]
        public void ReducedMotionSelectsStaticEquivalentBeforeFirstAnimatedFrame(AvatarChoice choice)
        {
            var presenter = Presenter();

            Assert.That(presenter.BeginPreview(choice, 20, true), Is.True);

            Assert.That(presenter.CurrentFrame.Phase, Is.EqualTo(AvatarMotionPhase.StaticReduced));
            Assert.That(presenter.CurrentFrame.Sprite,
                Is.SameAs(choice == AvatarChoice.Boy
                    ? Content().BoyMotionSprites[5]
                    : Content().GirlMotionSprites[5]));
            Assert.That(presenter.TryEvaluatePreview(20, 0.0, out var frame), Is.True);
            Assert.That(frame.Phase, Is.EqualTo(AvatarMotionPhase.StaticReduced));
        }

        [Test]
        public void RepeatInvalidatesOldTokenAndDuplicateCompletionDoesNothing()
        {
            var presenter = Presenter();
            presenter.BeginPreview(AvatarChoice.Boy, 30, false);
            presenter.BeginPreview(AvatarChoice.Boy, 31, false);

            Assert.That(presenter.TryEvaluatePreview(30, 1.0, out _), Is.False);
            Assert.That(presenter.TryCompletePreview(30), Is.False);
            Assert.That(presenter.TryCompletePreview(31), Is.True);
            Assert.That(presenter.TryCompletePreview(31), Is.False);
        }

        [Test]
        public void EveryFrameKeepsFeetFixedAtApprovedPivot()
        {
            var presenter = Presenter();
            presenter.BeginPreview(AvatarChoice.Girl, 40, false);

            foreach (var elapsed in new[] { 0.0, 1.0, 2.0, 2.8, 3.2, 3.9 })
            {
                presenter.TryEvaluatePreview(40, elapsed, out var frame);
                Assert.That(frame.FeetFixed, Is.True);
                Assert.That(frame.Pivot, Is.EqualTo(new Vector2(0.5f, 0.08f)));
            }
        }

        private static void AssertParity(AvatarMotionFrame boy, AvatarMotionFrame girl)
        {
            Assert.That(boy.Phase, Is.EqualTo(girl.Phase));
            Assert.That(boy.PhaseProgress, Is.EqualTo(girl.PhaseProgress).Within(0.000001d));
            Assert.That(boy.BreathingOffsetRatio, Is.EqualTo(girl.BreathingOffsetRatio).Within(0.000001f));
            Assert.That(boy.Pivot, Is.EqualTo(girl.Pivot));
            Assert.That(boy.FeetFixed, Is.EqualTo(girl.FeetFixed));
            Assert.That(boy.Overshoot, Is.EqualTo(girl.Overshoot));
        }

        private static AvatarMotionPresenter Presenter() => new AvatarMotionPresenter(Content());

        private static AvatarContent Content()
        {
            return AssetDatabase.LoadAssetAtPath<AvatarContent>(
                "Assets/Exloris/Resources/Content/AvatarContent.asset");
        }
    }
}
