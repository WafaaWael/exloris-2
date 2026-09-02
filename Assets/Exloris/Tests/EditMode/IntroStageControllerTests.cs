using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;
using Exloris.Core.Navigation;
using Exloris.Runtime.Accessibility;
using Exloris.Runtime.Intro;
using NUnit.Framework;

namespace Exloris.Tests.EditMode
{
    public sealed class IntroStageControllerTests
    {
        [Test]
        public async Task ColdStartLatchesAccessibilityBeforePreparingExactVariant()
        {
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(
                navigation,
                accessibility,
                playback,
                view,
                () => Guid.Parse("c9679a2a-0787-4bd3-a6b0-a3ea08b00b2a"));

            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 2d);

            Assert.That(AccessibilityCoordinator.MaximumPreflight.TotalMilliseconds, Is.EqualTo(250d));
            Assert.That(accessibility.IntroModeLatched, Is.True);
            Assert.That(accessibility.IntroReducedMotion, Is.False);
            Assert.That(playback.PreparedVariant.FileName, Is.EqualTo("intro-16x9.mp4"));
            Assert.That(navigation.ActiveTransaction.Destination, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(view.IntroShownCount, Is.Zero, "No child-facing frame appears before media preparation.");
        }

        [Test]
        public async Task PreparedExactMediaShowsFirstFrameWithSkipOnlyAndEntersIntroOnce()
        {
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(navigation, accessibility, playback, view);
            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 1d);

            var accepted = controller.HandlePrepared(
                controller.CurrentPlaybackToken.Value,
                new IntroPreparedMedia(frameRate: 30d, frameCount: 288, durationSeconds: 9.6d));

