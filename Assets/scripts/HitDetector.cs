using UnityEngine;

public class HitDetector : MonoBehaviour
{
    [Header("Hit Detection")]
    [SerializeField] private LayerMask _hitLayerMask;
    [SerializeField] private LayerMask _coinLayerMask;
    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody;
    [Header("Score System")]
    [SerializeField] private ScoreSystem _scoreSystem;
    [Header("Events")]
    [SerializeField] private UnityEngine.Events.UnityEvent onHit;
    [SerializeField] private UnityEngine.Events.UnityEvent onCoinCollected;

    private void Awake()
    {
        onHit.AddListener(() => _scoreSystem.AddScore(1));
       // onCoinCollected.AddListener(() => _scoreSystem.AddScore(1));
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hitLayerMask == ( _hitLayerMask | (1 << collision.gameObject.layer) ))
        {
            onHit?.Invoke();
            Debug.Log("Hit detected with object: " + collision.gameObject.name);
            // Handle hit
        }

        if (_coinLayerMask == ( _coinLayerMask | (1 << collision.gameObject.layer) ))
        {
            onCoinCollected?.Invoke();
            // Handle coin collection
        }
    }
    
}
