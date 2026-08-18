#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Ardalis.GuardClauses;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace ActDim.Emitron
{
    /// <summary>
    /// Compiles arbitrary C# code and templates into reusable, high-performance cached evaluator delegates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>code</c> string passed to <see cref="Compile{T}(string, string, EmitronOptions?)"/> is a block of C# statements
    /// (or a single expression) that the Roslyn scripting engine compiles into a <c>Func&lt;object, T&gt;</c>.
    /// Inside the code, caller-supplied properties are accessed through a dynamic parameter variable
    /// (default <c>@params</c>, customizable via <c>inputParameterName</c>), e.g.:
    /// <code>
    /// // String template compilation
    /// var formatInvoice = Emitron.CompileTemplate("$\"Invoice #{InvoiceId:D6}: {ClientName} - ${Total:N2}\"");
    /// string summary = formatInvoice(new { InvoiceId = 42, ClientName = "Acme", Total = 1250.50 });
    /// // → "Invoice #000042: Acme - $1,250.50"
    ///
    /// // Multi-statement business calculation
    /// var calcDiscount = Emitron.Compile&lt;decimal&gt;("""
    ///     var total = (decimal)@params.Total;
    ///     var isVip = (bool)@params.IsVip;
    ///     return isVip ? total * 0.15m : total * 0.05m;
    /// """);
    /// decimal discount = calcDiscount(new { Total = 500m, IsVip = true }); // → 75.00
    /// </code>
    /// </para>
    /// <para>
    /// Scripts also support Roslyn directives such as <c>#r "AssemblyName"</c> (resolved via <see cref="EmitronOptions.SearchPaths"/>)
    /// and <c>using Namespace;</c> statements:
    /// <code>
    /// var parseJson = Emitron.Compile&lt;string&gt;("""
    ///     #r "System.Text.Json"
    ///     using System.Text.Json;
    ///     return JsonDocument.Parse((string)@params.Json).RootElement.GetProperty("title").GetString();
    /// """);
    /// </code>
    /// </para>
    /// <para>
    /// Compilation is performed <b>once per unique (code, inputParameterName, T, options) tuple</b> and the resulting delegate
    /// is cached for the lifetime of the process.
    /// </para>
    /// </remarks>
    public static class Emitron
    {
        /// <summary>
        /// Default input parameter variable name bound inside the compiled script.
        /// </summary>
        public const string DefaultInputParameterName = "@params";

        /// <summary>
        /// Gets or sets the global default configuration options used for script compilation.
        /// </summary>
        public static EmitronOptions DefaultOptions { get; set; } = new EmitronOptions();

        // Cache key = (source code, input parameter name, return Type, options); value = compiled Delegate (Func<object, T>).
        private static readonly ConcurrentDictionary<(string Code, string InputParameterName, Type ReturnType, EmitronOptions Options), Delegate> _cache =
            new ConcurrentDictionary<(string, string, Type, EmitronOptions), Delegate>();

        /// <summary>
        /// Compiles the given C# <paramref name="code"/> into a reusable <see cref="Func{Object, T}"/> delegate.
        /// </summary>
        /// <typeparam name="T">The expected return type of the code.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="inputParameterName">
        /// The variable name bound to caller inputs inside the script (defaults to <c>@params</c>).
        /// </param>
        /// <param name="options">
        /// Optional compilation options (assemblies, usings, search paths). If <see langword="null"/>, <see cref="DefaultOptions"/> is used.
        /// </param>
        /// <returns>A compiled, cached delegate accepting an input object and returning <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="code"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <exception cref="CompilationException">
        /// Thrown when <paramref name="code"/> contains C# syntax or semantic errors.
        /// </exception>
        public static Func<object, T> Compile<T>(
            string code,
            string inputParameterName = DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            Guard.Against.NullOrWhiteSpace(code, nameof(code));
            var normParam = NormalizeInputParameterName(inputParameterName);
            var effectiveOptions = options ?? DefaultOptions;
            var key = (code, normParam, typeof(T), effectiveOptions);
            var cached = _cache.GetOrAdd(key, k => CompileInternal<T>(k.Code, k.InputParameterName, k.Options));
            return (Func<object, T>)cached;
        }

        /// <summary>
        /// Compiles the given C# <paramref name="code"/> into a reusable <see cref="Func{Object, T}"/> delegate
        /// using the specified <paramref name="options"/>.
        /// </summary>
        /// <typeparam name="T">The expected return type of the code.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="options">Compilation options (assemblies, usings, search paths).</param>
        /// <returns>A compiled, cached delegate accepting an input object and returning <typeparamref name="T"/>.</returns>
        public static Func<object, T> Compile<T>(string code, EmitronOptions options)
        {
            return Compile<T>(code, DefaultInputParameterName, options);
        }

        /// <summary>
        /// Compiles the given C# <paramref name="code"/> into a reusable <see cref="Func{Object, T}"/> delegate
        /// with caller-specified <paramref name="assemblies"/> and <paramref name="usings"/>.
        /// </summary>
        /// <typeparam name="T">The expected return type of the code.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="assemblies">Assemblies to reference in the script.</param>
        /// <param name="usings">Namespaces to import in the script via <c>using</c>.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>A compiled, cached delegate accepting an input object and returning <typeparamref name="T"/>.</returns>
        public static Func<object, T> Compile<T>(
            string code,
            IEnumerable<Assembly> assemblies,
            IEnumerable<string>? usings = null,
            string inputParameterName = DefaultInputParameterName)
        {
            var options = DefaultOptions.Clone();
            if (assemblies != null)
            {
                foreach (var asm in assemblies)
                {
                    options.AddAssemblies(asm);
                }
            }

            if (usings != null)
            {
                foreach (var ns in usings)
                {
                    options.AddUsings(ns);
                }
            }

            return Compile<T>(code, inputParameterName, options);
        }

        /// <summary>
        /// Compiles the given C# <paramref name="code"/> into a reusable <see cref="Func{Object, T}"/> delegate
        /// with references containing the specified <paramref name="types"/> and <paramref name="usings"/>.
        /// </summary>
        /// <typeparam name="T">The expected return type of the code.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="types">Types whose containing assemblies should be referenced in the script.</param>
        /// <param name="usings">Namespaces to import in the script via <c>using</c>.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>A compiled, cached delegate accepting an input object and returning <typeparamref name="T"/>.</returns>
        public static Func<object, T> Compile<T>(
            string code,
            IEnumerable<Type> types,
            IEnumerable<string>? usings = null,
            string inputParameterName = DefaultInputParameterName)
        {
            var options = DefaultOptions.Clone();
            if (types != null)
            {
                foreach (var t in types)
                {
                    options.AddAssemblies(t);
                }
            }

            if (usings != null)
            {
                foreach (var ns in usings)
                {
                    options.AddUsings(ns);
                }
            }

            return Compile<T>(code, inputParameterName, options);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> and immediately evaluates it
        /// with the given <paramref name="input"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block (see <see cref="Compile{T}(string, string, EmitronOptions?)"/>).</param>
        /// <param name="input">
        /// An object (anonymous type, POCO, <see cref="System.Dynamic.ExpandoObject"/>,
        /// <see cref="System.Collections.Generic.IDictionary{String,Object}"/>, etc.)
        /// whose public properties are accessible via <paramref name="inputParameterName"/> inside the code.
        /// </param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <param name="options">
        /// Optional compilation options (assemblies, usings, search paths). If <see langword="null"/>, <see cref="DefaultOptions"/> is used.
        /// </param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(
            string code,
            object input,
            string inputParameterName = DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            Guard.Against.Null(input, nameof(input));
            return Compile<T>(code, inputParameterName, options)(input);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> with the specified <paramref name="options"/>
        /// and immediately evaluates it with the given <paramref name="input"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="input">The input parameter bag containing properties.</param>
        /// <param name="options">Compilation options (assemblies, usings, search paths).</param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(string code, object input, EmitronOptions options)
        {
            return Evaluate<T>(code, input, DefaultInputParameterName, options);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> with <paramref name="assemblies"/> and <paramref name="usings"/>,
        /// and immediately evaluates it with the given <paramref name="input"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="input">The input parameter bag containing properties.</param>
        /// <param name="assemblies">Assemblies to reference in the script.</param>
        /// <param name="usings">Namespaces to import in the script via <c>using</c>.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(
            string code,
            object input,
            IEnumerable<Assembly> assemblies,
            IEnumerable<string>? usings = null,
            string inputParameterName = DefaultInputParameterName)
        {
            Guard.Against.Null(input, nameof(input));
            return Compile<T>(code, assemblies, usings, inputParameterName)(input);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="code"/> with <paramref name="types"/> and <paramref name="usings"/>,
        /// and immediately evaluates it with the given <paramref name="input"/> object.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="code">A C# expression or statement block.</param>
        /// <param name="input">The input parameter bag containing properties.</param>
        /// <param name="types">Types whose containing assemblies should be referenced in the script.</param>
        /// <param name="usings">Namespaces to import in the script via <c>using</c>.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The evaluated value of type <typeparamref name="T"/>.</returns>
        public static T Evaluate<T>(
            string code,
            object input,
            IEnumerable<Type> types,
            IEnumerable<string>? usings = null,
            string inputParameterName = DefaultInputParameterName)
        {
            Guard.Against.Null(input, nameof(input));
            return Compile<T>(code, types, usings, inputParameterName)(input);
        }

        /// <summary>
        /// Compiles a C# string interpolation template into a reusable formatter delegate.
        /// </summary>
        /// <param name="template">
        /// A C# interpolated-string expression, e.g. <c>$"Hello, {Name}! Balance: {Balance:C2}"</c>.
        /// Properties can be referenced directly by name (e.g. <c>{Name}</c>) or via full C# expressions.
        /// </param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <param name="options">
        /// Optional compilation options (assemblies, usings, search paths). If <see langword="null"/>, <see cref="DefaultOptions"/> is used.
        /// </param>
        /// <returns>A compiled, cached formatting delegate.</returns>
        public static Func<object, string> CompileTemplate(
            string template,
            string inputParameterName = DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            return Interpolator.Compile(template, inputParameterName, options);
        }

        /// <summary>
        /// Compiles a C# string interpolation template into a reusable formatter delegate with the given <paramref name="options"/>.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression.</param>
        /// <param name="options">Compilation options (assemblies, usings, search paths).</param>
        /// <returns>A compiled, cached formatting delegate.</returns>
        public static Func<object, string> CompileTemplate(string template, EmitronOptions options)
        {
            return Interpolator.Compile(template, DefaultInputParameterName, options);
        }

        /// <summary>
        /// Compiles and immediately evaluates a C# string interpolation template with the given <paramref name="input"/>.
        /// </summary>
        /// <param name="template">
        /// A C# interpolated-string expression, e.g. <c>$"Hello, {Name}! Balance: {Balance:C2}"</c>.
        /// Properties can be referenced directly by name (e.g. <c>{Name}</c>) or via full C# expressions.
        /// </param>
        /// <param name="input">The input parameter bag containing properties matching the interpolation slots.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <param name="options">
        /// Optional compilation options (assemblies, usings, search paths). If <see langword="null"/>, <see cref="DefaultOptions"/> is used.
        /// </param>
        /// <returns>The formatted result string.</returns>
        public static string Interpolate(
            string template,
            object input,
            string inputParameterName = DefaultInputParameterName,
            EmitronOptions? options = null)
        {
            return Interpolator.Format(template, input, inputParameterName, options);
        }

        /// <summary>
        /// Compiles and immediately evaluates a C# string interpolation template with the given <paramref name="input"/> and <paramref name="options"/>.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression.</param>
        /// <param name="input">The input parameter bag containing properties matching the interpolation slots.</param>
        /// <param name="options">Compilation options (assemblies, usings, search paths).</param>
        /// <returns>The formatted result string.</returns>
        public static string Interpolate(string template, object input, EmitronOptions options)
        {
            return Interpolator.Format(template, input, DefaultInputParameterName, options);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        internal static string NormalizeInputParameterName(string inputParameterName)
        {
            return string.IsNullOrWhiteSpace(inputParameterName) ? DefaultInputParameterName : inputParameterName;
        }

        private static Func<object, T> CompileInternal<T>(string code, string inputParameterVar, EmitronOptions? options)
        {
            var scriptSource = ScriptInternals.PrepareScriptSource(code, inputParameterVar);
            var scriptOptions = ScriptInternals.GetDefaultScriptOptions(options);

            var script = CSharpScript.Create<T>(
                scriptSource,
                scriptOptions,
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
