using UnityEngine;
using UnityEngine.UI;

namespace BoatGame.Presentation.BoatLevelUI
{
    /// <summary>
    /// Magic dust for UI screens: small glowing images that either float upward and fade (Drift),
    /// or pop in and out at random spots like twinkling stars (Twinkle). Images are created once on Awake.
    /// </summary>
    public class UiMotes : MonoBehaviour
    {
        public enum MoteMode
        {
            Drift,
            Twinkle,
        }

        private struct Mote
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Position;
            public float Age;
            public float Life;
            public float Size;
            public float Sway;
        }

        [SerializeField] private MoteMode _mode = MoteMode.Drift;
        [SerializeField] private Sprite _sprite;
        [SerializeField] private Material _material;
        [SerializeField] private int _count = 18;
        [SerializeField] private Vector2 _sizeRange = new Vector2(10f, 28f);
        [SerializeField] private Vector2 _lifeRange = new Vector2(4f, 8f);
        [SerializeField] private float _riseSpeed = 40f;
        [SerializeField] private Color _color = new Color(1f, 0.9f, 0.6f, 0.8f);

        private Mote[] _motes;
        private RectTransform _area;

        private void Awake()
        {
            _area = (RectTransform)transform;
            _motes = new Mote[_count];
            for (int i = 0; i < _count; i++)
            {
                var go = new GameObject("Mote", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)go.transform;
                rect.SetParent(_area, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                var image = go.GetComponent<Image>();
                image.sprite = _sprite;
                image.material = _material;
                image.raycastTarget = false;
                _motes[i] = new Mote { Rect = rect, Image = image };
                Respawn(ref _motes[i], Random.value);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            for (int i = 0; i < _motes.Length; i++)
            {
                ref Mote mote = ref _motes[i];
                mote.Age += dt;
                if (mote.Age >= mote.Life)
                {
                    Respawn(ref mote, 0f);
                }

                float t = mote.Age / mote.Life;
                float alpha;
                float scale;
                if (_mode == MoteMode.Drift)
                {
                    mote.Position.y += _riseSpeed * dt;
                    alpha = Mathf.Sin(t * Mathf.PI);
                    scale = 1f;
                    mote.Rect.anchoredPosition = mote.Position + new Vector2(Mathf.Sin(time * 0.8f + mote.Sway) * 18f, 0f);
                }
                else
                {
                    // quick pop in, slower fade, with a little spin
                    alpha = Mathf.Sin(Mathf.Pow(t, 0.6f) * Mathf.PI);
                    scale = 0.3f + 0.7f * alpha;
                    mote.Rect.anchoredPosition = mote.Position;
                    mote.Rect.localRotation = Quaternion.Euler(0f, 0f, t * 90f + mote.Sway * 30f);
                }

                mote.Rect.sizeDelta = Vector2.one * (mote.Size * scale);
                Color color = _color;
                color.a *= alpha;
                mote.Image.color = color;
            }
        }

        private void Respawn(ref Mote mote, float startAge01)
        {
            Rect area = _area.rect;
            mote.Life = Random.Range(_lifeRange.x, _lifeRange.y);
            mote.Age = startAge01 * mote.Life;
            mote.Size = Random.Range(_sizeRange.x, _sizeRange.y);
            mote.Sway = Random.Range(0f, 6.2831f);
            float x = Random.Range(area.xMin, area.xMax);
            float y = _mode == MoteMode.Drift ? area.yMin + Random.Range(0f, area.height * 0.5f) : Random.Range(area.yMin, area.yMax);
            mote.Position = new Vector2(x, y);
        }
    }
}
