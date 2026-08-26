using System;
using Ardalis.GuardClauses;

namespace ActDim.Emitron.Razor.Extensions
{
    /// <summary>
    /// Extension methods for Razor template compilation and formatting.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Compiles and formats the Razor template using properties from <paramref name="model"/>.
        /// </summary>
        /// <param name="template">The Razor syntax template string.</param>
        /// <param name="model">The model input object.</param>
        /// <param name="inputParameterName">The variable name bound to model inputs (defaults to <c>@params</c>).</param>
        /// <returns>The formatted result string.</returns>
        public static string FormatRazor(
            this string template,
            object model,
            string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.Null(template, nameof(template));
            Guard.Against.Null(model, nameof(model));

            return EmitronRazor.Format(template, model, inputParameterName);
        }

        /// <summary>
        /// Compiles the Razor template string into a cached evaluator delegate.
        /// </summary>
        /// <param name="template">The Razor syntax template string.</param>
        /// <param name="inputParameterName">The variable name bound to model inputs (defaults to <c>@params</c>).</param>
        /// <returns>A compiled, cached delegate accepting an input model object.</returns>
        public static Func<object, string> CompileRazor(
            this string template,
            string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.Null(template, nameof(template));

            return EmitronRazor.Compile(template, inputParameterName);
        }
    }
}

