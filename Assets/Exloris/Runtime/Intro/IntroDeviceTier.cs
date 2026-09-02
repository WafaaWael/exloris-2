using System;
using UnityEngine;

namespace Exloris.Runtime.Intro
{
    public static class IntroDeviceTier
    {
        public static bool ShouldPreferLowResolution(
            string deviceModel,
            RuntimePlatform platform,
            int androidApiLevel,
            int systemMemoryMb)
        {
            if (systemMemoryMb > 0 && systemMemoryMb <= 2048)
            {
                return true;
            }

            var model = deviceModel ?? string.Empty;
            if (platform == RuntimePlatform.IPhonePlayer)
            {
                return model.IndexOf("iPhone14,6", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       model.IndexOf("iPhone SE (3rd generation)", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return platform == RuntimePlatform.Android &&
                   androidApiLevel <= 27 &&
                   model.IndexOf("Nexus 5X", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool ShouldPreferLowResolutionOnCurrentDevice()
        {
            var apiLevel = int.MaxValue;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    apiLevel = version.GetStatic<int>("SDK_INT");
                }
            }
            catch
            {
                apiLevel = int.MaxValue;
            }
#endif
            return ShouldPreferLowResolution(
                SystemInfo.deviceModel,
                Application.platform,
                apiLevel,
                SystemInfo.systemMemorySize);
        }
    }
}
