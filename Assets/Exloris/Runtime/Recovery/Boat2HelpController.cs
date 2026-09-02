using System;
using Exloris.Runtime.Boat;

namespace Exloris.Runtime.Recovery
{
    public interface IBoat2HelpView
    {
        void Show(Boat2Snapshot snapshot);
        void Hide();
    }

    public sealed class Boat2HelpController
    {
        private readonly Boat2Controller _parent;
        private readonly IBoat2HelpView _view;
        private Boat2Snapshot _snapshot;
        public Boat2HelpController(Boat2Controller parent, IBoat2HelpView view)
        { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); _view = view ?? throw new ArgumentNullException(nameof(view)); }
        public bool IsVisible { get; private set; }
        public Boat2Snapshot CapturedSnapshot => _snapshot;
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
