using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Emitron.Tests
{
	public class EmitronTests
	{
		// ------------------------------------------------------------------
		// Compile → returns a reusable Func<object, T>
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_ReturnsNonNullDelegate()
		{
			var eval = Emitron.Compile<int>("@params.Value");
			Assert.NotNull(eval);
		}

		[Fact]
		public void Compile_SameCodeAndType_ReturnsCachedDelegate()
		{
			const string code = "@params.X + @params.Y";
			var first = Emitron.Compile<int>(code);
			var second = Emitron.Compile<int>(code);
			Assert.Same(first, second);
		}

		[Fact]
		public void Compile_SameCodeDifferentType_ReturnsDifferentDelegate()
		{
			const string code = "@params.Value";
			var intEval = Emitron.Compile<int>(code);
			var objEval = Emitron.Compile<object>(code);
			Assert.NotSame(intEval, objEval);
		}

		// ------------------------------------------------------------------
		// Single-expression evaluation
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_SingleExpression_ReturnsInt()
		{
			var result = Emitron.Evaluate<int>("(int)@params.A + (int)@params.B", new { A = 3, B = 4 });
			Assert.Equal(7, result);
		}

		[Fact]
		public void Evaluate_UsingEscapedParamsPropertyAccess()
		{
			var result = Emitron.Evaluate<int>("(int)@params.A + (int)@params.B", new { A = 10, B = 40 });
			Assert.Equal(50, result);
		}

		[Fact]
		public void Evaluate_UsingCustomInputParameterName()
		{
			var result = Emitron.Evaluate<int>(
				"(int)@ctx.A + (int)@ctx.B",
				new { A = 15, B = 25 },
				inputParameterName: "@ctx");
			Assert.Equal(40, result);
		}

		[Fact]
		public void Evaluate_UsingCustomInputParameterNameWithoutPrefix()
		{
			var result = Emitron.Evaluate<int>(
				"(int)p.A + (int)p.B",
				new { A = 100, B = 200 },
				inputParameterName: "p");
			Assert.Equal(300, result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsString()
		{
			var result = Emitron.Evaluate<string>(
				"((string)@params.FirstName) + \" \" + ((string)@params.LastName)",
				new { FirstName = "Jane", LastName = "Doe" });
			Assert.Equal("Jane Doe", result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsBool()
		{
			var result = Emitron.Evaluate<bool>("(int)@params.Score >= 60", new { Score = 75 });
			Assert.True(result);
		}

		[Fact]
		public void Evaluate_SingleExpression_ReturnsDouble()
		{
			var result = Emitron.Evaluate<double>(
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

			var result = Emitron.Evaluate<int>(code, new { A = 3, B = 4 });
			Assert.Equal(25, result);
		}

		[Fact]
		public void Evaluate_MultiStatementBlock_ReturnsString()
		{
			const string code = """
				var name = (string)@params.Name;
				return name.Length > 5 ? name.Substring(0, 5) + "…" : name;
				""";

			Assert.Equal("Hello…", Emitron.Evaluate<string>(code, new { Name = "Hello World" }));
			Assert.Equal("Hi", Emitron.Evaluate<string>(code, new { Name = "Hi" }));
		}

		// ------------------------------------------------------------------
		// Dictionary<string,object> as inputs
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_WithDictionaryInputs()
		{
			var result = Emitron.Evaluate<int>(
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
			var square = Emitron.Compile<int>("(int)@params.N * (int)@params.N");

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
			var result = Emitron.Evaluate<object>("@params.Value", new { Value = 42 });
			Assert.Equal(42, result);
		}

		// ------------------------------------------------------------------
		// Template interpolation facade tests
		// ------------------------------------------------------------------

		[Fact]
		public void Emitron_Interpolate_FormatsTemplateCorrectly()
		{
			var result = Emitron.Interpolate("$\"Hello, {Name}!\"", new { Name = "Emitron" });
			Assert.Equal("Hello, Emitron!", result);
		}

		[Fact]
		public void Emitron_CompileTemplate_ProducesWorkingFormatter()
		{
			var formatter = Emitron.CompileTemplate("$\"Count = {Count}\"");
			var result = formatter(new { Count = 42 });
			Assert.Equal("Count = 42", result);
		}

		// ------------------------------------------------------------------
		// Guard clauses
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NullCode_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => Emitron.Compile<string>(null));
		}

		[Fact]
		public void Compile_EmptyCode_Throws()
		{
			Assert.Throws<ArgumentException>(() => Emitron.Compile<string>(string.Empty));
		}

		[Fact]
		public void Evaluate_NullInput_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				Emitron.Evaluate<string>("@params.Name", null));
		}

		// ------------------------------------------------------------------
		// Invalid code throws CompilationException
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_InvalidCode_ThrowsCompilationException()
		{
			Assert.Throws<CompilationException>(() =>
				Emitron.Compile<int>("this is not valid C#!!!"));
		}

		// ------------------------------------------------------------------
		// #r "Assembly" and using Namespace; directives
		// ------------------------------------------------------------------

		[Fact]
		public void Evaluate_WithUsingDirective_ImportsNamespaceSuccessfully()
		{
			const string code = """
				using System.Text;

				var sb = new StringBuilder();
				sb.Append((string)@params.First);
				sb.Append(" ");
				sb.Append((string)@params.Last);
				return sb.ToString();
				""";

			var result = Emitron.Evaluate<string>(code, new { First = "Hello", Last = "World" });
			Assert.Equal("Hello World", result);
		}

		[Fact]
		public void Evaluate_WithAssemblyDirectiveAndUsing_LoadsAssemblyAndEvaluates()
		{
			const string code = """
				#r "System.Text.Json"
				using System.Text.Json;

				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("name").GetString();
				""";

			var result = Emitron.Evaluate<string>(code, new { Json = """{"name":"Emitron"}""" });
			Assert.Equal("Emitron", result);
		}

		[Fact]
		public void Evaluate_WithAssemblyDirectiveWithoutUsing_EvaluatesWithFullName()
		{
			const string code = """
				#r "System.Text.Json"

				var doc = System.Text.Json.JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("count").GetInt32();
				""";

			var result = Emitron.Evaluate<int>(code, new { Json = """{"count":42}""" });
			Assert.Equal(42, result);
		}

		[Fact]
		public void Evaluate_WithAssemblyAndUsingAndCustomInputParameterName()
		{
			const string code = """
				#r "System.Text.Json"
				using System.Text.Json;

				var doc = JsonDocument.Parse((string)ctx.Json);
				return doc.RootElement.GetProperty("id").GetInt32();
				""";

			var result = Emitron.Evaluate<int>(
				code,
				new { Json = """{"id":101}""" },
				inputParameterName: "ctx");

			Assert.Equal(101, result);
		}

		[Fact]
		public void Evaluate_WithCustomSearchPathsInOptions_ResolvesAssembly()
		{
			var options = new EmitronOptions()
				.AddSearchPaths(AppContext.BaseDirectory);

			const string code = """
				#r "System.Text.Json"
				using System.Text.Json;

				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("status").GetString();
				""";

			var result = Emitron.Evaluate<string>(code, new { Json = """{"status":"active"}""" }, options: options);
			Assert.Equal("active", result);
		}

		[Fact]
		public void Evaluate_WithOptionsAssembliesAndUsings_WorksWithoutInlineDirectives()
		{
			var options = new EmitronOptions()
				.AddAssemblies(typeof(System.Text.Json.JsonDocument).Assembly)
				.AddUsings("System.Text.Json");

			const string code = """
				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("valid").GetBoolean();
				""";

			var result = Emitron.Evaluate<bool>(code, new { Json = """{"valid":true}""" }, options: options);
			Assert.True(result);
		}

		[Fact]
		public void Compile_WithInvalidAssemblyDirective_ThrowsCompilationException()
		{
			const string code = """
				#r "NonExistentAssemblyThatDoesNotExistAtAll_12345"

				return 1;
				""";

			Assert.Throws<CompilationException>(() => Emitron.Compile<int>(code));
		}

		[Fact]
		public void Evaluate_WithAssembliesAndUsingsOverload_EvaluatesSuccessfully()
		{
			const string code = """
				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("count").GetInt32();
				""";

			var result = Emitron.Evaluate<int>(
				code,
				new { Json = """{"count":99}""" },
				assemblies: [typeof(System.Text.Json.JsonDocument).Assembly],
				usings: ["System.Text.Json"]);

			Assert.Equal(99, result);
		}

		[Fact]
		public void Evaluate_WithTypesAndUsingsOverload_EvaluatesSuccessfully()
		{
			const string code = """
				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("name").GetString();
				""";

			var result = Emitron.Evaluate<string>(
				code,
				new { Json = """{"name":"Practix"}""" },
				types: [typeof(System.Text.Json.JsonDocument)],
				usings: ["System.Text.Json"]);

			Assert.Equal("Practix", result);
		}

		[Fact]
		public void Compile_WithTypesAndUsingsOverload_ReturnsWorkingDelegate()
		{
			const string code = """
				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("id").GetInt64();
				""";

			var func = Emitron.Compile<long>(
				code,
				types: [typeof(System.Text.Json.JsonDocument)],
				usings: ["System.Text.Json"]);

			Assert.Equal(123456789L, func(new { Json = """{"id":123456789}""" }));
		}

		[Fact]
		public void Evaluate_WithStringAssemblyAndUsingsInOptions_EvaluatesSuccessfully()
		{
			var options = new EmitronOptions()
				.AddAssemblies("System.Text.Json")
				.AddUsings("System.Text.Json");

			const string code = """
				var doc = JsonDocument.Parse((string)@params.Json);
				return doc.RootElement.GetProperty("active").GetBoolean();
				""";

			var result = Emitron.Evaluate<bool>(code, new { Json = """{"active":true}""" }, options);
			Assert.True(result);
		}
	}
}
