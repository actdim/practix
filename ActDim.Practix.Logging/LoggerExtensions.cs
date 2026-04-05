using ActDim.Practix.Abstractions.Logging;
using System;

namespace ActDim.Practix.Logging
{
    /// <summary>
    /// ILogger extension methods for common scenarios.
    /// </summary>
    public static class LoggerExtensions
    {
        // private static readonly Func<FormattedLogValues, Exception, string> _messageFormatter = MessageFormatter;

        // ------------------------------------------DEBUG------------------------------------------	

        public static void LogDebug<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Debug, state, exception, formatter);
        }

        public static void LogDebug<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Debug, state, null, formatter);
        }

        public static void LogDebug<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Debug, state, null, (_, error) => formatter(_));
        }

        public static void LogDebug<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Debug, state, exception, null);
        }

        public static void LogDebug<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Debug, state, null, null);
        }

        // ------------------------------------------TRACE------------------------------------------

        public static void LogTrace<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Trace, state, exception, formatter);
        }

        public static void LogTrace<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Trace, state, null, formatter);
        }

        public static void LogTrace<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Trace, state, null, (_, error) => formatter(_));
        }

        public static void LogTrace<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Trace, state, exception, null);
        }

        public static void LogTrace<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Trace, state, null, null);
        }

        // ------------------------------------------INFORMATION------------------------------------------

        public static void LogInformation<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Information, state, exception, formatter);
        }

        public static void LogInformation<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Information, state, null, formatter);
        }

        public static void LogInformation<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Information, state, null, (_, error) => formatter(_));
        }

        public static void LogInformation<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Information, state, exception, null);
        }

        public static void LogInformation<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Information, state, null, null);
        }

        // ------------------------------------------WARNING------------------------------------------

        public static void LogWarning<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Warning, state, exception, formatter);
        }

        public static void LogWarning<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Warning, state, null, formatter);
        }

        public static void LogWarning<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Warning, state, null, (_, error) => formatter(_));
        }

        public static void LogWarning<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Warning, state, exception, null);
        }

        public static void LogWarning<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Warning, state, null, null);
        }

        // ------------------------------------------ERROR------------------------------------------

        public static void LogError<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Error, state, exception, formatter);
        }

        public static void LogError<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Error, state, null, formatter);
        }

        public static void LogError<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Error, state, null, (_, error) => formatter(_));
        }

        public static void LogError<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Error, state, exception, null);
        }

        public static void LogError<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Error, state, null, null);
        }

        public static void LogError(this ILogger logger, Exception exception)
        {
            logger.Log<object>(LogLevel.Error, null, exception, null);
        }

        // ------------------------------------------CRITICAL------------------------------------------

        public static void LogCritical<TState>(this ILogger logger, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Critical, state, exception, formatter);
        }

        public static void LogCritical<TState>(this ILogger logger, TState state, Func<TState, Exception, string> formatter)
        {
            logger.Log(LogLevel.Critical, state, null, formatter);
        }

        public static void LogCritical<TState>(this ILogger logger, TState state, Func<TState, string> formatter)
        {
            logger.Log(LogLevel.Critical, state, null, (_, error) => formatter(_));
        }

        public static void LogCritical<TState>(this ILogger logger, TState state, Exception exception)
        {
            logger.Log(LogLevel.Critical, state, exception, null);
        }

        public static void LogCritical<TState>(this ILogger logger, TState state)
        {
            logger.Log(LogLevel.Critical, state, null, null);
        }
    }
}
