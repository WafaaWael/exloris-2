using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Navigation;
using Exloris.Runtime.Accessibility;

namespace Exloris.Runtime.Intro
{
    public interface IIntroPlayback
    {
        void Prepare(Guid tokenId, IntroMediaVariant variant);
        void Play(Guid tokenId);
        void Pause(Guid tokenId);
        void Resume(Guid previousTokenId, Guid replacementTokenId);
        void Stop(Guid tokenId);
    }

    public interface IIntroStageView
    {
        void ShowIntro(
            Guid tokenId,
            IntroMediaVariant variant,
            IntroFirstFramePresentation presentation);
        bool ReplacePlaybackToken(Guid previousTokenId, Guid replacementTokenId);
        void ShowWelcomeBoundary();
    }

    public sealed class IntroPreparedMedia
    {
        public IntroPreparedMedia(double frameRate, long frameCount, double durationSeconds)
        {
            FrameRate = frameRate;
            FrameCount = frameCount;
            DurationSeconds = durationSeconds;
        }

        public double FrameRate { get; }
        public long FrameCount { get; }
        public double DurationSeconds { get; }
    }

    public sealed class IntroFirstFramePresentation
    {
        public const string SkipControlId = "intro.skip";

        public IntroFirstFramePresentation()
        {
            ChildFacingControlIds = new[] { SkipControlId };
        }

        public IReadOnlyList<string> ChildFacingControlIds { get; }
    }

    public sealed class IntroStageController
    {
        private readonly NavigationAuthority _navigation;
        private readonly AccessibilityCoordinator _accessibility;
        private readonly IIntroPlayback _playback;
        private readonly IIntroStageView _view;
        private readonly Func<Guid> _playbackIdFactory;
        private readonly HashSet<Guid> _issuedPlaybackIds = new HashSet<Guid>();
        private bool _introVisible;
        private bool _finished;
        private bool _interrupted;
        private Guid _interruptedToken;

        public IntroStageController(
            NavigationAuthority navigation,
            AccessibilityCoordinator accessibility,
            IIntroPlayback playback,
            IIntroStageView view,
            Func<Guid> playbackIdFactory = null)
        {
            _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            _accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            _playback = playback ?? throw new ArgumentNullException(nameof(playback));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playbackIdFactory = playbackIdFactory ?? Guid.NewGuid;
        }

        public Guid? CurrentPlaybackToken { get; private set; }
        public IntroMediaVariant CurrentVariant { get; private set; }

        public async Task BeginAsync(
            int width,
            int height,
            bool preferLowResolution,
            double nowSeconds,
            CancellationToken cancellationToken = default)
        {
            if (CurrentPlaybackToken.HasValue)
            {
                throw new InvalidOperationException("The intro stage is already active.");
            }

            var reducedMotion = await _accessibility.PreflightIntroAsync(
                AccessibilityCoordinator.MaximumPreflight,
                cancellationToken);
            CurrentVariant = IntroVariantSelector.Select(width, height, reducedMotion, preferLowResolution);

            var request = _navigation.Request(RouteId.IntroVideo, nowSeconds);
            if (!request.Accepted)
            {
                throw new InvalidOperationException($"Intro navigation was rejected: {request.Failure}.");
            }

            var tokenId = NextPlaybackId();
            CurrentPlaybackToken = tokenId;
            _playback.Prepare(tokenId, CurrentVariant);
        }

