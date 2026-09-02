using System;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;

namespace Exloris.Runtime.Accessibility
{
    public interface IReduceMotionAdapter
    {
        event Action<ReduceMotionOsState> Changed;
        Task<ReduceMotionOsState> QueryAsync(CancellationToken cancellationToken);
    }

    public interface IReducedMotionParticipant
    {
        string StaticEquivalentId { get; }
        void ApplyReducedMotion(bool reducedMotion);
    }
}
