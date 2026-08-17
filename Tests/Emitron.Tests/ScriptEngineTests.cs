using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Emitron.Tests
{
	public class ScriptEngineTests
	{
		// ------------------------------------------------------------------
		// Compile → returns a reusable Func<object, T>
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_ReturnsNonNullDelegate()
		{
			var eval = ScriptEngine.Compile<int>("@params.Value");
			Assert.NotNull(eval);
		}

		[Fact]
		public void Compile_SameCodeAndType_ReturnsCachedDelegate()
		{
			const string code = "@params.X + @params.Y";
			var first = ScriptEngine.Compile<int>(code);
			var second = ScriptEngine.Compile<int>(code);
			Assert.Same(first, second);
		}

		[Fact]
		public void Compile_SameCodeDifferentType_ReturnsDifferentDelegate()
		{
			const string code = "@params.Value";
			var intEval = ScriptEngine.Compile<int>(code);
			var objEval = ScriptEngine.Compile<object>(code);
			Assert.NotSame(intEval, objEval);
		}

		// ------------------------------------------------------------------
		// Single-expression evaluation
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_SingleExpression_ReturnsInt()
		{
			var result = ScriptEngine.Evaluate<int>("(int)@params.A + (int)@params.B", new { A = 3, B = 4 });
			Assert.Equal(7, result);
		}

		[Fact]
		public void Evaluate_UsingEscapedParamsPropertyAccess()
		{
			var result = ScriptEngine.Evaluate<int>("(int)@params.A + (int)@params.B", new { A = 10, B = 40 });
			Assert.Equal(50, result);
		}

		[Fact]
		public void Evaluate_UsingCustomInputParameterName()
		{
			var result = ScriptEngine.Evaluate<int>(
				"(int)@ctx.A + (int)@ctx.B",
				new { A = 15, B = 25 },
				inputParameterName: "@ctx");
			Assert.Equal(40, result);
		}

		[Fact]
		public void Evaluate_UsingCustomInputParameterNameWithoutPrefix()
		{
			var result = ScriptEngine.Evaluate<int>(
				"(int)p.A + (int)p.B",
				new { A = 100, B = 200 },
				inputParameterName: "p");
			Assert.Equal(300, result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsString()
		{
			var result = ScriptEngine.Evaluate<string>(
				"((string)@params.FirstName) + \" \" + ((string)@params.LastName)",
				new { FirstName = "Jane", LastName = "Doe" });
			Assert.Equal("Jane Doe", result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsBool()
		{
			var result = ScriptEngine.Evaluate<bool>("(int)@params.Score >= 60", new { Score = 75 });
			Assert.True(result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsDouble()
		{
			var result = ScriptEngine.Evaluate<double>(
				"(double)@params.Price * (1.0 - (double)@params.Discount)",
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
				var a = (int)@params.A;
				var b = (int)@params.B;
				return a * a + b * b;
				""";

			var result = ScriptEngine.Evaluate<int>(code, new { A = 3, B = 4 });
			Assert.Equal(25, result);
		}

		[Fact]
		public void Evaluate_MultiStatementBlock_ReturnsString()
		{
			const string code = """
				var name = (string)@params.Name;
				return name.Length > 5 ? name.Substring(0, 5) + "…" : name;
				""";

			Assert.Equal("Hello…", ScriptEngine.Evaluate<string>(code, new { Name = "Hello World" }));
			Assert.Equal("Hi", ScriptEngine.Evaluate<string>(code, new { Name = "Hi" }));
		}

		// ------------------------------------------------------------------
		// Dictionary<string,object> as inputs
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_WithDictionaryInputs()
		{
			var result = ScriptEngine.Evaluate<int>(
				"(int)@params.X * (int)@params.Y",
				new Dictionary<string, object> { { "X", 6 }, { "Y", 7 } });

			Assert.Equal(42, result);
		}

		// ------------------------------------------------------------------
		// Compiled delegate is reusable with different inputs
		// ------------------------------------------------------------------

		[Fact]
		public void CompiledEvaluator_IsReusableWithDifferentInputs()
		{
			var square = ScriptEngine.Compile<int>("(int)@params.N * (int)@params.N");

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
			var result = ScriptEngine.Evaluate<object>("@params.Value", new { Value = 42 });
			Assert.Equal(42, result);
		}

		// ------------------------------------------------------------------
		// Guard clauses
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NullCode_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => ScriptEngine.Compile<string>(null));
		}

		[Fact]
		public void Compile_EmptyCode_Throws()
		{
			Assert.Throws<ArgumentException>(() => ScriptEngine.Compile<string>(string.Empty));
		}

		[Fact]
		public void Evaluate_NullInput_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				ScriptEngine.Evaluate<string>("@params.Name", null));
		}

		// ------------------------------------------------------------------
		// Invalid code throws CompilationException
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_InvalidCode_ThrowsCompilationException()
		{
			Assert.Throws<CompilationException>(() =>
				ScriptEngine.Compile<int>("this is not valid C#!!!"));
		}
	}
}
