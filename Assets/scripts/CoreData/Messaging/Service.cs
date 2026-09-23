using System.Runtime.CompilerServices;
using UnityEngine;

namespace BoatGame.CoreData.Messaging
{
    /// <summary>
    /// Lazily resolved handle to a scene service (a higher layer reaching a lower one without a hard reference).
    /// The first access finds the component in the scene and caches it; a destroyed instance is re-resolved.
    /// </summary>
    public struct Service<T> where T : Component
    {
        private T _cached;

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Unity's null check also catches destroyed objects (e.g. after a scene reload)
                if (_cached == null)
                {
                    _cached = SharedCache.Resolve();
                }

                return _cached;
            }
        }

        private static class SharedCache
        {
            private static T _instance;

            public static T Resolve()
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<T>();
                    if (_instance == null)
                    {
                        Debug.LogWarning($"Service<{typeof(T).Name}>: no instance found in the scene.");
                    }
                }

                return _instance;
            }
        }
    }
}
