using ActDim.Practix.Abstractions.Json;
using ActDim.Practix.Abstractions.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Text;
using Xunit;

namespace ActDim.Practix.Json.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddCoreJsonSerializer_ThrowsOnNullServices()
        {
            IServiceCollection services = null!;
            Assert.Throws<ArgumentNullException>(() => services.AddCoreJsonSerializer());
        }

        [Fact]
        public void AddCoreJsonSerializer_RegistersAllSerializationInterfacesAsSharedSingleton()
        {
            var services = new ServiceCollection();
            services.AddCoreJsonSerializer();

            using var provider = services.BuildServiceProvider();

            var jsonSerializer = provider.GetService<IJsonSerializer>();
            var stringSerializer = provider.GetService<IStringSerializer>();
            var binarySerializer = provider.GetService<IBinarySerializer>();
            var streamSerializer = provider.GetService<IStreamSerializer>();

            Assert.NotNull(jsonSerializer);
            Assert.NotNull(stringSerializer);
            Assert.NotNull(binarySerializer);
            Assert.NotNull(streamSerializer);

            Assert.Same(jsonSerializer, stringSerializer);
            Assert.Same(jsonSerializer, binarySerializer);
            Assert.Same(jsonSerializer, streamSerializer);
        }

        [Fact]
        public void AddCoreJsonSerializer_PreservesExistingRegistration()
        {
            var customSerializer = new CustomDummyBinarySerializer();
            var services = new ServiceCollection();
            services.AddSingleton<IBinarySerializer>(customSerializer);
            services.AddCoreJsonSerializer();

            using var provider = services.BuildServiceProvider();

            var resolved = provider.GetRequiredService<IBinarySerializer>();
            Assert.Same(customSerializer, resolved);
        }

        private sealed class CustomDummyBinarySerializer : IBinarySerializer
        {
            public byte[] Serialize(object value, Encoding encoding = default!) => Array.Empty<byte>();
            public byte[] Serialize(object value, Type type, Encoding encoding = default!) => Array.Empty<byte>();
            public byte[] Serialize<T>(T value, Encoding encoding = default!) => Array.Empty<byte>();
            public object Deserialize(byte[] data, Type type, Encoding encoding = default!) => default!;
            public T Deserialize<T>(byte[] data, Encoding encoding = default!) => default!;
        }
    }
}

