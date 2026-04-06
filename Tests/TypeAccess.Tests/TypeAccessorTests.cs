using ActDim.Practix.TypeAccess.Linq;
using ActDim.Practix.TypeAccess.Reflection;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace OrthoBits.InterCode.Tests
{
    public class TypeAccessorTests
    {
#pragma warning disable CS0659
        public class TestClass1
#pragma warning restore CS0659
        {
            public static TestClass1 Default = new TestClass1();
            public static readonly string NameOf_TestRefProp1_1 = nameof(TestRefProp1_1);
            public TestClass2 TestRefProp1_1 { get; set; }
            public static readonly string NameOf_TestValProp1_1 = nameof(TestValProp1_1);
            public string TestValProp1_1 { get; set; }

            public TestClass1()
            {
                TestRefProp1_1 = TestClass2.Default;
                TestValProp1_1 = nameof(TestValProp1_1);
            }

            public TestClass1(TestClass2 testRefProp1_1, string testValProp1_1)
            {
                TestRefProp1_1 = testRefProp1_1;
                TestValProp1_1 = testValProp1_1;
            }

            public string TestMethod(string arg1)
            {
                return TestValProp1_1 + TestRefProp1_1.TestValProp2_1 + arg1;
            }

            public static string TestStaticMethod(string arg1, string arg2)
            {
                return arg1 + arg2;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as TestClass1);
            }

            public bool Equals(TestClass1 other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (!Equals(TestRefProp1_1, other.TestRefProp1_1) || TestValProp1_1 != other.TestValProp1_1) return false;
                return true;
            }
        }

        public class TestClass2
        {
            public static TestClass2 Default = new TestClass2();
            public string TestValProp2_1 { get; set; }

            public TestClass2()
            {
                TestValProp2_1 = nameof(TestValProp2_1);
            }
        }

        [Fact]
        public void CanGetRefPropertyByName()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var p = obj1.GetProperty<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void CanGetValPropertyByName()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var p = obj1.GetProperty<string>(TestClass1.NameOf_TestValProp1_1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void NOBOXING_CanGetRefPropertyByNameUsingTypeHelper()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccessor<TestClass1>.GetPropertyGetter<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            var p = getter(obj1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void GettingRefPropertyByNameUsingTypeHelperIsFastest()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccessor<TestClass1>.GetPropertyGetter<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            var accessor = FastMember.TypeAccessor.Create(typeof(TestClass1));
            var sw1 = new Stopwatch();
            sw1.Start();
            for (var i = 0; i < 1000000; i++)
            {
                var p = getter(obj1);
            }
            sw1.Stop();

            var sw2 = new Stopwatch();
            sw2.Start();
            for (var i = 0; i < 1000000; i++)
            {
                var p = accessor[obj1, TestClass1.NameOf_TestRefProp1_1];
            }
            sw2.Stop();
            Assert.True(sw2.ElapsedTicks > sw1.ElapsedTicks);
        }

        [Fact]
        public void NOBOXING_CanGetValPropertyByNameUsingTypeHelper()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccessor<TestClass1>.GetPropertyGetter<string>(TestClass1.NameOf_TestValProp1_1);
            var p = getter(obj1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void NOBOXING_CanGetRefPropertyByNameUsingAccessor()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.GetAccessor();
            var p = accessor.GetProperty<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void NOBOXING_CanGetValPropertyByNameUsingAccessor()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.GetAccessor();
            var p = accessor.GetProperty<string>(TestClass1.NameOf_TestValProp1_1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void NOBOXING_CanGetRefPropertyByNameFromDynamic()
        {
            var obj1 = new { prop = new TestClass2() };
            var p = obj1.GetAccessor().GetProperty<TestClass2>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void NOBOXING_CanGetValPropertyByNameFromDynamic()
        {
            var obj1 = new { prop = "test" };
            var p = obj1.GetAccessor().GetProperty<string>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void NOBOXING_CanCallInstanceMethodByNameUsingConcreteDelegateType()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var val = "blabla";
            var methodCaller = obj1.GetMethodCaller<Func<TestClass1, string, string>>("TestMethod");
            Assert.Equal(methodCaller(obj1, val), obj1.TestMethod(val));
        }

        [Fact]
        public void NOBOXING_CanCallStaticMethodByNameUsingConcreteDelegateType()
        {
            var val1 = "test";
            var val2 = "blabla";
            var methodCaller = typeof(TestClass1).GetStaticMethodCaller<Func<string, string, string>>("TestStaticMethod");
            Assert.Equal(methodCaller(val1, val2), TestClass1.TestStaticMethod(val1, val2));
        }

        [Fact]
        public void NOBOXING_CanGetRefPropertyGetterByName()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var d = obj1.GetPropertyGetter<Func<TestClass1, TestClass2>>(TestClass1.NameOf_TestRefProp1_1);
            var p = d(obj1);
            Assert.Equal(p, obj1.TestRefProp1_1);
        }

        [Fact]
        public void NOBOXING_CanGetValPropertyGetterByName()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var d = obj1.GetPropertyGetter<Func<TestClass1, string>>(TestClass1.NameOf_TestValProp1_1);
            var p = d(obj1);
            Assert.Equal(p, obj1.TestValProp1_1);
        }

        [Fact]
        public void CanCreateInstance()
        {
            var obj1 = typeof(TestClass1).CreateInstance();
            Assert.NotNull(obj1);
            Assert.Equal(obj1, TestClass1.Default);
        }

        public record TestRecord(string Prop);

        [Fact]
        public void CanCreateInstanceWithArgs()
        {
            var obj1 = (TestRecord)typeof(TestRecord).CreateInstance("prop1");
            Assert.NotNull(obj1);
            Assert.Equal(obj1.Prop, "prop1");
        }

        [Fact]
        public void NOBOXING_CanCreateInstance()
        {
            var ctor = TypeAccessor.CreateConstructor<Func<TestClass1>>();
            var obj1 = ctor();
            Assert.NotNull(obj1);
            Assert.Equal(obj1, TestClass1.Default);
        }

        [Fact]
        public void NOBOXING_CanCreateInstanceWithCtorArguments()
        {
            var refObj = new TestClass2();
            var val = "test";
            var obj1 = new TestClass1(refObj, val);
            var obj2 = typeof(TestClass1).CreateInstance(refObj, val);
            Assert.NotNull(obj2);
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void CanCreateCtor()
        {
            var obj1 = new TestClass1();
            var type1 = typeof(TestClass1);
            var ctor = type1.GetConstructor<Func<TestClass1>>();
            var obj2 = ctor();
            Assert.NotNull(obj2);
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void NOBOXING_CanCreateCtorWithArgsUsingConcreteDelegateType()
        {
            var refObj = new TestClass2();
            var val = "test";
            var obj1 = new TestClass1(refObj, val);
            var type1 = typeof(TestClass1);
            var ctor = type1.GetConstructor<Func<TestClass2, string, TestClass1>>();
            var obj2 = ctor(refObj, val);
            Assert.NotNull(obj2);
            Assert.True(obj2.Equals(obj1));
        }
    }
}
