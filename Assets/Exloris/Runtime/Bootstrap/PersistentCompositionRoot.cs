using UnityEngine;

namespace Exloris.Runtime.Bootstrap
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class PersistentCompositionRoot : MonoBehaviour
    {
        public const string RootName = "ExlorisCompositionRoot";

        private static PersistentCompositionRoot _instance;

        public static PersistentCompositionRoot Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            gameObject.name = RootName;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
