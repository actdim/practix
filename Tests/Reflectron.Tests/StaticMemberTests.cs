using System;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
{
    public class StaticMemberTests
    {
        public static class SampleStaticHolder
        {
            public static string StaticProperty { get; set; } = "InitialProp";
            public static int StaticField = 100;
        }

        [Fact]
        public void GetPropertyGetterAndSetter_StaticProperty_GetsAndSetsValue()
        {
            SampleStaticHolder.StaticProperty = "TestVal";
            var propInfo = typeof(SampleStaticHolder).GetProperty(nameof(SampleStaticHolder.StaticProperty));
            Assert.NotNull(propInfo);

            var getter = Reflectron.GetPropertyGetter(propInfo);
            var valueBefore = (string)getter.DynamicInvoke((object)null);
            Assert.Equal("TestVal", valueBefore);

            var setter = Reflectron.GetPropertySetter(propInfo);
            setter.DynamicInvoke(null, "NewVal");
            Assert.Equal("NewVal", SampleStaticHolder.StaticProperty);
        }

        [Fact]
        public void GetFieldGetterAndSetter_StaticField_GetsAndSetsValue()
        {
            SampleStaticHolder.StaticField = 42;
            var fieldInfo = typeof(SampleStaticHolder).GetField(nameof(SampleStaticHolder.StaticField));
            Assert.NotNull(fieldInfo);

            var getter = Reflectron.GetFieldGetter(fieldInfo);
            var valBefore = (int)getter.DynamicInvoke((object)null);
            Assert.Equal(42, valBefore);

            var setter = Reflectron.GetFieldSetter(fieldInfo);
            setter.DynamicInvoke(null, 99);
            Assert.Equal(99, SampleStaticHolder.StaticField);
        }
    }
}
