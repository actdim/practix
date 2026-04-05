using System.Globalization;
using System.Text.Json;

namespace ActDim.Practix.Service.Json
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
