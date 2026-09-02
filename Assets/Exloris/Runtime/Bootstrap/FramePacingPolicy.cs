using UnityEngine;

namespace Exloris.Runtime.Bootstrap
{
    public readonly struct FramePacingSettings
    {
        public FramePacingSettings(int vSyncCount, int targetFrameRate)
        {
            VSyncCount = vSyncCount;
            TargetFrameRate = targetFrameRate;
        }

        public int VSyncCount { get; }
        public int TargetFrameRate { get; }
    }

    public static class FramePacingPolicy
    {
        public static FramePacingSettings For(RuntimePlatform platform)
        {
            if (platform == RuntimePlatform.OSXPlayer ||
                platform == RuntimePlatform.WindowsPlayer ||
                platform == RuntimePlatform.LinuxPlayer)
                return new FramePacingSettings(vSyncCount: 1, targetFrameRate: -1);

            return new FramePacingSettings(vSyncCount: 0, targetFrameRate: 60);
        }
    }
}
