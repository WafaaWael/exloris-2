using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>Makes the sun shafts slowly breathe in and out; Brighten() swells them (e.g. on arrival).</summary>
    public class GodRaysPulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _rays = new SpriteRenderer[0];
        [SerializeField] private float _baseAlpha = 0.8f;
        [SerializeField] private float _breatheAmount = 0.25f;
        [SerializeField] private float _breatheSpeed = 0.35f;
        [SerializeField] private float _brightenAlpha = 1f;
        [SerializeField] private float _brightenSeconds = 2f;

        private float _boost;

        public void Brighten()
        {
            _boost = 1f;
        }

        private void Update()
        {
            float time = Time.time;
            float target = _boost > 0f ? _brightenAlpha : _baseAlpha;
            if (_boost > 0f)
            {
                _boost = Mathf.Max(0f, _boost - Time.deltaTime / (_brightenSeconds * 3f));
            }

            for (int i = 0; i < _rays.Length; i++)
            {
                float wave = Mathf.Sin(time * _breatheSpeed * 6.2831f + i * 2.1f) * 0.5f + 0.5f;
                Color color = _rays[i].color;
                color.a = Mathf.Clamp01(target * (1f - _breatheAmount + _breatheAmount * wave));
                _rays[i].color = color;
            }
        }
    }
}
