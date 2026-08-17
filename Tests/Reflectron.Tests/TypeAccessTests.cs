using System;
using System.Diagnostics;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
{
    public class TypeAccessTests
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

            public string TestField = "FieldInitial";

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

        public record TestRecord(string Prop);

        public class PrivateCtorClass
        {
            private PrivateCtorClass(int x) { }
        }

        // ------------------------------------------------------------------
        // Property Access Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetProperty_ReferenceTypeProperty_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var p = obj1.GetProperty<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void GetProperty_StringTypeProperty_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var p = obj1.GetProperty<string>(TestClass1.NameOf_TestValProp1_1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void GetPropertyGetter_TypedDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccess<TestClass1>.GetPropertyGetter<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            var p = getter(obj1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void GetPropertyGetter_Performance_OutperformsFastMember()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccess<TestClass1>.GetPropertyGetter<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            var accessor = FastMember.TypeAccessor.Create(typeof(TestClass1));

            var sw1 = Stopwatch.StartNew();
            for (var i = 0; i < 100_000; i++)
            {
                var p = getter(obj1);
            }
            sw1.Stop();

            var sw2 = Stopwatch.StartNew();
            for (var i = 0; i < 100_000; i++)
            {
                var p = accessor[obj1, TestClass1.NameOf_TestRefProp1_1];
            }
            sw2.Stop();
            
            Assert.True(sw2.Elapsed > sw1.Elapsed);
        }

        [Fact]
        public void GetPropertyGetter_TypedValueDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = TypeAccess<TestClass1>.GetPropertyGetter<string>(TestClass1.NameOf_TestValProp1_1);
            var p = getter(obj1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void GetPropertySetter_SetsPropertyValueCorrectly()
        {
            var obj1 = new TestClass1();
            var propInfo = typeof(TestClass1).GetProperty(TestClass1.NameOf_TestValProp1_1);
            var setter = TypeAccess.GetPropertySetter(propInfo);
            setter.DynamicInvoke(obj1, "NewVal");
            Assert.Equal("NewVal", obj1.TestValProp1_1);
        }

        // ------------------------------------------------------------------
        // Field Access Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetField_ValidFieldName_ReturnsFieldValue()
        {
            var obj = new TestClass1();
            var val = obj.GetField<string>(nameof(TestClass1.TestField));
            Assert.Equal("FieldInitial", val);
        }

        [Fact]
        public void GetFieldSetter_SetsFieldValueCorrectly()
        {
            var obj = new TestClass1();
            var fieldInfo = typeof(TestClass1).GetField(nameof(TestClass1.TestField));
            var setter = TypeAccess.GetFieldSetter(fieldInfo);
            setter.DynamicInvoke(obj, "FieldUpdated");
            Assert.Equal("FieldUpdated", obj.TestField);
        }

        // ------------------------------------------------------------------
        // ObjectAccess Wrapper Tests
        // ------------------------------------------------------------------

        [Fact]
        public void ObjectAccess_GetProperty_ReferenceType_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.GetAccessor();
            var p = accessor.GetProperty<TestClass2>(TestClass1.NameOf_TestRefProp1_1);
            Assert.Equal(obj1.TestRefProp1_1, p);
        }

        [Fact]
        public void ObjectAccess_GetProperty_StringType_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.GetAccessor();
            var p = accessor.GetProperty<string>(TestClass1.NameOf_TestValProp1_1);
            Assert.Equal(obj1.TestValProp1_1, p);
        }

        [Fact]
        public void ObjectAccess_AnonymousTypeReferenceProperty_ReturnsValue()
        {
            var obj1 = new { prop = new TestClass2() };
            var p = obj1.GetAccessor().GetProperty<TestClass2>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void ObjectAccess_AnonymousTypeValueProperty_ReturnsValue()
        {
            var obj1 = new { prop = "test" };
            var p = obj1.GetAccessor().GetProperty<string>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void ObjectAccess_WeakReferenceCollected_ThrowsReflectionException()
        {
            WeakReference<TestClass1> weakRef;
            ObjectAccess<TestClass1> accessor;

            void CreateScope()
            {
                var instance = new TestClass1();
                weakRef = new WeakReference<TestClass1>(instance);
                accessor = new ObjectAccess<TestClass1>(instance);
            }

            CreateScope();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.Throws<ReflectionException>(() => _ = accessor.Instance);
        }

        // ------------------------------------------------------------------
        // Method Caller Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetMethodCaller_InstanceMethod_InvokesCorrectly()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var val = "blabla";
            var methodCaller = obj1.GetMethodCaller<Func<TestClass1, string, string>>("TestMethod");
            Assert.Equal(methodCaller(obj1, val), obj1.TestMethod(val));
        }

        [Fact]
        public void GetStaticMethodCaller_StaticMethod_InvokesCorrectly()
        {
            var val1 = "test";
            var val2 = "blabla";
            var methodCaller = typeof(TestClass1).GetStaticMethodCaller<Func<string, string, string>>("TestStaticMethod");
            Assert.Equal(methodCaller(val1, val2), TestClass1.TestStaticMethod(val1, val2));
        }

        [Fact]
        public void GetPropertyGetter_FuncDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var d = obj1.GetPropertyGetter<Func<TestClass1, TestClass2>>(TestClass1.NameOf_TestRefProp1_1);
            var p = d(obj1);
            Assert.Equal(p, obj1.TestRefProp1_1);
        }

        [Fact]
        public void GetPropertyGetter_StringFuncDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var d = obj1.GetPropertyGetter<Func<TestClass1, string>>(TestClass1.NameOf_TestValProp1_1);
            var p = d(obj1);
            Assert.Equal(p, obj1.TestValProp1_1);
        }

        // ------------------------------------------------------------------
        // Instantiation & Constructor Tests
        // ------------------------------------------------------------------

        [Fact]
        public void CreateInstance_DefaultConstructor_CreatesDefaultObject()
        {
            var obj1 = typeof(TestClass1).CreateInstance();
            Assert.NotNull(obj1);
            Assert.Equal(obj1, TestClass1.Default);
        }

        [Fact]
        public void CreateInstance_RecordWithArgs_CreatesRecordInstance()
        {
            var obj1 = (TestRecord)typeof(TestRecord).CreateInstance("prop1");
            Assert.NotNull(obj1);
            Assert.Equal("prop1", obj1.Prop);
        }

        [Fact]
        public void CreateConstructor_FuncDelegate_CreatesInstance()
        {
            var ctor = TypeAccess.CreateConstructor<Func<TestClass1>>();
            var obj1 = ctor();
            Assert.NotNull(obj1);
            Assert.Equal(obj1, TestClass1.Default);
        }

        [Fact]
        public void CreateInstance_WithParametrizedCtor_CreatesInstance()
        {
            var refObj = new TestClass2();
            var val = "test";
            var obj1 = new TestClass1(refObj, val);
            var obj2 = typeof(TestClass1).CreateInstance(refObj, val);
            Assert.NotNull(obj2);
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void GetConstructor_Parameterless_CreatesInstance()
        {
            var obj1 = new TestClass1();
            var type1 = typeof(TestClass1);
            var ctor = type1.GetConstructor<Func<TestClass1>>();
            var obj2 = ctor();
            Assert.NotNull(obj2);
            Assert.True(obj2.Equals(obj1));
        }

        [Fact]
        public void GetConstructor_Parametrized_CreatesInstance()
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

        // ------------------------------------------------------------------
        // Exception & Guard Clause Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetProperty_NullObject_ThrowsArgumentNullException()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => obj.GetProperty<string>("Prop"));
        }

        [Fact]
        public void GetProperty_NonExistentProperty_ThrowsArgumentNullException()
        {
            var obj = new TestClass1();
            Assert.Throws<ArgumentNullException>(() => obj.GetProperty<string>("NonExistentProperty"));
        }

        [Fact]
        public void GetField_NullObject_ThrowsArgumentNullException()
        {
            object obj = null;
            Assert.Throws<ArgumentNullException>(() => obj.GetField<string>("Field"));
        }

        [Fact]
        public void GetField_NonExistentField_ThrowsArgumentNullException()
        {
            var obj = new TestClass1();
            Assert.Throws<ArgumentNullException>(() => obj.GetField<string>("NonExistentField"));
        }

        [Fact]
        public void GetMemberInfo_NullExpression_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TypeAccess.GetMemberInfo(null));
        }

        [Fact]
        public void GetMemberInfo_LambdaExpression_ReturnsMemberInfo()
        {
            var memberInfo = TypeAccess.GetMemberInfo((TestClass1 c) => c.TestValProp1_1);
            Assert.NotNull(memberInfo);
            Assert.Equal(nameof(TestClass1.TestValProp1_1), memberInfo.Name);
        }

        [Fact]
        public void GetConstructorEx_MissingConstructor_ThrowsArgumentExceptionWithDetails()
        {
            var ex = Assert.Throws<ArgumentException>(() => TypeAccess.CreateConstructorEx(typeof(Func<PrivateCtorClass, PrivateCtorClass>)));
            Assert.Contains("Cannot find constructor", ex.Message);
        }
    }
}
