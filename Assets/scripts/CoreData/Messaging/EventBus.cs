using System;

namespace BoatGame.CoreData.Messaging
{
    /// <summary>Marker for event payloads. Events are small readonly structs, so publishing never allocates.</summary>
    public interface IEvent
    {
    }

    /// <summary>
    /// Type-safe, allocation-free publish/subscribe. Each event type gets its own static channel
    /// (no dictionary lookups, no boxing). Subscribing or unsubscribing from inside a callback is safe.
    /// </summary>
    public static class EventBus
    {
        public static void Subscribe<T>(Action<T> callback) where T : struct, IEvent
        {
            Channel<T>.Add(callback);
        }

        public static void Unsubscribe<T>(Action<T> callback) where T : struct, IEvent
        {
            Channel<T>.Remove(callback);
        }

        public static void Publish<T>(T evt) where T : struct, IEvent
        {
            Channel<T>.Invoke(evt);
        }

        private static class Channel<T> where T : struct, IEvent
        {
            private const int InitialCapacity = 8;

            private static Action<T>[] _callbacks = new Action<T>[InitialCapacity];
            private static int _count;
            private static int _publishDepth;
            private static bool _hasRemovals;

            public static void Add(Action<T> callback)
            {
                if (callback == null || IndexOf(callback) >= 0)
                {
                    return;
                }

                if (_count == _callbacks.Length)
                {
                    Array.Resize(ref _callbacks, _callbacks.Length * 2);
                }

                _callbacks[_count++] = callback;
            }

            public static void Remove(Action<T> callback)
            {
                int index = IndexOf(callback);
                if (index < 0)
                {
                    return;
                }

                // while publishing, only blank the slot; the list is compacted once publishing finishes
                _callbacks[index] = null;
                _hasRemovals = true;
                if (_publishDepth == 0)
                {
                    Compact();
                }
            }

            public static void Invoke(T evt)
            {
                _publishDepth++;
                try
                {
                    // callbacks added during this publish are not called until the next one
                    int count = _count;
                    for (int i = 0; i < count; i++)
                    {
                        _callbacks[i]?.Invoke(evt);
                    }
                }
                finally
                {
                    _publishDepth--;
                    if (_publishDepth == 0 && _hasRemovals)
                    {
                        Compact();
                    }
                }
            }

            private static int IndexOf(Action<T> callback)
            {
                for (int i = 0; i < _count; i++)
                {
                    if (_callbacks[i] == callback)
                    {
                        return i;
                    }
                }

                return -1;
            }

            private static void Compact()
            {
                int write = 0;
                for (int read = 0; read < _count; read++)
                {
                    if (_callbacks[read] != null)
                    {
                        _callbacks[write++] = _callbacks[read];
                    }
                }

                for (int i = write; i < _count; i++)
                {
                    _callbacks[i] = null;
                }

                _count = write;
                _hasRemovals = false;
            }
        }
    }
}
