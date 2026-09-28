using System.Collections.Generic;

namespace UnityEngine.UI.Utility
{
    static class ListPool2<T>
    {
        // Object pool to avoid allocations.
        private static readonly ObjectPool2<List<T>> s_ListPool = new ObjectPool2<List<T>>(null, l => l.Clear());

        public static List<T> Get()
        {
            return s_ListPool.Get();
        }

        public static void Release(List<T> toRelease)
        {
            s_ListPool.Release(toRelease);
        }
    }
}