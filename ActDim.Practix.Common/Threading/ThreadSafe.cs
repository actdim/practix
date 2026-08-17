using System;
using System.Threading;

namespace ActDim.Practix.Common.Threading
{
    /// <summary>
    /// Encapsulates a thread-local resource created with a context object, ensuring all created thread instances are disposed when the resource is disposed.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public class ThreadLocalResource<T, TContext> : IDisposable where T : class
    {
        private readonly ThreadLocal<T> _storage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadLocalResource{T, TContext}"/> class using the specified factory and context.
        /// </summary>
        /// <param name="factory">The factory delegate creating the resource per thread.</param>
        /// <param name="context">The context argument supplied to the factory.</param>
        public ThreadLocalResource(Func<TContext, T> factory, TContext context)
        {
            ArgumentNullException.ThrowIfNull(factory);

            _storage = new ThreadLocal<T>(
                () => factory(context),
                trackAllValues: true);
        }

        /// <summary>
        /// Gets the thread-local resource instance for the calling thread.
        /// </summary>
        public T Value => _storage.Value!;

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var value in _storage.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _storage.Dispose();
        }
    }
}
