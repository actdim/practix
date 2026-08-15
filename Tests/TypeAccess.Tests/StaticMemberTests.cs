using ActDim.Practix.TypeAccess.Reflection;
using System;
using Xunit;

namespace ActDim.Practix.TypeAccess.Tests
{
    public class StaticMemberTests
    {
        public static class SampleStaticHolder
        {
            public static string StaticProperty { get; set; } = "InitialProp";
            public static int StaticField = 100;
        }

        [Fact]
        public void CanGetAndSetStaticProperty()
        {
            SampleStaticHolder.StaticProperty = "TestVal";
            var propInfo = typeof(SampleStaticHolder).GetProperty(nameof(SampleStaticHolder.StaticProperty));
            Assert.NotNull(propInfo);

            var getter = TypeAccessor.GetPropertyGetter(propInfo);
            var valueBefore = (string)getter.DynamicInvoke((object)null);
            Assert.Equal("TestVal", valueBefore);

            var setter = TypeAccessor.GetPropertySetter(propInfo);
            setter.DynamicInvoke(null, "NewVal");
            Assert.Equal("NewVal", SampleStaticHolder.StaticProperty);
        }

        [Fact]
        public void CanGetAndSetStaticField()
        {
            SampleStaticHolder.StaticField = 42;
            var fieldInfo = typeof(SampleStaticHolder).GetField(nameof(SampleStaticHolder.StaticField));
            Assert.NotNull(fieldInfo);

            var getter = TypeAccessor.GetFieldGetter(fieldInfo);
            var valBefore = (int)getter.DynamicInvoke((object)null);
            Assert.Equal(42, valBefore);

            var setter = TypeAccessor.GetFieldSetter(fieldInfo);
            setter.DynamicInvoke(null, 99);
            Assert.Equal(99, SampleStaticHolder.StaticField);
        }
    }
}
