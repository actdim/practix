using System;
using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ActDim.Emitron
{
    /// <summary>
    /// Compiles arbitrary C# code and templates into reusable, high-performance cached evaluator delegates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>code</c> string passed to <see cref="Compile{T}(string, string)"/> is a block of C# statements
    /// (or a single expression) that the Roslyn scripting engine compiles into a <c>Func&lt;object, T&gt;</c>.
    /// Inside the code, caller-supplied properties are accessed through a dynamic parameter variable
    /// (default <c>@params</c>, customizable via <c>inputParameterName</c>), e.g.:
    /// <code>
    /// // Single expression
    /// var greet = Emitron.Compile&lt;string&gt;("@params.Name.ToUpper() + \"!\"");
    /// string result = greet(new { Name = "world" }); // → "WORLD!"
    ///
    /// // Multi-statement block with explicit return
    /// var calc = Emitron.Compile&lt;int&gt;("""
    ///     var x = (int)@params.A;
    ///     var y = (int)@params.B;
    ///     return x * x + y * y;
    /// """);
    /// int r = calc(new { A = 3, B = 4 }); // → 25
    /// </code>
    /// </para>
    /// <para>
    /// Compilation is performed <b>once per unique (code, inputParameterName, T) tuple</b> and the resulting delegate
    /// is cached for the lifetime of the process.
    /// </para>
    /// </remarks>
    public static class Emitron
    {
        /// <summary>
        /// Default input parameter variable name bound inside the compiled script.
        /// </summary>
        public const string DefaultInputParameterName = "@params";

        // Cache key = (source code, input parameter name, return Type); value = compiled Delegate (Func<object, T>).
        private static readonly ConcurrentDictionary<(string Code, string InputParameterName, Type ReturnType), Delegate> _cache =
            new ConcurrentDictionary<(string, string, Type), Delegate>();

        /// <summary>
        /// Compiles the given C# <paramref name="code"/> into a reusable <see cref="Func{Object, T}"/> delegate.
        /// </summary>
        /// <typeparam name="T">The expected return type of the code.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="inputParameterName">
        /// The variable name bound to caller inputs inside the script (defaults to <c>@params</c>).
        /// </param>
        /// <returns>A compiled, cached delegate accepting an input object and returning <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="code"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <exception cref="CompilationException">
        /// Thrown when <paramref name="code"/> contains C# syntax or semantic errors.
        /// </exception>
        public static Func<object, T> Compile<T>(string code, string inputParameterName = DefaultInputParameterName)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));
            var normParam = NormalizeInputParameterName(inputParameterName);
            var key = (code, normParam, typeof(T));
            var cached = _cache.GetOrAdd(key, k => CompileInternal<T>(k.Code, k.InputParameterName));
            return (Func<object, T>)cached;
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> and immediately evaluates it
        /// with the given <paramref name="input"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block (see <see cref="Compile{T}(string, string)"/>).</param>
        /// <param name="input">
        /// An object (anonymous type, POCO, <see cref="System.Dynamic.ExpandoObject"/>,
        /// <see cref="System.Collections.Generic.IDictionary{String,Object}"/>, etc.)
        /// whose public properties are accessible via <paramref name="inputParameterName"/> inside the code.
        /// </param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(string code, object input, string inputParameterName = DefaultInputParameterName)
        {
            Guard.Against.Null(input, nameof(input));
            return Compile<T>(code, inputParameterName)(input);
        }

        /// <summary>
        /// Compiles a C# string interpolation template into a reusable formatter delegate.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression, e.g. <c>$"Hello, {Name}!"</c>.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>A compiled, cached formatting delegate.</returns>
        public static Func<object, string> CompileTemplate(string template, string inputParameterName = DefaultInputParameterName)
        {
            return Interpolator.Compile(template, inputParameterName);
        }

        /// <summary>
        /// Compiles and immediately evaluates a C# string interpolation template with the given <paramref name="input"/>.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression, e.g. <c>$"Hello, {Name}!"</c>.</param>
        /// <param name="input">The input parameter bag containing properties matching the interpolation slots.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The formatted result string.</returns>
        public static string Interpolate(string template, object input, string inputParameterName = DefaultInputParameterName)
        {
            return Interpolator.Format(template, input, inputParameterName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        internal static string NormalizeInputParameterName(string inputParameterName)
        {
            return string.IsNullOrWhiteSpace(inputParameterName) ? DefaultInputParameterName : inputParameterName;
        }

        private static Func<object, T> CompileInternal<T>(string code, string inputParameterVar)
        {
            var scriptSource = (inputParameterVar == "@params" || inputParameterVar == "params")
                ? code
                : $"dynamic {inputParameterVar} = @params;\n{code}";

            var script = CSharpScript.Create<T>(
                scriptSource,
                ScriptInternals.DefaultOptions,
                globalsType: typeof(ScriptGlobals));

            ScriptInternals.ThrowOnErrors(code, script.Compile());

            return (inputObj) =>
            {
                var globals = ScriptInternals.BuildGlobals(inputObj);
                return script.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;
            };
        }
    }
}
