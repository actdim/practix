using ActDim.Practix.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Observability.Tests.VictoriaLogs
{
    public sealed class VictoriaLogsLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private readonly VictoriaLogsClient _client;
        private readonly IAmbientContext? _ambientContext;
        private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
        private readonly ConcurrentQueue<Dictionary<string, object?>> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _backgroundTask;

        public VictoriaLogsLoggerProvider(VictoriaLogsClient client, IAmbientContext? ambientContext = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _ambientContext = ambientContext;
            _backgroundTask = Task.Run(ProcessQueueAsync);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new VictoriaLogsLogger(this, categoryName);
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
        }

        internal void EnqueueLog(string categoryName, LogLevel logLevel, EventId eventId, string message, Exception? exception)
        {
            var record = new Dictionary<string, object?>
            {
                ["_time"] = DateTimeOffset.UtcNow.ToString("O"),
                ["_stream"] = _client.Options.Stream,
                ["msg"] = message,
                ["level"] = logLevel.ToString().ToLowerInvariant(),
                ["logger"] = categoryName,
                ["event.id"] = eventId.Id != 0 ? eventId.Id : null,
                ["event.name"] = !string.IsNullOrEmpty(eventId.Name) ? eventId.Name : null,
            };

            if (exception != null)
            {
                record["exception.type"] = exception.GetType().FullName;
                record["exception.message"] = exception.Message;
                record["exception.stacktrace"] = exception.ToString();
            }

            var activity = Activity.Current;
            if (activity != null)
            {
                record["trace.id"] = activity.TraceId.ToString();
                record["span.id"] = activity.SpanId.ToString();
            }

            if (_ambientContext != null)
            {
                foreach (var kvp in _ambientContext.Properties)
                {
                    if (kvp.Value != null && !kvp.Key.StartsWith("__Ambient_", StringComparison.Ordinal))
                    {
                        record[kvp.Key] = kvp.Value.ToString();
                    }
                }
            }

            _scopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> kvps)
                {
                    foreach (var pair in kvps)
                    {
                        state[pair.Key] = pair.Value;
                    }
                }
                else if (scope != null)
                {
                    state["scope"] = scope.ToString();
                }
            }, record);

            _queue.Enqueue(record);
        }

        private async Task ProcessQueueAsync()
        {
            var batch = new List<Dictionary<string, object?>>();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_client.Options.BatchInterval, _cts.Token);

                    while (_queue.TryDequeue(out var record))
                    {
                        batch.Add(record);
                        if (batch.Count >= _client.Options.MaxBatchSize)
                        {
                            break;
                        }
                    }

                    if (batch.Count > 0)
                    {
                        await _client.IngestRecordsAsync(batch, CancellationToken.None);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                }
            }

            while (_queue.TryDequeue(out var record))
            {
                batch.Add(record);
            }

            if (batch.Count > 0)
            {
                try
                {
                    await _client.IngestRecordsAsync(batch, CancellationToken.None);
                }
                catch { }
            }
        }

        public void Flush()
        {
            var batch = new List<Dictionary<string, object?>>();
            while (_queue.TryDequeue(out var record))
            {
                batch.Add(record);
            }

            if (batch.Count > 0)
            {
                _client.IngestRecordsAsync(batch, CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _backgroundTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch { }

            Flush();
            _cts.Dispose();
        }

        private sealed class VictoriaLogsLogger : ILogger
        {
            private readonly VictoriaLogsLoggerProvider _provider;
            private readonly string _categoryName;

            public VictoriaLogsLogger(VictoriaLogsLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return _provider._scopeProvider.Push(state);
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var message = formatter(state, exception);
                _provider.EnqueueLog(_categoryName, logLevel, eventId, message, exception);
            }
        }
    }
}
