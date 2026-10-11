using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>
    /// Turns keyboard (A/D, arrows), swipes and left/right screen taps into lane steps (-1 / +1).
    /// A touch that swipes far enough counts as a swipe; otherwise its release counts as a tap on that screen half.
    /// </summary>
    public class BoatInput : MonoBehaviour
    {
        public event Action<int> OnLaneStep;

        private const float SwipeInches = 0.3f;
        private const float FallbackDpi = 160f;

        [field: SerializeField] public bool IsInputEnabled { get; set; }

        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);
        private PointerEventData _pointerData;
        private Vector2 _pressStart;
        private bool _isTracking;
        private bool _didSwipe;
        private float _swipePixels;

        private void Awake()
        {
            float dpi = Screen.dpi > 1f ? Screen.dpi : FallbackDpi;
            _swipePixels = SwipeInches * dpi;
        }

        private void Update()
        {
            if (IsInputEnabled == false)
            {
                _isTracking = false;
                return;
            }

            ReadKeyboard();
            ReadPointer();
        }

        private void ReadKeyboard()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                OnLaneStep?.Invoke(-1);
            }

            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                OnLaneStep?.Invoke(1);
            }
        }

        private void ReadPointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            Vector2 position = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
            {
                _isTracking = IsOverUi(position) == false;
                _didSwipe = false;
                _pressStart = position;
            }

            if (_isTracking == false)
            {
                return;
            }

            if (_didSwipe == false)
            {
                float dx = position.x - _pressStart.x;
                if (Mathf.Abs(dx) >= _swipePixels)
                {
                    _didSwipe = true;
                    OnLaneStep?.Invoke(dx > 0f ? 1 : -1);
                }
            }

            if (pointer.press.wasReleasedThisFrame)
            {
                _isTracking = false;
                if (_didSwipe == false)
                {
                    OnLaneStep?.Invoke(_pressStart.x < Screen.width * 0.5f ? -1 : 1);
                }
            }
        }

        private bool IsOverUi(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return false;
            }

            if (_pointerData == null)
            {
                _pointerData = new PointerEventData(eventSystem);
            }

            _pointerData.position = screenPosition;
            _uiHits.Clear();
            eventSystem.RaycastAll(_pointerData, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
