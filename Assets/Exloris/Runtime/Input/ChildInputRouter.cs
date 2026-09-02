using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Exloris.Runtime.Input
{
    public interface IInputGate
    {
        void SetVisibleStateReady(bool ready);
    }

    public sealed class ChildInputRouter : IDisposable, IInputGate
    {
        public const double DebounceSeconds = 0.250d;
        public const float MinimumDragPixels = 8f;

        private readonly InputAction _pointAction;
        private readonly InputAction _pressAction;
        private readonly List<SemanticControl> _controls = new List<SemanticControl>();
        private readonly Dictionary<string, double> _lastAcceptedTimes =
            new Dictionary<string, double>();
        private bool _enableRequested;
        private bool _visibleStateReady;
        private bool _gestureActive;
        private SemanticControl _activeControl;
        private Vector2 _gestureStart;
        private double _gestureStartedAt;
        private double _latestInputTime;
        private bool _disposed;

        public ChildInputRouter(InputAction pointAction, InputAction pressAction)
        {
            _pointAction = pointAction ?? throw new ArgumentNullException(nameof(pointAction));
            _pressAction = pressAction ?? throw new ArgumentNullException(nameof(pressAction));
            _pressAction.started += HandlePressStarted;
            _pressAction.canceled += HandlePressCancelled;
            _pointAction.performed += HandlePointPerformed;
        }

        public event Action<SemanticInputCommand> SemanticCommandIssued;

        public bool InputsActive => _enableRequested && _visibleStateReady;

        public bool TryGetActiveGesture(
            string controlId,
            out double startedAtSeconds,
            out double inputTimeSeconds,
            out bool pointerInside)
        {
            ThrowIfDisposed();
            startedAtSeconds = 0d;
            inputTimeSeconds = Math.Max(_latestInputTime, InputState.currentTime);
            pointerInside = false;
            if (!_gestureActive || _activeControl == null
                || !string.Equals(_activeControl.Id, controlId, StringComparison.Ordinal))
                return false;
            startedAtSeconds = _gestureStartedAt;
            pointerInside = ContainsInclusive(
                _activeControl.ScreenBounds, _pointAction.ReadValue<Vector2>());
            return true;
        }

        public bool CancelActiveGesture(string controlId)
        {
            ThrowIfDisposed();
            if (!_gestureActive || _activeControl == null
                || !string.Equals(_activeControl.Id, controlId, StringComparison.Ordinal))
                return false;
            CancelGesture();
            return true;
        }

        public bool EnabledControlsOverlap()
        {
            ThrowIfDisposed();
            for (var first = 0; first < _controls.Count; first++)
            {
                if (!_controls[first].Enabled) continue;
                var firstBounds = _controls[first].ScreenBounds;
                for (var second = first + 1; second < _controls.Count; second++)
                {
                    if (!_controls[second].Enabled) continue;
                    var secondBounds = _controls[second].ScreenBounds;
                    if (firstBounds.xMin < secondBounds.xMax
                        && firstBounds.xMax > secondBounds.xMin
                        && firstBounds.yMin < secondBounds.yMax
                        && firstBounds.yMax > secondBounds.yMin) return true;
                }
            }
            return false;
        }

        public void Register(SemanticControl control)
        {
            ThrowIfDisposed();
            if (control == null)
            {
                throw new ArgumentNullException(nameof(control));
            }

            foreach (var existing in _controls)
            {
                if (string.Equals(existing.Id, control.Id, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Duplicate semantic control ID '{control.Id}'.");
                }
            }

            _controls.Add(control);
        }

        public void ClearControls()
        {
            ThrowIfDisposed();
            CancelGesture();
            _controls.Clear();
        }

        public bool Unregister(string controlId)
        {
            ThrowIfDisposed();
            if (string.IsNullOrEmpty(controlId)) return false;
            for (var index = 0; index < _controls.Count; index++)
            {
                if (!string.Equals(_controls[index].Id, controlId, StringComparison.Ordinal)) continue;
                if (_activeControl == _controls[index]) CancelGesture();
                _controls.RemoveAt(index);
                _lastAcceptedTimes.Remove(controlId);
                return true;
            }
            return false;
        }

        public void Enable()
        {
            ThrowIfDisposed();
            _enableRequested = true;
            ApplyActionState();
        }

        public void Disable()
        {
            if (_disposed)
            {
                return;
            }

            _enableRequested = false;
            CancelGesture();
            ApplyActionState();
        }

        public void SetVisibleStateReady(bool ready)
        {
            ThrowIfDisposed();
            _visibleStateReady = ready;
            if (!ready)
            {
                CancelGesture();
            }

            ApplyActionState();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _enableRequested = false;
            _visibleStateReady = false;
            CancelGesture();
            _pressAction.started -= HandlePressStarted;
            _pressAction.canceled -= HandlePressCancelled;
            _pointAction.performed -= HandlePointPerformed;
            _pressAction.Disable();
            _pointAction.Disable();
            _disposed = true;
        }

        private void ApplyActionState()
        {
            if (InputsActive)
            {
                _pointAction.Enable();
                _pressAction.Enable();
            }
            else
            {
                _pressAction.Disable();
                _pointAction.Disable();
            }
        }

        private void HandlePressStarted(InputAction.CallbackContext context)
        {
            if (!InputsActive || _gestureActive)
            {
                return;
            }

            var position = ReadPointerPosition(context);
            var control = FindSingleControl(position);
            if (control == null)
            {
                return;
            }

            _gestureActive = true;
            _activeControl = control;
            _gestureStart = position;
            _gestureStartedAt = context.time;
            _latestInputTime = context.time;
        }

        private void HandlePointPerformed(InputAction.CallbackContext context)
        {
            _latestInputTime = Math.Max(_latestInputTime, context.time);
        }

        private void HandlePressCancelled(InputAction.CallbackContext context)
        {
            if (!InputsActive || !_gestureActive || _activeControl == null)
            {
                CancelGesture();
                return;
            }

            var active = _activeControl;
            var start = _gestureStart;
            var end = ReadPointerPosition(context);
            var duration = Math.Max(0d, context.time - _gestureStartedAt);
            _latestInputTime = Math.Max(_latestInputTime, context.time);
            CancelGesture();

            if (!active.Enabled)
            {
                return;
            }

            var movement = Vector2.Distance(start, end);
            SemanticInputKind kind;
            if (active.SupportsDrag && movement >= MinimumDragPixels)
            {
                kind = SemanticInputKind.Drag;
            }
            else
            {
                if (!ContainsInclusive(active.ScreenBounds, end))
                {
                    return;
                }

                kind = SemanticInputKind.Tap;
            }

            if (IsDebounced(active.Id, context.time))
            {
                return;
            }

            _lastAcceptedTimes[active.Id] = context.time;
            SemanticCommandIssued?.Invoke(new SemanticInputCommand(
                active.Id,
                kind,
                start,
                end,
                context.time,
                duration));
        }

        private SemanticControl FindSingleControl(Vector2 position)
        {
            SemanticControl match = null;
            foreach (var control in _controls)
            {
                if (!control.Enabled || !ContainsInclusive(control.ScreenBounds, position))
                {
                    continue;
                }

                if (match != null)
                {
                    return null;
                }

                match = control;
            }

            return match;
        }

        private bool IsDebounced(string controlId, double inputTime)
        {
            return _lastAcceptedTimes.TryGetValue(controlId, out var lastAccepted) &&
                   inputTime - lastAccepted < DebounceSeconds;
        }

        private Vector2 ReadPointerPosition(InputAction.CallbackContext context)
        {
            if (context.control?.device is Pointer pointer)
            {
                return pointer.position.ReadValue();
            }

            return _pointAction.ReadValue<Vector2>();
        }

        private static bool ContainsInclusive(Rect rect, Vector2 point)
        {
            return point.x >= rect.xMin && point.x <= rect.xMax &&
                   point.y >= rect.yMin && point.y <= rect.yMax;
        }

        private void CancelGesture()
        {
            _gestureActive = false;
            _activeControl = null;
            _gestureStart = default;
            _gestureStartedAt = 0d;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ChildInputRouter));
            }
        }
    }
}
