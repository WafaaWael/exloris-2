using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;
using UnityEngine;

namespace Exloris.Runtime.Accessibility
{
    public sealed class IOSReduceMotionAdapter : MonoBehaviour, IReduceMotionAdapter
    {
        public event Action<ReduceMotionOsState> Changed;

        public Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if UNITY_IOS && !UNITY_EDITOR
            return Task.FromResult(
                ExlorisIsReduceMotionEnabled()
                    ? ReduceMotionOsState.Reduced
                    : ReduceMotionOsState.NotReduced);
#else
            return Task.FromResult(ReduceMotionOsState.Unknown);
#endif
        }

        public void NotifyNativeStatusChanged(bool reduced)
        {
            Changed?.Invoke(reduced ? ReduceMotionOsState.Reduced : ReduceMotionOsState.NotReduced);
        }

        private void Awake()
        {
            gameObject.name = "ExlorisAccessibilityBridge";
        }

        private void OnEnable()
        {
#if UNITY_IOS && !UNITY_EDITOR
            ExlorisStartReduceMotionObserver();
#endif
        }

        private void OnDisable()
        {
#if UNITY_IOS && !UNITY_EDITOR
            ExlorisStopReduceMotionObserver();
#endif
        }

        private void OnExlorisReduceMotionChanged(string reduced)
        {
            NotifyNativeStatusChanged(reduced == "1");
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern bool ExlorisIsReduceMotionEnabled();

        [DllImport("__Internal")]
        private static extern void ExlorisStartReduceMotionObserver();

        [DllImport("__Internal")]
        private static extern void ExlorisStopReduceMotionObserver();
#endif
    }
}
