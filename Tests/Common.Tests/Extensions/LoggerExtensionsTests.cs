using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Practix.Common.Tests.Extensions
{
    public class LoggerExtensionsTests
    {
        [Fact]
        public void BeginMethodScope_CapturesOpenTelemetrySemanticConventions()
        {
            var testLogger = new FakeScopeLogger();

            using var scope = testLogger.BeginMethodScope();

            Assert.NotNull(testLogger.CapturedScope);
            var dict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(testLogger.CapturedScope);

            Assert.Equal(nameof(BeginMethodScope_CapturesOpenTelemetrySemanticConventions), dict["code.function"]);
            Assert.Equal("LoggerExtensionsTests.cs", dict["code.filename"]);
            Assert.True(dict.ContainsKey("code.filepath"));
            Assert.True((int)dict["code.lineno"]! > 0);
        }

        [Fact]
        public void BeginMethodScope_WithCustomState_MergesStateProperties()
        {
            var testLogger = new FakeScopeLogger();
            var extraState = new Dictionary<string, object?>
            {
                ["tenant.id"] = "tenant-123",
                ["order.id"] = 42
            };

            using var scope = testLogger.BeginMethodScope(extraState);

            Assert.NotNull(testLogger.CapturedScope);
            var dict = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object?>>(testLogger.CapturedScope);

            Assert.Equal(nameof(BeginMethodScope_WithCustomState_MergesStateProperties), dict["code.function"]);
            Assert.Equal("tenant-123", dict["tenant.id"]);
            Assert.Equal(42, dict["order.id"]);
        }

        private class FakeScopeLogger : ILogger
        {
            public object? CapturedScope { get; private set; }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                CapturedScope = state;
                return new DummyDisposable();
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
            }

            private class DummyDisposable : IDisposable
            {
                public void Dispose() { }
            }
        }
    }
}
