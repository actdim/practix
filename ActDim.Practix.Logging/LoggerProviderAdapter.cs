using ActDim.Practix.Abstractions.Logging;

namespace ActDim.Practix.Logging
{
    public class LoggerProviderAdapter : Microsoft.Extensions.Logging.ILoggerProvider
    {
        private readonly Func<string, IScopedLogger> _loggerFactory;

        public LoggerProviderAdapter(Func<string, IScopedLogger> loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
        }

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
        {
            return new LoggerAdapter(_loggerFactory(categoryName));
        }
    }
}