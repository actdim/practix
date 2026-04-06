using ActDim.Practix.Abstractions.Compression;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Messaging;
using ActDim.Practix.Common.Json;
using ActDim.Practix.Compression;
using ActDim.Practix.Config;
using ActDim.Practix.Messaging;
using Autofac;

namespace ActDim.Practix
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
        }
    }
}