namespace ActDim.Practix.Service
{
    public class AppSettings
    {
        public Dictionary<string, AuthSchemeConfig> AuthSchemes { get; set; }

        // public string DefaulAuthSchemeName { get; set; }

        public Dictionary<string, ApiConfig> Apis { get; set; }

        public string ApiExplorerPath { get; set; }

        public string ApiDocRouteTemplate { get; set; }

        public string SchemaPrefix { get; set; }

        public string ClassPrefix { get; set; }

        public Dictionary<string, string> Routes { get; set; }

        public AppSettings()
        {
            SchemaPrefix = string.Empty;
            ClassPrefix = "__API__";
        }
    }

}
