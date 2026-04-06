using Xunit;
using System.Diagnostics;
using System.Dynamic;
using System;
using System.Collections.Generic;
using System.Linq;
using ActDim.Practix.TypeAccess.Linq.Dynamic;
using ActDim.Practix.TypeAccess.Reflection;

namespace OrthoBits.InterCode.Tests
{
    public class DynamicExpressionTests
    {
        [Fact]
        public void CanCreateAnonymousTypeFromPropertyInfos()
        {
            Assert.NotNull(DynamicTypeFactory.Instance.CreateType(typeof(TestClass).GetProperties()));
        }

        [Fact]
        public void CanCreateAnonymousTypeFromPropertyTypes()
        {
            Assert.NotNull(DynamicTypeFactory.Instance.CreateType(typeof(TestClass).GetProperties().ToDictionary(p => p.Name, p => p.PropertyType)));
        }

        [Fact]
        public void CanHandleAggregationMethods()
        {
            var testObj = new
            {
                q = new TestClass[] { new TestClass() { IntProperty = 1 }, new TestClass() { IntProperty = 2 }, new TestClass() { IntProperty = 3 } },
                l = new List<object>()
            };
            var lambda1 = DynamicExpression.ParseLambda([System.Linq.Expressions.Expression.Parameter(testObj.GetType(), ""), System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "this")], null, "l.Add(this.q[1]) ").Compile();
            lambda1.DynamicInvoke(testObj, testObj);
            Assert.Equal(1, testObj.l.Count);
            Assert.Equal(testObj.q[1], testObj.l[0]);
            var lambda2 = DynamicExpression.ParseLambda([System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "")], null, "q.Max(IntProperty)").Compile();
            var result = lambda2.DynamicInvoke(testObj);
            Assert.Equal(testObj.q.Max(item => item.IntProperty), result);
        }

        [Fact]
        public void CanInvokMethodsWithParams()
        {
            var testObj = new TestClass();

            var stringArray = new[] { "test1", "test2", "test3" };
            var lambda = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "") }, null, string.Format("MethodWithStringParams(\"{0}\", \"{1}\")", stringArray[0], stringArray[1])).Compile();
            Assert.Equal(testObj.MethodWithStringParams(stringArray[0], stringArray[1]), lambda.DynamicInvoke(testObj));

            lambda = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "") }, null, string.Format("MethodWithStringParams(\"{0}\", \"{1}\", \"{2}\")", stringArray[0], stringArray[1], stringArray[2])).Compile();
            Assert.Equal(testObj.MethodWithStringParams2(stringArray[0], stringArray[1], stringArray[2]), lambda.DynamicInvoke(testObj));
        }

        [Fact]
        public void CanCreateAnonymousTypeInstanceFromNamedValuesDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                {"stringProperty","StringPropertyInitialValue"},
                {"intProperty",int.MinValue},
                {"refValue1", new TestClass()},
                {"refValue2", new BaseTestClass()}
            };

            object instance = DynamicTypeFactory.Instance.CreateObject(dict);

            Assert.NotNull(instance);
            dynamic d = instance;
            Assert.Equal(dict["stringProperty"], d.stringProperty);
            Assert.Equal(dict["intProperty"], d.intProperty);
            Assert.Equal(dict["refValue1"], d.refValue1);
            Assert.Equal(dict["refValue2"], d.refValue2);
        }

        [Fact]
        public void CanAssignProperty()
        {
            var obj = new TestClass();
            var otherObj = new TestClass();
            string res = (string)DynamicHelper.EvalGet(obj, "RefProperty.StringPropertyAuto=StringProperty", (Type)null);

            Assert.Equal(obj.StringProperty, res);
            Assert.Equal(obj.StringProperty, obj.RefProperty.StringPropertyAuto);
            res = (string)DynamicHelper.EvalGet(otherObj, string.Format("StringProperty=\"{0}\"", otherObj.RefProperty.StringPropertyAuto), (Type)null);
            Assert.Equal(otherObj.RefProperty.StringPropertyAuto, otherObj.StringProperty);
            var baseObj = (BaseTestClass)DynamicHelper.EvalGet(obj, "RefProperty = null", (Type)null);
            Assert.Null(obj.RefProperty);
        }

        [Fact]
        public void CanAssignField()
        {
            var obj = new TestClass();
            int value = new Random().Next();
            DynamicHelper.EvalGet(obj, "IntField=" + value.ToString(), (Type)null);
            Assert.Equal(value, obj.IntField);
        }

        [Fact]
        public void CanBeUsedForDynamicObjectGeneration()
        {
            var c = 1000000;
            var sw1 = new Stopwatch();
            sw1.Start();
            var sample = new
            {
                Id = default(int),
                Name = default(string),
                CreatedAt = default(DateTime),
                Number1 = default(double),
                Number2 = default(float)
            };

            var data1 = GenericGeneratorMethod(c, sample).ToList();
            sw1.Stop();

            var sw2 = new Stopwatch();
            sw2.Start();
            var data2 = DynamicGeneratorMethod(c, _ => _).Select(d => new
            {
                Id = (int)d.Id,
                Name = (string)d.Name,
                CreatedAt = (DateTime)d.CreatedAt,
                Number1 = (double)d.Number1,
                Number2 = (float)d.Number2
            }).ToList();
            sw2.Stop();

            var sw3 = new Stopwatch();
            sw3.Start();
            var data3 = DynamicGeneratorMethod<(int Id, string Name, DateTime CreatedAt, double Number1, float Number2)>(c,
                _ => (_.Id, _.Name, _.CreatedAt, _.Number1, _.Number2)).ToList();
            sw3.Stop();

            Assert.Equal(c, data1.ToList().Count);
            Assert.Equal(c, data2.ToList().Count);
            Assert.True(sw1.ElapsedTicks == new[] { sw1.ElapsedTicks, sw2.ElapsedTicks, sw3.ElapsedTicks }.Min());
        }

        private IEnumerable<T> GenericGeneratorMethod<T>(int count, T sample)
        {
            var type = sample.GetType();
            var ctorInfo = type.GetConstructors().First(ci => ci.GetParameters().Length > 0);
            var ctor = TypeAccessor.GetConstructorEx(ctorInfo);
            for (var i = 0; i < count; i++)
            {
                var obj = ctor(i, "dynamic" + i, DateTime.Now, double.MaxValue, float.MinValue);
                yield return (T)obj;
            }
        }

        private IEnumerable<T> DynamicGeneratorMethod<T>(int count, Func<dynamic, T> project)
        {
            for (var i = 0; i < count; i++)
            {
                dynamic obj = new ExpandoObject();
                obj.Id = i;
                obj.Name = "dynamic" + i;
                obj.CreatedAt = DateTime.Now;
                obj.Number1 = double.MaxValue;
                obj.Number2 = float.MinValue;
                T element = default(T);
                if (project != null)
                    element = project(obj);
                else
                    element = obj;
                yield return element;
            }
        }
    }
}
