#nullable enable
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using System;
using System.Collections.Generic;

namespace ActDim.Practix.Context
{
    /// <summary>
    /// Extension methods for <see cref="ICallContext"/>.
    /// </summary>
    public static class CallContextExtensions
    {
        /// <summary>
        /// Suppresses external scopes (e.g. ASP.NET Core, HttpClient) from being written into telemetry tags for the returned scope.
        /// </summary>
        public static IDisposable SuppressExternalScopes(this ICallContext callContext)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            return callContext.Push(CallContextPropertyNames.IncludeExternalScopes, false);
        }

        /// <summary>
        /// Suppresses ambient CallContext properties from being written into telemetry tags for the returned scope.
        /// </summary>
        public static IDisposable SuppressCallContext(this ICallContext callContext)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            return callContext.Push(CallContextPropertyNames.IncludeCallContext, false);
        }

        /// <summary>
        /// Suppresses log output to console logger providers for the returned scope, while retaining OpenTelemetry Activity trace enrichment and other logger providers.
        /// </summary>
        public static IDisposable SuppressConsole(this ICallContext callContext)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            return callContext.Push(CallContextPropertyNames.SuppressConsole, true);
        }

        /// <summary>
        /// Suppresses log output to specific logger providers (e.g. "Console", "File", "Otlp") by their alias or name for the returned scope.
        /// </summary>
        public static IDisposable SuppressProviders(this ICallContext callContext, params string[] providerNames)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            if (providerNames == null || providerNames.Length == 0)
            {
                return new DisposableAction(() => { });
            }

            var current = callContext.Data.TryGetValue(CallContextPropertyNames.SuppressedProviders, out var val) && val is HashSet<string> set
                ? new HashSet<string>(set, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var name in providerNames)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    current.Add(name);
                }
            }

            return callContext.Push(CallContextPropertyNames.SuppressedProviders, current);
        }

        /// <summary>
        /// Reports operation progress percentage (0..100) for the returned scope.
        /// </summary>
        public static IDisposable ReportProgress(this ICallContext callContext, double percentage)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            return callContext.Push(CallContextPropertyNames.Progress, Math.Clamp(percentage, 0.0, 100.0));
        }

        /// <summary>
        /// Sets current operation status text and optional icon/emoji for the returned scope.
        /// </summary>
        public static IDisposable SetStatus(this ICallContext callContext, string status, string? icon = null)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            var d1 = callContext.Push(CallContextPropertyNames.Status, status);
            if (!string.IsNullOrEmpty(icon))
            {
                var d2 = callContext.Push(CallContextPropertyNames.Icon, icon);
                return new CompositeDisposable(d1, d2);
            }

            return d1;
        }

        /// <summary>
        /// Pushes arbitrary tags/labels into the ambient call context for the returned scope.
        /// </summary>
        public static IDisposable PushTags(this ICallContext callContext, params string[] tags)
        {
            if (callContext == null)
            {
                throw new ArgumentNullException(nameof(callContext));
            }

            if (tags == null || tags.Length == 0)
            {
                return new DisposableAction(() => { });
            }

            var current = callContext.Data.TryGetValue(CallContextPropertyNames.Tags, out var val) && val is HashSet<string> set
                ? new HashSet<string>(set, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    current.Add(tag);
                }
            }

            return callContext.Push(CallContextPropertyNames.Tags, current);
        }
    }

    internal sealed class CompositeDisposable : IDisposable
    {
        private readonly IDisposable _d1;
        private readonly IDisposable _d2;

        public CompositeDisposable(IDisposable d1, IDisposable d2)
        {
            _d1 = d1;
            _d2 = d2;
        }

        public void Dispose()
        {
            _d2.Dispose();
            _d1.Dispose();
        }
    }
}
