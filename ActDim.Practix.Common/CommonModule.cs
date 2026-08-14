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
        public class CommonModule : Module
{
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<JsonConfigurationManager>()
                .As<IJsonConfigurationManager>()
                .SingleInstance();
            // builder.RegisterType<ConcurrencyManager>()
            //    .As<IConcurrencyManager>()
            //    .SingleInstance();
            builder.RegisterInstance(CallContextProvider.Instance)
                .As<ICallContextProvider>();
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
