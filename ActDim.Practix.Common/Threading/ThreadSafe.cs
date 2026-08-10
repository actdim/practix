using System;
using System.Threading;

namespace ActDim.Practix.Common.Threading
{
    public class ThreadLocalResource<T, TContext> : IDisposable where T : class
    {
        private readonly ThreadLocal<T> _storage;

        public ThreadLocalResource(Func<TContext, T> factory, TContext context)
        {
            ArgumentNullException.ThrowIfNull(factory);

            _storage = new ThreadLocal<T>(
                () => factory(context),
                trackAllValues: true);
        }

        public T Value => _storage.Value!;

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
