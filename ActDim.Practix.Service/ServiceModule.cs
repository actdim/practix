using Autofac;
using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Service.Json;

namespace ActDim.Practix
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<StandardJsonSerializer>()
                .As<IJsonSerializer>()
                .SingleInstance();
        }
    }
}