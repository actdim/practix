using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Emitron.Tests
{
	public class InterpolatorTests
	{
		// ------------------------------------------------------------------
		// Compile → returns a reusable Func<object,string>
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_ReturnsNonNullDelegate()
		{
			var formatter = Interpolator.Compile("$\"{Name}\"");
			Assert.NotNull(formatter);
		}

		[Fact]
		public void Compile_SameTemplate_ReturnsCachedDelegate()
		{
			const string template = "$\"{Value}\"";
			var first = Interpolator.Compile(template);
			var second = Interpolator.Compile(template);
			Assert.Same(first, second);
		}

		// ------------------------------------------------------------------
		// Basic interpolation with anonymous objects
		// ------------------------------------------------------------------

		[Fact]
		public void Format_SimpleStringProperty()
		{
			var result = Interpolator.Format("$\"Hello, {Name}!\"", new { Name = "World" });
			Assert.Equal("Hello, World!", result);
		}

		[Fact]
		public void Format_IntegerProperty()
		{
			var result = Interpolator.Format("$\"Count: {Count}\"", new { Count = 42 });
			Assert.Equal("Count: 42", result);
		}

		[Fact]
		public void Format_MultipleProperties()
		{
			var result = Interpolator.Format(
				"$\"{FirstName} {LastName} is {Age} years old.\"",
				new { FirstName = "Jane", LastName = "Doe", Age = 30 });

			Assert.Equal("Jane Doe is 30 years old.", result);
		}

		// ------------------------------------------------------------------
		// Format specifiers (e.g. {Price:C2}, {Date:dd.MM.yy}, {Rate:P1})
		// ------------------------------------------------------------------

		[Fact]
		public void Format_WithFormatSpecifier_DateTime()
		{
			var date = new DateTime(2024, 3, 15, 14, 30, 0);
			var result = Interpolator.Format("$\"{Date:yyyy-MM-dd HH:mm}\"", new { Date = date });
			Assert.Equal("2024-03-15 14:30", result);
		}

		[Fact]
		public void Format_WithFormatSpecifier_NumericPadding()
		{
			var result = Interpolator.Format("$\"{Value:D6}\"", new { Value = 42 });
			Assert.Equal("000042", result);
		}

		[Fact]
		public void Format_WithFormatSpecifier_FixedPointAndPercentage()
		{
			var result = Interpolator.Format(
				"$\"Price: {Price:F2}, Rate: {Discount:P0}\"",
				new { Price = 19.999m, Discount = 0.15m });

			Assert.Equal("Price: 20.00, Rate: 15%", result);
		}

		[Fact]
		public void Format_WithAlignmentAndFormatSpecifier()
		{
			var result = Interpolator.Format("$\"Value: |{Amount,8:N2}|\"", new { Amount = 123.456m });
			Assert.Equal("Value: |  123.46|", result);
		}

		[Fact]
		public void Interpolate_ExtensionMethod_WithCustomFormatSpecifiers()
		{
			var data = new { CreatedAt = new DateTime(2026, 8, 17), Score = 0.9825 };
			var template = "$\"Date: {CreatedAt:yyyy/MM/dd}, Score: {Score:P1}\"";

			var result = template.Interpolate(data);
			Assert.Equal("Date: 2026/08/17, Score: 98.3%", result);
		}

		// ------------------------------------------------------------------
		// Expressions inside holes (property chains, method calls)
		// ------------------------------------------------------------------

		[Fact]
		public void Format_PropertyChainExpression()
		{
			var result = Interpolator.Format(
				"$\"Length: {Text.Length}\"",
				new { Text = "hello" });
			Assert.Equal("Length: 5", result);
		}

		[Fact]
		public void Format_MethodCallExpression()
		{
			var result = Interpolator.Format(
				"$\"{Name.ToUpperInvariant()}\"",
				new { Name = "world" });
			Assert.Equal("WORLD", result);
		}

		// ------------------------------------------------------------------
		// Dictionary<string,object> parameter overload
		// ------------------------------------------------------------------

		[Fact]
		public void Format_WithDictionaryParameters()
		{
			var formatter = Interpolator.Compile("$\"{Product} costs {Price:F2}\"");
			var parameters = new Dictionary<string, object>
			{
				{ "Product", "Widget" },
				{ "Price", 9.99m }
			};

			var result = formatter(parameters);
			Assert.Equal("Widget costs 9.99", result);
		}

		// ------------------------------------------------------------------
		// Custom inputParameterName in template
		// ------------------------------------------------------------------

		[Fact]
		public void Interpolate_CustomInputParameterName_ExplicitContextAccess_FormatsCorrectly()
		{
			var result = "$\"Hello, {@ctx.Name}!\"".Interpolate(new { Name = "Bob" }, inputParameterName: "@ctx");
			Assert.Equal("Hello, Bob!", result);
		}

		[Fact]
		public void Format_InvokingFunctionPassedInParameters()
		{
			Func<string, string> transform = name => "Dr. " + name;
			var result = Interpolator.Format(
				"$\"Welcome, {@params.Transform(\"Smith\")}\"",
				new { Transform = transform });

			Assert.Equal("Welcome, Dr. Smith", result);
		}

		[Fact]
		public void Format_InvokingDelegateInInterpolationSlot()
		{
			Func<int, int> doubleValue = x => x * 2;
			var result = Interpolator.Format(
				"$\"Calculated: {@params.DoubleValue(21)}\"",
				new { DoubleValue = doubleValue });

			Assert.Equal("Calculated: 42", result);
		}

		// ------------------------------------------------------------------
		// Compiled delegate is reusable with different inputs
		// ------------------------------------------------------------------

		[Fact]
		public void CompiledFormatter_IsReusableWithDifferentInputs()
		{
			var formatter = Interpolator.Compile("$\"Hi, {Name}!\"");

			Assert.Equal("Hi, Alice!", formatter(new { Name = "Alice" }));
			Assert.Equal("Hi, Bob!", formatter(new { Name = "Bob" }));
		}

		// ------------------------------------------------------------------
		// Guard clauses
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NullTemplate_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => Interpolator.Compile(null));
		}

		[Fact]
		public void Compile_EmptyTemplate_Throws()
		{
			Assert.Throws<ArgumentException>(() => Interpolator.Compile(string.Empty));
		}

		[Fact]
		public void Format_NullParameters_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				Interpolator.Format("$\"{Name}\"", (object)null));
		}

		// ------------------------------------------------------------------
		// Invalid template (non-interpolated string) throws CompilationException
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NonInterpolatedTemplate_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => Interpolator.Compile("\"just a plain string\""));
		}

		// ------------------------------------------------------------------
		// Verbatim interpolated strings (@$"...")
		// ------------------------------------------------------------------

		[Fact]
		public void Format_VerbatimInterpolatedString_NewLine()
		{
			var result = Interpolator.Format(
				"$@\"Line1\nLine2: {Value}\"",
				new { Value = "X" });
			Assert.Contains("Line2: X", result);
		}
	}
}
