using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Exloris.Core.Accessibility;

namespace Exloris.Runtime.Accessibility
{
    public sealed class AccessibilityCoordinator : IDisposable
    {
        public static readonly TimeSpan MaximumPreflight = TimeSpan.FromMilliseconds(250);

        private readonly AccessibilityState _state;
        private readonly IReduceMotionAdapter _adapter;
        private readonly List<IReducedMotionParticipant> _participants =
            new List<IReducedMotionParticipant>();
        private bool _introModeLatched;
        private bool _introReducedMotion = true;

        public AccessibilityCoordinator(AccessibilityState state, IReduceMotionAdapter adapter)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _adapter.Changed += HandleAdapterChanged;
            CurrentAppliedReducedMotion = _state.EffectiveReducedMotion;
        }

        public bool IntroModeLatched => _introModeLatched;
        public bool IntroReducedMotion => _introReducedMotion;
        public bool CurrentAppliedReducedMotion { get; private set; }
        public int OsQueryCount { get; private set; }

        public void Register(IReducedMotionParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            if (string.IsNullOrWhiteSpace(participant.StaticEquivalentId))
            {
                throw new ArgumentException(
                    "Every animated participant must name its static equivalent.",
                    nameof(participant));
            }

            if (_participants.Contains(participant))
            {
                return;
            }

            _participants.Add(participant);
            participant.ApplyReducedMotion(
                _introModeLatched ? _introReducedMotion : _state.EffectiveReducedMotion);
        }

        public async Task<bool> PreflightIntroAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var boundedTimeout = timeout <= MaximumPreflight ? timeout : MaximumPreflight;
            if (boundedTimeout <= TimeSpan.Zero)
            {
                boundedTimeout = MaximumPreflight;
            }

            _state.SetOsState(ReduceMotionOsState.Unknown);
            ApplyParticipants(_state.EffectiveReducedMotion);

            var result = await QueryWithTimeoutAsync(boundedTimeout, cancellationToken);
            _state.SetOsState(result);
            _introReducedMotion = _state.EffectiveReducedMotion;
            _introModeLatched = true;
            ApplyParticipants(_introReducedMotion);
            return _introReducedMotion;
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            var result = await QueryWithTimeoutAsync(MaximumPreflight, cancellationToken);
            _state.SetOsState(result);
            ApplyUnlessIntroLatched();
        }

        public void SetAlwaysReduce(bool value)
        {
            _state.SetAlwaysReduce(value);
            ApplyUnlessIntroLatched();
        }

        public void ReleaseIntroLatchAtWelcome()
        {
            _introModeLatched = false;
            ApplyParticipants(_state.EffectiveReducedMotion);
        }

        public void Dispose()
        {
            _adapter.Changed -= HandleAdapterChanged;
        }

        private async Task<ReduceMotionOsState> QueryWithTimeoutAsync(
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            OsQueryCount++;
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    var query = _adapter.QueryAsync(linked.Token);
                    var delay = Task.Delay(timeout, cancellationToken);
                    var completed = await Task.WhenAny(query, delay);
                    if (completed != query)
                    {
                        linked.Cancel();
                        return ReduceMotionOsState.Unknown;
                    }

                    return await query;
                }
                catch (OperationCanceledException)
                {
                    return ReduceMotionOsState.Unknown;
                }
                catch
                {
                    return ReduceMotionOsState.Unknown;
                }
            }
        }

        private void HandleAdapterChanged(ReduceMotionOsState state)
        {
            _state.SetOsState(state);
            ApplyUnlessIntroLatched();
        }

        private void ApplyUnlessIntroLatched()
        {
            if (!_introModeLatched)
            {
                ApplyParticipants(_state.EffectiveReducedMotion);
            }
        }

        private void ApplyParticipants(bool reducedMotion)
        {
            CurrentAppliedReducedMotion = reducedMotion;
            foreach (var participant in _participants)
            {
                participant.ApplyReducedMotion(reducedMotion);
            }
        }
    }
}
