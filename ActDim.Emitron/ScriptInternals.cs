using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace ActDim.Emitron
{
    /// <summary>
    /// Shared infrastructure for Roslyn-script based evaluators in this assembly.
    /// </summary>
    internal static class ScriptInternals
    {
        // Per-Type reflection cache — avoids repeated GetProperties() calls at runtime.
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache =
            new ConcurrentDictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Default Roslyn script options with references and imports common to all evaluators.
        /// </summary>
        internal static readonly ScriptOptions DefaultOptions = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,
                typeof(Enumerable).Assembly,
                typeof(ExpandoObject).Assembly,
                typeof(Microsoft.CSharp.RuntimeBinder.Binder).Assembly)
            .WithImports(
                "System",
                "System.Linq",
                "System.Collections.Generic",
                "System.Dynamic")
            .WithOptimizationLevel(OptimizationLevel.Release);

        /// <summary>
        /// Converts <paramref name="parametersObj"/> into a <see cref="ScriptGlobals"/> whose
        /// <see cref="ScriptGlobals.@params"/> property holds the parameter bag.
        /// </summary>
        internal static ScriptGlobals BuildGlobals(object parametersObj)
        {
            var expando = new ExpandoObject();
            var bag = (IDictionary<string, object>)expando;

            if (parametersObj != null)
            {
                switch (parametersObj)
                {
                    // ── 1. Dictionary / ExpandoObject ─────────────────────────────────────
                    case IDictionary<string, object> dict:
                    {
                        foreach (var pair in dict)
                        {
                            bag[pair.Key] = pair.Value;
                        }

                        break;
                    }

                    // ── 2. DynamicObject subclass ──────────────────────────────────────────
                    case DynamicObject dynObj:
                    {
                        foreach (var name in dynObj.GetDynamicMemberNames())
                        {
                            var binder = Microsoft.CSharp.RuntimeBinder.Binder.GetMember(
                                Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.None,
                                name,
                                dynObj.GetType(),
                                [Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(
                                    Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null)]);

                            var site = System.Runtime.CompilerServices
                                .CallSite<Func<System.Runtime.CompilerServices.CallSite, object, object>>
                                .Create(binder);

                            bag[name] = site.Target(site, dynObj);
                        }

                        break;
                    }

                    // ── 3. Anonymous type / POCO / record — reflected, cached per Type ────
                    default:
                    {
                        var props = _propertyCache.GetOrAdd(
                            parametersObj.GetType(),
                            t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                   .Where(p => p.CanRead)
                                   .ToArray());

                        foreach (var prop in props)
                        {
                            bag[prop.Name] = prop.GetValue(parametersObj);
                        }

                        break;
                    }
                }
            }

            return new ScriptGlobals { @params = expando };
        }

        /// <summary>
        /// Throws <see cref="CompilationException"/> if <paramref name="diagnostics"/> contains
        /// any error-severity entries.
        /// </summary>
        internal static void ThrowOnErrors(
            string source,
            System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics)
        {
            var sb = new StringBuilder();
            foreach (var d in diagnostics)
            {
                if (d.Severity == DiagnosticSeverity.Error)
                {
                    sb.AppendLine(d.ToString());
                }
            }

            if (sb.Length > 0)
            {
                throw new CompilationException(source, sb.ToString());
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────
    // Shared types
    // ─────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Globals type injected into every compiled Roslyn script.
    /// Exposes caller properties via the dynamic <see cref="@params"/> property (accessible as <c>@params</c>).
    /// </summary>
    public class ScriptGlobals
    {
        /// <summary>
        /// Dynamic parameter bag exposed to the script as <c>@params</c> / <c>params</c>.
        /// </summary>
        public dynamic @params { get; set; }
    }

    /// <summary>
    /// Exception thrown when Roslyn fails to compile a script.
    /// </summary>
    public sealed class CompilationException : Exception
    {
        /// <summary>Gets the source code that failed to compile.</summary>
        public string SourceCode { get; }

        /// <summary>Gets the Roslyn diagnostic messages.</summary>
        public string Diagnostics { get; }

        /// <inheritdoc />
        public CompilationException(string source, string diagnostics)
            : base($"Roslyn compilation failed.\nSource:\n{source}\nDiagnostics:\n{diagnostics}")
        {
            SourceCode = source;
            Diagnostics = diagnostics;
        }
    }
}
