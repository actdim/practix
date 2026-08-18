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
    /// The <c>template</c> passed to <see cref="Compile"/> must be a valid C# interpolated-string
    /// expression, for example: <code>$"Hello, {Name}! You have {Count} messages."</code>
    /// </para>
    /// <para>
    /// Internally the template is rewritten so that every interpolation slot is prefixed with
    /// inputParameterName (default <c>@params.</c>) and compiled via <see cref="Emitron.Compile{T}"/>.
    /// </para>
    /// </remarks>
    public static class Interpolator
    {
        /// <summary>
        /// Compiles <paramref name="template"/> and returns a cached <c>Func&lt;object, string&gt;</c>
        /// that accepts an input object and produces the formatted string.
        /// </summary>
        /// <param name="template">
        /// A C# interpolated-string expression, e.g. <c>$"Hello, {Name}!"</c>.
        /// Interpolation slots may contain full C# expressions: <c>{Date:dd.MM.yy}</c>,
        /// <c>{Items.Count}</c>, <c>{Name.ToUpper()}</c>.
        /// </param>
        /// <param name="inputParameterName">
        /// The variable name bound to caller inputs inside the template (defaults to <c>@params</c>).
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
        public static Func<object, string> Compile(string template, string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.NullOrWhiteSpace(template, nameof(template));
            var normParam = Emitron.NormalizeInputParameterName(inputParameterName);
            var code = BuildCode(template, normParam);
            return Emitron.Compile<string>(code, normParam);
        }

        /// <summary>
        /// Convenience overload: compiles <paramref name="template"/> and immediately formats it
        /// with <paramref name="input"/>.
        /// </summary>
        /// <param name="template">A C# interpolated-string expression.</param>
        /// <param name="input">
        /// An object (anonymous type, POCO, <see cref="System.Dynamic.ExpandoObject"/>,
        /// <see cref="System.Collections.Generic.IDictionary{String,Object}"/>, etc.)
        /// whose public properties are exposed inside the interpolated string.
        /// </param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The formatted result string.</returns>
        public static string Format(string template, object input, string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.Null(input, nameof(input));
            return Compile(template, inputParameterName)(input);
        }

        // -----------------------------------------------------------------------------------------
        // Private helpers
        // -----------------------------------------------------------------------------------------

        private static string BuildCode(string template, string inputParameterName)
        {
            var rewritten = RewriteInterpolationSlots(template, inputParameterName);
            return $"return {rewritten};";
        }

        private static string RewriteInterpolationSlots(string template, string inputParameterName)
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

                if (isVerbatim && c == '"' && i + 1 < template.Length && template[i + 1] == '"')
                {
                    sb.Append("\"\"");
                    i += 2;
                    continue;
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

                    var slotSb = new StringBuilder();
                    var depth = 1;
                    while (i < template.Length && depth > 0)
                    {
                        var sc = template[i];
                        if (sc == '{')
                        {
                            depth++;
                        }
                        else if (sc == '}')
                        {
                            depth--;
                            if (depth == 0)
                            {
                                break;
                            }
                        }

                        slotSb.Append(sc);
                        i++;
                    }

                    sb.Append(PrefixLeadingIdentifier(slotSb.ToString(), inputParameterName));

                    if (i < template.Length && template[i] == '}')
                    {
                        sb.Append('}');
                        i++;
                    }

                    continue;
                }

                sb.Append(c);
                i++;
            }

            return sb.ToString();
        }

        private static string PrefixLeadingIdentifier(string slot, string inputParameterName)
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

            return inputParameterName + "." + slot;
        }
    }
}
