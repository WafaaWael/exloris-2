using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Stage2;

namespace Exloris.Runtime.Recovery
{
    public sealed class Stage2PauseAdapter : IPauseCallerAdapter
    {
        private readonly Stage2Controller _controller;
        private readonly Stage2MediaPresenter _media;
        private readonly Stage2OutcomePresenter _outcome;
        private readonly Func<Guid> _tokenSource;
        private Stage2Snapshot _snapshot;
        private Stage2MediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAtSeconds;
        private Stage2PauseReason _preparedReason;
        private bool _pausePrepared;
        private bool _preparedOutcome;

        public Stage2PauseAdapter(Stage2Controller controller, Stage2MediaPresenter media,
            Stage2OutcomePresenter outcome, Func<Guid> tokenSource = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _media = media ?? throw new ArgumentNullException(nameof(media));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _tokenSource = tokenSource ?? Guid.NewGuid;
        }

        public RouteId Route => RouteId.Stage2;
        public Stage2Snapshot CapturedSnapshot => _snapshot;

        public void BindMediaHandle(Stage2MediaHandle handle)
        {
            if (handle.RouteTokenId != _controller.CurrentTokenId)
                throw new InvalidOperationException("Stage 2 media handle is stale.");
            _mediaHandle = handle;
        }

        public bool PreparePause(double nowSeconds, Stage2PauseReason reason)
        {
            var snapshot = _controller.Snapshot;
            var hasOutcome = snapshot.State == Stage2State.ConsequencePlaying
                && snapshot.ConsequenceTokenId != Guid.Empty;
            if (_pausePrepared || !CanPauseState(snapshot.State)
                || _mediaHandle.RouteTokenId != snapshot.TokenId
                || !_media.CanPause(snapshot.TokenId, _mediaHandle.MediaTokenId, nowSeconds)
                || (hasOutcome && !_outcome.CanFreeze(snapshot.TokenId, snapshot.ConsequenceTokenId, nowSeconds)))
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
            _preparedReason = Stage2PauseReason.User;
            _preparedOutcome = false;
        }

        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (!_pausePrepared || _snapshot.TokenId == Guid.Empty || _mediaHandle.RouteTokenId != _snapshot.TokenId)
            { handle = default; return false; }
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
                || _outcome.Freeze(handle.CallerTokenId, _snapshot.ConsequenceTokenId, _preparedAtSeconds);
            if (!outcomeFrozen) return false;
            if (!_media.Pause(handle.CallerTokenId, _mediaHandle.MediaTokenId, _preparedAtSeconds, _preparedReason))
            {
                if (_preparedOutcome) _outcome.Resume(handle.CallerTokenId, _snapshot.ConsequenceTokenId, _preparedAtSeconds);
                CancelPreparedPause();
                return false;
            }
            CancelPreparedPause();
            return true;
        }

        public bool ResumeMedia(PauseCallerHandle handle, double nowSeconds)
        {
            var hasOutcome = _snapshot.State == Stage2State.ConsequencePlaying
                && _snapshot.ConsequenceTokenId != Guid.Empty;
            if (!IsCurrent(handle) || !_media.CanResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds)
                || (hasOutcome && !_outcome.CanResume(handle.CallerTokenId, _snapshot.ConsequenceTokenId, nowSeconds)))
                return false;
            if (hasOutcome && !_outcome.Resume(handle.CallerTokenId, _snapshot.ConsequenceTokenId, nowSeconds)) return false;
            if (_media.ResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds).HasValue) return true;
            if (hasOutcome) _outcome.Freeze(handle.CallerTokenId, _snapshot.ConsequenceTokenId, nowSeconds);
            return false;
        }

        public void RefreshSettings(double nowSeconds) => _media.RefreshSettings(nowSeconds);

        private static bool CanPauseState(Stage2State state) => state == Stage2State.Intro
            || state == Stage2State.Choosing || state == Stage2State.Inspecting
            || state == Stage2State.ConsequencePlaying || state == Stage2State.Help
            || state == Stage2State.Leaving;
    }
}
