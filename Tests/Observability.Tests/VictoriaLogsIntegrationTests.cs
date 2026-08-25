using ActDim.Observability.Tests.VictoriaLogs;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;

namespace ActDim.Observability.Tests
{
    public class VictoriaLogsIntegrationTests
    {
        [Fact]
        public async Task VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully()
        {
            // =========================================================================
            // 1. ARRANGE: Prepare VictoriaLogs server, DI container, and ambient context
            // =========================================================================
            var options = new VictoriaLogsOptions
            {
                BaseUrl = "http://localhost:9428",
                Stream = "{app=\"actdim\",env=\"test\"}",
                BatchInterval = TimeSpan.FromMilliseconds(50)
            };

            var client = new VictoriaLogsClient(options);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Process? startedProcess = null;
            string? tempStoragePath = null;

            bool isServerRunning = await client.IsServerAvailableAsync(cts.Token);

            if (!isServerRunning)
            {
                // Auto-start local VictoriaLogs executable if available
                var binaryPath = FindVictoriaLogsBinary();
                if (binaryPath != null)
                {
                    tempStoragePath = Path.Combine(Path.GetTempPath(), "actdim-vl-data-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempStoragePath);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = binaryPath,
                        Arguments = $"-storageDataPath \"{tempStoragePath}\" -httpListenAddr \":9428\" -retentionPeriod 1d",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    startedProcess = Process.Start(startInfo);

                    for (int i = 0; i < 30; i++)
                    {
                        await Task.Delay(100, cts.Token);
                        if (await client.IsServerAvailableAsync(cts.Token))
                        {
                            isServerRunning = true;
                            break;
                        }
                    }
                }
            }

            if (!isServerRunning)
            {
                // VictoriaLogs instance is not running. Place binary into Tools folder or run Docker.
                return;
            }

            try
            {
                // Configure application DI container with AmbientContext and VictoriaLogs provider
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
                var orderId = "ord-999";

                // =========================================================================
                // 2. ACT: Execute business logic with logging and method scope
                // =========================================================================
                using (var _methodScope = logger.BeginMethodScope(new[] { KeyValuePair.Create("order.id", (object?)orderId) }))
                {
                    logger.LogInformation("{TestMessage}", testMessage);
                }

                // Flush logger queue to send batch payload to VictoriaLogs
                provider.Flush();

                // =========================================================================
                // 3. ASSERT: Execute LogsQL queries and verify recorded telemetry
                // =========================================================================
                var query1 = $"""msg:"{uniqueId}" """;
                IReadOnlyList<Dictionary<string, object?>> results1 = Array.Empty<Dictionary<string, object?>>();

                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(200, cts.Token);
                    results1 = await client.QueryLogsQLAsync(query1, cts.Token);
                    if (results1.Count > 0)
                    {
                        break;
                    }
                }

                if (results1.Count == 0)
                {
                    results1 = await client.QueryLogsQLAsync("_time:5m", cts.Token);
                }

                Assert.NotEmpty(results1);
                var logRecord = results1.FirstOrDefault(r => (r.TryGetValue("_msg", out var m) || r.TryGetValue("msg", out m)) && m?.ToString()?.Contains(uniqueId) == true) ?? results1[0];

                var msgValue = logRecord.TryGetValue("_msg", out var msgObj) ? msgObj?.ToString() : logRecord["msg"]?.ToString();
                Assert.True(logRecord["level"]?.ToString() == "info" || logRecord["level"]?.ToString() == "information");
                Assert.Contains(testMessage, msgValue);
                Assert.Equal("tenant-test-777", logRecord["tenant.id"]?.ToString());
                Assert.Equal("ord-999", logRecord["order.id"]?.ToString());
                Assert.Equal(nameof(VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully), logRecord["code.function"]?.ToString());
                Assert.Equal("VictoriaLogsIntegrationTests.cs", logRecord["code.filename"]?.ToString());

                // Query 2: Filter by OpenTelemetry caller function attribute in LogsQL
                var query2 = $"""code.function:"{nameof(VictoriaLogs_WriteAndQueryLogs_ExecutesLogsQLSuccessfully)}" """;
                var results2 = await client.QueryLogsQLAsync(query2, cts.Token);
                Assert.NotEmpty(results2);

                // Query 3: Filter by tenant.id ambient context property in LogsQL
                var query3 = $"""tenant.id:"tenant-test-777" """;
                var results3 = await client.QueryLogsQLAsync(query3, cts.Token);
                Assert.NotEmpty(results3);
            }
            finally
            {
                if (startedProcess != null && !startedProcess.HasExited)
                {
                    startedProcess.Kill(entireProcessTree: true);
                    startedProcess.Dispose();
                }

                if (tempStoragePath != null && Directory.Exists(tempStoragePath))
                {
                    try { Directory.Delete(tempStoragePath, recursive: true); } catch { }
                }
            }
        }

        private static string? FindVictoriaLogsBinary()
        {
            var candidateNames = new[]
            {
                "victoria-logs-windows-amd64-prod.exe",
                "victoria-logs-windows-amd64.exe",
                "victoria-logs.exe"
            };

            var searchDirectories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "victoria-logs"),
                Path.Combine(Directory.GetCurrentDirectory(), "Tools"),
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Tools", "victoria-logs"),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Tools")
            };

            foreach (var dir in searchDirectories)
            {
                if (!Directory.Exists(dir)) continue;

                foreach (var name in candidateNames)
                {
                    var fullPath = Path.Combine(dir, name);
                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }

                try
                {
                    var found = Directory.EnumerateFiles(dir, "*victoria-logs*.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch { }
            }

            return null;
        }
    }
}
