namespace Exloris.Core.Accessibility
{
    public enum ReduceMotionOsState
    {
        Unknown,
        NotReduced,
        Reduced,
    }

    public sealed class AccessibilityState
    {
        public AccessibilityState(bool alwaysReduce, bool captionsEnabled)
        {
            AlwaysReduce = alwaysReduce;
            CaptionsEnabled = captionsEnabled;
            OsState = ReduceMotionOsState.Unknown;
        }

        public bool AlwaysReduce { get; private set; }
        public bool CaptionsEnabled { get; private set; }
        public ReduceMotionOsState OsState { get; private set; }

        public bool EffectiveReducedMotion =>
            OsState != ReduceMotionOsState.NotReduced || AlwaysReduce;

        public void SetAlwaysReduce(bool value)
        {
            AlwaysReduce = value;
        }

        public void SetCaptionsEnabled(bool value)
        {
            CaptionsEnabled = value;
        }

        public void SetOsState(ReduceMotionOsState state)
        {
            OsState = state;
        }
    }
}
