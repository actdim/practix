using System.Threading;
using System.Threading.Tasks;
using Autofac;
using CanarySystems.Misc.Common;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;

namespace CanarySystems.Caching
{
    public partial class CachingModule : TrackableModule
    {
        protected override void LoadOnce(ContainerBuilder builder)
        {
            builder
             .RegisterGeneric(typeof(CachingProxyFactory<>))
                .As(typeof(ICachingProxyFactory<>))
                // .SingleInstance()
                .InstancePerLifetimeScope()
                ;
            builder.RegisterType<InvocationContextSerializer>()
                .As(typeof(IInvocationContextSerializer))
                // .SingleInstance()
                .InstancePerLifetimeScope()
                ;

            builder.RegisterType<SystemClock>()
                .As(typeof(ISystemClock))
                .SingleInstance()
                // .InstancePerLifetimeScope()
                ;

            builder.RegisterInstance(new MemoryCache(new MemoryCacheOptions())).As<IMemoryCache>();

            builder.RegisterType<DummyDistributedCache>()
               .As<IDistributedCache>()
               // .SingleInstance()
               .InstancePerLifetimeScope();
        }

        private class DummyDistributedCache : IDistributedCache
        {
            public byte[] Get(string key)
            {
                throw new System.NotImplementedException();
            }

            public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
            {
                throw new System.NotImplementedException();
            }

            public void Refresh(string key)
            {
                throw new System.NotImplementedException();
            }

            public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
            {
                throw new System.NotImplementedException();
            }

            public void Remove(string key)
            {
                throw new System.NotImplementedException();
            }

            public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
            {
                throw new System.NotImplementedException();
            }

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
            {
                throw new System.NotImplementedException();
            }

            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
