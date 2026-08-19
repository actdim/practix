using ActDim.BytePath;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading;

namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Extension methods providing typed access and scoped overrides on <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientContextExtensions
    {
        /// <summary>
        /// Gets the scoped <see cref="IServiceProvider"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static IServiceProvider? GetServices(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.Services, out var val) && val is IServiceProvider sp ? sp : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="IServiceProvider"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithServices(this IAmbientContext context, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));
            return context.PushProperty(AmbientKeys.Services, serviceProvider);
        }

        /// <summary>
        /// Gets the scoped <see cref="ClaimsPrincipal"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static ClaimsPrincipal? GetUser(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.User, out var val) && val is ClaimsPrincipal user ? user : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="ClaimsPrincipal"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithUser(this IAmbientContext context, ClaimsPrincipal user)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            return context.PushProperty(AmbientKeys.User, user);
        }

        /// <summary>
        /// Gets the scoped <see cref="CancellationToken"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static CancellationToken? GetCancellationToken(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.CancellationToken, out var val) && val is CancellationToken ct ? ct : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="CancellationToken"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithCancellationToken(this IAmbientContext context, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.PushProperty(AmbientKeys.CancellationToken, ct);
        }

        /// <summary>
        /// Gets the scoped <see cref="IBlobManager"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static IBlobManager? GetBlobManager(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.BlobManager, out var val) && val is IBlobManager bm ? bm : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="IBlobManager"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithBlobManager(this IAmbientContext context, IBlobManager blobManager)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(blobManager, nameof(blobManager));
            return context.PushProperty(AmbientKeys.BlobManager, blobManager);
        }

        /// <summary>
        /// Gets the scoped <see cref="ILoggerFactory"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static ILoggerFactory? GetLoggerFactory(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.LoggerFactory, out var val) && val is ILoggerFactory lf ? lf : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="ILoggerFactory"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithLoggerFactory(this IAmbientContext context, ILoggerFactory loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(loggerFactory, nameof(loggerFactory));
            return context.PushProperty(AmbientKeys.LoggerFactory, loggerFactory);
        }

        /// <summary>
        /// Gets the scoped <see cref="ActDim.Practix.Abstractions.Compression.ICompressionManager"/> from the ambient context, or <c>null</c> if not set.
        /// </summary>
        public static ActDim.Practix.Abstractions.Compression.ICompressionManager? GetCompressionManager(this IAmbientContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            return context.Properties.TryGetValue(AmbientKeys.CompressionManager, out var val) && val is ActDim.Practix.Abstractions.Compression.ICompressionManager cm ? cm : null;
        }

        /// <summary>
        /// Temporarily sets the scoped <see cref="ActDim.Practix.Abstractions.Compression.ICompressionManager"/> for the duration of the returned disposable scope.
        /// </summary>
        public static IDisposable WithCompressionManager(this IAmbientContext context, ActDim.Practix.Abstractions.Compression.ICompressionManager compressionManager)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            ArgumentNullException.ThrowIfNull(compressionManager, nameof(compressionManager));
            return context.PushProperty(AmbientKeys.CompressionManager, compressionManager);
        }
    }
}
