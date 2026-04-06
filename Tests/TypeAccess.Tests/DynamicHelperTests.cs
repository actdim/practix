using System;
using Xunit;
using ActDim.Practix.TypeAccess.Linq.Dynamic;

namespace OrthoBits.InterCode.Tests
{
	public class DynamicHelperTests
	{
		[Fact]
		public void TestEvalGetGeneric()
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			otherObj.RefProperty.StringProperty = otherObj.StringPropertyAuto;
			Assert.Equal(obj.RefProperty.StringProperty, DynamicHelper.EvalGet<TestClass, string>(obj, "RefProperty.StringProperty"));
			Assert.Equal(otherObj.StringPropertyAuto, DynamicHelper.EvalGet<TestClass, string>(otherObj, "RefProperty.StringProperty"));
		}

		[Fact]
		public void TestEvalGetWithAnonymousTypes()
		{
			var obj = new { RefProperty = new TestClass(), StringProperty = "TestString" };
			var result = DynamicHelper.EvalGet(obj, "RefProperty.StringProperty.Length.ToString()+StringProperty");
			Assert.Equal(obj.RefProperty.StringProperty.Length.ToString() + obj.StringProperty, (string)result);
		}

		[Fact]
		public void CanCreateTypedPropertyOrFieldGetter()
		{
			var obj = new TestClass();
			var getter = DynamicHelper.CreateEvalGetter<TestClass, BaseTestClass>("RefProperty");
			Assert.NotNull(getter);
			Assert.Equal(obj.RefProperty, getter(obj));
		}

		[Fact]
		public void CanCreatePropertyOrFieldGetter()
		{
			var obj = new TestClass();
			var getter = DynamicHelper.CreateEvalGetter(typeof(TestClass), "RefProperty", typeof(BaseTestClass));
			Assert.NotNull(getter);
			Assert.Equal(obj.RefProperty, (BaseTestClass)getter.DynamicInvoke(obj));
		}

		[Fact]
		public void CanHandleThisKeyword()
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			var doubleValue = double.MaxValue;
			var enumValue = StringComparison.CurrentCulture;
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this==@0", obj));
			Assert.False((bool)DynamicHelper.EvalGet(obj, "this==@0", otherObj));
			Assert.False((bool)DynamicHelper.EvalGet(obj, "this!=@0", obj));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this!=@0", otherObj));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this.RefProperty==@0", obj.RefProperty));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this.StringField==@0", obj.StringField));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this.StringProperty==@0", obj.StringProperty));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this.IntField==@0", obj.IntField));
			Assert.True((bool)DynamicHelper.EvalGet(obj, "this.IntProperty==@0", obj.IntProperty));
			Assert.True((bool)DynamicHelper.EvalGet(doubleValue, "this==@0", doubleValue));
			Assert.True((bool)DynamicHelper.EvalGet(enumValue, "this==@0", StringComparison.CurrentCulture));
			Assert.True((bool)DynamicHelper.EvalGet(enumValue, "this.Tostring()+this.Tostring()==@0+@1", enumValue.ToString(), enumValue.ToString()));
		}

		[Fact]
		public void TestEvalPropertySet()
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			DynamicHelper.EvalSet(obj, "RefProperty.StringProperty", obj, "StringProperty");
			Assert.Equal(obj.StringProperty, obj.RefProperty.StringProperty);
			DynamicHelper.EvalSet(obj, "RefProperty", otherObj, "RefProperty");
			Assert.Equal(otherObj.RefProperty, obj.RefProperty);
			var baseObj = new BaseTestClass();
			DynamicHelper.EvalSet(obj, "RefProperty", baseObj);
			Assert.Equal(baseObj, obj.RefProperty);
		}

		[Fact]
		public void CanSetField()
		{
			var obj = new TestClass();
			int value = obj.IntField + 1;
			DynamicHelper.EvalSet(obj, "IntField", value);
			Assert.Equal(value, obj.IntField);
		}
	}
}
