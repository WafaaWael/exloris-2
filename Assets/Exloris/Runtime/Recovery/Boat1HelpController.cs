using System;
using Exloris.Runtime.Boat;

namespace Exloris.Runtime.Recovery
{
    public interface IBoat1HelpView
    {
        void Show(Boat1Snapshot snapshot);
        void Hide();
    }

    public sealed class Boat1HelpController
    {
        private readonly Boat1Controller _parent;
        private readonly IBoat1HelpView _view;
        private Boat1Snapshot _snapshot;
        public Boat1HelpController(Boat1Controller parent, IBoat1HelpView view)
        { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); _view = view ?? throw new ArgumentNullException(nameof(view)); }
        public bool IsVisible { get; private set; }
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
