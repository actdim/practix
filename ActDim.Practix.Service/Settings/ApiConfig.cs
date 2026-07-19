using Microsoft.OpenApi;

namespace ActDim.Practix.Service.Settings
{    
    public class ApiConfig
    {
        // EndpointTemplate
        // string RouteTemplate { get; set; }

        public string TitleTemplate { get; set; }

        /// <summary>
        /// Doc(Info)
        /// </summary>
        public OpenApiInfo Info { get; set; }

        public bool Explorable { get; set; } // ShowInExplorer

        public string[] Versions { get; set; }

        public ApiConfigOverride[] Overrides { get; set; }
    }

}
