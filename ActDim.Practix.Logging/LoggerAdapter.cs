using Microsoft.Extensions.Logging;

namespace ActDim.Practix.Logging
{
    public class LoggerAdapter : ILogger
    {
        private readonly Abstractions.Logging.IScopedLogger _canaryLogger;

        public LoggerAdapter(Abstractions.Logging.IScopedLogger canaryLogger)
        {
            _canaryLogger = canaryLogger;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _canaryLogger.Log((Abstractions.Logging.LogLevel)logLevel, state, exception, formatter);
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return _canaryLogger.IsEnabled((Abstractions.Logging.LogLevel)logLevel);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return _canaryLogger.BeginScope(state);
        }
    }
}