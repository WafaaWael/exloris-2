using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Boat;

namespace Exloris.Runtime.Recovery
{
    public sealed class Boat3PauseAdapter : IPauseCallerAdapter
    {
        private readonly Boat3Controller _controller;
        private readonly Boat3MediaPresenter _media;
        private readonly Boat3MotionPresenter _motion;
        private readonly Func<Guid> _tokenSource;
        private Boat3Snapshot _snapshot;
        private Boat3MediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAtSeconds;
        private Boat3PauseReason _preparedReason;
        private bool _pausePrepared;
        public Boat3PauseAdapter(Boat3Controller controller, Boat3MediaPresenter media, Boat3MotionPresenter motion, Func<Guid> tokenSource = null)
        { _controller = controller ?? throw new ArgumentNullException(nameof(controller)); _media = media ?? throw new ArgumentNullException(nameof(media)); _motion = motion ?? throw new ArgumentNullException(nameof(motion)); _tokenSource = tokenSource ?? Guid.NewGuid; }
        public RouteId Route => RouteId.Boat3;
        public Boat3Snapshot CapturedSnapshot => _snapshot;
        public void BindMediaHandle(Boat3MediaHandle handle) { if (handle.RouteTokenId != _controller.CurrentTokenId) throw new InvalidOperationException("Boat media handle is stale."); _mediaHandle = handle; }
        public bool PreparePause(double nowSeconds, Boat3PauseReason reason)
        {
            if (_pausePrepared || !_controller.CanHandlePause(nowSeconds)
                || !_media.CanPause(_controller.CurrentTokenId, _mediaHandle.MediaTokenId, nowSeconds)
                || !_motion.CanFreeze(_controller.CurrentTokenId)) return false;
            _preparedAtSeconds = nowSeconds;
            _preparedReason = reason;
            _pausePrepared = true;
            return true;
        }
        public void CancelPreparedPause()
        {
            _pausePrepared = false;
            _preparedAtSeconds = 0d;
            _preparedReason = Boat3PauseReason.Manual;
        }
        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (_snapshot.TokenId == Guid.Empty || _mediaHandle.RouteTokenId != _snapshot.TokenId) { handle = default; return false; }
            var snapshotToken = _tokenSource(); if (snapshotToken == Guid.Empty) throw new InvalidOperationException("Pause snapshot token cannot be empty.");
            _handle = new PauseCallerHandle(Route, _snapshot.TokenId, snapshotToken); handle = _handle; return true;
        }
        public bool IsCurrent(PauseCallerHandle handle) => handle.Equals(_handle) && _controller.CurrentTokenId == handle.CallerTokenId;
        public bool Restore(PauseCallerHandle handle, double nowSeconds) => IsCurrent(handle) && _controller.Restore(_snapshot, nowSeconds);
        public bool PauseMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle) || !_pausePrepared || !_motion.Freeze(handle.CallerTokenId)) return false;
            if (!_media.Pause(handle.CallerTokenId, _mediaHandle.MediaTokenId, _preparedAtSeconds, _preparedReason))
            {
                _motion.Resume(handle.CallerTokenId);
                CancelPreparedPause();
                return false;
            }
            CancelPreparedPause();
            return true;
        }
        public bool ResumeMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle) || !_media.ResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds).HasValue) return false;
            return _motion.Resume(handle.CallerTokenId) || _motion.LastFrame.TokenId == handle.CallerTokenId;
        }
        public void RefreshSettings(double nowSeconds) => _media.RefreshSettings(nowSeconds);
    }
}
