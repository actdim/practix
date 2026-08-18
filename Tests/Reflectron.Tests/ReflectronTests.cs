using System;
using System.Diagnostics;
using Xunit;
using ActDim.Reflectron;

namespace ActDim.Reflectron.Tests
{
    public class ReflectronTests
    {
#pragma warning disable CS0659
        public class TestClass1
#pragma warning restore CS0659
        {
            public static TestClass1 Default = new TestClass1();
            public TestClass2 RefProperty { get; set; }
            public string StringProperty { get; set; }
            public int IntProperty { get; set; } = 42;

            public string TestField = "FieldInitial";
            public int IntField = 100;

            public TestClass1()
            {
                RefProperty = TestClass2.Default;
                StringProperty = nameof(StringProperty);
            }

            public TestClass1(TestClass2 refProperty, string stringProperty)
            {
                RefProperty = refProperty;
                StringProperty = stringProperty;
            }

            public string TestMethod(string arg1)
            {
                return StringProperty + RefProperty.TextProperty + arg1;
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
                if (ReferenceEquals(null, other))
                {
                    return false;
                }
                if (ReferenceEquals(this, other))
                {
                    return true;
                }
                return Equals(RefProperty, other.RefProperty) && StringProperty == other.StringProperty;
            }
        }

        public class TestClass2
        {
            public static TestClass2 Default = new TestClass2();
            public string TextProperty { get; set; }

            public TestClass2()
            {
                TextProperty = nameof(TextProperty);
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
            var p = obj1.Reflectron().Get<TestClass2>(nameof(TestClass1.RefProperty));
            Assert.Equal(obj1.RefProperty, p);
        }

        [Fact]
        public void GetProperty_StringTypeProperty_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var p = obj1.Reflectron().Get<string>(nameof(TestClass1.StringProperty));
            Assert.Equal(obj1.StringProperty, p);
        }

        [Fact]
        public void GetPropertyGetter_TypedDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = Reflectron<TestClass1>.GetPropertyGetter<TestClass2>(nameof(TestClass1.RefProperty));
            var p = getter(obj1);
            Assert.Equal(obj1.RefProperty, p);
        }

        [Fact]
        public void GetPropertyGetter_Performance_OutperformsFastMember()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = Reflectron<TestClass1>.GetPropertyGetter<TestClass2>(nameof(TestClass1.RefProperty));
            var accessor = FastMember.TypeAccessor.Create(typeof(TestClass1));
            var reflectron = obj1.Reflect();

            // Warm up
            _ = getter(obj1);
            _ = accessor[obj1, nameof(TestClass1.RefProperty)];
            _ = reflectron[nameof(TestClass1.RefProperty)];

            var sw1 = Stopwatch.StartNew();
            for (var i = 0; i < 100_000; i++)
            {
                var p = getter(obj1);
            }
            sw1.Stop();

            var sw2 = Stopwatch.StartNew();
            for (var i = 0; i < 100_000; i++)
            {
                var p = accessor[obj1, nameof(TestClass1.RefProperty)];
            }
            sw2.Stop();

            var sw3 = Stopwatch.StartNew();
            for (var i = 0; i < 100_000; i++)
            {
                var p = reflectron[nameof(TestClass1.RefProperty)];
            }
            sw3.Stop();

            // Direct delegate is fastest
            Assert.True(sw2.Elapsed > sw1.Elapsed);