        public bool HandlePrepared(
            Guid tokenId,
            IntroPreparedMedia preparedMedia,
            double nowSeconds = 0d)
        {
            if (!CurrentPlaybackToken.HasValue || CurrentPlaybackToken.Value != tokenId || _introVisible)
            {
                return false;
            }

            if (preparedMedia == null ||
                Math.Abs(preparedMedia.FrameRate - CurrentVariant.FrameRate) > 0.01d ||
                preparedMedia.FrameCount != CurrentVariant.FrameCount ||
                Math.Abs(preparedMedia.DurationSeconds - CurrentVariant.DurationSeconds) > 0.001d)
            {
                FailForwardToWelcome(tokenId, nowSeconds);
                return false;
            }

            var transaction = _navigation.ActiveTransaction;
            if (transaction == null ||
                transaction.Destination != RouteId.IntroVideo ||
                _navigation.Complete(transaction.Id) != NavigationCompletion.Completed)
            {
                return false;
            }

            _introVisible = true;
            _view.ShowIntro(tokenId, CurrentVariant, new IntroFirstFramePresentation());
            _playback.Play(tokenId);
            return true;
        }

        public bool HandleSkip(Guid tokenId, double nowSeconds)
        {
            return Finish(tokenId, nowSeconds);
        }

        public bool HandlePlaybackCompleted(Guid tokenId, double nowSeconds)
        {
            return Finish(tokenId, nowSeconds);
        }

        public bool HandlePlaybackFailed(Guid tokenId, double nowSeconds)
        {
            return FailForwardToWelcome(tokenId, nowSeconds);
        }

        public bool HandleInterruption(Guid tokenId)
        {
            if (_finished || _interrupted || !_introVisible ||
                !CurrentPlaybackToken.HasValue || CurrentPlaybackToken.Value != tokenId)
            {
                return false;
            }

            _playback.Pause(tokenId);
            _interruptedToken = tokenId;
            CurrentPlaybackToken = null;
            _interrupted = true;
            return true;
        }

        public Guid? ResumeAfterMappedUserAction()
        {
            if (!_interrupted || _finished)
            {
                return CurrentPlaybackToken;
            }

            var replacement = NextPlaybackId();
            if (!_view.ReplacePlaybackToken(_interruptedToken, replacement))
            {
                throw new InvalidOperationException("The intro view rejected the replacement playback token.");
            }
            _playback.Resume(_interruptedToken, replacement);
            CurrentPlaybackToken = replacement;
            _interrupted = false;
            _interruptedToken = Guid.Empty;
            return replacement;
        }

        private bool Finish(Guid tokenId, double nowSeconds)
        {
            if (_finished || !_introVisible ||
                !CurrentPlaybackToken.HasValue || CurrentPlaybackToken.Value != tokenId)
            {
                return false;
            }

            var request = _navigation.Request(RouteId.Welcome, nowSeconds);
            if (!request.Accepted ||
                _navigation.Complete(request.Transaction.Id) != NavigationCompletion.Completed)
            {
                return false;
            }

            _finished = true;
            _playback.Stop(tokenId);
            CurrentPlaybackToken = null;
            _accessibility.ReleaseIntroLatchAtWelcome();
            _view.ShowWelcomeBoundary();
            return true;
        }

        private bool FailForwardToWelcome(Guid tokenId, double nowSeconds)
        {
            if (_finished || !CurrentPlaybackToken.HasValue || CurrentPlaybackToken.Value != tokenId)
            {
                return false;
            }

            NavigationRequestResult request;
            if (_navigation.ActiveTransaction != null)
            {
                if (!_navigation.Cancel())
                {
                    return false;
                }

                request = _navigation.Home(nowSeconds);
            }
            else
            {
                request = _navigation.Request(RouteId.Welcome, nowSeconds);
            }

            if (!request.Accepted ||
                _navigation.Complete(request.Transaction.Id) != NavigationCompletion.Completed)
            {
                return false;
            }

            _finished = true;
            _playback.Stop(tokenId);
            CurrentPlaybackToken = null;
            _accessibility.ReleaseIntroLatchAtWelcome();
            _view.ShowWelcomeBoundary();
            return true;
        }

        private Guid NextPlaybackId()
        {
            var id = _playbackIdFactory();
            if (id == Guid.Empty || !_issuedPlaybackIds.Add(id))
            {
                throw new InvalidOperationException("Intro playback IDs must be non-empty and unique.");
            }

            return id;
        }
    }
}
