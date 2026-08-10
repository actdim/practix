using ActDim.Practix.Common.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Runtime
{
    public class ReachabilityObserverTests
    {
        [Fact]
        public void SubscribeAndFinalize_CallsHandler_WhenKeyIsCollected()
        {
            var handled = false;
            var key = SubscribeAndRelease(() =>
            {
                handled = true;
            });

            CollectUntilUnreachable(key);

            Assert.True(handled, "Handler was not called after key was finalized");
        }

        [Fact]
        public void SubscribeAndUnsubscribe_DoesNotCallHandler_WhenKeyIsCollected()
        {
            var handled = false;

            void Handler() => handled = true;

            var key = SubscribeAndUnsubscribeAndRelease(Handler);
            CollectUntilUnreachable(key);

            Assert.False(handled, "Handler should not be called after unsubscribing");
        }

        [Fact]
        public void Subscribe_MultipleHandlers_AllCalled_WhenKeyIsCollected()
        {
            var count = 0;
            var key = SubscribeAndRelease(
                () => ++count,
                () => ++count);
            CollectUntilUnreachable(key);

            Assert.Equal(2, count);
        }

        [Fact]
        public void Subscribe_DifferentKeys_DontInterfere()
        {
            var key1Handled = false;
            var key2Handled = false;
            var key1 = SubscribeAndRelease(() => key1Handled = true);
            var key2 = SubscribeAndRelease(() => key2Handled = true);
            CollectUntilUnreachable(key1, key2);

            Assert.True(key1Handled);
            Assert.True(key2Handled);
        }

        [Fact]
        public async Task Subscribe_HandlerThrows_CrashesProcess()
        {
            // Documents the bug: the finalizer has no try/catch,
            // so a throwing handler propagates out and terminates the process.
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using ActDim.Practix.Common.Runtime;

                Subscription.Create();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                Console.WriteLine("OK");

                class TestKey { }

                static class Subscription
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public static void Create()
                    {
                        var key = new TestKey();
                        ReachabilityObserver<TestKey>.Subscribe(key, () => throw new InvalidOperationException("boom"));
                    }
                }
                """;

            await RunInSubprocessAsync(source, expectCrash: true);
        }

        [Fact]
        public async Task HandlerWithTryCatch_DoesNotCrash()
        {
            // If the user wraps their handler in try/catch, the process survives.
            var source = """
                using System;
                using System.Runtime.CompilerServices;
                using ActDim.Practix.Common.Runtime;

                Subscription.Create();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                Console.WriteLine("OK");

                class TestKey { }

                static class Subscription
                {
                    [MethodImpl(MethodImplOptions.NoInlining)]
                    public static void Create()
                    {
                        var key = new TestKey();
                        ReachabilityObserver<TestKey>.Subscribe(key, () =>
                        {
                            try
                            {
                                throw new InvalidOperationException("boom");
                            }
                            catch
                            {
                                // Handled by the callback.
                            }
                        });
                    }
                }
                """;

            await RunInSubprocessAsync(source, expectCrash: false);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeAndRelease(
            Action firstHandler,
            Action secondHandler = null)
        {
            var key = new TestKey();
            ReachabilityObserver<TestKey>.Subscribe(key, firstHandler);

            if (secondHandler is not null)
            {
                ReachabilityObserver<TestKey>.Subscribe(key, secondHandler);
            }

            return new WeakReference(key);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference SubscribeAndUnsubscribeAndRelease(Action handler)
        {
            var key = new TestKey();
            ReachabilityObserver<TestKey>.Subscribe(key, handler);
            ReachabilityObserver<TestKey>.Unsubscribe(key, handler);

            return new WeakReference(key);
        }

        private static void CollectUntilUnreachable(params WeakReference[] references)
        {
            const int maxAttempts = 10;

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

                var hasReachableReference = false;

                foreach (var reference in references)
                {
                    if (reference.IsAlive)
                    {
                        hasReachableReference = true;
                        break;
                    }
                }

                if (!hasReachableReference)
                {
                    return;
                }
            }

            foreach (var reference in references)
            {
                Assert.False(reference.IsAlive, "Observed key remained reachable after forced garbage collection.");
            }
        }

        private static async Task RunInSubprocessAsync(string source, bool expectCrash)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "finalization-test-" + Guid.NewGuid().ToString("n"));
            var commonAssemblyPath = Path.Combine(
                AppContext.BaseDirectory,
                "ActDim.Practix.Common.dll");
            Directory.CreateDirectory(tempDir);

            var csproj = $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net10.0</TargetFramework>
                    <Nullable>disable</Nullable>
                    <ImplicitUsings>disable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <Reference Include="ActDim.Practix.Common">
                      <HintPath>{commonAssemblyPath}</HintPath>
                      <Private>true</Private>
                    </Reference>
                  </ItemGroup>
                </Project>
                """;

            File.WriteAllText(Path.Combine(tempDir, "Program.cs"), source);
            File.WriteAllText(Path.Combine(tempDir, "temp.csproj"), csproj);

            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project \"" + Path.Combine(tempDir, "temp.csproj") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi) ?? throw new Exception("Failed to start process");
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            proc.WaitForExit();

            if (expectCrash)
            {
                Assert.NotEqual(0, proc.ExitCode);
            }
            else
            {
                Assert.Equal(0, proc.ExitCode);
                Assert.Contains("OK", stdout);
            }

            try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
        }

        public class TestKey
        {
            public override string ToString() => "ReachabilityObserverTests.TestKey";
        }
    }
}
