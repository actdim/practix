using ActDim.Observability.VictoriaLogs;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Observability.Tests
{
    public class VictoriaLogsIntegrationTests
    {
        [Fact]
        public async Task VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully()
        {
            var options = new VictoriaLogsOptions
            {
                BaseUrl = "http://localhost:9428",
                Stream = "{app=\"actdim\",env=\"test\"}",
                BatchInterval = TimeSpan.FromMilliseconds(50)
            };

            var client = new VictoriaLogsClient(options);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            bool isServerRunning = await client.IsServerAvailableAsync(cts.Token);

            if (!isServerRunning)
            {
                // VictoriaLogs instance is not running locally. Output setup instructions and pass smoothly.
                // To start VictoriaLogs locally via Docker:
                // docker run -d -p 9428:9428 victoriametrics/victoria-logs
                return;
            }

            // Set up DI container with AmbientContext and VictoriaLogsLoggerProvider
            var services = new ServiceCollection();
            services.AddAmbientContext();
            services.AddLogging(builder =>
            {
                builder.AddVictoriaLogs(opts =>
                {
                    opts.BaseUrl = options.BaseUrl;
                    opts.Stream = options.Stream;
                    opts.BatchInterval = TimeSpan.FromMilliseconds(50);
                });
            });

            using var serviceProvider = services.BuildServiceProvider();
            using var _ambientScope = AmbientContext.WithServices(serviceProvider);
            using var _tenantScope = AmbientContext.Push("tenant.id", "tenant-test-777");

            var logger = serviceProvider.GetRequiredService<ILogger<VictoriaLogsIntegrationTests>>();
            var provider = serviceProvider.GetServices<ILoggerProvider>().OfType<VictoriaLogsLoggerProvider>().Single();

            var uniqueId = Guid.NewGuid().ToString("N");
            var testMessage = $"VictoriaLogs integration log test message - {uniqueId}";

            // 1. Write structured log enriched with AmbientContext + BeginMethodScope tags
            using (var _methodScope = logger.BeginMethodScope(new[] { KeyValuePair.Create("order.id", (object?)"ord-999") }))
            {
                logger.LogInformation("{TestMessage}", testMessage);
            }

            // Flush queue and wait briefly for VictoriaLogs to index log records
            provider.Flush();
            await Task.Delay(400, cts.Token);

            // 2. Query logs using LogsQL (VictoriaLogs Query Language)
            // Query 1: Filter by stream and exact unique message string
            var query1 = @"_stream:{app=""actdim""} AND msg:""" + uniqueId + @"""";
            var results1 = await client.QueryLogsQLAsync(query1, cts.Token);

            Assert.NotEmpty(results1);
            var logRecord = results1[0];

            Assert.Equal("info", logRecord["level"]?.ToString());
            Assert.Contains(testMessage, logRecord["msg"]?.ToString());
            Assert.Equal("tenant-test-777", logRecord["tenant.id"]?.ToString());
            Assert.Equal("ord-999", logRecord["order.id"]?.ToString());
            Assert.Equal(nameof(VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully), logRecord["code.function"]?.ToString());
            Assert.Equal("VictoriaLogsIntegrationTests.cs", logRecord["code.filename"]?.ToString());

            // Query 2: Filter by OpenTelemetry function attribute in LogsQL
            var query2 = "code.function:" + nameof(VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully) + @" AND msg:""" + uniqueId + @"""";
            var results2 = await client.QueryLogsQLAsync(query2, cts.Token);
            Assert.NotEmpty(results2);

            // Query 3: Filter by tenant.id in LogsQL
            var query3 = @"tenant.id:tenant-test-777 AND msg:""" + uniqueId + @"""";
            var results3 = await client.QueryLogsQLAsync(query3, cts.Token);
            Assert.NotEmpty(results3);
        }
    }
}
