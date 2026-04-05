using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Messaging;
using Serilog;
using Microsoft.Extensions.Configuration;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ActDim.Practix.Abstractions.Logging;

namespace ActDim.Practix.Logging
{
    public static class LoggerBuilderExtensions
    {
        public static ILoggingBuilder AddAppLog(this ILoggingBuilder loggingBuilder, IConfiguration config, IServiceProvider serviceProvider)
        {
            loggingBuilder.AddConfiguration(config, serviceProvider);
            loggingBuilder.AddProvider(
                new LoggerProviderAdapter(categoryName =>
                {
                    return new LoggerProvider(serviceProvider).GetScoped(categoryName);
                }));

            return loggingBuilder;
        }

        public static void AddConfiguration(this ILoggingBuilder loggingBuilder, IConfiguration config, IServiceProvider seriveProvider)
        {
            ICallContextProvider callContextProvider = default;

            callContextProvider = seriveProvider.GetRequiredService<ICallContextProvider>();
            if (config == default)
            {
                config = seriveProvider.GetService<IConfiguration>();
            }

            // TODO:
            // ConfigurationBinder

            var appSection = config.GetSection("app");
            var appName = "";

            // suppressConsole
            var suppressStdout = false;

            if (string.IsNullOrWhiteSpace(appName))
            {
                appName = "common";
            }
            else
            {
                var invalidChars = Path.GetInvalidFileNameChars();
                if (appName.IndexOfAny(invalidChars) > -1)
                {
                    throw new InvalidOperationException($"Invalid application name. Check for invalid chars: {string.Join(", ", invalidChars)}");
                }
            }

            // https://github.com/serilog/serilog-sinks-console
            // SerializerConstants.DateTimeFormat
            // MM/dd/yyyy HH:mm:ss.FFF

            // Log.ForContext(new CallContextEnricher(_callContextProvider));

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Information()
                // .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                // .MinimumLevel.Override("System", LogEventLevel.Warning)
                // .ReadFrom.Settings(...)
                .Enrich.FromLogContext()
                .Enrich.With(new CallContextEnricher(callContextProvider))
                // .Enrich.WithProperty("Application", application)
                // .Enrich.WithProperty("Environment", ConfigurationManager.AppSettings["Environment"])
                .Enrich.With(new ThreadIdEnricher()) // .Enrich.With<ThreadIdEnricher>()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs\\{appName}.txt"),
                    retainedFileCountLimit: null,
                    fileSizeLimitBytes: 31457280,
                    rollOnFileSizeLimit: true,
                    rollingInterval: RollingInterval.Day,
                    // shared: true,
                    // + ContextProperty.CallContext can be used here
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] [{ThreadId}] [{ParentCorrelationId}/{CorrelationId}] {Message}{NewLine}{Exception}")
                .WriteTo.Logger(lc =>
                {
                    lc
                    .Filter.ByIncludingOnly(logEvent =>
                    {
                        // Matching.FromSource                        
                        // logEvent.RemovePropertyIfPresent(...);
                        return TestForPrimaryOutput(logEvent, callContextProvider, suppressStdout);
                    })
                    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{SourceContext}] [{ThreadId}] {Message}{NewLine}{Exception}",
                    standardErrorFromLevel: LogEventLevel.Error,
                    theme: AnsiConsoleTheme.Literate); // formatter: new JsonFormatter()
                })
                // Secondary (console) output
                .WriteTo.Logger(lc =>
                {
                    lc
                    .Filter.ByIncludingOnly(logEvent =>
                    {
                        // TODO: switch log configuration using CallContextProvider and CallContextProperty.LogConfigurationName
                        var include = !TestForPrimaryOutput(logEvent, callContextProvider, suppressStdout); // primary output is disabled

                        include = include && (logEvent.Level == LogEventLevel.Warning || logEvent.Level == LogEventLevel.Error || logEvent.Level == LogEventLevel.Fatal ||
                            logEvent.Properties.ContainsKey(ContextProperty.Status.ToString()) || logEvent.Properties.ContainsKey(ContextProperty.Progress.ToString()));

                        return include;
                    })
                    // we can use {Status}, {Progress} log properties here but currently they are presented in the message (serialized state)
                    // TODO: use smart serilog message template (expression) or custom formatter to improve rendering (presentation) of {Status}/{Progress} log properies
                    // https://nblumhardt.com/2021/06/customize-serilog-text-output/
                    .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message}{NewLine}{Exception}", // I think that we need to have SourceContext (LogCategory) in the message tempate: "[{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}" 
                    standardErrorFromLevel: LogEventLevel.Error,
                    theme: AnsiConsoleTheme.Literate); // formatter: new JsonFormatter()
                });
#if DEBUG
            // loggerConfiguration.MinimumLevel.Debug();
#endif
            var logger = loggerConfiguration.CreateLogger();

            // add redis
            // add elastic
            // https://www.humankode.com/asp-net-core/logging-with-elasticsearch-kibana-asp-net-core-and-docker/

            loggingBuilder.AddSerilog(logger);
        }

        private static bool TestForPrimaryOutput(LogEvent logEvent, ICallContextProvider callContextProvider, bool suppressStdout)
        {
            // check if primary output is disabled
            suppressStdout = suppressStdout || callContextProvider.Get().Data.Any(e => e.Key == CallContextProperty.SuppressStdout.ToString() && (e.Value is bool flag) && flag);
            return !suppressStdout;
        }
    }
}
