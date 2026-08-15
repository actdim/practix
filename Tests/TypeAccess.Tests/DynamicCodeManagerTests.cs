using ActDim.Practix.TypeAccess.Linq.Dynamic;
using ActDim.Practix.TypeAccess.Reflection;
using System;
using System.Collections.Generic;

using Xunit;

namespace ActDim.Practix.TypeAccess.Tests
{
    public class DynamicCodeManagerTests
    {
        [Fact]
        public void GetDynamicName_ReturnsUniqueFormattedName()
        {
            var name1 = DynamicCodeManager.GetDynamicName("TestTag");
            var name2 = DynamicCodeManager.GetDynamicName("TestTag");

            Assert.StartsWith("TestTag.", name1);
            Assert.StartsWith("TestTag.", name2);
            Assert.NotEqual(name1, name2);
        }

        [Fact]
        public void GetAssemblyBuilder_CreatesAndCachesAssembly()
        {
            var asmName = "DynamicTestAsm_" + Guid.NewGuid().ToString("N");
            var asm1 = DynamicCodeManager.GetAssemblyBuilder(asmName);
            var asm2 = DynamicCodeManager.GetAssemblyBuilder(asmName);

            Assert.NotNull(asm1);
            Assert.Same(asm1, asm2);
        }

        [Fact]
        public void GetModuleBuilder_CreatesAndCachesModule()
        {
            var asmName = "DynamicTestAsm_" + Guid.NewGuid().ToString("N");
            var modName = "DynamicTestMod_" + Guid.NewGuid().ToString("N");

            var mod1 = DynamicCodeManager.GetModuleBuilder(asmName, modName);
            var mod2 = DynamicCodeManager.GetModuleBuilder(asmName, modName);

            Assert.NotNull(mod1);
            Assert.Same(mod1, mod2);
        }

        [Fact]
        public void DynamicTypeFactory_CreatesTypeAndSupportsEqualsAndGetHashCode()
        {
            var props = new Dictionary<string, Type>
            {
                { "Id", typeof(int) },
                { "Name", typeof(string) }
            };

            var dynamicType = DynamicTypeFactory.Instance.CreateType(props);
            Assert.NotNull(dynamicType);

            dynamic obj1 = Activator.CreateInstance(dynamicType);
            obj1.Id = 10;
            obj1.Name = "Item1";

            dynamic obj2 = Activator.CreateInstance(dynamicType);
            obj2.Id = 10;
            obj2.Name = "Item1";

            dynamic obj3 = Activator.CreateInstance(dynamicType);
            obj3.Id = 20;
            obj3.Name = "Item2";

            Assert.Equal(obj1, obj2);
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
            Assert.NotEqual(obj1, obj3);
        }

        [Fact]
        public void DynamicTypeFactory_CreateObjectFromDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                { "Age", 30 },
                { "City", "New York" }
            };

            dynamic obj = DynamicTypeFactory.Instance.CreateObject(dict);

            Assert.NotNull(obj);
            Assert.Equal(30, obj.Age);
            Assert.Equal("New York", obj.City);
        }
    }
}
