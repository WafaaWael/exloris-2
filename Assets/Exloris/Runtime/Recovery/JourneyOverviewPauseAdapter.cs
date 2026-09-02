using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Overview;

namespace Exloris.Runtime.Recovery
{
    public sealed class JourneyOverviewPauseAdapter : IPauseCallerAdapter
    {
        private readonly JourneyOverviewController _controller;
        private readonly IJourneyOverviewPauseMedia _media;
        private readonly Func<Guid> _tokenSource;
        private JourneyOverviewSnapshot _snapshot;
        private PauseCallerHandle _handle;
        public JourneyOverviewPauseAdapter(JourneyOverviewController controller, IJourneyOverviewPauseMedia media, Func<Guid> tokenSource = null)
        { _controller = controller ?? throw new ArgumentNullException(nameof(controller)); _media = media ?? throw new ArgumentNullException(nameof(media)); _tokenSource = tokenSource ?? Guid.NewGuid; }
        public RouteId Route => RouteId.JourneyOverview;
        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (_snapshot.TokenId == Guid.Empty || _media.CurrentRevealTokenId != _snapshot.TokenId) { handle = default; return false; }
            var snapshotToken = _tokenSource(); if (snapshotToken == Guid.Empty) throw new InvalidOperationException("Pause snapshot token cannot be empty.");
            _handle = new PauseCallerHandle(Route, _snapshot.TokenId, snapshotToken); handle = _handle; return true;
        }
        public bool IsCurrent(PauseCallerHandle handle) => handle.Equals(_handle) && _controller.CurrentTokenId == handle.CallerTokenId;
        public bool Restore(PauseCallerHandle handle, double nowSeconds) => IsCurrent(handle) && _controller.Restore(_snapshot) && _controller.Advance(handle.CallerTokenId, nowSeconds);
        public bool PauseMedia(PauseCallerHandle handle, double nowSeconds) => IsCurrent(handle) && _media.IsPaused;
        public bool ResumeMedia(PauseCallerHandle handle, double nowSeconds) => IsCurrent(handle) && _media.ResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds).HasValue;
        public void RefreshSettings(double nowSeconds) => _media.RefreshSettings(nowSeconds);
        public JourneyOverviewSnapshot CapturedSnapshot => _snapshot;
    }
}
