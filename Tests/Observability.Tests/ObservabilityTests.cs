#nullable enable
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Context;
using ActDim.Practix.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace ActDim.Practix.Observability.Tests
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestProviderAliasAttribute : Attribute
    {
        public string Alias { get; }
        public TestProviderAliasAttribute(string alias) => Alias = alias;
    }

    public class ObservabilityTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly TestLoggerCollector _consoleCollector = new();
        private readonly TestLoggerCollector _customAliasCollector = new();
        private readonly TestLoggerCollector _fileCollector = new();

        public ObservabilityTests()
        {
            var services = new ServiceCollection();
            services.AddEventObservability(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddProvider(new TestConsoleLoggerProvider(_consoleCollector));
                builder.AddProvider(new TestCustomAliasedLoggerProvider(_customAliasCollector));
                builder.AddProvider(new TestFileLoggerProvider(_fileCollector));
            });
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void ToOtelName_ConvertsPascalCaseToDotNotation()
        {
            Assert.Equal("user.id", EventObservabilityHelper.ToOtelName("UserId"));
            Assert.Equal("order.customer.name", EventObservabilityHelper.ToOtelName("{Order.Customer.Name}"));
            Assert.Equal("event.name", EventObservabilityHelper.ToOtelName("EventName"));
            Assert.Equal("user.profile.zip.code", EventObservabilityHelper.ToOtelName("{User_Profile.ZipCode}"));
        }

        [Fact]
        public void IsSimple_RecognizesPrimitivesAndCommonValueTypes()
        {
            Assert.True(EventObservabilityHelper.IsSimple(42));
            Assert.True(EventObservabilityHelper.IsSimple("hello"));
            Assert.True(EventObservabilityHelper.IsSimple(12.34m));
            Assert.True(EventObservabilityHelper.IsSimple(DateTime.UtcNow));
            Assert.True(EventObservabilityHelper.IsSimple(DateTimeOffset.UtcNow));
            Assert.True(EventObservabilityHelper.IsSimple(TimeSpan.FromSeconds(1)));
            Assert.True(EventObservabilityHelper.IsSimple(Guid.NewGuid()));
            Assert.True(EventObservabilityHelper.IsSimple(LogLevel.Information));
            
            Assert.False(EventObservabilityHelper.IsSimple(new { Id = 1 }));
        }

        [Fact]
        public void Flatten_FlattensNestedObjectStructure()
        {
            var testObj = new
            {
                UserId = 42,
                Profile = new
                {
                    FirstName = "Alice",
                    IsActive = true
                }
            };

            var flat = EventObservabilityHelper.Flatten(testObj);

            Assert.Equal(42, flat["user.id"]);
            Assert.Equal("Alice", flat["profile.first.name"]);
            Assert.Equal(true, flat["profile.is.active"]);
        }

        [Fact]
        public void Flatten_HandlesDictionariesAndCollections()
        {
            var testObj = new
            {
                Attributes = new Dictionary<string, object>
                {
                    ["Role"] = "Admin",
                    ["Level"] = 10
                },
                Tags = new[] { "alpha", "beta" }
            };

            var flat = EventObservabilityHelper.Flatten(testObj);

            Assert.Equal("Admin", flat["attributes.role"]);
            Assert.Equal(10, flat["attributes.level"]);
            Assert.Equal("alpha", flat["tags[0]"]);
            Assert.Equal("beta", flat["tags[1]"]);
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_EnrichesCurrentActivityTags_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("TestScopeActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            var scopeState = new LogEvent("TestScope", new Dictionary<string, object>
            {
                ["TenantId"] = "EU-1",
                ["Priority"] = 5
            });

            using (logger.BeginScope(scopeState))
            {
                Assert.Equal("EU-1", scope.Activity.GetTagItem("tenant.id"));
                Assert.Equal(5, scope.Activity.GetTagItem("priority"));
            }
        }

        [Fact]
        public void RealWorld_DeveloperExperience_ReadingCallContextAndLogging_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("RealWorldActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var callContext = _serviceProvider.GetRequiredService<ICallContextProvider>().Get();

            using (callContext.Push("TenantId", "Tenant_EU_West"))
            using (callContext.Push("UserId", "user_12345"))
            {
                var currentTenant = callContext.Data.TryGetValue("TenantId", out var tenant) ? tenant : "Unknown";

                logger.LogInformation("Processing batch for tenant {TenantId}", currentTenant);

                var activityEvent = scope.Activity.Events.FirstOrDefault(e => e.Name == "LogMessage");
                Assert.NotEqual(default, activityEvent);

                var tags = activityEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
                Assert.Equal("Processing batch for tenant Tenant_EU_West", tags["message"]);
                Assert.Equal("Tenant_EU_West", tags["tenant.id"]);
                Assert.Equal("user_12345", tags["user.id"]);
            }
        }

        [Fact]
        public void EventObservabilityBridge_Supports_Status_Progress_Icon_And_Tags()
        {
            using var scope = StartTestActivity("StatusProgressActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var callContext = _serviceProvider.GetRequiredService<ICallContextProvider>().Get();

            using (callContext.SetStatus("Downloading Dataset", icon: "🚀"))
            using (callContext.ReportProgress(45.5))
            using (callContext.PushTags("billing", "priority-high"))
            {
                logger.LogInformation("Importing rows into database");

                var activityEvent = scope.Activity.Events.Last(e => e.Name == "LogMessage");
                var tags = activityEvent.Tags.ToDictionary(t => t.Key, t => t.Value);

                Assert.Equal("Downloading Dataset", tags["status"]);
                Assert.Equal("🚀", tags["icon"]);
                Assert.Equal(45.5, tags["progress"]);

                Assert.NotNull(tags["tags"]);
                var tagsSet = Assert.IsType<HashSet<string>>(tags["tags"]);
                Assert.Contains("billing", tagsSet);
                Assert.Contains("priority-high", tagsSet);
            }
        }

        [Fact]
        public void EventObservabilityBridge_Log_VerifiesRealProviderSuppression()
        {
            using var scope = StartTestActivity("SuppressionActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var callContext = _serviceProvider.GetRequiredService<ICallContextProvider>().Get();

            // Clear collectors
            _consoleCollector.Logs.Clear();
            _customAliasCollector.Logs.Clear();
            _fileCollector.Logs.Clear();

            // 0. Normal log: All 3 providers MUST receive the log
            logger.LogInformation("Normal message");

            Assert.Single(_consoleCollector.Logs);
            Assert.Single(_customAliasCollector.Logs);
            Assert.Single(_fileCollector.Logs);

            // 1. Suppress Console: TestConsoleLoggerProvider MUST NOT receive log, others MUST receive log
            using (callContext.SuppressConsole())
            {
                logger.LogInformation("Console suppressed message");

                // Console collector count stays at 1 (NOT incremented!)
                Assert.Single(_consoleCollector.Logs);

                // CustomAlias and File collectors WERE incremented (count = 2)
                Assert.Equal(2, _customAliasCollector.Logs.Count);
                Assert.Equal(2, _fileCollector.Logs.Count);

                // OTel Activity STILL RECEIVED the event!
                var evt1 = scope.Activity.Events.Last(e => e.Name == "LogMessage");
                Assert.Equal("Console suppressed message", evt1.Tags.First(t => t.Key == "message").Value);
            }

            // 2. Suppress specific provider by [TestProviderAlias("CustomAlias")]
            using (callContext.SuppressProviders("CustomAlias"))
            {
                logger.LogInformation("CustomAlias suppressed message");

                // CustomAlias collector count stays at 2 (NOT incremented!)
                Assert.Equal(2, _customAliasCollector.Logs.Count);

                // Console and File WERE incremented (Console=2, File=3)
                Assert.Equal(2, _consoleCollector.Logs.Count);
                Assert.Equal(3, _fileCollector.Logs.Count);
            }

            // 3. Suppress CallContext: Verify CorrelationId is missing from Activity tags
            using (callContext.Push("CorrelationId", "corr-777"))
            {
                using (callContext.SuppressCallContext())
                {
                    logger.LogInformation("Log without CallContext");

                    var evtCc = scope.Activity.Events.Last(e => e.Name == "LogMessage");
                    var tagsCc = evtCc.Tags.ToDictionary(t => t.Key, t => t.Value);

                    Assert.False(tagsCc.ContainsKey("correlation.id"));
                }
            }
        }

        private static TestActivityScope StartTestActivity(string activityName)
        {
            return new TestActivityScope(activityName);
        }

        private class TestLoggerCollector
        {
            public List<string> Logs { get; } = [];
        }

        [TestProviderAlias("Console")]
        private class TestConsoleLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestConsoleLoggerProvider(TestLoggerCollector collector) => _collector = collector;
            public ILogger CreateLogger(string categoryName) => new TestRecordingLogger(_collector);
            public void Dispose() { }
        }

        [TestProviderAlias("CustomAlias")]
        private class TestCustomAliasedLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestCustomAliasedLoggerProvider(TestLoggerCollector collector) => _collector = collector;
            public ILogger CreateLogger(string categoryName) => new TestRecordingLogger(_collector);
            public void Dispose() { }
        }

        private class TestFileLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestFileLoggerProvider(TestLoggerCollector collector) => _collector = collector;
            public ILogger CreateLogger(string categoryName) => new TestRecordingLogger(_collector);
            public void Dispose() { }
        }

        private class TestRecordingLogger : ILogger
        {
            private readonly TestLoggerCollector _collector;

            public TestRecordingLogger(TestLoggerCollector collector)
            {
                _collector = collector;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                var msg = formatter != null ? formatter(state, exception) : state?.ToString() ?? string.Empty;
                _collector.Logs.Add(msg);
            }
        }

        private sealed class TestActivityScope : IDisposable
        {
            private readonly ActivitySource _source;
            private readonly ActivityListener _listener;
            public Activity Activity { get; }

            public TestActivityScope(string name)
            {
                _source = new ActivitySource(name);
                _listener = new ActivityListener
                {
                    ShouldListenTo = _ => true,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
                };
                ActivitySource.AddActivityListener(_listener);

                Activity = _source.StartActivity(name) ?? throw new InvalidOperationException($"Failed to start activity {name}");
            }

            public void Dispose()
            {
                Activity.Dispose();
                _listener.Dispose();
                _source.Dispose();
            }
        }
    }
}
