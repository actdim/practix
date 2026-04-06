using System.Globalization;
using System.Text.Json;

namespace ActDim.Practix.Common.Json
{
    public class UpperCaseNamingPolicy : JsonNamingPolicy
    {
        public static readonly UpperCaseNamingPolicy Instance = new();

        public override string ConvertName(string name)
        {
            return name.ToUpper(CultureInfo.InvariantCulture);
        }
    }
}
