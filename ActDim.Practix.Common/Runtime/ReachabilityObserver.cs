using System;
using System.Runtime.CompilerServices;

namespace ActDim.Practix.Common.Runtime
{
    /// <summary>
    /// Observes when an object becomes unreachable by the garbage collector.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The callback is invoked after the observed object becomes unreachable.
    /// This does not directly observe <see cref="IDisposable.Dispose"/>.
    /// </para>
    /// <para>
    /// If the observed object implements <see cref="IDisposable"/> and is disposed
    /// before becoming unreachable, the callback is necessarily invoked after
    /// <c>Dispose</c>. However, the callback may execute considerably later,
    /// depending on garbage collection.
    /// </para>
    /// <para>
    /// The callback must not capture or otherwise reference the observed object.
    /// Such a reference would keep the object reachable through the callback itself
    /// and prevent the observer from detecting that the object is no longer reachable.
    /// </para>
    /// </remarks>
    public static class ReachabilityObserver<T> // ObjectLifetimeObserver
        where T : class
    {
        /// <summary>
        /// Subscribes a callback that is invoked when <paramref name="key"/> becomes
        /// unreachable by the garbage collector.
        /// </summary>
        /// <param name="key">The object whose reachability is observed.</param>
        /// <param name="handler">
        /// The callback to invoke when the object becomes unreachable.
        /// The callback must not capture or otherwise reference <paramref name="key"/>.
        /// </param>
        public static void Subscribe(T key, Action handler)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(handler);

            var observer = WeakTable.GetValue(
                key,
                static _ => new Observer());

            observer._handler += handler;
        }

        /// <summary>
        /// Unsubscribes a previously registered callback.
        /// </summary>
        /// <param name="key">The observed object.</param>
        /// <param name="handler">The callback to remove.</param>
        public static void Unsubscribe(T key, Action handler)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(handler);

            if (WeakTable.TryGetValue(key, out var observer))
            {
                observer._handler -= handler;
            }
        }

        private sealed class Observer
        {
            ~Observer()
            {
                _handler?.Invoke();
            }

            internal event Action _handler;
        }

        private static readonly ConditionalWeakTable<T, Observer> WeakTable = [];
    }
}
