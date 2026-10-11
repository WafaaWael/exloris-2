using System;
using BoatGame.CoreData;
using BoatGame.CoreData.Messaging;
using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.SharedUI
{
    /// <summary>
    /// Pose-swapping character (world sprite or UI image) with gentle breathing and an optional hop.
    /// A voice-driven character holds the pose's talking frame for the whole voice line and settles back to the
    /// idle pose when the line ends (no per-syllable flicker, no pop on swaps). Missing poses fall back to Idle.
    /// </summary>
    public class CharacterExpressions : MonoBehaviour
    {
        [Serializable]
        public struct MoodFrames
        {
            public CharacterMood Mood;
            public Sprite Closed;
            public Sprite Talk;
        }

        private const float HopDuration = 0.45f;

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Image _image;
        [SerializeField] private MoodFrames[] _frames = Array.Empty<MoodFrames>();
        [SerializeField] private bool _isVoiceDriven;
        [SerializeField] private float _breathAmount = 0.012f;
        [SerializeField] private float _breathSpeed = 2.2f;
        [SerializeField] private float _hopHeight = 0.35f;

        private Transform _transform;
        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private MoodFrames _current;
        private MoodFrames _nextLine;     // voice-driven: pose picked for the coming line, shown once the voice starts
        private bool _isTalking;
        private float _breathPhase;
        private float _hopTime = HopDuration;

        public CharacterMood Mood => _current.Mood;

        public void SetMood(CharacterMood mood)
        {
            MoodFrames frames = FindFrames(mood);
            if (_isVoiceDriven && _isTalking == false)
            {
                // stay idle until the line is actually spoken, so the only swaps are idle -> talk -> idle
                _nextLine = frames;
                return;
            }

            if (frames.Closed == _current.Closed && frames.Mood == _current.Mood)
            {
                return;
            }

            _current = frames;
            ApplySprite();
        }

        public void PlayHop()
        {
            _hopTime = 0f;
        }

        private void Awake()
        {
            _transform = transform;
            _baseScale = _transform.localScale;
            _basePosition = _transform.localPosition;
            _breathPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            _current = FindFrames(CharacterMood.Idle);
            _nextLine = _current;
            ApplySprite();
        }

        private void OnEnable()
        {
            if (_isVoiceDriven)
            {
                EventBus.Subscribe<VoiceActivityEvent>(VoiceActivityCallback);
            }
        }

        private void OnDisable()
        {
            if (_isVoiceDriven)
            {
                EventBus.Unsubscribe<VoiceActivityEvent>(VoiceActivityCallback);
            }

            _isTalking = false;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _breathPhase += dt * _breathSpeed;
            _hopTime += dt;

            float breath = Mathf.Sin(_breathPhase) * _breathAmount;
            _transform.localScale = new Vector3(_baseScale.x * (1f - breath * 0.5f), _baseScale.y * (1f + breath), _baseScale.z);
            _transform.localPosition = _basePosition + new Vector3(0f, ComputeHop(_hopTime, _hopHeight), 0f);
        }

        private static float ComputeHop(float time, float height)
        {
            if (time >= HopDuration)
            {
                return 0f;
            }

            float t = time / HopDuration;
            return 4f * t * (1f - t) * height;
        }

        private MoodFrames FindFrames(CharacterMood mood)
        {
            int idle = -1;
            for (int i = 0; i < _frames.Length; i++)
            {
                if (_frames[i].Mood == mood)
                {
                    return _frames[i];
                }

                if (_frames[i].Mood == CharacterMood.Idle)
                {
                    idle = i;
                }
            }

            return idle >= 0 ? _frames[idle] : default;
        }

        private void ApplySprite()
        {
            Sprite sprite = _isTalking && _current.Talk != null ? _current.Talk : _current.Closed;
            if (sprite == null)
            {
                return;
            }

            if (_renderer != null)
            {
                _renderer.sprite = sprite;
            }

            if (_image != null)
            {
                _image.sprite = sprite;
            }
        }

        private void VoiceActivityCallback(VoiceActivityEvent evt)
        {
            _isTalking = evt.IsSpeaking;
            if (_isTalking)
            {
                _current = _nextLine;
            }
            else
            {
                // line finished: back to the resting pose until the next line picks a new one
                _current = FindFrames(CharacterMood.Idle);
                _nextLine = _current;
            }

            ApplySprite();
        }
    }
}
