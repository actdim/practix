using System;
using System.Collections.Generic;
using ActDim.Emitron.Razor;
using ActDim.Emitron.Razor.Extensions;
using Xunit;

namespace ActDim.Emitron.Razor.Tests
{
    public class EmitronRazorTests
    {
        [Fact]
        public void Format_SimpleProperties_RendersCorrectly()
        {
            var template = "Hello @Model.Name, welcome to @Model.City!";
            var model = new { Name = "Alice", City = "Prague" };

            var result = EmitronRazor.Format(template, model);

            Assert.Equal("Hello Alice, welcome to Prague!", result);
        }

        [Fact]
        public void Format_ParenthesizedExpression_RendersCorrectly()
        {
            var template = "Total: $@((Model.Price * Model.Quantity).ToString(\"F2\"))";
            var model = new { Price = 15.5m, Quantity = 4 };

            var result = EmitronRazor.Format(template, model);

            Assert.Equal("Total: $62.00", result);
        }

        [Fact]
        public void Format_CommentsAndEscapedAt_HandledCorrectly()
        {
            var template = "Email us at support@@example.com @* Ignore this *@ - User: @Model.User";
            var model = new { User = "Bob" };

            var result = EmitronRazor.Format(template, model);

            Assert.Equal("Email us at support@example.com  - User: Bob", result);
        }

        [Fact]
        public void Format_ConditionalIfElse_RendersCorrectly()
        {
            var template = """
                @if (Model.IsAdmin) {
                    ADMIN PANEL
                } else if (Model.IsVip) {
                    VIP DASHBOARD
                } else {
                    USER DASHBOARD
                }
                """;

            var adminResult = EmitronRazor.Format(template, new { IsAdmin = true, IsVip = false }).Trim();
            var vipResult = EmitronRazor.Format(template, new { IsAdmin = false, IsVip = true }).Trim();
            var userResult = EmitronRazor.Format(template, new { IsAdmin = false, IsVip = false }).Trim();

            Assert.Equal("ADMIN PANEL", adminResult);
            Assert.Equal("VIP DASHBOARD", vipResult);
            Assert.Equal("USER DASHBOARD", userResult);
        }

        [Fact]
        public void Format_ForeachLoop_RendersCorrectly()
        {
            var template = """
                Items:
                @foreach (var item in Model.Items) {
                - @item
                }
                """;

            var model = new { Items = new[] { "Apple", "Banana", "Cherry" } };
            var result = EmitronRazor.Format(template, model);

            Assert.Contains("- Apple", result);
            Assert.Contains("- Banana", result);
            Assert.Contains("- Cherry", result);
        }

        [Fact]
        public void Format_CodeBlock_ExecutesStatements()
        {
            var template = """
                @{
                    var doubleValue = (int)Model.Value * 2;
                }
                Result: @doubleValue
                """;

            var result = EmitronRazor.Format(template, new { Value = 21 }).Trim();

            Assert.Equal("Result: 42", result);
        }

        [Fact]
        public void ExtensionMethod_FormatRazor_WorksSeamlessly()
        {
            var template = "Order #@Model.OrderId for @Model.Customer";
            var model = new { OrderId = 1001, Customer = "Charlie" };

            var result = template.FormatRazor(model);

            Assert.Equal("Order #1001 for Charlie", result);
        }

        [Fact]
        public void Compile_ReturnsCachedDelegate()
        {
            var template = "Greeting: @Model.Greeting";
            var formatter1 = EmitronRazor.Compile(template);
            var formatter2 = EmitronRazor.Compile(template);

            Assert.Same(formatter1, formatter2);
            Assert.Equal("Greeting: Hi", formatter1(new { Greeting = "Hi" }));
        }
    }
}

