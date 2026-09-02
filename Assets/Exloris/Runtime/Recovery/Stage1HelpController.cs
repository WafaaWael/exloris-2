using System;
using Exloris.Runtime.Stage1;

namespace Exloris.Runtime.Recovery
{
    public enum Stage1HelpMode
    {
        AttachThenMove,
        Attach,
        Move,
        AdjustUp,
        AdjustDown,
    }

    public interface IStage1HelpView
    {
        void Show(Stage1Snapshot snapshot, Stage1HelpMode mode);
        void Hide();
    }

    public sealed class Stage1HelpController
    {
        private readonly Stage1Controller _parent;
        private readonly IStage1HelpView _view;
        private Stage1Snapshot _snapshot;

        public Stage1HelpController(Stage1Controller parent, IStage1HelpView view)
        {
            _parent = parent ?? throw new ArgumentNullException(nameof(parent));
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsVisible { get; private set; }
        public Stage1HelpMode LastMode { get; private set; }

        public bool TryShow()
        {
            if (IsVisible) return false;
            _snapshot = _parent.Snapshot;
            if (!TryResolveMode(_snapshot, out var mode) || !_parent.HandleHelp(_snapshot.TokenId)) return false;
            LastMode = mode;
            IsVisible = true;
            _view.Show(_snapshot, mode);
            return true;
        }

        public bool HandleBack()
        {
            if (!IsVisible || !_parent.RestoreFromHelp(_snapshot)) return false;
            IsVisible = false;
            _view.Hide();
            return true;
        }

        public static Stage1HelpMode ResolveMode(Stage1Snapshot snapshot)
        {
            if (!TryResolveMode(snapshot, out var mode))
                throw new InvalidOperationException("Stage 1 Help has no mapped next action for this state.");
            return mode;
        }

        private static bool TryResolveMode(Stage1Snapshot snapshot, out Stage1HelpMode mode)
        {
            if (snapshot.State == Stage1State.Intro)
            {
                mode = Stage1HelpMode.AttachThenMove;
                return true;
            }
            if (snapshot.State == Stage1State.Playing)
            {
                mode = snapshot.MarkerAttached ? Stage1HelpMode.Move : Stage1HelpMode.Attach;
                return true;
            }
            if (snapshot.State == Stage1State.OutcomeReady && snapshot.Outcome == Stage1OutcomeKind.OutsideLow)
            {
                mode = Stage1HelpMode.AdjustUp;
                return true;
            }
            if (snapshot.State == Stage1State.OutcomeReady && snapshot.Outcome == Stage1OutcomeKind.OutsideHigh)
            {
                mode = Stage1HelpMode.AdjustDown;
                return true;
            }
            mode = default;
            return false;
        }
    }
}
