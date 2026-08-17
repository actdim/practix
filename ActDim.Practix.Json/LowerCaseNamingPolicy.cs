using System.Globalization;
using System.Text.Json;

namespace ActDim.Practix.Json
{
    /// <summary>
    /// A <see cref="JsonNamingPolicy"/> that converts property names to lower-case string representation.
    /// </summary>
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        /// <summary>
        /// Gets the singleton instance of <see cref="LowerCaseNamingPolicy"/>.
        /// </summary>
        public static readonly LowerCaseNamingPolicy Instance = new();

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            return name.ToLower(CultureInfo.InvariantCulture);
        }
    }
}
