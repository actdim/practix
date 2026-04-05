using NUnit.Framework;
using OrthoBits.InterCode.Linq.Dynamic;
using OrthoBits.InterCode.Reflection;
using System.Diagnostics;
using System.Dynamic;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace OrthoBits.InterCode.Tests // Linq.Dynamic
{   
    [TestFixture]
    public class DynamicExpressionTests
    {
        [Test]
        public void CanCreateAnonymousTypeFromPropertyInfos()
        {
            Assert.IsNotNull(DynamicTypeFactory.Instance.CreateType(typeof(TestClass).GetProperties()));
        }

        [Test]
        public void CanCreateAnonymousTypeFromPropertyTypes()
        {
            Assert.IsNotNull(DynamicTypeFactory.Instance.CreateType(typeof(TestClass).GetProperties().ToDictionary(p => p.Name, p => p.PropertyType)));
        }

        [Test]
        public void CanHandleAggregationMethods()
        {
            var testObj = new
            {
                q = new TestClass[] { new TestClass() { IntProperty = 1 }, new TestClass() { IntProperty = 2 }, new TestClass() { IntProperty = 3 } },
                l = new List<object>()
            };
            // DynamicHelper.EvalGet(testObj, "q.Max(IntProperty)");
            var lambda1 = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), ""), System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "this") }, null, "l.Add(this.q[1]) ").Compile();
            lambda1.DynamicInvoke(testObj, testObj);
            Assert.AreEqual(1, testObj.l.Count);
            Assert.AreEqual(testObj.q[1], testObj.l[0]);
            var lambda2 = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "") }, null, "q.Max(IntProperty)").Compile();
            var result = lambda2.DynamicInvoke(testObj);
            Assert.AreEqual(testObj.q.Max(item => item.IntProperty), result);

        }

        [Test]
        public void CanInvokMethodsWithParams()
        {
            var testObj = new TestClass();

            var stringArray = new[] { "test1", "test2", "test3" };
            var lambda = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "") }, null, string.Format("MethodWithStringParams(\"{0}\", \"{1}\")", stringArray[0], stringArray[1])).Compile();
            Assert.AreEqual(testObj.MethodWithStringParams(stringArray[0], stringArray[1]), lambda.DynamicInvoke(testObj));

            lambda = DynamicExpression.ParseLambda(new[] { System.Linq.Expressions.Expression.Parameter(testObj.GetType(), "") }, null, string.Format("MethodWithStringParams(\"{0}\", \"{1}\", \"{2}\")", stringArray[0], stringArray[1], stringArray[2])).Compile();
            Assert.AreEqual(testObj.MethodWithStringParams2(stringArray[0], stringArray[1], stringArray[2]), lambda.DynamicInvoke(testObj));
        }
        
        // CreateAnonymousTypeObject
        [Test]
        public void CanCreateAnonymousTypeInstanceFromNamedValuesDictionary()
        {
            var dict = new Dictionary<string, object>
            {
                {"stringProperty","StringPropertyInitialValue"},
                {"intProperty",int.MinValue},
                {"refValue1", new TestClass()},
                {"refValue2", new BaseTestClass()}
            };

            object instance;
            //var propertyTypes = dict.ToDictionary(pair => pair.Key, pair => pair.Value == null ? typeof(object) : pair.Value.GetType());
            //var type = DynamicTypeFactory.Instance.CreateType(propertyTypes);
            //instance = DynamicTypeFactory.Instance.CreateObject(dict, type);			
            instance = DynamicTypeFactory.Instance.CreateObject(dict);

            Assert.IsNotNull(instance);
            dynamic d = instance;
            Assert.AreEqual(dict["stringProperty"], d.stringProperty);
            Assert.AreEqual(dict["intProperty"], d.intProperty);
            Assert.AreEqual(dict["refValue1"], d.refValue1);
            Assert.AreEqual(dict["refValue2"], d.refValue2);
        }

        [Test]
        [Description("")]
        public void CanAssignProperty() //Generic
        {
            var obj = new TestClass();
            var otherObj = new TestClass();
            string res = (string)DynamicHelper.EvalGet(obj, "RefProperty.StringPropertyAuto=StringProperty", (Type)null); // typeof(void)??
                                                                                                                          
            Assert.AreEqual(obj.StringProperty, res);
            Assert.AreEqual(obj.StringProperty, obj.RefProperty.StringPropertyAuto);
            res = (string)DynamicHelper.EvalGet(otherObj, string.Format("StringProperty=\"{0}\"", otherObj.RefProperty.StringPropertyAuto), (Type)null); //typeof(void)??
                                                                                                                                                         //Assert.AreEqual(otherObj.RefProperty.StringPropertyAuto, res);
            Assert.AreEqual(otherObj.RefProperty.StringPropertyAuto, otherObj.StringProperty);
            var baseObj = (BaseTestClass)DynamicHelper.EvalGet(obj, "RefProperty = null", (Type)null);
            // Assert.AreEqual(null, baseObj);
            Assert.AreEqual(null, obj.RefProperty);
        }

        [Test]
        [Description("")]
        public void CanAssignField() //Generic
        {
            var obj = new TestClass();
            int value = new Random().Next();
            DynamicHelper.EvalGet(obj, "IntField=" + value.ToString(), (Type)null); //typeof(void)??                                    
            Assert.AreEqual(value, obj.IntField);
        }

        [Test]
        public void CanBeUsedForDynamicObjectGeneration()
        {
            // var data = GenericGeneratorMethod<dynamic>(c, new { _.Id<int>(), _.Name, _.CreatedAt });

            var c = 1000000;
            var sw1 = new Stopwatch();
            sw1.Start();
            // NOTE: sample has no property setters
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

            Assert.AreEqual(data1.ToList().Count, c);
            Assert.AreEqual(data2.ToList().Count, c);
            Assert.IsTrue(sw1.ElapsedTicks == new[] { sw1.ElapsedTicks, sw2.ElapsedTicks, sw3.ElapsedTicks }.Min());
        }

        private IEnumerable<T> GenericGeneratorMethod<T>(int count, T sample)
        {
            // var type = typeof(T);
            // var isDynamic = type.IsDefined(typeof(DynamicAttribute));
            var type = sample.GetType();
            // tuple constructor to create object from property set
            // constructor calling should be smart (pass default values),
            // to support partial property set (if properties are comming from DB query columns etc.)			
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
                {
                    element = project(obj);
                }
                else
                {
                    element = obj;
                }

                yield return element;
            }
        }
    }
}