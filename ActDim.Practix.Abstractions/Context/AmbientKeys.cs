namespace ActDim.Practix.Abstractions.Context
{
    /// <summary>
    /// Well-known keys used to store typed execution context state inside <see cref="IAmbientContext"/>.
    /// </summary>
    public static class AmbientKeys
    {
        private const string Prefix = "__Ambient_";

        /// <summary>
        /// Key storing the active <see cref="System.IServiceProvider"/> execution scope.
        /// </summary>
        public const string Services = Prefix + "Services";

        /// <summary>
        /// Key storing the active <see cref="System.Security.Claims.ClaimsPrincipal"/> user identity.
        /// </summary>
        public const string User = Prefix + "User";

        /// <summary>
        /// Key storing the active <see cref="System.Threading.CancellationToken"/> for operation cancellation.
        /// </summary>
        public const string CancellationToken = Prefix + "CancellationToken";

        /// <summary>
        /// Key storing the active <see cref="ActDim.BytePath.IBlobManager"/> storage manager.
        /// </summary>
        public const string BlobManager = Prefix + "BlobManager";

        /// <summary>
        /// Key storing the active <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>.
        /// </summary>
        public const string LoggerFactory = Prefix + "LoggerFactory";

        /// <summary>
        /// Key storing the active <see cref="ActDim.Practix.Abstractions.Compression.ICompressionManager"/>.
        /// </summary>
        public const string CompressionManager = Prefix + "CompressionManager";

        /// <summary>
        /// Key storing the active <see cref="Microsoft.IO.RecyclableMemoryStreamManager"/>.
        /// </summary>
        public const string MemoryManager = Prefix + "MemoryManager";
    }
}