            Assert.That(accepted, Is.True);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(navigation.ActiveTransaction, Is.Null);
            Assert.That(view.IntroShownCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { IntroFirstFramePresentation.SkipControlId }, view.LastPresentation.ChildFacingControlIds);
            Assert.That(playback.PlayCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task CompletionOrSkipEntersWelcomeExactlyOnceAndRejectsStaleCallbacks(bool finishBySkip)
        {
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(navigation, accessibility, playback, view);
            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 0d);
            var token = controller.CurrentPlaybackToken.Value;
            Assert.That(controller.HandlePrepared(token, new IntroPreparedMedia(30d, 288, 9.6d)), Is.True);

            var first = finishBySkip
                ? controller.HandleSkip(token, nowSeconds: 1d)
                : controller.HandlePlaybackCompleted(token, nowSeconds: 9.6d);
            var duplicate = controller.HandlePlaybackCompleted(token, nowSeconds: 9.7d);

            Assert.That(first, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(navigation.ActiveTransaction, Is.Null);
            Assert.That(view.WelcomeShownCount, Is.EqualTo(1));
            Assert.That(playback.StopCount, Is.EqualTo(1));
            Assert.That(accessibility.IntroModeLatched, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task MissingOrCorruptMediaFailsForwardToWelcomeExactlyOnce(bool corruptMetadata)
        {
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(navigation, accessibility, playback, view);
            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 3d);
            var token = controller.CurrentPlaybackToken.Value;

            var first = corruptMetadata
                ? controller.HandlePrepared(token, new IntroPreparedMedia(30d, 900, 30d), nowSeconds: 3.2d)
                : controller.HandlePlaybackFailed(token, nowSeconds: 3.2d);
            var duplicate = controller.HandlePlaybackFailed(token, nowSeconds: 3.3d);

            Assert.That(first, Is.EqualTo(!corruptMetadata), "Invalid metadata is rejected after performing the same fail-forward.");
            Assert.That(duplicate, Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(navigation.ActiveTransaction, Is.Null);
            Assert.That(navigation.Recovery, Is.Null);
            Assert.That(controller.CurrentPlaybackToken, Is.Null);
            Assert.That(view.IntroShownCount, Is.Zero);
            Assert.That(view.WelcomeShownCount, Is.EqualTo(1));
            Assert.That(playback.StopCount, Is.EqualTo(1));
            Assert.That(controller.HandlePlaybackCompleted(token, 10d), Is.False);
        }

        [Test]
        public async Task OverlongPreparedMediaConsumesTokenAndFailsForwardWithoutRecovery()
        {
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(navigation, accessibility, playback, view);
            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 3d);
            var token = controller.CurrentPlaybackToken.Value;

            Assert.That(
                controller.HandlePrepared(
                    token,
                    new IntroPreparedMedia(frameRate: 30d, frameCount: 288, durationSeconds: 9.7d),
                    nowSeconds: 3.2d),
                Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.Welcome));
            Assert.That(navigation.Recovery, Is.Null);
            Assert.That(controller.CurrentPlaybackToken, Is.Null);
            Assert.That(view.WelcomeShownCount, Is.EqualTo(1));
            Assert.That(controller.HandlePlaybackCompleted(token, 10d), Is.False);
        }

        [Test]
        public async Task InterruptionRequiresMappedResumeAndOldCompletionCannotAdvance()
        {
            var oldToken = Guid.Parse("4a5bc0bb-c9ea-4510-8944-45d08f423e1a");
            var newToken = Guid.Parse("b94e79bb-e798-4910-92dd-ce815189389d");
            var tokens = new Queue<Guid>(new[] { oldToken, newToken });
            var navigation = new NavigationAuthority(RouteId.StartupLoading);
            var accessibility = new AccessibilityCoordinator(
                new AccessibilityState(alwaysReduce: false, captionsEnabled: false),
                new FakeReduceMotionAdapter(ReduceMotionOsState.NotReduced));
            var playback = new FakeIntroPlayback();
            var view = new FakeIntroStageView();
            var controller = new IntroStageController(
                navigation,
                accessibility,
                playback,
                view,
                () => tokens.Dequeue());
            await controller.BeginAsync(1920, 1080, preferLowResolution: false, nowSeconds: 0d);
            Assert.That(controller.HandlePrepared(oldToken, new IntroPreparedMedia(30d, 288, 9.6d)), Is.True);

            Assert.That(controller.HandleInterruption(oldToken), Is.True);
            Assert.That(controller.HandlePlaybackCompleted(oldToken, 5d), Is.False);
            Assert.That(navigation.CurrentRoute, Is.EqualTo(RouteId.IntroVideo));
            Assert.That(view.WelcomeShownCount, Is.Zero);

            var resumedToken = controller.ResumeAfterMappedUserAction();

            Assert.That(resumedToken, Is.EqualTo(newToken));
            Assert.That(playback.PauseCount, Is.EqualTo(1));
            Assert.That(playback.ResumeCount, Is.EqualTo(1));
            Assert.That(controller.HandlePlaybackCompleted(oldToken, 5.1d), Is.False);
            Assert.That(controller.HandlePlaybackCompleted(newToken, 9.6d), Is.True);
            Assert.That(view.WelcomeShownCount, Is.EqualTo(1));
        }

        private sealed class FakeReduceMotionAdapter : IReduceMotionAdapter
        {
            private readonly ReduceMotionOsState _state;

            public FakeReduceMotionAdapter(ReduceMotionOsState state)
            {
                _state = state;
            }

            public event Action<ReduceMotionOsState> Changed;

            public Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(_state);
            }
        }

        private sealed class FakeIntroPlayback : IIntroPlayback
        {
            public Guid PreparedToken { get; private set; }
            public IntroMediaVariant PreparedVariant { get; private set; }
            public int PlayCount { get; private set; }
            public int StopCount { get; private set; }
            public int PauseCount { get; private set; }
            public int ResumeCount { get; private set; }

            public void Prepare(Guid tokenId, IntroMediaVariant variant)
            {
                PreparedToken = tokenId;
                PreparedVariant = variant;
            }

            public void Play(Guid tokenId) { PlayCount++; }
            public void Pause(Guid tokenId) { PauseCount++; }
            public void Resume(Guid previousTokenId, Guid replacementTokenId) { ResumeCount++; }
            public void Stop(Guid tokenId) { StopCount++; }
        }

        private sealed class FakeIntroStageView : IIntroStageView
        {
            public int IntroShownCount { get; private set; }
            public int WelcomeShownCount { get; private set; }
            public IntroFirstFramePresentation LastPresentation { get; private set; }

            public void ShowIntro(
                Guid tokenId,
                IntroMediaVariant variant,
                IntroFirstFramePresentation presentation)
            {
                IntroShownCount++;
                LastPresentation = presentation;
            }

            public bool ReplacePlaybackToken(Guid previousTokenId, Guid replacementTokenId)
            {
                return previousTokenId != Guid.Empty && replacementTokenId != Guid.Empty;
            }

            public void ShowWelcomeBoundary() { WelcomeShownCount++; }
        }
    }
}
