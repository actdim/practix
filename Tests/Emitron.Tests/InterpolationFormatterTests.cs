using System;
using System.Collections.Generic;
using Xunit;

namespace ActDim.Emitron.Tests
{
	public class InterpolationFormatterTests
	{
		// ------------------------------------------------------------------
		// Compile → returns a reusable Func<object,string>
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_ReturnsNonNullDelegate()
		{
			var formatter = InterpolationFormatter.Compile("$\"{Name}\"");
			Assert.NotNull(formatter);
		}

		[Fact]
		public void Compile_SameTemplate_ReturnsCachedDelegate()
		{
			const string template = "$\"{Value}\"";
			var first = InterpolationFormatter.Compile(template);
			var second = InterpolationFormatter.Compile(template);
			Assert.Same(first, second);
		}

		// ------------------------------------------------------------------
		// Basic interpolation with anonymous objects
		// ------------------------------------------------------------------

		[Fact]
		public void Format_SimpleStringProperty()
		{
			var result = InterpolationFormatter.Format("$\"Hello, {Name}!\"", new { Name = "World" });
			Assert.Equal("Hello, World!", result);
		}

		[Fact]
		public void Format_IntegerProperty()
		{
			var result = InterpolationFormatter.Format("$\"Count: {Count}\"", new { Count = 42 });
			Assert.Equal("Count: 42", result);
		}

		[Fact]
		public void Format_MultipleProperties()
		{
			var result = InterpolationFormatter.Format(
				"$\"{FirstName} {LastName} is {Age} years old.\"",
				new { FirstName = "Jane", LastName = "Doe", Age = 30 });

			Assert.Equal("Jane Doe is 30 years old.", result);
		}

		// ------------------------------------------------------------------
		// Format specifiers (e.g. {Price:C2}, {Date:dd.MM.yy})
		// ------------------------------------------------------------------

		[Fact]
		public void Format_WithFormatSpecifier_DateTime()
		{
			var date = new DateTime(2024, 3, 15);
			var result = InterpolationFormatter.Format("$\"{Date:dd.MM.yy}\"", new { Date = date });
			Assert.Equal(date.ToString("dd.MM.yy"), result);
		}

		[Fact]
		public void Format_WithFormatSpecifier_Numeric()
		{
			var result = InterpolationFormatter.Format("$\"{Value:D6}\"", new { Value = 42 });
			Assert.Equal("000042", result);
		}

		// ------------------------------------------------------------------
		// Expressions inside holes (property chains, method calls)
		// ------------------------------------------------------------------

		[Fact]
		public void Format_PropertyChainExpression()
		{
			var result = InterpolationFormatter.Format(
				"$\"Length: {Text.Length}\"",
				new { Text = "hello" });
			Assert.Equal("Length: 5", result);
		}

		[Fact]
		public void Format_MethodCallExpression()
		{
			var result = InterpolationFormatter.Format(
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
			var formatter = InterpolationFormatter.Compile("$\"{Product} costs {Price:C}\"");
			var parameters = new Dictionary<string, object>
			{
				{ "Product", "Widget" },
				{ "Price", 9.99m }
			};

			var result = formatter(parameters);
			Assert.Equal($"Widget costs {9.99m:C}", result);
		}

		// ------------------------------------------------------------------
		// Compiled delegate is reusable with different inputs
		// ------------------------------------------------------------------

		[Fact]
		public void CompiledFormatter_IsReusableWithDifferentInputs()
		{
			var formatter = InterpolationFormatter.Compile("$\"Hi, {Name}!\"");

			Assert.Equal("Hi, Alice!", formatter(new { Name = "Alice" }));
			Assert.Equal("Hi, Bob!", formatter(new { Name = "Bob" }));
		}

		// ------------------------------------------------------------------
		// Guard clauses
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NullTemplate_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => InterpolationFormatter.Compile(null));
		}

		[Fact]
		public void Compile_EmptyTemplate_Throws()
		{
			Assert.Throws<ArgumentException>(() => InterpolationFormatter.Compile(string.Empty));
		}

		[Fact]
		public void Format_NullParameters_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				InterpolationFormatter.Format("$\"{Name}\"", (object)null));
		}

		// ------------------------------------------------------------------
		// Invalid template (non-interpolated string) throws CompilationException
		// ------------------------------------------------------------------

		[Fact]
		public void Compile_NonInterpolatedTemplate_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => InterpolationFormatter.Compile("\"just a plain string\""));
		}

		// ------------------------------------------------------------------
		// Verbatim interpolated strings (@$"...")
		// ------------------------------------------------------------------

		[Fact]
		public void Format_VerbatimInterpolatedString_NewLine()
		{
			var result = InterpolationFormatter.Format(
				"$@\"Line1\nLine2: {Value}\"",
				new { Value = "X" });
			Assert.Contains("Line2: X", result);
		}
	}
}
