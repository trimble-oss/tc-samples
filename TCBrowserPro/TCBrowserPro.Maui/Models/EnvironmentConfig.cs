namespace TCBrowserPro.Maui.Models
{
    /// <summary>
    /// Configuration model for environment settings
    /// </summary>
    public class EnvironmentConfig
    {
        /// <summary>
        /// The OAuth authority URI (e.g., https://id.trimble.com/oauth/ or https://stage.id.trimblecloud.com/oauth/)
        /// </summary>
        public string AuthorityUri { get; set; } = "https://id.trimble.com/oauth/";

        /// <summary>
        /// The Trimble Connect API service URI (e.g., https://app.connect.trimble.com/tc/api/2.0/ or https://app.stage.connect.trimble.com/tc/api/2.0/)
        /// </summary>
        public string ServiceUri { get; set; } = "https://app.connect.trimble.com/tc/api/2.0/";

        /// <summary>
        /// The web application URI (e.g., https://app.connect.trimble.com or https://app.stage.connect.trimble.com)
        /// </summary>
        public string WebAppUri { get; set; } = "https://app.connect.trimble.com";

        /// <summary>
        /// The web viewer URI (e.g., https://web.connect.trimble.com or https://web.stage.connect.trimble.com)
        /// </summary>
        public string WebViewerUri { get; set; } = "https://web.connect.trimble.com";

        /// <summary>
        /// Environment name (e.g., "Production", "Stage", "Development")
        /// </summary>
        public string EnvironmentName { get; set; } = "Production";

        /// <summary>
        /// Client ID for this environment (optional - falls back to hardcoded production values if not provided)
        /// </summary>
        public string? ClientId { get; set; }

        /// <summary>
        /// Client Secret for this environment (optional - falls back to hardcoded production values if not provided)
        /// </summary>
        public string? ClientSecret { get; set; }
    }
}

