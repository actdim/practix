#nullable enable
using ActDim.Practix.Abstractions.Context;
using ActDim.Practix.Context;
using System.Threading.Tasks;
using Xunit;

namespace ActDim.Practix.Common.Tests.Context
{
    public class AmbientContextTests
    {
        [Fact]
        public void AmbientContext_PushProperty_SetsAndRestoresValues()
        {
            var provider = AmbientContextProvider.Instance;
            var context = provider.Get();

            Assert.False(context.Properties.ContainsKey("TenantId"));

            using (context.PushProperty("TenantId", "Tenant_1"))
            {
                Assert.Equal("Tenant_1", context.Properties["TenantId"]);
                Assert.Equal("Tenant_1", AmbientContext.CurrentProperties["TenantId"]);

                using (context.PushProperty("TenantId", "Tenant_2"))
                {
                    Assert.Equal("Tenant_2", context.Properties["TenantId"]);
                }

                Assert.Equal("Tenant_1", context.Properties["TenantId"]);
            }

            Assert.False(context.Properties.ContainsKey("TenantId"));
        }

        [Fact]
        public async Task AmbientContext_FlowsAcrossAsyncCalls_WithoutCrossTaskPollution()
        {
            var provider = AmbientContextProvider.Instance;
            var context = provider.Get();

            using (AmbientContext.Push("FlowId", "MainFlow"))
            {
                Assert.Equal("MainFlow", context.Properties["FlowId"]);

                var task1 = Task.Run(async () =>
                {
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                    using (AmbientContext.Push("FlowId", "Branch_1"))
                    {
                        await Task.Yield();
                        Assert.Equal("Branch_1", context.Properties["FlowId"]);
                    }
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                });

                var task2 = Task.Run(async () =>
                {
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                    using (AmbientContext.Push("FlowId", "Branch_2"))
                    {
                        await Task.Yield();
                        Assert.Equal("Branch_2", context.Properties["FlowId"]);
                    }
                    Assert.Equal("MainFlow", context.Properties["FlowId"]);
                });

                await Task.WhenAll(task1, task2);

                Assert.Equal("MainFlow", context.Properties["FlowId"]);
            }

            Assert.False(context.Properties.ContainsKey("FlowId"));
        }
    }
}
