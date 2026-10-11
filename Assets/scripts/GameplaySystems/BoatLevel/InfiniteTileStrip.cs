using UnityEngine;

namespace BoatGame.GameplaySystems.BoatLevel
{
    /// <summary>
    /// A tiled SpriteRenderer that always covers the camera view vertically by snapping to whole tiles,
    /// with optional flow (water drifting down the screen).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class InfiniteTileStrip : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private float _width = 12f;
        [SerializeField] private float _flowSpeed;
        [SerializeField] private float _swayAmplitude;
        [SerializeField] private float _swaySpeed = 0.6f;

        private Transform _transform;
        private Transform _cameraTransform;
        private float _tileHeight;
        private float _baseX;
        private float _flowOffset;

        private void Awake()
        {
            _transform = transform;
            _cameraTransform = _camera.transform;
            _baseX = _transform.position.x;

            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            _tileHeight = spriteRenderer.sprite.bounds.size.y;

            // tall enough for the view plus two tiles of slack on each side
            float viewHeight = _camera.orthographicSize * 2f;
            int tiles = Mathf.CeilToInt(viewHeight / _tileHeight) + 4;
            spriteRenderer.size = new Vector2(_width, tiles * _tileHeight);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _flowOffset = Mathf.Repeat(_flowOffset + _flowSpeed * dt, _tileHeight);

            float cameraY = _cameraTransform.position.y;
            float y = ComputeSnappedY(cameraY, _tileHeight, _flowOffset);
            float x = _baseX + Mathf.Sin(Time.time * _swaySpeed) * _swayAmplitude;
            _transform.position = new Vector3(x, y, _transform.position.z);
        }

        private static float ComputeSnappedY(float cameraY, float tileHeight, float flowOffset)
        {
            return (Mathf.Floor(cameraY / tileHeight) + 1f) * tileHeight - flowOffset;
        }
    }
}
