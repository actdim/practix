using System;
using Ardalis.GuardClauses;

namespace ActDim.Emitron
{
    /// <summary>
    /// Extension methods for string template interpolation using <see cref="Interpolator"/>.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Interpolates the C# template string using properties from <paramref name="input"/>.
        /// Properties are referenced directly by name (e.g. <c>$"Hello, {Name}!"</c>) without requiring <c>@params.</c>.
        /// </summary>
        /// <param name="template">The C# interpolated-string template (e.g. <c>$"Hello, {Name}! Balance: {Balance:C2}"</c>).</param>
        /// <param name="input">The input parameter bag containing properties matching the interpolation slots.</param>
        /// <param name="inputParameterName">The variable name bound to caller inputs (defaults to <c>@params</c>).</param>
        /// <returns>The formatted result string.</returns>
        public static string Interpolate(
            this string template,
            object input,
            string inputParameterName = Emitron.DefaultInputParameterName)
        {
            Guard.Against.Null(template, nameof(template));
            Guard.Against.Null(input, nameof(input));

            return Interpolator.Format(template, input, inputParameterName);
        }
    }
}