            // Verify correctness across all approaches
            Assert.Equal(obj1.RefProperty, getter(obj1));
            Assert.Equal(obj1.RefProperty, accessor[obj1, nameof(TestClass1.RefProperty)]);
            Assert.Equal(obj1.RefProperty, reflectron[nameof(TestClass1.RefProperty)]);
            Assert.Equal(obj1.RefProperty, reflectron.Get<TestClass2>(nameof(TestClass1.RefProperty)));
        }

        [Fact]
        public void GetPropertyGetter_TypedValueDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var getter = Reflectron<TestClass1>.GetPropertyGetter<string>(nameof(TestClass1.StringProperty));
            var p = getter(obj1);
            Assert.Equal(obj1.StringProperty, p);
        }

        [Fact]
        public void GetPropertySetter_SetsPropertyValueCorrectly()
        {
            var obj1 = new TestClass1();
            var propInfo = typeof(TestClass1).GetProperty(nameof(TestClass1.StringProperty));
            var setter = Reflectron.GetPropertySetter(propInfo);
            setter.DynamicInvoke(obj1, "NewVal");
            Assert.Equal("NewVal", obj1.StringProperty);
        }

        // ------------------------------------------------------------------
        // Field Access Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetField_ValidFieldName_ReturnsFieldValue()
        {
            var obj = new TestClass1();
            var val = obj.Reflectron().Get<string>(nameof(TestClass1.TestField));
            Assert.Equal("FieldInitial", val);
        }

        [Fact]
        public void GetFieldSetter_SetsFieldValueCorrectly()
        {
            var obj = new TestClass1();
            var fieldInfo = typeof(TestClass1).GetField(nameof(TestClass1.TestField));
            var setter = Reflectron.GetFieldSetter(fieldInfo);
            setter.DynamicInvoke(obj, "FieldUpdated");
            Assert.Equal("FieldUpdated", obj.TestField);
        }

        // ------------------------------------------------------------------
        // Reflectron Wrapper Tests
        // ------------------------------------------------------------------

        [Fact]
        public void Reflectron_Get_ReferenceType_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.Reflectron();
            var p = accessor.Get<TestClass2>(nameof(TestClass1.RefProperty));
            Assert.Equal(obj1.RefProperty, p);
        }

        [Fact]
        public void Reflectron_Get_StringType_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var accessor = obj1.Reflectron();
            var p = accessor.Get<string>(nameof(TestClass1.StringProperty));
            Assert.Equal(obj1.StringProperty, p);
        }

        [Fact]
        public void Reflectron_AnonymousTypeReferenceProperty_ReturnsValue()
        {
            var obj1 = new { prop = new TestClass2() };
            var p = obj1.Reflectron().Get<TestClass2>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void Reflectron_AnonymousTypeValueProperty_ReturnsValue()
        {
            var obj1 = new { prop = "test" };
            var p = obj1.Reflectron().Get<string>("prop");
            Assert.Equal(obj1.prop, p);
        }

        [Fact]
        public void Reflectron_Indexer_PropertyAndField_GetsAndSetsValue()
        {
            var obj = new TestClass1();
            var reflector = obj.Reflectron();

            reflector["StringProperty"] = "IndexerVal";
            Assert.Equal("IndexerVal", obj.StringProperty);
            Assert.Equal("IndexerVal", reflector["StringProperty"]);

            reflector["TestField"] = "IndexerFieldVal";
            Assert.Equal("IndexerFieldVal", obj.TestField);
            Assert.Equal("IndexerFieldVal", reflector["TestField"]);
        }

        [Fact]
        public void Reflectron_WeakReference_AllowsGarbageCollection_And_ThrowsWhenCollected()
        {
            WeakReference<TestClass1> weakRef;
            IReflectron<TestClass1> reflector;

            void CreateScope()
            {
                var instance = new TestClass1 { StringProperty = "Initial" };
                weakRef = new WeakReference<TestClass1>(instance);
                reflector = instance.Reflect();
                Assert.Equal("Initial", reflector.Get(x => x.StringProperty));
            }

            CreateScope();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.TryGetTarget(out _), "Target object should have been collected by GC.");
            Assert.Throws<ReflectionException>(() => reflector.Get(x => x.StringProperty));
            Assert.Throws<ReflectionException>(() => reflector.Get<string>("StringProperty"));
            Assert.Throws<ReflectionException>(() => reflector.Set(x => x.StringProperty, "New"));
            Assert.Throws<ReflectionException>(() => reflector.Set("StringProperty", "New"));
            Assert.Throws<ReflectionException>(() => _ = reflector["StringProperty"]);
            Assert.Throws<ReflectionException>(() => reflector["StringProperty"] = "New");
        }

        [Fact]
        public void Reflectron_Untyped_WeakReference_AllowsGarbageCollection_And_ThrowsWhenCollected()
        {
            WeakReference<object> weakRef;
            IReflectron<object> reflector;

            void CreateScope()
            {
                object instance = new TestClass1 { StringProperty = "InitialUntyped" };
                weakRef = new WeakReference<object>(instance);
                reflector = typeof(TestClass1).Reflect()(instance);
                Assert.Equal("InitialUntyped", reflector["StringProperty"]);
            }

            CreateScope();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(weakRef.TryGetTarget(out _), "Target object should have been collected by GC.");
            Assert.Throws<ReflectionException>(() => _ = reflector["StringProperty"]);
            Assert.Throws<ReflectionException>(() => reflector["StringProperty"] = "New");
            Assert.Throws<ReflectionException>(() => reflector.Get<string>("StringProperty"));
            Assert.Throws<ReflectionException>(() => reflector.Set("StringProperty", "New"));
        }

        // ------------------------------------------------------------------
        // Method Caller Tests
        // ------------------------------------------------------------------

        [Fact]
        public void GetMethod_InstanceMethod_InvokesCorrectly()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var val = "blabla";
            var method = obj1.Reflectron().GetMethod<Func<TestClass1, string, string>>("TestMethod");
            Assert.Equal(method(obj1, val), obj1.TestMethod(val));
        }

        [Fact]
        public void GetMethod_Expression_InvokesCorrectly()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var val = "blabla";
            var method = obj1.Reflect().GetMethod<Func<TestClass1, string, string>, string>(x => x.TestMethod(default));
            Assert.Equal(method(obj1, val), obj1.TestMethod(val));
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
            var d = Reflectron<TestClass1>.GetPropertyGetter<TestClass2>(nameof(TestClass1.RefProperty));
            var p = d(obj1);
            Assert.Equal(p, obj1.RefProperty);
        }

        [Fact]
        public void GetPropertyGetter_StringFuncDelegate_ReturnsValue()
        {
            var obj1 = new TestClass1(new TestClass2(), "test");
            var d = Reflectron<TestClass1>.GetPropertyGetter<string>(nameof(TestClass1.StringProperty));
            var p = d(obj1);
            Assert.Equal(p, obj1.StringProperty);
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
            var ctor = Reflectron.CreateConstructor<Func<TestClass1>>();
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
        public void Reflectron_NullObject_ThrowsArgumentNullException()
        {
            TestClass1 obj = null;
            Assert.Throws<ArgumentNullException>(() => obj.Reflectron());
        }

        [Fact]
        public void Reflectron_Get_NonExistentMember_ThrowsArgumentException()
        {
            var obj = new TestClass1();
            Assert.Throws<ArgumentException>(() => obj.Reflectron().Get<string>("NonExistentProperty"));
        }

        [Fact]
        public void GetMemberInfo_NullExpression_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => Reflectron.GetMemberInfo(null));
        }

        [Fact]
        public void GetMemberInfo_LambdaExpression_ReturnsMemberInfo()
        {
            var memberInfo = Reflectron.GetMemberInfo((TestClass1 c) => c.StringProperty);
            Assert.NotNull(memberInfo);
            Assert.Equal(nameof(TestClass1.StringProperty), memberInfo.Name);
        }

        [Fact]
        public void GetConstructorEx_MissingConstructor_ThrowsArgumentExceptionWithDetails()
        {
            var ex = Assert.Throws<ArgumentException>(() => Reflectron.CreateConstructorEx(typeof(Func<PrivateCtorClass, PrivateCtorClass>)));
            Assert.Contains("Cannot find constructor", ex.Message);
        }

        [Fact]
        public void Reflectron_Set_And_Get_ByName_WorksCorrectly()
        {
            var obj = new TestClass1();
            var returnedProp = obj.Reflectron().Set("StringProperty", "UpdatedViaName");

            Assert.Equal("UpdatedViaName", returnedProp);
            Assert.Equal("UpdatedViaName", obj.StringProperty);
            Assert.Equal("UpdatedViaName", obj.Reflectron().Get<string>("StringProperty"));

            var returnedField = obj.Reflect().Set("TestField", "UpdatedFieldViaName");
            Assert.Equal("UpdatedFieldViaName", returnedField);
            Assert.Equal("UpdatedFieldViaName", obj.TestField);
            Assert.Equal("UpdatedFieldViaName", obj.Reflect().Get<string>("TestField"));
        }

        [Fact]
        public void Reflectron_Get_And_Set_ByExpression_Property_WorksCorrectly()
        {
            var obj = new TestClass1();
            var returned = obj.Reflectron().Set(x => x.StringProperty, "UpdatedViaSetExpr");

            Assert.Equal("UpdatedViaSetExpr", returned);
            Assert.Equal("UpdatedViaSetExpr", obj.StringProperty);
            Assert.Equal("UpdatedViaSetExpr", obj.Reflectron().Get(x => x.StringProperty));
        }

        [Fact]
        public void Reflectron_Get_And_Set_ByExpression_Field_WorksCorrectly()
        {
            var obj = new TestClass1();
            var returned = obj.Reflect().Set(x => x.TestField, "UpdatedFieldViaSetExpr");

            Assert.Equal("UpdatedFieldViaSetExpr", returned);
            Assert.Equal("UpdatedFieldViaSetExpr", obj.TestField);
            Assert.Equal("UpdatedFieldViaSetExpr", obj.Reflect().Get(x => x.TestField));
        }

        [Fact]
        public void Reflectron_StaticFactory_For_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflector = Reflectron.For(obj);

            reflector.Set(x => x.StringProperty, "ViaStaticFactory");
            Assert.Equal("ViaStaticFactory", reflector.Get(x => x.StringProperty));
            Assert.Equal("ViaStaticFactory", obj.StringProperty);
        }

        [Fact]
        public void TypeExtensions_Reflect_TypedFactory_WorksCorrectly()
        {
            var factory = typeof(TestClass1).Reflect<TestClass1>();
            var obj = new TestClass1();
            var reflector = factory(obj);

            reflector.Set(x => x.StringProperty, "ViaTypedTypeFactory");
            Assert.Equal("ViaTypedTypeFactory", reflector.Get(x => x.StringProperty));
            Assert.Equal("ViaTypedTypeFactory", obj.StringProperty);
        }

        [Fact]
        public void TypeExtensions_Reflect_UntypedFactory_WorksCorrectly()
        {
            Type runtimeType = typeof(TestClass1);
            var factory = runtimeType.Reflect();
            var obj = new TestClass1();
            var reflector = factory(obj);

            reflector["StringProperty"] = "ViaUntypedTypeFactory";
            Assert.Equal("ViaUntypedTypeFactory", reflector["StringProperty"]);
            Assert.Equal("ViaUntypedTypeFactory", obj.StringProperty);

            reflector.Set("TestField", "ViaUntypedField");
            Assert.Equal("ViaUntypedField", reflector.Get<string>("TestField"));
            Assert.Equal("ViaUntypedField", obj.TestField);
        }

        // ------------------------------------------------------------------
        // Comprehensive Indexer and Get/Set Tests
        // ------------------------------------------------------------------

        [Fact]
        public void Reflectron_Indexer_Get_And_Set_Property_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            // Read initial
            Assert.Equal(nameof(TestClass1.StringProperty), reflectron["StringProperty"]);

            // Write new value
            reflectron["StringProperty"] = "IndexerUpdatedProp";

            // Verify via indexer and instance
            Assert.Equal("IndexerUpdatedProp", reflectron["StringProperty"]);
            Assert.Equal("IndexerUpdatedProp", obj.StringProperty);
        }

        [Fact]
        public void Reflectron_Indexer_Get_And_Set_Field_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            // Read initial
            Assert.Equal("FieldInitial", reflectron["TestField"]);

            // Write new value
            reflectron["TestField"] = "IndexerUpdatedField";

            // Verify via indexer and instance
            Assert.Equal("IndexerUpdatedField", reflectron["TestField"]);
            Assert.Equal("IndexerUpdatedField", obj.TestField);
        }

        [Fact]
        public void Reflectron_Indexer_Get_And_Set_ValueTypes_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            // Value type property
            Assert.Equal(42, reflectron["IntProperty"]);
            reflectron["IntProperty"] = 999;
            Assert.Equal(999, reflectron["IntProperty"]);
            Assert.Equal(999, obj.IntProperty);

            // Value type field
            Assert.Equal(100, reflectron["IntField"]);
            reflectron["IntField"] = 555;
            Assert.Equal(555, reflectron["IntField"]);
            Assert.Equal(555, obj.IntField);
        }

        [Fact]
        public void Reflectron_Indexer_NullOrEmptyName_ThrowsArgumentException()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            Assert.Throws<ArgumentNullException>(() => reflectron[null]);
            Assert.Throws<ArgumentException>(() => reflectron[""]);
            Assert.Throws<ArgumentNullException>(() => reflectron[null] = "val");
            Assert.Throws<ArgumentException>(() => reflectron[""] = "val");
        }

        [Fact]
        public void Reflectron_Indexer_NonExistentMember_ThrowsArgumentException()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            Assert.Throws<ArgumentException>(() => reflectron["NonExistentMember"]);
            Assert.Throws<ArgumentException>(() => reflectron["NonExistentMember"] = "val");
        }

        [Fact]
        public void Reflectron_Get_And_Set_ByName_ValueTypes_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflectron();

            // Property
            int returnedProp = reflectron.Set("IntProperty", 1234);
            Assert.Equal(1234, returnedProp);
            Assert.Equal(1234, obj.IntProperty);
            Assert.Equal(1234, reflectron.Get<int>("IntProperty"));

            // Field
            int returnedField = reflectron.Set("IntField", 5678);
            Assert.Equal(5678, returnedField);
            Assert.Equal(5678, obj.IntField);
            Assert.Equal(5678, reflectron.Get<int>("IntField"));
        }

        [Fact]
        public void Reflectron_Get_And_Set_ByExpression_ValueTypes_WorksCorrectly()
        {
            var obj = new TestClass1();
            var reflectron = obj.Reflect();

            // Property
            int returnedProp = reflectron.Set(x => x.IntProperty, 777);
            Assert.Equal(777, returnedProp);
            Assert.Equal(777, obj.IntProperty);
            Assert.Equal(777, reflectron.Get(x => x.IntProperty));

            // Field
            int returnedField = reflectron.Set(x => x.IntField, 888);
            Assert.Equal(888, returnedField);
            Assert.Equal(888, obj.IntField);
            Assert.Equal(888, reflectron.Get(x => x.IntField));
        }
    }
}
