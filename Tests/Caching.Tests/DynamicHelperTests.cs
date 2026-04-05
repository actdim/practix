using System;
using OrthoBits.InterCode.Linq.Dynamic;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

//http://statlight.codeplex.com/
namespace OrthoBits.InterCode.Tests //Linq.Dynamic
{

	[TestFixture]
	public class DynamicHelperTests
	{
		[Test]
		[Description("")]
		public void TestEvalGetGeneric()
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			otherObj.RefProperty.StringProperty = otherObj.StringPropertyAuto;
			Assert.AreEqual(obj.RefProperty.StringProperty, DynamicHelper.EvalGet<TestClass, string>(obj, "RefProperty.StringProperty"));

			Assert.AreEqual(otherObj.StringPropertyAuto, DynamicHelper.EvalGet<TestClass, string>(otherObj, "RefProperty.StringProperty"));
		}

		[Test]
		[Description("")]
		public void TestEvalGetWithAnonymousTypes()
		{
			var obj = new { RefProperty = new TestClass(), StringProperty = "TestString" };
			var result = DynamicHelper.EvalGet(obj, "RefProperty.StringProperty.Length.ToString()+StringProperty");
			Assert.AreEqual(obj.RefProperty.StringProperty.Length.ToString() + obj.StringProperty, (string)result);
		}

		[Test]
		[Description("")]
		public void CanCreateTypedPropertyOrFieldGetter()
		{
			var obj = new TestClass();
			var getter = DynamicHelper.CreateEvalGetter<TestClass, BaseTestClass>("RefProperty");
			Assert.IsNotNull(getter);
			Assert.AreEqual(obj.RefProperty, getter(obj));
		}

		[Test]
		[Description("")]
		public void CanCreatePropertyOrFieldGetter() //Generic
		{
			var obj = new TestClass();
			var getter = DynamicHelper.CreateEvalGetter(typeof(TestClass), "RefProperty", typeof(BaseTestClass));
			Assert.IsNotNull(getter);
			Assert.AreEqual(obj.RefProperty, (BaseTestClass)getter.DynamicInvoke(obj));
		}

		[Test]
		[Description("")]
		public void CanHandleThisKeyword()
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			var doubleValue = double.MaxValue;
			var enumValue = StringComparison.CurrentCulture;
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this==@0", obj));
			Assert.IsFalse((bool)DynamicHelper.EvalGet(obj, "this==@0", otherObj));
			Assert.IsFalse((bool)DynamicHelper.EvalGet(obj, "this!=@0", obj));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this!=@0", otherObj));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this.RefProperty==@0", obj.RefProperty));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this.StringField==@0", obj.StringField));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this.StringProperty==@0", obj.StringProperty));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this.IntField==@0", obj.IntField));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(obj, "this.IntProperty==@0", obj.IntProperty));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(doubleValue, "this==@0", doubleValue));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(enumValue, "this==@0", StringComparison.CurrentCulture));
			Assert.IsTrue((bool)DynamicHelper.EvalGet(enumValue, "this.Tostring()+this.Tostring()==@0+@1", enumValue.ToString(), enumValue.ToString()));
		}

		[Test]
		[Description("")]
		public void TestEvalPropertySet() //Generic
		{
			var obj = new TestClass();
			var otherObj = new TestClass();
			DynamicHelper.EvalSet(obj, "RefProperty.StringProperty", obj, "StringProperty");
			Assert.AreEqual(obj.StringProperty, obj.RefProperty.StringProperty);
			DynamicHelper.EvalSet(obj, "RefProperty", otherObj, "RefProperty");
			Assert.AreEqual(otherObj.RefProperty, obj.RefProperty);
			var baseObj = new BaseTestClass();
			DynamicHelper.EvalSet(obj, "RefProperty", baseObj);
			Assert.AreEqual(baseObj, obj.RefProperty);
		}

		[Test]
		[Description("")]
		public void CanSetField() //Generic
		{
			var obj = new TestClass();
			int value = obj.IntField + 1;
			//DynamicHelper.EvalSet(obj, "IntField", obj, "IntField+1");
			//Assert.AreEqual<int>(value, obj.IntField);
			DynamicHelper.EvalSet(obj, "IntField", value);
			Assert.AreEqual(value, obj.IntField);
		}

	}
}
