using System.Globalization;
using System.Text.Json;

namespace ActDim.Practix.Common.Json
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public static readonly LowerCaseNamingPolicy Instance = new();

        public override string ConvertName(string name)
        {
            return name.ToLower(CultureInfo.InvariantCulture);
        }
    }
}
