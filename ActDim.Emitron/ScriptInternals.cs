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
        // Per-Type reflection cache - avoids repeated GetProperties() calls at runtime.
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache =
            new();

        /// <summary>
        /// Default Roslyn script options with references, imports, and search paths common to all evaluators.
        /// </summary>
        internal static ScriptOptions GetDefaultScriptOptions(EmitronOptions? options = null)
        {
            return (options ?? Emitron.DefaultOptions).ToScriptOptions();
        }

        /// <summary>
        /// Prepares the script source code, ensuring that any custom input parameter declaration
        /// is inserted after all Roslyn preprocessor directives (<c>#r</c>, <c>#load</c>, etc.)
        /// and <c>using</c> statements.
        /// </summary>
        internal static string PrepareScriptSource(string code, string inputParameterVar)
        {
            if (string.IsNullOrEmpty(code) || inputParameterVar == "@params" || inputParameterVar == "params")
            {
                return code;
            }

            var insertIndex = FindInjectionIndex(code);
            var decl = $"dynamic {inputParameterVar} = @params;\n";

            if (insertIndex <= 0)
            {
                return decl + code;
            }

            if (insertIndex >= code.Length)
            {
                return code + "\n" + decl;
            }

            return code.Substring(0, insertIndex) + decl + code.Substring(insertIndex);
        }

        private static int FindInjectionIndex(string code)
        {
            var i = 0;
            var len = code.Length;
            var lastValidHeaderEnd = 0;

            while (i < len)
            {
                // 1. Skip whitespace
                while (i < len && char.IsWhiteSpace(code[i]))
                {
                    i++;
                }

                if (i >= len)
                {
                    break;
                }

                // 2. Single-line comment //
                if (i + 1 < len && code[i] == '/' && code[i + 1] == '/')
                {
                    while (i < len && code[i] != '\n')
                    {
                        i++;
                    }

                    if (i < len && code[i] == '\n')
                    {
                        i++;
                    }

                    lastValidHeaderEnd = i;
                    continue;
                }

                // 3. Multi-line comment /* ... */
                if (i + 1 < len && code[i] == '/' && code[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < len && !(code[i] == '*' && code[i + 1] == '/'))
                    {
                        i++;
                    }

                    if (i + 1 < len)
                    {
                        i += 2;
                    }

                    lastValidHeaderEnd = i;
                    continue;
                }

                // 4. Directive starting with '#' (#r, #load, #nullable, #pragma, #define, etc.)
                if (code[i] == '#')
                {
                    while (i < len && code[i] != '\n')
                    {
                        i++;
                    }

                    if (i < len && code[i] == '\n')
                    {
                        i++;
                    }

                    lastValidHeaderEnd = i;
                    continue;
                }

                // 5. Check for using directive (using ..., global using ...)
                if (TryConsumeUsingDirective(code, ref i))
                {
                    lastValidHeaderEnd = i;
                    continue;
                }

                // Any other token means we have reached the script body
                break;
            }

            return lastValidHeaderEnd;
        }

        private static bool TryConsumeUsingDirective(string code, ref int i)
        {
            var len = code.Length;
            var cur = i;

            // Check optional "global"
            if (HasKeyword(code, cur, "global"))
            {
                cur += 6;
                SkipWhitespaceAndComments(code, ref cur);
            }

            if (!HasKeyword(code, cur, "using"))
            {
                return false;
            }

            cur += 5;
            SkipWhitespaceAndComments(code, ref cur);

            if (cur >= len)
            {
                return false;
            }

            // A using directive never has '(' immediately after using: e.g. using (var x = ...)
            if (code[cur] == '(')
            {
                return false;
            }

            // Check optional "static" or "unsafe"
            if (HasKeyword(code, cur, "static"))
            {
                cur += 6;
                SkipWhitespaceAndComments(code, ref cur);
            }
            else if (HasKeyword(code, cur, "unsafe"))
            {
                cur += 6;
                SkipWhitespaceAndComments(code, ref cur);
            }

            if (cur >= len || code[cur] == '(')
            {
                return false;
            }

            // A using directive never starts with "var": e.g. using var x = ...
            if (HasKeyword(code, cur, "var"))
            {
                return false;
            }

            // Scan until semicolon ';'
            // Validate that between cur and ';':
            // 1. No '{' or '}'
            // 2. No "new" keyword
            // 3. If there is '=', verify alias syntax (exactly one identifier before '=')
            // 4. If no '=', verify no '(' before ';'
            var equalsIndex = -1;
            var semiIndex = -1;
            var scan = cur;
            var parenDepth = 0;
            var bracketDepth = 0;

            while (scan < len)
            {
                // Skip comments inside using directive
                if (scan + 1 < len && code[scan] == '/' && code[scan + 1] == '/')
                {
                    scan += 2;
                    while (scan < len && code[scan] != '\n')
                    {
                        scan++;
                    }
                    continue;
                }

                if (scan + 1 < len && code[scan] == '/' && code[scan + 1] == '*')
                {
                    scan += 2;
                    while (scan + 1 < len && !(code[scan] == '*' && code[scan + 1] == '/'))
                    {
                        scan++;
                    }
                    if (scan + 1 < len)
                    {
                        scan += 2;
                    }
                    continue;
                }

                var c = code[scan];

                if (c == '{' || c == '}')
                {
                    return false;
                }

                if (c == '(')
                {
                    // If '(' occurs before '=', this is not a using directive (e.g. using (expr))
                    if (equalsIndex == -1)
                    {
                        return false;
                    }
                    parenDepth++;
                }
                else if (c == ')')
                {
                    parenDepth--;
                }
                else if (c == '<')
                {
                    bracketDepth++;
                }
                else if (c == '>')
                {
                    if (bracketDepth > 0)
                    {
                        bracketDepth--;
                    }
                }
                else if (c == '=' && equalsIndex == -1 && parenDepth == 0 && bracketDepth == 0)
                {
                    // Found '=', check if this is '=='
                    if (scan + 1 < len && code[scan + 1] == '=')
                    {
                        return false;
                    }
                    equalsIndex = scan;
                }
                else if (c == ';' && parenDepth == 0 && bracketDepth == 0)
                {
                    semiIndex = scan;
                    break;
                }
                else if (HasKeyword(code, scan, "new"))
                {
                    return false;
                }

                scan++;
            }

            if (semiIndex == -1)
            {
                return false;
            }

            // If there is an '=', check what is before '=':
            // In a using alias, there must be exactly ONE identifier between cur and equalsIndex:
            // e.g. "using Alias = ..."
            if (equalsIndex != -1)
            {
                var aliasPart = code.Substring(cur, equalsIndex - cur).Trim();
                if (!IsValidIdentifier(aliasPart))
                {
                    return false;
                }
            }

            // Advance index past the semicolon and trailing inline spaces and newline
            var next = semiIndex + 1;
            while (next < len && (code[next] == ' ' || code[next] == '\t' || code[next] == '\r'))
            {
                next++;
            }

            if (next < len && code[next] == '\n')
            {
                next++;
            }

            i = next;
            return true;
        }

        private static bool HasKeyword(string code, int index, string keyword)
        {
            var len = code.Length;
            var klen = keyword.Length;
            if (index + klen > len)
            {
                return false;
            }

            if (string.CompareOrdinal(code, index, keyword, 0, klen) != 0)
            {
                return false;
            }

            if (index + klen < len)
            {
                var nextChar = code[index + klen];
                if (char.IsLetterOrDigit(nextChar) || nextChar == '_')
                {
                    return false;
                }
            }

            return true;
        }

        private static void SkipWhitespaceAndComments(string code, ref int i)
        {
            var len = code.Length;
            while (i < len)
            {
                if (char.IsWhiteSpace(code[i]))
                {
                    i++;
                    continue;
                }

                if (i + 1 < len && code[i] == '/' && code[i + 1] == '/')
                {
                    i += 2;
                    while (i < len && code[i] != '\n')
                    {
                        i++;
                    }
                    if (i < len && code[i] == '\n')
                    {
                        i++;
                    }
                    continue;
                }

                if (i + 1 < len && code[i] == '/' && code[i + 1] == '*')
                {
                    i += 2;
                    while (i + 1 < len && !(code[i] == '*' && code[i + 1] == '/'))
                    {
                        i++;
                    }
                    if (i + 1 < len)
                    {
                        i += 2;
                    }
                    continue;
                }

                break;
            }
        }

        private static bool IsValidIdentifier(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return false;
            }

            var start = 0;
            if (s[0] == '@')
            {
                start = 1;
                if (s.Length == 1)
                {
                    return false;
                }
            }

            if (!char.IsLetter(s[start]) && s[start] != '_')
            {
                return false;
            }

            for (var k = start + 1; k < s.Length; k++)
            {
                if (!char.IsLetterOrDigit(s[k]) && s[k] != '_')
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts <paramref name="parametersObj"/> into a <see cref="ScriptGlobals"/> whose
        /// <see cref="ScriptGlobals.@params"/> property holds the parameter bag.
        /// </summary>
        internal static ScriptGlobals BuildGlobals(object? parametersObj)
        {
            var expando = new ExpandoObject();
            var bag = (IDictionary<string, object?>)expando;

            if (parametersObj != null)
            {
                switch (parametersObj)
                {
                    // ── 1. Dictionary / ExpandoObject ─────────────────────────────────────
                    case IDictionary<string, object?> dict:
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

                    // ── 3. Anonymous type / POCO / record - reflected, cached per Type ────
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
#pragma warning disable IDE1006 // Naming Styles
        public dynamic? @params { get; set; }
#pragma warning restore IDE1006 // Naming Styles
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
