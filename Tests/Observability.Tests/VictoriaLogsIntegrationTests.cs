using ActDim.Observability.Tests.VictoriaLogs;
using ActDim.Practix.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
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
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));

            Process? startedProcess = null;
            string? tempStoragePath = null;

            bool isServerRunning = await client.IsServerAvailableAsync(cts.Token);

            if (!isServerRunning)
            {
                // Try finding standalone VictoriaLogs binary in output/project/tools directories
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

                    // Wait up to 3 seconds for server readiness
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
                // VictoriaLogs instance is not running. Place victoria-logs-windows-amd64.exe into Tools folder or run Docker.
                return;
            }

            try
            {
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
