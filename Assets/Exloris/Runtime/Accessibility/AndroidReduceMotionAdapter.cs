using System;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;
using UnityEngine;

namespace Exloris.Runtime.Accessibility
{
    public sealed class AndroidReduceMotionAdapter : MonoBehaviour, IReduceMotionAdapter
    {
        private const float PollIntervalSeconds = 0.5f;
        private float _nextPollTime;
        private ReduceMotionOsState _lastState = ReduceMotionOsState.Unknown;

        public event Action<ReduceMotionOsState> Changed;

        public Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _lastState = Query();
            return Task.FromResult(_lastState);
        }

        public void NotifySystemSettingChanged()
        {
            var state = Query();
            if (state != ReduceMotionOsState.Unknown)
            {
                _lastState = state;
                Changed?.Invoke(state);
            }
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Time.unscaledTime < _nextPollTime)
            {
                return;
            }

            _nextPollTime = Time.unscaledTime + PollIntervalSeconds;
            var state = Query();
            if (state != ReduceMotionOsState.Unknown && state != _lastState)
            {
                _lastState = state;
                Changed?.Invoke(state);
            }
#endif
        }

        private static ReduceMotionOsState Query()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    var sdk = version.GetStatic<int>("SDK_INT");
                    if (sdk >= 26)
                    {
                        using (var animator = new AndroidJavaClass("android.animation.ValueAnimator"))
                        {
                            return animator.CallStatic<bool>("areAnimatorsEnabled")
                                ? ReduceMotionOsState.NotReduced
                                : ReduceMotionOsState.Reduced;
                        }
                    }
                }

                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var resolver = activity.Call<AndroidJavaObject>("getContentResolver"))
                using (var settings = new AndroidJavaClass("android.provider.Settings$Global"))
                {
                    var scale = settings.CallStatic<float>(
                        "getFloat",
                        resolver,
                        "animator_duration_scale",
                        1.0f);
                    return scale <= 0.0f
                        ? ReduceMotionOsState.Reduced
                        : ReduceMotionOsState.NotReduced;
                }
            }
            catch
            {
                return ReduceMotionOsState.Unknown;
            }
#else
            return ReduceMotionOsState.Unknown;
#endif
        }
    }
}
