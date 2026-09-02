namespace Exloris.Runtime.Diagnostics
{
    public static class ReleaseSoakGate
    {
        public const double RequiredSeconds = 1800d;
        public const float P95BudgetMilliseconds = 33.3f;
        public const float P99BudgetMilliseconds = 50f;
        public const int MaximumConsecutiveSlowFrames = 2;
        public const float ReservedMemoryBudgetMiB = 512f;

        public static bool Passes(
            double elapsedSeconds,
            int routeCount,
            int pauseCount,
            int audioInterruptionCount,
            float p95FrameMilliseconds,
            float p99FrameMilliseconds,
            int maximumConsecutiveSlowFrames,
            float maximumReservedMiB,
            int maximumActiveVoiceCount,
            bool routeDrift)
        {
            return elapsedSeconds >= RequiredSeconds
                && routeCount == 15
                && pauseCount > 0
                && audioInterruptionCount > 0
                && p95FrameMilliseconds <= P95BudgetMilliseconds
                && p99FrameMilliseconds <= P99BudgetMilliseconds
                && maximumConsecutiveSlowFrames <= MaximumConsecutiveSlowFrames
                && maximumReservedMiB <= ReservedMemoryBudgetMiB
                && maximumActiveVoiceCount <= 1
                && !routeDrift;
        }
    }
}
