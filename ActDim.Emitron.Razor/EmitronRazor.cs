using System;
using Ardalis.GuardClauses;

namespace ActDim.Emitron.Razor
{
    /// <summary>
    /// Compiles Razor-syntax templates into reusable, high-performance cached evaluator delegates using <see cref="Emitron"/>.
    /// </summary>
    public static class EmitronRazor
    {
        /// <summary>
        /// Compiles <paramref name="razorTemplate"/> into a cached <see cref="Func{Object, String}"/> delegate.
        /// </summary>
        /// <param name="razorTemplate">The Razor syntax template string.</param>
        /// <param name="inputParameterName">The parameter variable name bound inside the template (defaults to <c>@params</c>).</param>
        /// <param name="options">Optional compilation options.</param>
        /// <returns>A compiled, cached delegate that accepts a model input object and returns formatted string output.</returns>
        public static Func<object, string> Compile(
            string razorTemplate,
            string inputParameterName = Emitron.DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            Guard.Against.NullOrWhiteSpace(razorTemplate, nameof(razorTemplate));
            var normParam = Emitron.NormalizeInputParameterName(inputParameterName);
            var code = RazorParser.Transpile(razorTemplate, normParam);
            return Emitron.Compile<string>(code, normParam, options);
        }

        /// <summary>
        /// Compiles <paramref name="razorTemplate"/> with the specified <paramref name="options"/>
        /// and returns a cached <see cref="Func{Object, String}"/>.
        /// </summary>
        /// <param name="razorTemplate">The Razor syntax template string.</param>
        /// <param name="options">Compilation options (references, imports, search paths).</param>
        /// <returns>A compiled, cached delegate accepting an input model object.</returns>
        public static Func<object, string> Compile(string razorTemplate, EmitronOptions options)
        {
            return Compile(razorTemplate, Emitron.DefaultInputParameterName, options);
        }

        /// <summary>
        /// Compiles <paramref name="razorTemplate"/> and immediately formats it using <paramref name="model"/>.
        /// </summary>
        /// <param name="razorTemplate">The Razor syntax template string.</param>
        /// <param name="model">The input model object containing values accessed in the template.</param>
        /// <param name="inputParameterName">The variable name bound to model inputs (defaults to <c>@params</c>).</param>
        /// <param name="options">Optional compilation options.</param>
        /// <returns>The formatted result string.</returns>
        public static string Format(
            string razorTemplate,
            object model,
            string inputParameterName = Emitron.DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            Guard.Against.Null(model, nameof(model));
            return Compile(razorTemplate, inputParameterName, options)(model);
        }

        /// <summary>
        /// Compiles <paramref name="razorTemplate"/> with <paramref name="options"/> and immediately formats it using <paramref name="model"/>.
        /// </summary>
        /// <param name="razorTemplate">The Razor syntax template string.</param>
        /// <param name="model">The input model object.</param>
        /// <param name="options">Compilation options (references, imports, search paths).</param>
        /// <returns>The formatted result string.</returns>
        public static string Format(string razorTemplate, object model, EmitronOptions options)
        {
            return Format(razorTemplate, model, Emitron.DefaultInputParameterName, options);
        }
    }
}

