using ActDim.Observability;
using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for <see cref="ILogger"/> providing structured caller and method execution scopes
    /// adhering to OpenTelemetry Source Code Semantic Conventions (<c>code.function</c>, <c>code.filepath</c>, <c>code.lineno</c>).
    /// </summary>
    public static class LoggerExtensions
    {
        /// <summary>
        /// Begins a structured logging scope containing caller context details: method/member name, source file, path, and line number.
        /// </summary>
        /// <remarks>
        /// The scope keys follow the official OpenTelemetry Source Code Semantic Conventions specification:
        /// <list type="bullet">
        ///   <item><description><c>code.function</c> (<see cref="ObservabilityTagNames.Code.Function"/>): Caller method name.</description></item>
        ///   <item><description><c>code.filename</c> (<see cref="ObservabilityTagNames.Code.FileName"/>): Source file name.</description></item>
        ///   <item><description><c>code.filepath</c> (<see cref="ObservabilityTagNames.Code.FilePath"/>): Full source file path.</description></item>
        ///   <item><description><c>code.lineno</c> (<see cref="ObservabilityTagNames.Code.LineNumber"/>): Source line number.</description></item>
        /// </list>
        /// Using these standard attributes ensures native indexing and code navigation across modern APM tools
        /// (Jaeger, Grafana Tempo, Datadog, Dynatrace, and .NET Aspire).
        /// </remarks>
        /// <param name="logger">The logger instance.</param>
        /// <param name="memberName">The name of the caller member or method (automatically supplied by compiler).</param>
        /// <param name="filePath">The source file path of the caller (automatically supplied by compiler).</param>
        /// <param name="lineNumber">The line number in the source file (automatically supplied by compiler).</param>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose, or <c>null</c> if scopes are not supported.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IDisposable? BeginMethodScope(
            this ILogger logger,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            return logger.BeginMethodScope(null, memberName, filePath, lineNumber);
        }

        /// <summary>
        /// Begins a structured logging scope containing caller context details alongside additional custom state properties.
        /// </summary>
        /// <remarks>
        /// The scope keys follow the official OpenTelemetry Source Code Semantic Conventions specification:
        /// <list type="bullet">
        ///   <item><description><c>code.function</c> (<see cref="ObservabilityTagNames.Code.Function"/>): Caller method name.</description></item>
        ///   <item><description><c>code.filename</c> (<see cref="ObservabilityTagNames.Code.FileName"/>): Source file name.</description></item>
        ///   <item><description><c>code.filepath</c> (<see cref="ObservabilityTagNames.Code.FilePath"/>): Full source file path.</description></item>
        ///   <item><description><c>code.lineno</c> (<see cref="ObservabilityTagNames.Code.LineNumber"/>): Source line number.</description></item>
        /// </list>
        /// Using these standard attributes ensures native indexing and code navigation across modern APM tools
        /// (Jaeger, Grafana Tempo, Datadog, Dynatrace, and .NET Aspire).
        /// </remarks>
        /// <param name="logger">The logger instance.</param>
        /// <param name="state">Additional custom key/value state pairs to merge into the scope.</param>
        /// <param name="memberName">The name of the caller member or method (automatically supplied by compiler).</param>
        /// <param name="filePath">The source file path of the caller (automatically supplied by compiler).</param>
        /// <param name="lineNumber">The line number in the source file (automatically supplied by compiler).</param>
        /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose, or <c>null</c> if scopes are not supported.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IDisposable? BeginMethodScope(
            this ILogger logger,
            IEnumerable<KeyValuePair<string, object?>>? state,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Guard.Against.Null(logger, nameof(logger));

            var fileName = string.IsNullOrEmpty(filePath) ? string.Empty : Path.GetFileName(filePath);
            var scopeData = new Dictionary<string, object?>
            {
                [ObservabilityTagNames.Code.Function] = memberName,
                [ObservabilityTagNames.Code.FileName] = fileName,
                [ObservabilityTagNames.Code.FilePath] = filePath,
                [ObservabilityTagNames.Code.LineNumber] = lineNumber
            };

            if (state is not null)
            {
                foreach (var kvp in state)
                {
                    scopeData[kvp.Key] = kvp.Value;
                }
            }

            return logger.BeginScope(scopeData);
        }
    }
}
