namespace TCBrowser.Maui.Models
{
    /// <summary>
    /// Configuration model for multiple environments
    /// </summary>
    public class EnvironmentsConfig
    {
        /// <summary>
        /// Dictionary of available environments by name
        /// </summary>
        public Dictionary<string, EnvironmentConfig> Environments { get; set; } = new();

        /// <summary>
        /// Default environment name
        /// </summary>
        public string DefaultEnvironment { get; set; } = "Production";
    }
}

