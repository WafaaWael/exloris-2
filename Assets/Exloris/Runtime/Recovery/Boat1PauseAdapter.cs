using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Boat;

namespace Exloris.Runtime.Recovery
{
    public sealed class Boat1PauseAdapter : IPauseCallerAdapter
    {
        private readonly Boat1Controller _controller;
        private readonly Boat1MediaPresenter _media;
        private readonly Boat1MotionPresenter _motion;
        private readonly Func<Guid> _tokenSource;
        private Boat1Snapshot _snapshot;
        private Boat1MediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAtSeconds;
        private Boat1PauseReason _preparedReason;
        private bool _pausePrepared;
        public Boat1PauseAdapter(Boat1Controller controller, Boat1MediaPresenter media, Boat1MotionPresenter motion, Func<Guid> tokenSource = null)
        { _controller = controller ?? throw new ArgumentNullException(nameof(controller)); _media = media ?? throw new ArgumentNullException(nameof(media)); _motion = motion ?? throw new ArgumentNullException(nameof(motion)); _tokenSource = tokenSource ?? Guid.NewGuid; }
        public RouteId Route => RouteId.Boat1;
        public Boat1Snapshot CapturedSnapshot => _snapshot;
        public void BindMediaHandle(Boat1MediaHandle handle) { if (handle.RouteTokenId != _controller.CurrentTokenId) throw new InvalidOperationException("Boat media handle is stale."); _mediaHandle = handle; }
        public bool PreparePause(double nowSeconds, Boat1PauseReason reason)
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
            _preparedReason = Boat1PauseReason.Manual;
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
