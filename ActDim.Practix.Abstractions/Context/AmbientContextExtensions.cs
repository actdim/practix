using System;
using System.Security.Claims;
using System.Threading;

namespace ActDim.Practix.Abstractions.Context.Extensions
{
    /// <summary>
    /// Core execution flow extension methods providing typed access and scoped overrides on <see cref="IAmbientContext"/>.
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
        /// Applies a temporary linked timeout to the current ambient <see cref="CancellationToken"/> within a <see langword="using"/> scope.
        /// </summary>
        public static IDisposable WithTimeout(this IAmbientContext context, TimeSpan timeout, out CancellationToken token)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));
            var currentToken = context.GetCancellationToken() ?? CancellationToken.None;
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(currentToken);
            linkedCts.CancelAfter(timeout);

            token = linkedCts.Token;
            var scope = context.WithCancellationToken(linkedCts.Token);

            return new TimeoutScope(scope, linkedCts);
        }

        private sealed class TimeoutScope : IDisposable
        {
            private readonly IDisposable _scope;
            private readonly CancellationTokenSource _cts;

            public TimeoutScope(IDisposable scope, CancellationTokenSource cts)
            {
                _scope = scope;
                _cts = cts;
            }

            public void Dispose()
            {
                _scope.Dispose();
                _cts.Dispose();
            }
        }
    }
}
