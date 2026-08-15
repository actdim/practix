using ActDim.Practix.TypeAccess.Linq;
using ActDim.Practix.TypeAccess.Reflection;
using System;
using Xunit;

namespace ActDim.Practix.TypeAccess.Tests
{
    public class StructAccessTests
    {
        public struct PointStruct
        {
            public int X { get; set; }
            public int Y;

            public PointStruct(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int GetSum()
            {
                return X + Y;
            }
        }

        [Fact]
        public void CanReadStructPropertyAndField()
        {
            var p = new PointStruct(10, 20);
            var xProp = p.GetProperty<int>(nameof(PointStruct.X));
            var yField = p.GetField<int>(nameof(PointStruct.Y));

            Assert.Equal(10, xProp);
            Assert.Equal(20, yField);
        }

        [Fact]
        public void CanGetTypedGetterForStruct()
        {
            var p = new PointStruct(15, 25);
            var xGetter = TypeAccessor<PointStruct>.GetPropertyGetter<int>(nameof(PointStruct.X));
            var yGetter = TypeAccessor<PointStruct>.GetFieldGetter<int>(nameof(PointStruct.Y));

            Assert.Equal(15, xGetter(p));
            Assert.Equal(25, yGetter(p));
        }

        [Fact]
        public void CanInvokeMethodOnStruct()
        {
            var p = new PointStruct(30, 40);
            var methodCaller = TypeAccessor.GetMethodCaller<Func<PointStruct, int>>(typeof(PointStruct).GetMethod(nameof(PointStruct.GetSum)));
            var sum = methodCaller(p);

            Assert.Equal(70, sum);
        }
    }
}
