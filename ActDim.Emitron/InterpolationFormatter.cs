using System;
using System.Text;
using Ardalis.GuardClauses;

namespace ActDim.Emitron
{
    /// <summary>
    /// Compiles a C# interpolated-string expression into a reusable, cached formatter delegate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <paramref name="template"/> passed to <see cref="Compile"/> must be a valid C# interpolated-string
    /// expression, for example: <code>$"Hello, {Name}! You have {Count} messages."</code>
    /// </para>
    /// <para>
    /// Internally the template is rewritten so that every interpolation slot is prefixed with
    /// <c>__emitron_p.</c> and then compiled via <see cref="ScriptEvaluator.Compile{T}"/>.
    /// Compilation is performed <b>once per unique template</b>; the resulting
    /// <c>Func&lt;object, string&gt;</c> is cached for the lifetime of the process.
    /// </para>
    /// <para>
    /// Parameters are supplied through any object whose public properties (or dictionary keys)
    /// match the interpolation slots.  See <see cref="ScriptInternals.BuildGlobals"/> for the
    /// full list of supported parameter source types.
    /// </para>
    /// </remarks>
    public static class InterpolationFormatter
    {
        /// <summary>
        /// Compiles <paramref name="template"/> and returns a cached <c>Func&lt;object, string&gt;</c>
        /// that accepts a parameter object and produces the formatted string.
        /// </summary>
        /// <param name="template">
        /// A C# interpolated-string expression, e.g. <c>$"Hello, {Name}!"</c>.
        /// Interpolation slots may contain full C# expressions: <c>{Date:dd.MM.yy}</c>,
        /// <c>{Items.Count}</c>, <c>{Name.ToUpper()}</c>.
        /// </param>
        /// <returns>
        /// A compiled, cached <c>Func&lt;object, string&gt;</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="template"/> is <see langword="null"/> or whitespace.
        /// </exception>
        /// <exception cref="CompilationException">
        /// Thrown when the template contains C# syntax or semantic errors.
        /// </exception>
        public static Func<object, string> Compile(string template)
        {
            Guard.Against.NullOrWhiteSpace(template, nameof(template));
            // Rewrite the interpolation slots and delegate compilation + caching to ScriptEvaluator.
            var code = BuildCode(template);
            return ScriptEvaluator.Compile<string>(code);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="template"/> and immediately formats it
        /// with <paramref name="parameters"/>.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression.</param>
        /// <param name="parameters">
        /// An object (anonymous type, POCO, <see cref="System.Dynamic.ExpandoObject"/>,
        /// <see cref="System.Collections.Generic.IDictionary{String,Object}"/>, etc.)
        /// whose public properties are exposed as globals inside the interpolated string.
        /// </param>
        /// <returns>The formatted result string.</returns>
        public static string Format(string template, object parameters)
        {
            Guard.Against.Null(parameters, nameof(parameters));
            return Compile(template)(parameters);
        }

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Rewrites the template's interpolation slots with an <c>__emitron_p.</c> prefix and wraps
        /// the result in a <c>return …;</c> statement suitable for <see cref="ScriptEvaluator"/>.
        /// </summary>
        private static string BuildCode(string template)
        {
            var rewritten = RewriteInterpolationSlots(template);
            return $"return {rewritten};";
        }

        /// <summary>
        /// Rewrites a C# interpolated-string literal so that every interpolation slot's leading
        /// identifier is prefixed with <c>__emitron_p.</c>, giving the script access to the caller's
        /// parameter bag.
        /// </summary>
        /// <remarks>
        /// For example <c>$"Hello, {Name}! Items: {Items.Count}"</c> becomes
        /// <c>$"Hello, {__emitron_p.Name}! Items: {__emitron_p.Items.Count}"</c>.
        /// Only the leading simple identifier is prefixed; nested expressions, method calls,
        /// operators, and format specifiers are left intact.
        /// </remarks>
        private static string RewriteInterpolationSlots(string template)
        {
            var trimmed = template.TrimStart();
            if (!trimmed.StartsWith("$", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "The template must be a C# interpolated-string expression starting with '$', e.g. $\"Hello, {Name}!\".",
                    nameof(template));
            }

            var sb = new StringBuilder();
            var i = 0;

            // Copy the $" prefix (and optional @)
            while (i < template.Length && (template[i] == '$' || template[i] == '@'))
            {
                sb.Append(template[i]);
                i++;
            }

            if (i >= template.Length || (template[i] != '"' && template[i] != '\''))
            {
                throw new ArgumentException("The template must be a quoted interpolated string.", nameof(template));
            }

            var isVerbatim = sb.ToString().Contains('@');
            sb.Append(template[i]); // opening quote
            i++;

            while (i < template.Length)
            {
                var c = template[i];

                if (!isVerbatim && c == '\\')
                {
                    sb.Append(c);
                    i++;
                    if (i < template.Length)
                    {
                        sb.Append(template[i]);
                        i++;
                    }

                    continue;
                }

                if (c == '"')
                {
                    if (isVerbatim && i + 1 < template.Length && template[i + 1] == '"')
                    {
                        sb.Append("\"\"");
                        i += 2;
                        continue;
                    }

                    sb.Append(c);
                    i++;
                    break;
                }

                if (c == '{')
                {
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        sb.Append("{{");
                        i += 2;
                        continue;
                    }

                    sb.Append('{');
                    i++;

                    var holeStart = i;
                    var depth = 1;
                    while (i < template.Length && depth > 0)
                    {
                        if (template[i] == '{') { depth++; }
                        else if (template[i] == '}') { depth--; }

                        if (depth > 0)
                        {
                            i++;
                        }
                    }

                    var slot = template.Substring(holeStart, i - holeStart);
                    sb.Append(PrefixLeadingIdentifier(slot));
                    sb.Append('}');
                    i++; // skip '}'
                    continue;
                }

                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    sb.Append("}}");
                    i += 2;
                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private static string PrefixLeadingIdentifier(string slot)
        {
            var j = 0;
            while (j < slot.Length && (char.IsLetterOrDigit(slot[j]) || slot[j] == '_'))
            {
                j++;
            }

            if (j == 0)
            {
                return slot;
            }

            return "__emitron_p." + slot;
        }
    }
}
