using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Dynamic;
using Xunit;

namespace SalientBits.InterString.Tests
{
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

		[Fact]
		public void CanFormatObjectSource()
		{
			Assert.Equal(string.Format("{0} {1} {2}", TestDictionary.ElementAt(0).Value, TestDictionary.ElementAt(1).Value, TestDictionary.ElementAt(2).Value),
			StringFormatter.Format(string.Format("{{P[\"{0}\"]}} {{P[\"{1}\"]}} {{P[\"{2}\"]}}", TestDictionary.ElementAt(0).Key, TestDictionary.ElementAt(1).Key, TestDictionary.ElementAt(2).Key), new { P = TestDictionary }));
		}

		[Fact]
		public void CanFormatDictionarySource()
		{
			Assert.Equal(string.Format("{0} {1} {2}", TestDictionary.ElementAt(0).Value, TestDictionary.ElementAt(1).Value, TestDictionary.ElementAt(2).Value),
			StringFormatter.Format(string.Format("{{{0}}} {{{1}}} {{{2}}}", TestDictionary.ElementAt(0).Key, TestDictionary.ElementAt(1).Key, TestDictionary.ElementAt(2).Key), TestDictionary));
		}

		[Fact]
		public void CanFormatDictionarySourceWithEval()
		{
			var strArr = (TestDictionary["TestStringArray"] as string[]);
			Assert.Equal(string.Format("{0}, {1}, {2}", strArr[0].Length, strArr[1].Length, strArr[2].Length),
			StringFormatter.Format("{TestStringArray[0].Length}, {TestStringArray[1].Length}, {TestStringArray[2].Length}", TestDictionary));

			var objArr = (TestDictionary["TestObjectArray"] as object[]);
			Assert.Equal(string.Format("{0}, {1}, {2}", objArr[0], objArr[1], objArr[2]),
			StringFormatter.Format("{TestObjectArray[0].ToString()}, {TestObjectArray[1].ToString()}, {TestObjectArray[2].ToString()}", TestDictionary));
		}

		[Fact]
		public void CanFormatObjectSourceWithEval()
		{
			var _testObject = new { TestString = "TestString", TestDateTime = DateTime.Now };
			Assert.Equal(_testObject.TestDateTime.ToString("dd.MM.yy"),
			StringFormatter.Format(string.Format("{{{0}}}", "TestDateTime.ToString(\"dd.MM.yy\")"), _testObject));
		}

		[Fact]
		public void CanFormatObjectSourceWithEvalAndCompositeFormatting()
		{
			var testObject = new { TestString = "TestString", TestDateTime = DateTime.Now, TestInt = int.MaxValue };
			var result = StringFormatter.Format(string.Format("{{{0}:dd.MM.yy}}", "TestDateTime"), testObject);
			Assert.Equal(string.Format("{0:dd.MM.yy}", testObject.TestDateTime), result);
			result = StringFormatter.Format(string.Format("{{{0},-10:C}}", "TestInt"), testObject);
			Assert.Equal(string.Format("{0,-10:C}", testObject.TestInt), result);
		}

		[Fact]
		public void IsFastestOnMarket()
		{
			return; // TODO: optimize
			var nIterations = 10000;
			var timings = new List<long>();
			string s;
			var testObj = new { Int32Prop = int.MaxValue, DoubleProp = double.MaxValue, StringProp = DateTime.Now.ToShortDateString(), DateTimeProp = DateTime.MinValue };
			var format = "{Int32Prop}: Int32, {DoubleProp}: Double, \"{StringProp}\": String, {DateTimeProp}: DateTime";
			var sw = new Stopwatch();

			dynamic testObj2 = new ExpandoObject();
			testObj2.Int32Prop = testObj.Int32Prop;
			testObj2.DoubleProp = testObj.DoubleProp;
			testObj2.StringProp = testObj.StringProp;
			testObj2.DateTimeProp = testObj.DateTimeProp;

			sw.Reset();
			sw.Start();
			for (var i = 0; i < nIterations; i++)
			{
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
			Assert.True(timings.Last() == timings.Min(t => t));
		}
	}
}
