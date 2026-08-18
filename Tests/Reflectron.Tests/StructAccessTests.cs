using System;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
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
        public void ObjectExtensions_StructInstance_ReadsPropertyAndField()
        {
            var p = new PointStruct(10, 20);
            var xGetter = Reflectron.GetPropertyGetter<PointStruct, int>(typeof(PointStruct).GetProperty(nameof(PointStruct.X)));
            var yGetter = Reflectron.GetFieldGetter<PointStruct, int>(typeof(PointStruct).GetField(nameof(PointStruct.Y)));

            Assert.Equal(10, xGetter(p));
            Assert.Equal(20, yGetter(p));
        }

        [Fact]
        public void GetPropertyGetterAndFieldGetter_StructType_ReadsValuesCorrectly()
        {
            var p = new PointStruct(15, 25);
            var xGetter = Reflectron.GetPropertyGetter<PointStruct, int>(nameof(PointStruct.X));
            var yGetter = Reflectron.GetFieldGetter<PointStruct, int>(nameof(PointStruct.Y));

            Assert.Equal(15, xGetter(p));
            Assert.Equal(25, yGetter(p));
        }

        [Fact]
        public void GetMethodCaller_StructMethod_InvokesAndReturnsSum()
        {
            var p = new PointStruct(30, 40);
            var methodCaller = Reflectron.GetMethodCaller<Func<PointStruct, int>>(typeof(PointStruct).GetMethod(nameof(PointStruct.GetSum)));
            var sum = methodCaller(p);

            Assert.Equal(70, sum);
        }
    }
}
