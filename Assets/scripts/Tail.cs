using UnityEngine;
using BrunoMikoski.AnimationSequencer;
using System.Collections;
using System.Collections.Generic;

public class Tail : MonoBehaviour
{
        [SerializeField] private List<AnimationSequencer>  _buttonAnimation;
        [SerializeField] private Color _hilightColor = Color.yellow;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField, Min(0f)] private float _glowDuration = 3f;

        private Coroutine _glowRoutine;
        private SpriteRenderer _activeTick;
        private GameObject _activeGlow;

        public float GlowDuration => _glowDuration;
        private void Start()
        {
            foreach (var button in _buttonAnimation)
            {
                button.Play();
            }
        }
        /// <summary>
        /// Highlights the tick and shows its glow briefly, then restores its normal appearance.
        /// The duration is controlled by <see cref="_glowDuration"/> in the Inspector.
        /// </summary>
        public void SetGlow(SpriteRenderer tick, GameObject glow)
        {
            if (tick == null)
            {
                return;
            }

            // A persistent button event may disable this Tail before its SetGlow
            // event is invoked. Restore the panel so the glow can be shown, then
            // let NilometerUI close it after the glow duration.
            if (gameObject.activeInHierarchy == false)
            {
                gameObject.SetActive(true);
            }

            if (isActiveAndEnabled == false)
            {
                return;
            }

            if (_glowRoutine != null)
            {
                StopCoroutine(_glowRoutine);
                ResetGlow();
            }

            _activeTick = tick;
            _activeGlow = glow;
            _glowRoutine = StartCoroutine(ShowGlowForDuration());
        }

        // Keeps existing UnityEvent hookups that pass a GameObject working.
        public void SetGlow(GameObject tick, GameObject glow)
        {
            SetGlow(tick != null ? tick.GetComponent<SpriteRenderer>() : null, glow);
        }

        private IEnumerator ShowGlowForDuration()
        {
            _activeTick.gameObject.SetActive(true);
            _activeTick.color = _hilightColor;

            if (_activeGlow != null)
            {
                _activeGlow.SetActive(true);
            }

            yield return new WaitForSeconds(_glowDuration);

            ResetGlow();
            _glowRoutine = null;
        }

        private void OnDisable()
        {
            if (_glowRoutine != null)
            {
                StopCoroutine(_glowRoutine);
                _glowRoutine = null;
            }

            ResetGlow();
        }

        private void ResetGlow()
        {
            if (_activeTick != null)
            {
                _activeTick.color = _normalColor;
            }

            if (_activeGlow != null)
            {
                _activeGlow.SetActive(false);
            }

            _activeTick = null;
            _activeGlow = null;
        }
}
