using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Dynamic;
using NUnit.Framework;

namespace SalientBits.InterString.Tests
{
	[TestFixture]
	public class StringFormatterTests
	{
		private static readonly IDictionary<string, object> TestDictionary = new Dictionary<string, object>()
		{
			{"TestIntProperty", int.MaxValue},
			{"TestDoubleProperty", double.MaxValue},			
			{"Test", "Test"},
			{"TestStringArray", new string[] {"1", "23", "456"} },
			{"TestObjectArray", new object[] {"1", 23, DateTime.Now} }			
		};

		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void CanFormatObjectSource()
		{ 					
			Assert.AreEqual(string.Format("{0} {1} {2}", TestDictionary.ElementAt(0).Value, TestDictionary.ElementAt(1).Value, TestDictionary.ElementAt(2).Value),
			StringFormatter.Format(string.Format("{{P[\"{0}\"]}} {{P[\"{1}\"]}} {{P[\"{2}\"]}}", TestDictionary.ElementAt(0).Key, TestDictionary.ElementAt(1).Key, TestDictionary.ElementAt(2).Key), new { P = TestDictionary }));
		}

		[Test]
		public void CanFormatDictionarySource()
		{
			Assert.AreEqual(string.Format("{0} {1} {2}", TestDictionary.ElementAt(0).Value, TestDictionary.ElementAt(1).Value, TestDictionary.ElementAt(2).Value),
			StringFormatter.Format(string.Format("{{{0}}} {{{1}}} {{{2}}}", TestDictionary.ElementAt(0).Key, TestDictionary.ElementAt(1).Key, TestDictionary.ElementAt(2).Key), TestDictionary));			
		}

		[Test]
		public void CanFormatDictionarySourceWithEval()
		{
			var strArr = (TestDictionary["TestStringArray"] as string[]);
			Assert.AreEqual(string.Format("{0}, {1}, {2}", strArr[0].Length, strArr[1].Length, strArr[2].Length),
			StringFormatter.Format("{TestStringArray[0].Length}, {TestStringArray[1].Length}, {TestStringArray[2].Length}", TestDictionary));

			var objArr = (TestDictionary["TestObjectArray"] as object[]);
			Assert.AreEqual(string.Format("{0}, {1}, {2}", objArr[0], objArr[1], objArr[2]),
			StringFormatter.Format("{TestObjectArray[0].ToString()}, {TestObjectArray[1].ToString()}, {TestObjectArray[2].ToString()}", TestDictionary));
		}		

		[Test]
		public void CanFormatObjectSourceWithEval()
		{
			var _testObject = new { TestString = "TestString", TestDateTime = DateTime.Now };
			Assert.AreEqual(_testObject.TestDateTime.ToString("dd.MM.yy"),
			StringFormatter.Format(string.Format("{{{0}}}", "TestDateTime.ToString(\"dd.MM.yy\")"), _testObject));
		}

		[Test]
		public void CanFormatObjectSourceWithEvalAndCompositeFormatting()
		{
			var testObject = new { TestString = "TestString", TestDateTime = DateTime.Now, TestInt = int.MaxValue };
			var result = StringFormatter.Format(string.Format("{{{0}:dd.MM.yy}}", "TestDateTime"), testObject);
			Assert.AreEqual(string.Format("{0:dd.MM.yy}", testObject.TestDateTime), result);			
			result = StringFormatter.Format(string.Format("{{{0},-10:C}}", "TestInt"), testObject);
			Assert.AreEqual(string.Format("{0,-10:C}", testObject.TestInt), result);
		}

		[Test]
		public void IsFastestOnMarket()
		{
			return; // TODO: optimize
			var nIterations = 10000;			
			var timings = new List<long>();			
			string s;
			var testObj = new { Int32Prop = int.MaxValue, DoubleProp = double.MaxValue, StringProp = DateTime.Now.ToShortDateString(), DateTimeProp = DateTime.MinValue };
			//var format = "{(Int32Prop - 1)}: Int32, {DoubleProp - 1}: Double, \"{StringProp + \"test\"}\": String, {DateTimeProp}: DateTime";
			var format = "{Int32Prop}: Int32, {DoubleProp}: Double, \"{StringProp}\": String, {DateTimeProp}: DateTime";
			var sw = new Stopwatch();

			// simple interpolators do not support expressions
			// sw.Reset();
			// sw.Start();
			// for (var i = 0; i < nIterations; i++)
			// {
			// 	s = format.HaackFormat(testObj);
			// }
			// sw.Stop();
			// timings.Add(sw.ElapsedTicks);

			// sw.Reset();
			// sw.Start();
			// for (var i = 0; i < nIterations; i++)
			// {
			// 	s = format.HanselmanFormat(testObj);
			// }
			// sw.Stop();
			// timings.Add(sw.ElapsedTicks);

			// sw.Reset();
			// sw.Start();
			// for (var i = 0; i < nIterations; i++)
			// {
			// 	s = format.HenriFormat(testObj);
			// }
			// sw.Stop();
			// timings.Add(sw.ElapsedTicks);

			// sw.Reset();
			// sw.Start();
			// for (var i = 0; i < nIterations; i++)
			// {
			// 	s = format.JamesFormat(testObj);
			// }
			// sw.Stop();
			// timings.Add(sw.ElapsedTicks);

			// sw.Reset();
			// sw.Start();
			// for (var i = 0; i < nIterations; i++)
			// {
			// 	s = format.OskarFormat(testObj);
			// }
			// sw.Stop();
			// timings.Add(sw.ElapsedTicks);

			dynamic testObj2 = new ExpandoObject();
			testObj2.Int32Prop = testObj.Int32Prop;
			testObj2.DoubleProp = testObj.DoubleProp;
			testObj2.StringProp = testObj.StringProp;
			testObj2.DateTimeProp = testObj.DateTimeProp;
			// var format2 = "{{Int32Prop - 1}}: Int32, {{DoubleProp - 1}}: Double, \"{{StringProp + \"test\"}}\": String, {{DateTimeProp}}: DateTime";

			sw.Reset();
			sw.Start();			
			for (var i = 0; i < nIterations; i++)
			{
				// TODO: s = Kea.StringInterpolation.Interpolate(format2, ((IDictionary<string, object>)testObj2));
				// s = $"{testObj.Int32Prop}: Int32, {testObj.DoubleProp}: Double, \"{testObj.StringProp}\": String, {testObj.DateTimeProp}: DateTime";
				// s = format. (testObj, Formatter.Oskar);
			}
			sw.Stop();
			timings.Add(sw.ElapsedTicks);

			sw.Reset();
			sw.Start();
			for (var i = 0; i < nIterations; i++)
			{
				s = format.Format(testObj);
			}
			sw.Stop();
			timings.Add(sw.ElapsedTicks);
			Assert.IsTrue(timings.Last() == timings.Min(t => t));
		}
	}
}