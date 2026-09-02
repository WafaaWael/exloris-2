using System;
using Exloris.Runtime.Boat;

namespace Exloris.Runtime.Recovery
{
    public interface IBoat3HelpView
    {
        void Show(Boat3Snapshot snapshot);
        void Hide();
    }

    public sealed class Boat3HelpController
    {
        private readonly Boat3Controller _parent;
        private readonly IBoat3HelpView _view;
        private Boat3Snapshot _snapshot;
        public Boat3HelpController(Boat3Controller parent, IBoat3HelpView view)
        { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); _view = view ?? throw new ArgumentNullException(nameof(view)); }
        public bool IsVisible { get; private set; }
        public Boat3Snapshot CapturedSnapshot => _snapshot;
        public bool TryShow()
        {
            if (IsVisible) return false;
            _snapshot = _parent.Snapshot;
            if (!_parent.HandleHelp()) return false;
            IsVisible = true; _view.Show(_snapshot); return true;
        }
        public bool HandleBack()
        {
            if (!IsVisible || !_parent.RestoreFromHelp(_snapshot)) return false;
            IsVisible = false; _view.Hide(); return true;
        }
    }
}
