using System;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;
using UnityEngine;

namespace Exloris.Runtime.Accessibility
{
    [DisallowMultipleComponent]
    public sealed class FixedReduceMotionAdapter : MonoBehaviour, IReduceMotionAdapter
    {
        private ReduceMotionOsState _state = ReduceMotionOsState.Unknown;

        public event Action<ReduceMotionOsState> Changed;

        public void Configure(bool reducedMotion)
        {
            _state = reducedMotion ? ReduceMotionOsState.Reduced : ReduceMotionOsState.NotReduced;
        }

        public Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_state);
        }
    }
}
