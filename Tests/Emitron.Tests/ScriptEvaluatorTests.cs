using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Emitron.Tests
{
	public class ScriptEvaluatorTests
	{
		// ------------------------------------------------------------------
		// Compile → returns a reusable Func<object, T>
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_ReturnsNonNullDelegate()
		{
			var eval = ScriptEvaluator.Compile<int>("__emitron_p.Value");
			Assert.NotNull(eval);
		}

		[Fact]
		public void Compile_SameCodeAndType_ReturnsCachedDelegate()
		{
			const string code = "__emitron_p.X + __emitron_p.Y";
			var first = ScriptEvaluator.Compile<int>(code);
			var second = ScriptEvaluator.Compile<int>(code);
			Assert.Same(first, second);
		}

		[Fact]
		public void Compile_SameCodeDifferentType_ReturnsDifferentDelegate()
		{
			const string code = "__emitron_p.Value";
			var intEval = ScriptEvaluator.Compile<int>(code);
			var objEval = ScriptEvaluator.Compile<object>(code);
			Assert.NotSame(intEval, objEval);
		}

		// ------------------------------------------------------------------
		// Single-expression evaluation
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_SingleExpression_ReturnsInt()
		{
			var result = ScriptEvaluator.Evaluate<int>("(int)__emitron_p.A + (int)__emitron_p.B", new { A = 3, B = 4 });
			Assert.Equal(7, result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsString()
		{
			var result = ScriptEvaluator.Evaluate<string>(
				"((string)__emitron_p.FirstName) + \" \" + ((string)__emitron_p.LastName)",
				new { FirstName = "Jane", LastName = "Doe" });
			Assert.Equal("Jane Doe", result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsBool()
		{
			var result = ScriptEvaluator.Evaluate<bool>("(int)__emitron_p.Score >= 60", new { Score = 75 });
			Assert.True(result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsDouble()
		{
			var result = ScriptEvaluator.Evaluate<double>(
				"(double)__emitron_p.Price * (1.0 - (double)__emitron_p.Discount)",
				new { Price = 100.0, Discount = 0.15 });
			Assert.Equal(85.0, result, precision: 10);
		}

		// ------------------------------------------------------------------
		// Multi-statement block with explicit return
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_MultiStatementBlock_ReturnsInt()
		{
			const string code = """
				var a = (int)__emitron_p.A;
				var b = (int)__emitron_p.B;
				return a * a + b * b;
				""";

			var result = ScriptEvaluator.Evaluate<int>(code, new { A = 3, B = 4 });
			Assert.Equal(25, result);
		}

		[Fact]
		public void Evaluate_MultiStatementBlock_ReturnsString()
		{
			const string code = """
				var name = (string)__emitron_p.Name;
				return name.Length > 5 ? name.Substring(0, 5) + "…" : name;
				""";

			Assert.Equal("Hello…", ScriptEvaluator.Evaluate<string>(code, new { Name = "Hello World" }));
			Assert.Equal("Hi", ScriptEvaluator.Evaluate<string>(code, new { Name = "Hi" }));
		}

		// ------------------------------------------------------------------
		// Dictionary<string,object> as parameters
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_WithDictionaryParameters()
		{
			var result = ScriptEvaluator.Evaluate<int>(
				"(int)__emitron_p.X * (int)__emitron_p.Y",
				new Dictionary<string, object> { { "X", 6 }, { "Y", 7 } });

			Assert.Equal(42, result);
		}

		// ------------------------------------------------------------------
		// Compiled delegate is reusable with different inputs
		// ------------------------------------------------------------------

		[Fact]
		public void CompiledEvaluator_IsReusableWithDifferentInputs()
		{
			var square = ScriptEvaluator.Compile<int>("(int)__emitron_p.N * (int)__emitron_p.N");

			Assert.Equal(4, square(new { N = 2 }));
			Assert.Equal(9, square(new { N = 3 }));
			Assert.Equal(25, square(new { N = 5 }));
		}

		// ------------------------------------------------------------------
		// Return type object (dynamic/boxed value)
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_ObjectReturnType_ReturnsBoxedValue()
		{
			var result = ScriptEvaluator.Evaluate<object>("__emitron_p.Value", new { Value = 42 });
			Assert.Equal(42, result);
		}

		// ------------------------------------------------------------------
		// Guard clauses
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NullCode_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => ScriptEvaluator.Compile<string>(null));
		}

		[Fact]
		public void Compile_EmptyCode_Throws()
		{
			Assert.Throws<ArgumentException>(() => ScriptEvaluator.Compile<string>(string.Empty));
		}

		[Fact]
		public void Evaluate_NullParameters_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				ScriptEvaluator.Evaluate<string>("__emitron_p.Name", null));
		}

		// ------------------------------------------------------------------
		// Invalid code throws CompilationException
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_InvalidCode_ThrowsCompilationException()
		{
			Assert.Throws<CompilationException>(() =>
				ScriptEvaluator.Compile<int>("this is not valid C#!!!"));
		}
	}
}
