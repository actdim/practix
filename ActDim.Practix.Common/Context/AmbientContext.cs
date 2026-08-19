using ActDim.BytePath;
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Disposal;
using Ardalis.GuardClauses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading;

namespace ActDim.Practix.Context
{
    /// <inheritdoc />
    public sealed class AmbientContext : IAmbientContext
    {
        private static readonly AsyncLocal<ImmutableDictionary<string, object>> _current = new();
        private static readonly AmbientContext _instance = new();
        private static readonly ClaimsPrincipal AnonymousUser = new(new ClaimsIdentity());

        private AmbientContext()
        {
        }

        /// <inheritdoc />
        public IDisposable PushProperty(string name, object value)
        {
            Guard.Against.NullOrEmpty(name, nameof(name));

            var previous = _current.Value ?? ImmutableDictionary<string, object>.Empty;
            var existed = previous.TryGetValue(name, out var oldValue);

            _current.Value = previous.SetItem(name, value);

            return new DisposableAction(() =>
            {
                var latest = _current.Value ?? ImmutableDictionary<string, object>.Empty;
                if (existed)
                {
                    _current.Value = latest.SetItem(name, oldValue!);
                }
                else
                {
                    _current.Value = latest.Remove(name);
                }
            });
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Properties => _current.Value ?? ImmutableDictionary<string, object>.Empty;

        // ══ Static Convenience API (zero-DI ceremony) ═════════════════════════

        /// <summary>
        /// Gets the current ambient context instance for the calling async flow.
        /// </summary>
        public static IAmbientContext Current => _instance;

        /// <summary>
        /// Gets the current ambient context properties for the calling async flow.
        /// </summary>
        public static IReadOnlyDictionary<string, object> CurrentProperties => Current.Properties;

        /// <summary>
        /// Pushes a property into the ambient context for the current async flow.
        /// </summary>
        public static IDisposable Push(string name, object value)
        {
            return Current.PushProperty(name, value);
        }

        // ══ Scoped Services Access & Overrides ════════════════════════════════

        /// <summary>
        /// Gets the active scoped <see cref="IServiceProvider"/> from ambient context.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when no service provider is available in ambient context.</exception>
        public static IServiceProvider Services =>
            Current.GetServices() ?? throw new InvalidOperationException(
                "No active IServiceProvider found in AmbientContext. Use 'using (AmbientContext.WithServices(serviceProvider))' to establish an execution scope.");

        /// <summary>
        /// Temporarily establishes the scoped <see cref="IServiceProvider"/> for the current async execution flow.
        /// </summary>
        public static IDisposable WithServices(IServiceProvider serviceProvider) => Current.WithServices(serviceProvider);

        // ══ User Context & Overrides ══════════════════════════════════════════

        /// <summary>
        /// Gets the current <see cref="ClaimsPrincipal"/> user identity. Resolves from ambient override -> default anonymous user.
        /// </summary>
        public static ClaimsPrincipal User => Current.GetUser() ?? AnonymousUser;

        /// <summary>
        /// Temporarily overrides the current user identity within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithUser(ClaimsPrincipal user) => Current.WithUser(user);

        // ══ CancellationToken & Timeout Overrides ═════════════════════════════

        /// <summary>
        /// Gets the current <see cref="CancellationToken"/>. Resolves from ambient override -> <see cref="CancellationToken.None"/>.
        /// </summary>
        public static CancellationToken CancellationToken => Current.GetCancellationToken() ?? CancellationToken.None;

        /// <summary>
        /// Temporarily overrides the current <see cref="CancellationToken"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithCancellationToken(CancellationToken customToken) => Current.WithCancellationToken(customToken);

        /// <summary>
        /// Applies a temporary linked timeout to the current <see cref="CancellationToken"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithTimeout(TimeSpan timeout, out CancellationToken token)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
            linkedCts.CancelAfter(timeout);

            token = linkedCts.Token;
            var scope = Current.WithCancellationToken(linkedCts.Token);

            return new DisposableAction(() =>
            {
                scope.Dispose();
                linkedCts.Dispose();
            });
        }

        // ══ Blob Management ═══════════════════════════════════════════════════

        /// <summary>
        /// Gets the active <see cref="IBlobManager"/> storage engine. Resolves from ambient override -> <see cref="Services"/>.
        /// </summary>
        public static IBlobManager Blobs => Current.GetBlobManager() ?? Services.GetRequiredService<IBlobManager>();

        /// <summary>
        /// Temporarily overrides the current <see cref="IBlobManager"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithBlobManager(IBlobManager blobManager) => Current.WithBlobManager(blobManager);

        // ══ Compression Management ═════════════════════════════════════════════

        /// <summary>
        /// Gets the active <see cref="ICompressionManager"/> instance. Resolves from ambient override -> <see cref="Services"/>.
        /// </summary>
        public static ActDim.Practix.Abstractions.Compression.ICompressionManager Compression => Current.GetCompressionManager() ?? Services.GetRequiredService<ActDim.Practix.Abstractions.Compression.ICompressionManager>();

        /// <summary>
        /// Temporarily overrides the current <see cref="ICompressionManager"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithCompressionManager(ActDim.Practix.Abstractions.Compression.ICompressionManager compressionManager) => Current.WithCompressionManager(compressionManager);

        // ══ Fast Logging ══════════════════════════════════════════════════════

        /// <summary>
        /// Gets the active <see cref="ILoggerFactory"/> from ambient override -> <see cref="Services"/> -> <see cref="NullLoggerFactory.Instance"/>.
        /// </summary>
        public static ILoggerFactory LoggerFactory => Current.GetLoggerFactory() ?? TryGetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

        /// <summary>
        /// Temporarily overrides the current <see cref="ILoggerFactory"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithLoggerFactory(ILoggerFactory loggerFactory) => Current.WithLoggerFactory(loggerFactory);

        /// <summary>
        /// Gets an <see cref="ILogger{T}"/> instance for the specified generic category type.
        /// </summary>
        public static ILogger<T> Log<T>() => LoggerFactory.CreateLogger<T>();

        /// <summary>
        /// Gets an <see cref="ILogger"/> instance for the specified runtime <see cref="Type"/>.
        /// </summary>
        public static ILogger Log(Type type)
        {
            ArgumentNullException.ThrowIfNull(type, nameof(type));
            return LoggerFactory.CreateLogger(type.FullName ?? type.Name);
        }

        /// <summary>
        /// Gets an <see cref="ILogger"/> instance for the caller instance's runtime type.
        /// </summary>
        public static ILogger Log(object instance)
        {
            ArgumentNullException.ThrowIfNull(instance, nameof(instance));
            return Log(instance.GetType());
        }

        private static T? TryGetService<T>() where T : class => Current.GetServices()?.GetService<T>();
    }
}
