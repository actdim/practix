using ActDim.Observability.Tests.Seq;
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
    public class SeqIntegrationTests
    {
        [Fact]
        public async Task Seq_WriteAndQueryLogs_ExecutesSuccessfully()
        {
            // =========================================================================
            // 1. ARRANGE: Prepare Seq server client, process launcher, and test payload
            // =========================================================================
            var options = new SeqOptions
            {
                BaseUrl = "http://localhost:5341",
                BatchInterval = TimeSpan.FromMilliseconds(50)
            };

            var client = new SeqClient(options);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            Process? startedProcess = null;
            string? tempStoragePath = null;

            bool isServerRunning = await client.IsServerAvailableAsync(cts.Token);

            if (!isServerRunning)
            {
                // Auto-start local Seq binary if present in Tools folder
                var binaryPath = FindSeqBinary();
                if (binaryPath != null)
                {
                    tempStoragePath = Path.Combine(Path.GetTempPath(), "actdim-seq-data-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempStoragePath);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = binaryPath,
                        Arguments = $"run --storage=\"{tempStoragePath}\" --listen=\"http://localhost:5341\"",
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
                // Seq instance is not running. Place binary into Tools folder or run Docker.
                return;
            }

            try
            {
                var uniqueId = Guid.NewGuid().ToString("N");
                var testMessage = $"Seq integration log test message - {uniqueId}";
                var orderId = "ord-seq-888";
                var tenantId = "tenant-seq-999";

                var record = new Dictionary<string, object?>
                {
                    ["@t"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["@mt"] = "Seq test log: {TestMessage}",
                    ["@l"] = "Information",
                    ["TestMessage"] = testMessage,
                    ["tenant.id"] = tenantId,
                    ["order.id"] = orderId,
                    ["code.function"] = nameof(Seq_WriteAndQueryLogs_ExecutesSuccessfully),
                    ["code.filename"] = "SeqIntegrationTests.cs"
                };

                // =========================================================================
                // 2. ACT: Ingest CLEF structured log payload into Seq
                // =========================================================================
                await client.IngestClefRecordsAsync(new[] { record }, cts.Token);

                // =========================================================================
                // 3. ASSERT: Query events from Seq API and verify recorded properties
                // =========================================================================
                IReadOnlyList<Dictionary<string, object?>> results = Array.Empty<Dictionary<string, object?>>();

                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(200, cts.Token);
                    results = await client.QueryEventsAsync(ct: cts.Token);
                    if (results.Count > 0 && results.Any(r => r.ToString()?.Contains(uniqueId) == true))
                    {
                        break;
                    }
                }

                Assert.NotEmpty(results);

                var matchedLog = results.FirstOrDefault(r => r.ToString()?.Contains(uniqueId) == true) ?? results[0];
                Assert.NotNull(matchedLog);

                var recordText = matchedLog.ToString() ?? "";
                Assert.Contains(uniqueId, recordText);
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

        private static string? FindSeqBinary()
        {
            var candidateNames = new[]
            {
                "seqcli.exe",
                "seq.exe",
                "seq-cli.exe"
            };

            var searchDirectories = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Tools", "seq"),
                Path.Combine(Directory.GetCurrentDirectory(), "Tools"),
                Directory.GetCurrentDirectory(),
                AppDomain.CurrentDomain.BaseDirectory,
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Tools", "seq"),
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
                    var found = Directory.EnumerateFiles(dir, "*seq*.exe", SearchOption.AllDirectories).FirstOrDefault();
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
