using System.Globalization;
using System.Text.Json;

namespace ActDim.Practix.Common.Json
{
    /// <summary>
    /// A <see cref="JsonNamingPolicy"/> that converts property names to upper-case string representation.
    /// </summary>
    public class UpperCaseNamingPolicy : JsonNamingPolicy
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="UpperCaseNamingPolicy"/>.
        /// </summary>
        public static readonly UpperCaseNamingPolicy Instance = new();

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return name.ToUpper(CultureInfo.InvariantCulture);
        }
    }
}
