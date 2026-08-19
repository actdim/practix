using ActDim.Observability.Tests.OpenObserve;
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
    public class OpenObserveIntegrationTests
    {
        [Fact]
        public async Task OpenObserve_WriteAndQueryLogs_ExecutesSqlSearchSuccessfully()
        {
            // =========================================================================
            // 1. ARRANGE: Подготовка сервера OpenObserve, DI контейнера и контекста
            // =========================================================================
            var options = new OpenObserveOptions
            {
                BaseUrl = "http://localhost:5080",
                Organization = "default",
                Stream = "actdim",
                UserEmail = "root@example.com",
                UserPassword = "Complexpass#123",
                BatchInterval = TimeSpan.FromMilliseconds(50)
            };

            var client = new OpenObserveClient(options);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Process? startedProcess = null;
            string? tempStoragePath = null;

            bool isServerRunning = await client.IsServerAvailableAsync(cts.Token);

            if (!isServerRunning)
            {
                // Запуск локального процесса OpenObserve при необходимости
                var binaryPath = FindOpenObserveBinary();
                if (binaryPath != null)
                {
                    tempStoragePath = Path.Combine(Path.GetTempPath(), "actdim-oo-data-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempStoragePath);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = binaryPath,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    startInfo.EnvironmentVariables["ZO_DATA_DIR"] = tempStoragePath;
                    startInfo.EnvironmentVariables["ZO_HTTP_PORT"] = "5080";
                    startInfo.EnvironmentVariables["ZO_ROOT_USER_EMAIL"] = options.UserEmail;
                    startInfo.EnvironmentVariables["ZO_ROOT_USER_PASS"] = options.UserPassword;

                    startedProcess = Process.Start(startInfo);

                    for (int i = 0; i < 50; i++)
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
                // Если сервер не доступен и бинарник не найден — аккуратно завершаем тест
                return;
            }

            try
            {
                // Настройка DI контейнера приложения с AmbientContext и провайдером OpenObserve
                var services = new ServiceCollection();
                services.AddAmbientContext();
                services.AddLogging(builder =>
                {
                    builder.AddOpenObserve(opts =>
                    {
                        opts.BaseUrl = options.BaseUrl;
                        opts.Organization = options.Organization;
                        opts.Stream = options.Stream;
                        opts.UserEmail = options.UserEmail;
                        opts.UserPassword = options.UserPassword;
                        opts.BatchInterval = TimeSpan.FromMilliseconds(50);
                    });
                });

                using var serviceProvider = services.BuildServiceProvider();
                using var _ambientScope = AmbientContext.WithServices(serviceProvider);
                using var _tenantScope = AmbientContext.Push("tenant.id", "tenant-oo-777");

                var logger = serviceProvider.GetRequiredService<ILogger<OpenObserveIntegrationTests>>();
                var provider = serviceProvider.GetServices<ILoggerProvider>().OfType<OpenObserveLoggerProvider>().Single();

                var uniqueId = Guid.NewGuid().ToString("N");
                var testMessage = $"OpenObserve integration log test message - {uniqueId}";
                var orderId = "ord-oo-999";

                // =========================================================================
                // 2. ACT: Выполнение бизнес-логики с логированием и скоупом метода
                // =========================================================================
                using (var _methodScope = logger.BeginMethodScope(new[] { KeyValuePair.Create("order.id", (object?)orderId) }))
                {
                    logger.LogInformation("{TestMessage}", testMessage);
                }

                // Сброс очереди логгера для моментальной отправки пакета в OpenObserve
                provider.Flush();
                await Task.Delay(500, cts.Token);

                // =========================================================================
                // 3. ASSERT: Выполнение SQL поиска и проверка сохраненных данных в OpenObserve
                // =========================================================================
                var sqlQuery = $"SELECT * FROM actdim WHERE msg LIKE '%{uniqueId}%'";
                var results = await client.QuerySqlQueryAsync(sqlQuery, cts.Token);

                Assert.NotEmpty(results);
                var logRecord = results[0];

                Assert.True(logRecord["level"]?.ToString() == "info" || logRecord["level"]?.ToString() == "information");
                Assert.Contains(testMessage, logRecord["msg"]?.ToString());
                Assert.Equal("tenant-oo-777", logRecord["tenant.id"]?.ToString());
                Assert.Equal("ord-oo-999", logRecord["order.id"]?.ToString());
                Assert.Equal(nameof(OpenObserve_WriteAndQueryLogs_ExecutesSqlSearchSuccessfully), logRecord["code.function"]?.ToString());
                Assert.Equal("OpenObserveIntegrationTests.cs", logRecord["code.filename"]?.ToString());
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

        private static string? FindOpenObserveBinary()
        {
            var candidateNames = new[]
            {
                "openobserve-v0.92.2-windows-amd64.exe",
                "openobserve-windows-amd64.exe",
                "openobserve.exe"
            };

            var searchDirectories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "openobserve"),
                Path.Combine(Directory.GetCurrentDirectory(), "Tools"),
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Tools", "openobserve"),
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
                    var found = Directory.EnumerateFiles(dir, "*openobserve*.exe", SearchOption.AllDirectories).FirstOrDefault();
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
