using System;
using Exloris.Runtime.Stage3;

namespace Exloris.Runtime.Recovery
{
    public sealed class Stage3HelpController
    {
        private readonly Stage3Controller _parent;
        private readonly IStage3HelpView _view;
        private Stage3Snapshot _snapshot;

        public Stage3HelpController(Stage3Controller parent, IStage3HelpView view)
        { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); _view = view ?? throw new ArgumentNullException(nameof(view)); }

        public bool IsVisible { get; private set; }
        public Stage3HelpMode LastMode { get; private set; }
        public Stage3Snapshot CapturedSnapshot => _snapshot;

        public bool TryShow(bool reducedMotion)
        {
            if (IsVisible) return false;
            _snapshot = _parent.Snapshot;
            if (!_parent.HandleHelp(_snapshot.TokenId)) return false;
            LastMode = ModeFor(_snapshot); IsVisible = true;
            _view.Present(LastMode, _snapshot, reducedMotion); return true;
        }

        public bool HandleBack()
        {
            if (!IsVisible || !_parent.RestoreFromHelp(_snapshot)) return false;
            IsVisible = false; _view.SetVisible(false); return true;
        }

        public static Stage3HelpMode ModeFor(Stage3Snapshot caller)
        {
            if (caller.State == Stage3State.OutcomePlaying || caller.State == Stage3State.OutcomeReady)
                return Stage3HelpMode.OutcomeFacts;
            if (caller.State == Stage3State.ChoosingRiver)
                return Stage3HelpMode.RiverSelection;
            return Stage3HelpMode.EqualPlans;
        }
    }
}
