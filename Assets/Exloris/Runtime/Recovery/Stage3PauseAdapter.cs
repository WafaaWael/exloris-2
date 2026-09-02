using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Stage3;

namespace Exloris.Runtime.Recovery
{
    public sealed class Stage3PauseAdapter : IPauseCallerAdapter
    {
        private readonly Stage3Controller _controller;
        private readonly Stage3MediaPresenter _media;
        private readonly Stage3OutcomePresenter _outcome;
        private readonly Func<Guid> _tokenSource;
        private Stage3Snapshot _snapshot;
        private Stage3MediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAt;
        private bool _pausePrepared;
        private bool _preparedOutcome;

        public Stage3PauseAdapter(Stage3Controller controller, Stage3MediaPresenter media,
            Stage3OutcomePresenter outcome, Func<Guid> tokenSource = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _media = media ?? throw new ArgumentNullException(nameof(media));
            _outcome = outcome ?? throw new ArgumentNullException(nameof(outcome));
            _tokenSource = tokenSource ?? Guid.NewGuid;
        }

        public RouteId Route => RouteId.Stage3;
        public Stage3Snapshot CapturedSnapshot => _snapshot;

        public void BindMediaHandle(Stage3MediaHandle handle)
        {
            if (handle.RouteTokenId != _controller.CurrentTokenId)
                throw new InvalidOperationException("Stage 3 media handle is stale.");
            _mediaHandle = handle;
        }

        public bool PreparePause(double now)
        {
            var snapshot = _controller.Snapshot;
            var hasOutcome = snapshot.State == Stage3State.OutcomePlaying && snapshot.OutcomeTokenId != Guid.Empty;
            if (_pausePrepared || !CanPauseState(snapshot.State)
                || _mediaHandle.RouteTokenId != snapshot.TokenId
                || !_media.CanPause(snapshot.TokenId, _mediaHandle.MediaTokenId, now)
                || (hasOutcome && !_outcome.CanFreeze(snapshot.TokenId, snapshot.OutcomeTokenId, now))) return false;
            _preparedAt = now; _preparedOutcome = hasOutcome; _pausePrepared = true; return true;
        }

        public void CancelPreparedPause() { _pausePrepared = false; _preparedAt = 0; _preparedOutcome = false; }

        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (!_pausePrepared || _snapshot.TokenId == Guid.Empty || _mediaHandle.RouteTokenId != _snapshot.TokenId)
            { handle = default; return false; }
            var snapshotToken = _tokenSource();
            if (snapshotToken == Guid.Empty) throw new InvalidOperationException("Pause snapshot token cannot be empty.");
            _handle = new PauseCallerHandle(Route, _snapshot.TokenId, snapshotToken); handle = _handle; return true;
        }

        public bool IsCurrent(PauseCallerHandle handle) => handle.Equals(_handle)
            && _controller.CurrentTokenId == handle.CallerTokenId;
        public bool Restore(PauseCallerHandle handle, double now) =>
            IsCurrent(handle) && _controller.RestoreFromPause(_snapshot, now);

        public bool PauseMedia(PauseCallerHandle handle, double now)
        {
            if (!IsCurrent(handle) || !_pausePrepared) return false;
            var outcomeFrozen = !_preparedOutcome
                || _outcome.Freeze(handle.CallerTokenId, _snapshot.OutcomeTokenId, _preparedAt);
            if (!outcomeFrozen) return false;
            if (!_media.Pause(handle.CallerTokenId, _mediaHandle.MediaTokenId, _preparedAt))
            {
                if (_preparedOutcome) _outcome.Resume(handle.CallerTokenId, _snapshot.OutcomeTokenId, _preparedAt);
                CancelPreparedPause(); return false;
            }
            CancelPreparedPause(); return true;
        }

        public bool ResumeMedia(PauseCallerHandle handle, double now)
        {
            var hasOutcome = _snapshot.State == Stage3State.OutcomePlaying && _snapshot.OutcomeTokenId != Guid.Empty;
            if (!IsCurrent(handle) || !_media.CanResumeAfterMappedUserAction(handle.CallerTokenId, now)
                || (hasOutcome && !_outcome.CanResume(handle.CallerTokenId, _snapshot.OutcomeTokenId, now))) return false;
            if (hasOutcome && !_outcome.Resume(handle.CallerTokenId, _snapshot.OutcomeTokenId, now)) return false;
            if (_media.ResumeAfterMappedUserAction(handle.CallerTokenId, now).HasValue) return true;
            if (hasOutcome) _outcome.Freeze(handle.CallerTokenId, _snapshot.OutcomeTokenId, now);
            return false;
        }

        public void RefreshSettings(double now) => _media.RefreshSettings(now);

        private static bool CanPauseState(Stage3State state) => state == Stage3State.Intro
            || state == Stage3State.ChoosingPlan || state == Stage3State.OutcomePlaying
            || state == Stage3State.OutcomeReady || state == Stage3State.ChoosingRiver
            || state == Stage3State.Help || state == Stage3State.Leaving;
    }
}
