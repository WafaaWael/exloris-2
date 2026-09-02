using System;
using Exloris.Runtime.Stage2;

namespace Exloris.Runtime.Recovery
{
    public enum Stage2HelpMode { ChooseStoneThenPlaces, InspectEachPlace }

    public interface IStage2HelpView
    {
        void Show(Stage2Snapshot snapshot, Stage2HelpMode mode);
        void Hide();
    }

    public sealed class Stage2HelpController
    {
        private readonly Stage2Controller _parent;
        private readonly IStage2HelpView _view;
        private Stage2Snapshot _snapshot;

        public Stage2HelpController(Stage2Controller parent, IStage2HelpView view)
        { _parent = parent ?? throw new ArgumentNullException(nameof(parent)); _view = view ?? throw new ArgumentNullException(nameof(view)); }

        public bool IsVisible { get; private set; }
        public Stage2HelpMode LastMode { get; private set; }
        public Stage2Snapshot CapturedSnapshot => _snapshot;

        public bool TryShow()
        {
            if (IsVisible) return false;
            _snapshot = _parent.Snapshot;
            if (!TryResolveMode(_snapshot, out var mode) || !_parent.HandleHelp(_snapshot.TokenId)) return false;
            LastMode = mode; IsVisible = true; _view.Show(_snapshot, mode); return true;
        }

        public bool HandleBack()
        {
            if (!IsVisible || !_parent.RestoreFromHelp(_snapshot)) return false;
            IsVisible = false; _view.Hide(); return true;
        }

        private static bool TryResolveMode(Stage2Snapshot snapshot, out Stage2HelpMode mode)
        {
            if (snapshot.State == Stage2State.Intro || snapshot.State == Stage2State.Choosing)
            { mode = Stage2HelpMode.ChooseStoneThenPlaces; return true; }
            if (snapshot.State == Stage2State.Inspecting)
            { mode = Stage2HelpMode.InspectEachPlace; return true; }
            mode = default; return false;
        }
    }
}
