using System;
using System.Collections.Concurrent;
using Ardalis.GuardClauses;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ActDim.Emitron
{
    /// <summary>
    /// Compiles arbitrary C# code into a reusable, cached evaluator delegate
    /// that returns a value of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type of the compiled code.</typeparam>
    /// <remarks>
    /// <para>
    /// The <c>code</c> string passed to <see cref="Compile{T}(string)"/> is a block of C# statements
    /// (or a single expression) that the Roslyn scripting engine compiles into a <c>Func&lt;object, T&gt;</c>.
    /// Inside the code, caller-supplied properties are accessed through a special <c>dynamic __emitron_p</c>
    /// variable, e.g.:
    /// <code>
    /// // Single expression
    /// var greet = ScriptEvaluator.Compile&lt;string&gt;("__emitron_p.Name.ToUpper() + \"!\"");
    /// string result = greet(new { Name = "world" }); // → "WORLD!"
    ///
    /// // Multi-statement block with explicit return
    /// var calc = ScriptEvaluator.Compile&lt;int&gt;("""
    ///     var x = (int)__emitron_p.A;
    ///     var y = (int)__emitron_p.B;
    ///     return x * x + y * y;
    /// """);
    /// int r = calc(new { A = 3, B = 4 }); // → 25
    /// </code>
    /// </para>
    /// <para>
    /// Compilation is performed <b>once per unique (code, T) pair</b> and the resulting delegate
    /// is cached for the lifetime of the process.
    /// </para>
    /// </remarks>
    public static class ScriptEvaluator
    {
        // Cache key = (source code, return Type); value = compiled Delegate (Func<object, T>).
        // ValueTuple is used as the key because both string and Type implement structural equality.
        private static readonly ConcurrentDictionary<(string Code, Type ReturnType), Delegate> _cache =
            new ConcurrentDictionary<(string, Type), Delegate>();

        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Compiles <paramref name="code"/> and returns a cached <c>Func&lt;object, <typeparamref name="T"/>&gt;</c>
        /// that accepts a parameter object and evaluates the code, returning a value of
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The return type of the compiled code.</typeparam>
        /// <param name="code">
        /// <para>
        /// A block of C# statements or a single C# expression. The code runs in the context of a
        /// Roslyn script where a <c>dynamic __emitron_p</c> variable is pre-declared and populated from
        /// the caller-supplied parameter object at each invocation.
        /// </para>
        /// <para>
        /// For a <b>single-expression</b> body just write the expression directly, e.g.
        /// <c>"__emitron_p.Price * (1 - __emitron_p.Discount)"</c>.
        /// </para>
        /// <para>
        /// For a <b>multi-statement</b> body include an explicit <c>return</c> statement, e.g.
        /// <c>"var n = (string)__emitron_p.Name; return n.Length &gt; 5 ? n.Substring(0,5) : n;"</c>.
        /// </para>
        /// </param>
        /// <returns>
        /// A compiled, cached <c>Func&lt;object, <typeparamref name="T"/>&gt;</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="code"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <exception cref="CompilationException">
        /// Thrown when <paramref name="code"/> contains C# syntax or semantic errors.
        /// </exception>
        public static Func<object, T> Compile<T>(string code)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));
            var key = (code, typeof(T));
            var cached = _cache.GetOrAdd(key, _ => CompileInternal<T>(code));
            return (Func<object, T>)cached;
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> and immediately evaluates it
        /// with the given <paramref name="parameters"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block (see <see cref="Compile{T}(string)"/>).</param>
        /// <param name="parameters">
        /// An object (anonymous type, POCO, <see cref="System.Dynamic.ExpandoObject"/>,
        /// <see cref="System.Collections.Generic.IDictionary{String,Object}"/>, etc.)
        /// whose public properties are accessible via <c>__emitron_p</c> inside the code.
        /// </param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(string code, object parameters)
        {
            Guard.Against.Null(parameters, nameof(parameters));
            return Compile<T>(code)(parameters);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Func<object, T> CompileInternal<T>(string code)
        {
            // Wrap the caller-supplied code in a preamble that binds __emitron_p to the globals bag.
            // The caller can then reference __emitron_p.Name, __emitron_p.Count, etc.
            var scriptSource = $"dynamic __emitron_p = __emitron_vars;\n{code}";

            var script = CSharpScript.Create<T>(
                scriptSource,
                ScriptInternals.DefaultOptions,
                globalsType: typeof(ScriptGlobals));

            ScriptInternals.ThrowOnErrors(code, script.Compile());

            // Return the cached runner delegate.
            return (parametersObj) =>
            {
                var globals = ScriptInternals.BuildGlobals(parametersObj);
                return script.RunAsync(globals).GetAwaiter().GetResult().ReturnValue;
            };
        }
    }
}
