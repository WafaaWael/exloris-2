using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Stage1;

namespace Exloris.Runtime.Recovery
{
    public sealed class Stage1PauseAdapter : IPauseCallerAdapter
    {
        private readonly Stage1Controller _controller;
        private readonly Stage1MediaPresenter _media;
        private readonly Stage1OutcomePresenter _outcome;
        private readonly Func<Guid> _tokenSource;
        private Stage1Snapshot _snapshot;
        private Stage1MediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAtSeconds;
        private Stage1PauseReason _preparedReason;
        private bool _pausePrepared;
        private bool _preparedOutcome;

        public Stage1PauseAdapter(
            Stage1Controller controller,
            Stage1MediaPresenter media,
            Stage1OutcomePresenter outcome,
            Func<Guid> tokenSource = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _media = media ?? throw new ArgumentNullException(nameof(media));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _tokenSource = tokenSource ?? Guid.NewGuid;
        }

        public RouteId Route => RouteId.Stage1;
        public Stage1Snapshot CapturedSnapshot => _snapshot;

        public void BindMediaHandle(Stage1MediaHandle handle)
        {
            if (handle.RouteTokenId != _controller.CurrentTokenId)
                throw new InvalidOperationException("Stage 1 media handle is stale.");
            _mediaHandle = handle;
        }

        public bool PreparePause(double nowSeconds, Stage1PauseReason reason)
        {
            var snapshot = _controller.Snapshot;
            var hasOutcome = snapshot.OutcomeTokenId != Guid.Empty;
            if (_pausePrepared
                || !CanPauseState(snapshot.State)
                || _mediaHandle.RouteTokenId != snapshot.TokenId
                || !_media.CanPause(snapshot.TokenId, _mediaHandle.MediaTokenId, nowSeconds)
                || (hasOutcome && !_outcome.CanFreeze(snapshot.TokenId, snapshot.OutcomeTokenId, nowSeconds)))
                return false;
            _preparedAtSeconds = nowSeconds;
            _preparedReason = reason;
            _preparedOutcome = hasOutcome;
            _pausePrepared = true;
            return true;
        }

        public void CancelPreparedPause()
        {
            _pausePrepared = false;
            _preparedAtSeconds = 0d;
            _preparedReason = Stage1PauseReason.Manual;
            _preparedOutcome = false;
        }

        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (!_pausePrepared || _snapshot.TokenId == Guid.Empty || _mediaHandle.RouteTokenId != _snapshot.TokenId)
            {
                handle = default;
                return false;
            }
            var snapshotToken = _tokenSource();
            if (snapshotToken == Guid.Empty) throw new InvalidOperationException("Pause snapshot token cannot be empty.");
            _handle = new PauseCallerHandle(Route, _snapshot.TokenId, snapshotToken);
            handle = _handle;
            return true;
        }

        public bool IsCurrent(PauseCallerHandle handle) =>
            handle.Equals(_handle) && _controller.CurrentTokenId == handle.CallerTokenId;

        public bool Restore(PauseCallerHandle handle, double nowSeconds) =>
            IsCurrent(handle) && _controller.RestoreFromPause(_snapshot, nowSeconds);

        public bool PauseMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle) || !_pausePrepared) return false;
            var outcomeFrozen = !_preparedOutcome
                || _outcome.Freeze(handle.CallerTokenId, _snapshot.OutcomeTokenId, _preparedAtSeconds);
            if (!outcomeFrozen) return false;
            if (!_media.Pause(handle.CallerTokenId, _mediaHandle.MediaTokenId, _preparedAtSeconds, _preparedReason))
            {
                if (_preparedOutcome)
                    _outcome.Resume(handle.CallerTokenId, _snapshot.OutcomeTokenId, _preparedAtSeconds);
                CancelPreparedPause();
                return false;
            }
            CancelPreparedPause();
            return true;
        }

        public bool ResumeMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle)
                || !_media.CanResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds)
                || (_snapshot.OutcomeTokenId != Guid.Empty
                    && !_outcome.CanResume(handle.CallerTokenId, _snapshot.OutcomeTokenId, nowSeconds)))
                return false;
            if (_snapshot.OutcomeTokenId != Guid.Empty
                && !_outcome.Resume(handle.CallerTokenId, _snapshot.OutcomeTokenId, nowSeconds))
                return false;
            if (_media.ResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds).HasValue) return true;
            if (_snapshot.OutcomeTokenId != Guid.Empty)
                _outcome.Freeze(handle.CallerTokenId, _snapshot.OutcomeTokenId, nowSeconds);
            return false;
        }

        public void RefreshSettings(double nowSeconds) => _media.RefreshSettings(nowSeconds);

        private static bool CanPauseState(Stage1State state) =>
            state == Stage1State.Intro
            || state == Stage1State.Playing
            || state == Stage1State.OutcomePlaying
            || state == Stage1State.OutcomeReady
            || state == Stage1State.Help
            || state == Stage1State.Leaving;
    }
}
