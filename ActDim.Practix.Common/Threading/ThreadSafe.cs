namespace ActDim.Practix.Threading
{
    public class ThreadSafe<T, TContext> : ThreadLocal<T>
    {
        private TContext _context;
        public ThreadSafe(Func<TContext, T> valueFactory, TContext context)
            : base(() => valueFactory(context))
        {
            _context = context;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _context is IDisposable disposableContext)
            {
                disposableContext.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}