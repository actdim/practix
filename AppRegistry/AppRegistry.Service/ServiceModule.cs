using ActDim.Practix.Service;
using Autofac;

namespace ActDim.AppRegistry.Service
{
    public class ServiceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AppRegistryService>()
               .As<IAppRegistryService>()
               .SingleInstance();

            builder.RegisterType<AppContext>()
               .As<IAppContext>()
               .InstancePerLifetimeScope();
        }
    }
}