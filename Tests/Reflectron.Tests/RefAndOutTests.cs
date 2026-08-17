using System;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
{
    public class RefAndOutTests
    {
        public class SampleRefOutClass
        {
            public int Value { get; set; }

            public SampleRefOutClass(ref int initialVal, out string status)
            {
                initialVal += 10;
                status = "Initialized";
                Value = initialVal;
            }

            public void ModifyValue(ref int x, out string message)
            {
                x *= 2;
                message = "Done:" + x;
                Value = x;
            }

            public static void StaticModify(ref int a, out int b)
            {
                b = a * 3;
                a += 5;
            }
        }

        [Fact]
        public void GetMethodCaller_InstanceMethodWithRefAndOut_ModifiesArgumentsInArray()
        {
            int initVal = 5;
            string initStatus;
            var target = new SampleRefOutClass(ref initVal, out initStatus);
            var methodInfo = typeof(SampleRefOutClass).GetMethod(nameof(SampleRefOutClass.ModifyValue));
            var caller = TypeAccess.GetMethodCaller(methodInfo);

            object[] args = new object[] { 7, null };
            var result = caller(target, args);

            Assert.Null(result);
            Assert.Equal(14, (int)args[0]);
            Assert.Equal("Done:14", (string)args[1]);
            Assert.Equal(14, target.Value);
        }

        [Fact]
        public void GetMethodCaller_StaticMethodWithRefAndOut_ModifiesArgumentsInArray()
        {
            var methodInfo = typeof(SampleRefOutClass).GetMethod(nameof(SampleRefOutClass.StaticModify));
            var caller = TypeAccess.GetMethodCaller(methodInfo);

            object[] args = new object[] { 4, null };
            caller(null, args);

            Assert.Equal(9, (int)args[0]);
            Assert.Equal(12, (int)args[1]);
        }

        [Fact]
        public void GetConstructorEx_ConstructorWithRefAndOut_InstantiatesAndModifiesArguments()
        {
            var ctorInfo = typeof(SampleRefOutClass).GetConstructor(new[] { typeof(int).MakeByRefType(), typeof(string).MakeByRefType() });
            Assert.NotNull(ctorInfo);

            var ctorInvoker = TypeAccess.GetConstructorEx(ctorInfo);
            object[] args = new object[] { 15, null };
            var instance = (SampleRefOutClass)ctorInvoker(args);

            Assert.NotNull(instance);
            Assert.Equal(25, (int)args[0]);
            Assert.Equal("Initialized", (string)args[1]);
            Assert.Equal(25, instance.Value);
        }
    }
}
