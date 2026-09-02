using System;

namespace Exloris.Runtime.Recovery
{
    public sealed class JourneyOverviewPauseHelpController : IJourneyOverviewPauseHelpSink
    {
        private readonly IPauseOverlayReturnSink _parent;
        private readonly IJourneyOverviewPauseHelpView _view;
        private PauseOverlaySnapshot _snapshot;
        private bool _visible;

        public JourneyOverviewPauseHelpController(
            IPauseOverlayReturnSink parent,
            IJourneyOverviewPauseHelpView view)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsVisible => _visible;

        public bool TryShow(PauseOverlaySnapshot snapshot)
        {
            if (_visible || !_parent.IsCurrentPause(snapshot)) return false;
            _snapshot = snapshot;
            _visible = true;
            _view.ShowAbovePause(snapshot);
            return true;
        }

        public bool HandleBack()
        {
            if (!_visible || !_parent.RestoreFromHelp(_snapshot)) return false;
            _visible = false;
            _view.Hide();
            return true;
        }
    }
}
