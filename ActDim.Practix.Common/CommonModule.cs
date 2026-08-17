using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Caching;
using ActDim.Practix.Common.Json;
using ActDim.Practix.Compression;
using ActDim.Practix.Config;
using ActDim.Practix.Context;
using Autofac;

namespace ActDim.Practix.Common
{
    /// <summary>
    /// Autofac module that registers core services provided by <c>ActDim.Practix.Common</c>:
    /// JSON configuration, ambient context, compression, JSON serializer, and caching proxies.
    /// </summary>
    public class CommonModule : Module
    {
        /// <inheritdoc />
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<JsonConfigurationManager>()
                .As<IJsonConfigurationManager>()
                .SingleInstance();
            builder.RegisterInstance(AmbientContextProvider.Instance)
                .As<IAmbientContextProvider>();
            builder.RegisterType<CompressionManager>()
                .As<ICompressionManager>()
                .SingleInstance();
            builder.RegisterType<StandardJsonSerializer>()
                .As<IJsonSerializer>()
                .SingleInstance();
            builder.RegisterType<MemoryCachingProxy>()
                .As<IMemoryCachingProxy>();
            builder.RegisterType<DistributedCachingProxy>()
                .As<IDistributedCachingProxy>();
        }
    }
}
