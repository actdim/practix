using ActDim.Practix.TypeAccess.Linq;
using ActDim.Practix.TypeAccess.Linq.Dynamic;
using ActDim.Practix.TypeAccess.Reflection;
using System;
using Xunit;

namespace ActDim.Practix.TypeAccess.Tests
{
    public class NullAndBoundaryTests
    {
        public class NullableClass
        {
            public string Name { get; set; }
            public NullableClass(string name)
            {
                Name = name;
            }
        }

        [Fact]
        public void CreateInstance_HandlesNullArgument()
        {
            var instance = (NullableClass)typeof(NullableClass).CreateInstance(new object[] { null });
            Assert.NotNull(instance);
            Assert.Null(instance.Name);
        }

        [Fact]
        public void ObjectExtensions_ThrowsOnNullTarget()
        {
            object target = null;
            Assert.Throws<ArgumentNullException>(() => target.GetProperty<string>("Name"));
            Assert.Throws<ArgumentNullException>(() => target.GetField<int>("Field"));
        }

        [Fact]
        public void ObjectExtensions_ThrowsOnMissingMember()
        {
            var target = new NullableClass("Test");
            Assert.Throws<ArgumentNullException>(() => target.GetProperty<string>("NonExistentProperty"));
            Assert.Throws<ArgumentNullException>(() => target.GetField<int>("NonExistentField"));
        }

        [Fact]
        public void DynamicHelper_EvalGet_HandlesNullPropertyValues()
        {
            var target = new NullableClass(null);
            var result = DynamicHelper.EvalGet(target, "Name");
            Assert.Null(result);
        }
    }
}
