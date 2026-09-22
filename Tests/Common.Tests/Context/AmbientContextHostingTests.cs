using ActDim.Practix.Abstractions.Context.Extensions;
using ActDim.Practix.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Context
{
    public class AmbientContextHostingTests
    {
        [Fact]
        public async Task GenericHost_RootAmbientContext_FlowsToHostedBackgroundService()
        {
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) =>
                {
                    services.AddAmbientContext();
                    services.AddSingleton<ITestWorkerService, TestWorkerService>();
                    services.AddHostedService<TestBackgroundWorker>();
                });

            using var host = hostBuilder.Build();
            var workerService = host.Services.GetRequiredService<ITestWorkerService>();

            using var appCts = new CancellationTokenSource();

            // Entry point pattern: establish root AmbientContext for the host run lifetime
            using (AmbientContext.Current.WithServices(host.Services))
            using (AmbientContext.Current.WithCancellationToken(appCts.Token))
            {
                await host.StartAsync(TestContext.Current.CancellationToken);

                // Give background worker a slice of time to execute within the ambient context
                await Task.Delay(60, TestContext.Current.CancellationToken);

                Assert.True(workerService.HasExecuted);
                Assert.Equal("BackgroundWorker_Processed", workerService.LastResult);

                await host.StopAsync(TestContext.Current.CancellationToken);
            }
        }

        [Fact]
        public async Task WebServerHost_PropagatesAmbientContext_WithScopedRequestServices()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAmbientContext();
            builder.Services.AddScoped<ITestOrderService, TestOrderService>();

            await using var app = builder.Build();

            // Middleware that initialises AmbientContext for the lifetime of each incoming HTTP request
            app.Use(async (context, next) =>
            {
                using var _s = AmbientContext.Current.WithServices(context.RequestServices);
                using var _u = AmbientContext.Current.WithUser(context.User);
                using var _c = AmbientContext.Current.WithCancellationToken(context.RequestAborted);
                using var _t = AmbientContext.Push("RequestId", "req-12345");

                await next();
            });

            app.MapGet("/test-ambient", () =>
            {
                // Business logic inside endpoint resolves dependencies and ambient state directly from AmbientContext
                var orderService = AmbientContext.Current.GetServices()!.GetRequiredService<ITestOrderService>();
                var requestId = AmbientContext.Current.Properties["RequestId"]?.ToString();

                return Results.Ok(new
                {
                    ServiceName = orderService.GetName(),
                    RequestId = requestId
                });
            });

            await app.StartAsync(TestContext.Current.CancellationToken);

            var client = app.GetTestClient();
            var response = await client.GetAsync("/test-ambient", TestContext.Current.CancellationToken);
            Assert.True(response.IsSuccessStatusCode);

            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("TestOrderService_Active", body);
            Assert.Contains("req-12345", body);

            await app.StopAsync(TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task WebApplication_RootContextWithScopedRequestMiddleware_DemonstratesFullLifecycle()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAmbientContext();
            builder.Services.AddSingleton<IRootConfigService>(new RootConfigService("Production_Config"));
            builder.Services.AddScoped<ITestOrderService, TestOrderService>();

            await using var app = builder.Build();

            // Middleware establishing per-request scoped ambient overrides over root context
            app.Use(async (context, next) =>
            {
                using var _s = AmbientContext.Current.WithServices(context.RequestServices);
                using var _u = AmbientContext.Current.WithUser(context.User);
                using var _c = AmbientContext.Current.WithCancellationToken(context.RequestAborted);
                using var _t = AmbientContext.Push("RequestId", "req-scoped-999");

                await next();
            });

            app.MapGet("/api/order", () =>
            {
                // In endpoint handler: resolves scoped services AND root services from AmbientContext
                var orderService = AmbientContext.Current.GetServices()!.GetRequiredService<ITestOrderService>();
                var rootConfig = AmbientContext.Current.GetServices()!.GetRequiredService<IRootConfigService>();
                var requestId = AmbientContext.Current.Properties["RequestId"]?.ToString();

                return Results.Ok(new
                {
                    OrderName = orderService.GetName(),
                    Config = rootConfig.ConfigName,
                    ReqId = requestId
                });
            });

            // Root Application Level Scope (disposed before post-scope assertion)
            using (AmbientContext.Current.WithServices(app.Services))
            using (AmbientContext.Current.WithCancellationToken(app.Lifetime.ApplicationStopping))
            {
                await app.StartAsync(TestContext.Current.CancellationToken);

                // Verify root services are accessible at root level
                var rootServiceAtRoot = AmbientContext.Current.GetServices()!.GetRequiredService<IRootConfigService>();
                Assert.Equal("Production_Config", rootServiceAtRoot.ConfigName);

                // Execute HTTP request
                var client = app.GetTestClient();
                var response = await client.GetAsync("/api/order", TestContext.Current.CancellationToken);
                Assert.True(response.IsSuccessStatusCode);

                var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                Assert.Contains("TestOrderService_Active", json);
                Assert.Contains("Production_Config", json);
                Assert.Contains("req-scoped-999", json);

                await app.StopAsync(TestContext.Current.CancellationToken);
            }

            // Outside root scope: AmbientContext.Current.GetServices() is null
            Assert.Null(AmbientContext.Current.GetServices());
        }

        private interface ITestWorkerService
        {
            bool HasExecuted { get; }
            string? LastResult { get; }
            void MarkExecuted(string result);
        }

        private sealed class TestWorkerService : ITestWorkerService
        {
            public bool HasExecuted { get; private set; }
            public string? LastResult { get; private set; }

            public void MarkExecuted(string result)
            {
                HasExecuted = true;
                LastResult = result;
            }
        }

        private sealed class TestBackgroundWorker : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                // Background worker resolves service from AmbientContext established at host root
                if (AmbientContext.Current.GetServices()?.GetService<ITestWorkerService>() is { } workerService)
                {
                    workerService.MarkExecuted("BackgroundWorker_Processed");
                }

                await Task.CompletedTask;
            }
        }

        private interface IRootConfigService
        {
            string ConfigName { get; }
        }

        private sealed class RootConfigService : IRootConfigService
        {
            public string ConfigName { get; }

            public RootConfigService(string configName)
            {
                ConfigName = configName;
            }
        }

        private interface ITestOrderService
        {
            string GetName();
        }

        private sealed class TestOrderService : ITestOrderService
        {
            public string GetName()
            {
                return "TestOrderService_Active";
            }
        }
    }
}
