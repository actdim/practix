using Microsoft.OpenApi;

namespace ActDim.Practix.Service.Settings
{
    public class ApiConfigOverride
    {
        public string Version { get; set; }

        public OpenApiInfo Info { get; set; }

        public bool Explorable { get; set; }
    }

}
