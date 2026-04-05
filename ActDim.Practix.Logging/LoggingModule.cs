using Autofac;
using ActDim.Practix.Abstractions.Logging;

namespace ActDim.Practix.Logging
{
    public class LoggingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterGeneric(typeof(LocalLoggerProviderGeneric<>))
                .As(typeof(ILocalLoggerProvider<>));
        }
    }
}
