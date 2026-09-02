using System;
using Exloris.Core.Navigation;
using Exloris.Runtime.Completion;

namespace Exloris.Runtime.Recovery
{
    public sealed class CompletionPauseAdapter : IPauseCallerAdapter
    {
        private readonly CompletionController _controller;
        private readonly CompletionMediaPresenter _media;
        private readonly Func<Guid> _tokenSource;
        private CompletionSnapshot _snapshot;
        private CompletionMediaHandle _mediaHandle;
        private PauseCallerHandle _handle;
        private double _preparedAt;
        private bool _prepared;

        public CompletionPauseAdapter(
            CompletionController controller,
            CompletionMediaPresenter media,
            Func<Guid> tokenSource = null)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _media = media ?? throw new ArgumentNullException(nameof(media));
            _tokenSource = tokenSource ?? Guid.NewGuid;
        }

        public RouteId Route => RouteId.Completion;
        public CompletionSnapshot CapturedSnapshot => _snapshot;

        public void BindMediaHandle(CompletionMediaHandle handle)
        {
            if (handle.CompletionTokenId != _controller.CurrentTokenId)
                throw new InvalidOperationException("Completion media handle is stale.");
            _mediaHandle = handle;
        }

        public bool PreparePause(double nowSeconds)
        {
            var snapshot = _controller.Snapshot;
            if (_prepared || (snapshot.State != CompletionState.Presenting && snapshot.State != CompletionState.Ready)
                || _mediaHandle.CompletionTokenId != snapshot.TokenId) return false;
            _preparedAt = nowSeconds;
            _prepared = true;
            return true;
        }

        public bool TryCapture(out PauseCallerHandle handle)
        {
            _snapshot = _controller.Snapshot;
            if (!_prepared || _snapshot.TokenId == Guid.Empty)
            { handle = default; return false; }
            var snapshotToken = _tokenSource();
            if (snapshotToken == Guid.Empty) throw new InvalidOperationException("Completion pause snapshot token is empty.");
            _handle = new PauseCallerHandle(Route, _snapshot.TokenId, snapshotToken);
            handle = _handle;
            return true;
        }

        public bool IsCurrent(PauseCallerHandle handle)
            => handle.Equals(_handle) && _controller.CurrentTokenId == handle.CallerTokenId;

        public bool Restore(PauseCallerHandle handle, double nowSeconds)
            => IsCurrent(handle) && _controller.RestoreFromPause(_snapshot, nowSeconds);

        public bool PauseMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle) || !_prepared) return false;
            var accepted = _media.Pause(handle.CallerTokenId, _mediaHandle.MediaTokenId, _preparedAt);
            _prepared = false;
            return accepted;
        }

        public bool ResumeMedia(PauseCallerHandle handle, double nowSeconds)
        {
            if (!IsCurrent(handle)) return false;
            var resumed = _media.ResumeAfterMappedUserAction(handle.CallerTokenId, nowSeconds);
            if (!resumed.HasValue) return false;
            _mediaHandle = resumed.Value;
            return true;
        }

        public void RefreshSettings(double nowSeconds) => _media.RefreshSettings(nowSeconds);
    }
}
