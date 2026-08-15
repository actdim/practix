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
using System.Reflection;
using Xunit;

namespace ActDim.Practix.Observability.Tests
{
    [AttributeUsage(AttributeTargets.Class)]
    public class TestProviderAliasAttribute : Attribute
    {
        public string Alias { get; }
        public TestProviderAliasAttribute(string alias)
        {
            Alias = alias;
        }
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
        public void RealWorld_DeveloperExperience_ReadingAmbientContextAndLogging_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("RealWorldActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            using (observability.Push("TenantId", "Tenant_EU_West"))
            using (observability.Push("UserId", "user_12345"))
            using (logger.BeginScope("ProcessBatch"))
            {
                logger.LogInformation("Processing batch for tenant {TenantId}", "Tenant_EU_West");

                // The scope owns the span: ambient observability properties describe the operation and are written once
                Assert.Equal("Tenant_EU_West", scope.Activity.GetTagItem("tenant.id"));
                Assert.Equal("user_12345", scope.Activity.GetTagItem("user.id"));

                // The log call is a log record only and leaves the span alone
                Assert.Empty(scope.Activity.Events);
            }
        }

        [Fact]
        public void EventObservabilityBridge_Supports_Status_Progress_And_Icon()
        {
            using var scope = StartTestActivity("StatusProgressActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            using (observability.SetStatus("Downloading Dataset", icon: "🚀"))
            using (observability.SetProgress(45.5))
            using (observability.Push("PriorityTag", "high"))
            using (logger.BeginScope("ImportRows"))
            {
                logger.LogInformation("Importing rows into database");

                Assert.Equal("Downloading Dataset", scope.Activity.GetTagItem("status"));
                Assert.Equal("🚀", scope.Activity.GetTagItem("icon"));
                Assert.Equal(45.5, scope.Activity.GetTagItem("progress"));
                Assert.Equal("high", scope.Activity.GetTagItem("priority.tag"));
            }
        }

        [Fact]
        public void ObservabilityContext_ExportsProperties_SetAfterTheScopeWasOpened()
        {
            using var scope = StartTestActivity("LateAmbientActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            using (logger.BeginScope("ImportBatch"))
            {
                // Push data properties while the operation is already in flight
                using (observability.SetStatus("Processing Rows", icon: "⚡"))
                using (observability.SetProgress(75.0))
                using (observability.Push("BatchId", "b-99"))
                {
                    Assert.Equal("Processing Rows", scope.Activity.GetTagItem("status"));
                    Assert.Equal("⚡", scope.Activity.GetTagItem("icon"));
                    Assert.Equal(75.0, scope.Activity.GetTagItem("progress"));
                    Assert.Equal("b-99", scope.Activity.GetTagItem("batch.id"));
                }

                // Disposing the handle restores previous values (or removes newly added attributes)
                Assert.Null(scope.Activity.GetTagItem("status"));
                Assert.Null(scope.Activity.GetTagItem("icon"));
                Assert.Null(scope.Activity.GetTagItem("progress"));
                Assert.Null(scope.Activity.GetTagItem("batch.id"));
            }
        }

        [Fact]
        public void AmbientContext_Properties_AreNotExportedToActivity_UnlessPushedViaObservabilityContext()
        {
            using var scope = StartTestActivity("ContextSeparationActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            // 1. Raw AmbientContext property (internal business state)
            using (AmbientContext.Push("InternalLargePayload", "heavy_serialized_payload_123"))
            // 2. ObservabilityContext property (explicit telemetry state)
            using (observability.Push("TelemetryMetric", "metric_value_456"))
            using (logger.BeginScope("SeparationScope"))
            {
                // Raw ambient context property is accessible to code:
                Assert.Equal("heavy_serialized_payload_123", AmbientContext.CurrentProperties["InternalLargePayload"]);

                // BUT it is NOT exported to Activity tags:
                Assert.Null(scope.Activity.GetTagItem("internal.large.payload"));

                // ObservabilityContext property IS exported to Activity tags:
                Assert.Equal("metric_value_456", scope.Activity.GetTagItem("telemetry.metric"));
            }
        }

        [Fact]
        public void DynamicSuppression_SelectivelyMutesConsoleAndCustomProviders_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("SuppressionActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            // Baseline: All 3 providers receive 1 log message
            logger.LogInformation("Baseline message");
            Assert.Single(_consoleCollector.Logs);
            Assert.Single(_customAliasCollector.Logs);
            Assert.Single(_fileCollector.Logs);

            // 1. Suppress Console only
            using (observability.SuppressConsole())
            {
                logger.LogInformation("Console suppressed message");

                // Console collector count stays at 1 (NOT incremented!)
                Assert.Single(_consoleCollector.Logs);

                // File and CustomAlias WERE incremented
                Assert.Equal(2, _customAliasCollector.Logs.Count);
                Assert.Equal(2, _fileCollector.Logs.Count);
            }

            // 2. Suppress specific provider by [TestProviderAlias("CustomAlias")]
            using (observability.SuppressProviders("CustomAlias"))
            {
                logger.LogInformation("CustomAlias suppressed message");

                // CustomAlias collector count stays at 2 (NOT incremented!)
                Assert.Equal(2, _customAliasCollector.Logs.Count);

                // Console and File WERE incremented (Console=2, File=3)
                Assert.Equal(2, _consoleCollector.Logs.Count);
                Assert.Equal(3, _fileCollector.Logs.Count);
            }
        }

        [Fact]
        public void ExternalScopes_NotExportedByDefault_AndExportedWhenConfigured()
        {
            using var listener = new TestAllActivityListener();

            // 1. Default setup: IncludeExternalScopes is false
            var defaultProvider = BuildServiceProviderWithScopeProvider(out var defaultScopeProvider);
            var defaultLogger = defaultProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            using (defaultScopeProvider.Push(new Dictionary<string, object> { ["ExtReqId"] = "req-123" }))
            {
                using (defaultLogger.BeginScope("DefaultScope"))
                {
                    Assert.NotNull(Activity.Current);
                    // External scopes must NOT be exported by default
                    Assert.Null(Activity.Current.GetTagItem("ext.req.id"));
                }
            }

            // 2. Configured setup: IncludeExternalScopes is true
            var enabledProvider = BuildServiceProviderWithScopeProvider(
                out var enabledScopeProvider,
                options => options.IncludeExternalScopes = true);
            var enabledLogger = enabledProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            using (enabledScopeProvider.Push(new Dictionary<string, object> { ["ExtReqId"] = "req-456" }))
            {
                using (enabledLogger.BeginScope("EnabledScope"))
                {
                    Assert.NotNull(Activity.Current);
                    // External scopes ARE exported when explicitly enabled
                    Assert.Equal("req-456", Activity.Current.GetTagItem("ext.req.id"));
                }
            }
        }

        [Fact]
        public void ToOtelName_StripsDestructuringHints()
        {
            Assert.Equal("user", EventObservabilityHelper.ToOtelName("{@User}"));
            Assert.Equal("order.id", EventObservabilityHelper.ToOtelName("{$OrderId}"));
        }

        [Fact]
        public void EventObservabilityBridge_Log_WritesNothingToTheSpan_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("LogRecordOnlyActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            logger.LogInformation(
                new EventId(42, "OrderProcessed"),
                "Order {OrderId} moved to {Status}",
                7,
                "Shipped");

            // A log call is a log record; its content reaches the backend through the logging pipeline
            Assert.Empty(scope.Activity.Events);
            Assert.Null(scope.Activity.GetTagItem("order.id"));
            Assert.Null(scope.Activity.GetTagItem("status"));
        }

        [Fact]
        public void EventObservabilityBridge_Log_EnrichesSpan_RegardlessOfLogLevelFiltering()
        {
            using var scope = StartTestActivity("LevelIndependenceActivity");
            var serviceProvider = BuildServiceProvider(configureLogging: builder => builder.SetMinimumLevel(LogLevel.Warning));
            var logger = serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = serviceProvider.GetRequiredService<IObservabilityContext>();

            using (observability.Push("TenantId", "EU-9"))
            using (logger.BeginScope("FilteredOperation"))
            {
                logger.LogDebug("This never reaches any sink");

                // The trace side does not depend on what the log sinks are configured to accept
                Assert.Equal("EU-9", scope.Activity.GetTagItem("tenant.id"));
            }
        }

        [Fact]
        public void EventObservabilityBridge_CountsTagCollisions_AndKeepsFirstValue_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("CollisionActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            // Both property names normalize to the same OpenTelemetry attribute name
            using (logger.BeginScope(new { UserId = 1, User_Id = 2 }))
            {
                Assert.Equal(1, scope.Activity.GetTagItem("user.id"));
                Assert.Equal(1, scope.Activity.GetTagItem(ObservabilityTagNames.Collisions));
            }
        }

        [Fact]
        public void EventObservabilityBridge_ThrowsOnTagCollision_WhenConfigured()
        {
            using var scope = StartTestActivity("CollisionThrowActivity");
            var serviceProvider = BuildServiceProvider(options => options.TagCollisions = TagCollisionBehavior.Throw);
            var logger = serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            var exception = Assert.Throws<InvalidOperationException>(
                () => logger.BeginScope(new { UserId = 1, User_Id = 2 }));

            Assert.Contains("user.id", exception.Message);
        }

        [Fact]
        public void EventObservabilityBridge_RecordsException_AsOpenTelemetryEvent_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("ExceptionActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var failure = new InvalidOperationException("disk is full");

            logger.LogError(failure, "Import failed for {Batch}", "b-17");

            // Failures are the single trace write performed by a log call
            var exceptionEvent = scope.Activity.Events.Single(e => e.Name == "exception");
            var exceptionTags = exceptionEvent.Tags.ToDictionary(t => t.Key, t => t.Value);
            Assert.Equal(typeof(InvalidOperationException).FullName, exceptionTags["exception.type"]);
            Assert.Equal("disk is full", exceptionTags["exception.message"]);

            Assert.Null(scope.Activity.GetTagItem("batch"));
        }

        [Fact]
        public void EventObservabilityBridge_RecordsSameException_OnlyOncePerSpan_ViaDependencyInjection()
        {
            using var scope = StartTestActivity("ExceptionDedupeActivity");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var failure = new InvalidOperationException("disk is full");

            // The everyday catch / log / rethrow pattern reports the same instance on every layer
            logger.LogError(failure, "repository failed");
            logger.LogWarning(failure, "service could not import");
            logger.LogError(failure, "request handler gave up");

            Assert.Equal(1, scope.Activity.Events.Count(e => e.Name == "exception"));
        }

        [Fact]
        public void EventObservabilityBridge_RecordsSameException_OnEveryDistinctSpan_ViaDependencyInjection()
        {
            using var outer = StartTestActivity("OuterOperation");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var failure = new InvalidOperationException("disk is full");

            using (var inner = StartTestActivity("InnerOperation"))
            {
                Assert.NotSame(outer.Activity, inner.Activity);
                logger.LogError(failure, "inner operation failed");
                Assert.Equal(1, inner.Activity.Events.Count(e => e.Name == "exception"));
            }

            // Propagating into the enclosing operation must still mark that operation
            logger.LogError(failure, "outer operation gave up");

            Assert.Equal(1, outer.Activity.Events.Count(e => e.Name == "exception"));
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_DerivesLowCardinalitySpanNames()
        {
            using var listener = new TestAllActivityListener();
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            // The raw template is used, never the formatted message
            using (logger.BeginScope("Processing order {OrderId}", 42))
            {
                Assert.Equal("Processing order {OrderId}", Activity.Current?.OperationName);
            }

            using (logger.BeginScope(new LogEvent("ImportBatch")))
            {
                Assert.Equal("ImportBatch", Activity.Current?.OperationName);
            }

            // Anonymous state carries no operation identity
            using (logger.BeginScope(new { TenantId = "acme", Attempt = 3 }))
            {
                Assert.Equal("Scope", Activity.Current?.OperationName);
            }
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_AutoCreatesActivity_WithDefaultActivitySourceName_WhenNoActiveSpan()
        {
            using var listener = new TestAllActivityListener();
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            Assert.Null(Activity.Current);

            using (logger.BeginScope("OrderProcessingScope"))
            {
                var currentActivity = Activity.Current;
                Assert.NotNull(currentActivity);
                Assert.Equal("OrderProcessingScope", currentActivity.OperationName);

                var expectedDefaultSource = Assembly.GetEntryAssembly()?.GetName().Name ?? "ActDim.Practix";
                Assert.Equal(expectedDefaultSource, currentActivity.Source.Name);
            }

            Assert.Null(Activity.Current);
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_AutoCreatesActivity_WithAmbientActivitySourceName_WhenSpecified()
        {
            using var listener = new TestAllActivityListener();
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();
            var observability = _serviceProvider.GetRequiredService<IObservabilityContext>();

            Assert.Null(Activity.Current);

            using (observability.PushActivitySourceName("Custom.Payment.Worker"))
            {
                using (logger.BeginScope("ExecutePayment"))
                {
                    var currentActivity = Activity.Current;
                    Assert.NotNull(currentActivity);
                    Assert.Equal("ExecutePayment", currentActivity.OperationName);
                    Assert.Equal("Custom.Payment.Worker", currentActivity.Source.Name);
                }
            }

            Assert.Null(Activity.Current);
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_DoesNotCreateActivity_WhenAutoCreateDisabled()
        {
            using var listener = new TestAllActivityListener();
            var serviceProvider = BuildServiceProvider(options => options.AutoCreateActivityOnScope = false);
            var logger = serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            Assert.Null(Activity.Current);

            using (logger.BeginScope("DisabledScope"))
            {
                Assert.Null(Activity.Current);
            }
        }

        [Fact]
        public void EventObservabilityBridge_BeginScope_PreservesExistingActivity_WhenAlreadyActive()
        {
            using var scope = StartTestActivity("ExistingSpan");
            var logger = _serviceProvider.GetRequiredService<ILogger<ObservabilityTests>>();

            var initialActivity = Activity.Current;
            Assert.NotNull(initialActivity);

            using (logger.BeginScope("NestedScope"))
            {
                Assert.Same(initialActivity, Activity.Current);
            }

            Assert.Same(initialActivity, Activity.Current);
        }

        private static TestActivityScope StartTestActivity(string activityName)
        {
            return new TestActivityScope(activityName);
        }

        private static IServiceProvider BuildServiceProvider(
            Action<EventObservabilityOptions>? configureOptions = null,
            Action<ILoggingBuilder>? configureLogging = null)
        {
            var services = new ServiceCollection();
            services.AddEventObservability(
                builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddProvider(new TestFileLoggerProvider(new TestLoggerCollector()));
                    configureLogging?.Invoke(builder);
                },
                configureOptions);

            return services.BuildServiceProvider();
        }

        private static IServiceProvider BuildServiceProviderWithScopeProvider(
            out LoggerExternalScopeProvider scopeProvider,
            Action<EventObservabilityOptions>? configureOptions = null)
        {
            var services = new ServiceCollection();
            scopeProvider = new LoggerExternalScopeProvider();
            var spInstance = scopeProvider;

            services.AddSingleton<IExternalScopeProvider>(spInstance);
            services.AddEventObservability(
                builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddProvider(new TestFileLoggerProvider(new TestLoggerCollector()));
                },
                configureOptions);

            return services.BuildServiceProvider();
        }

        private class TestLoggerCollector
        {
            public List<string> Logs { get; } = [];
        }

        [TestProviderAlias("Console")]
        private class TestConsoleLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestConsoleLoggerProvider(TestLoggerCollector collector)
            {
                _collector = collector;
            }
            public ILogger CreateLogger(string categoryName)
            {
                return new TestRecordingLogger(_collector);
            }
            public void Dispose() { }
        }

        [TestProviderAlias("CustomAlias")]
        private class TestCustomAliasedLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestCustomAliasedLoggerProvider(TestLoggerCollector collector)
            {
                _collector = collector;
            }
            public ILogger CreateLogger(string categoryName)
            {
                return new TestRecordingLogger(_collector);
            }
            public void Dispose() { }
        }

        private class TestFileLoggerProvider : ILoggerProvider
        {
            private readonly TestLoggerCollector _collector;
            public TestFileLoggerProvider(TestLoggerCollector collector)
            {
                _collector = collector;
            }
            public ILogger CreateLogger(string categoryName)
            {
                return new TestRecordingLogger(_collector);
            }
            public void Dispose() { }
        }

        private class TestRecordingLogger : ILogger
        {
            private readonly TestLoggerCollector _collector;

            public TestRecordingLogger(TestLoggerCollector collector)
            {
                _collector = collector;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

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

        private sealed class TestAllActivityListener : IDisposable
        {
            private readonly ActivityListener _listener;

            public TestAllActivityListener()
            {
                _listener = new ActivityListener
                {
                    ShouldListenTo = _ => true,
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
                };
                ActivitySource.AddActivityListener(_listener);
            }

            public void Dispose()
            {
                _listener.Dispose();
            }
        }
    }
}
