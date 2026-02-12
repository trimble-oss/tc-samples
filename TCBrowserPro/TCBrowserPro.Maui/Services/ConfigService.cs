using System.Text.Json;
using TCBrowserPro.Maui.Models;

namespace TCBrowserPro.Maui.Services
{
    /// <summary>
    /// Service for loading and managing environment configuration
    /// </summary>
    public class ConfigService
    {
        private EnvironmentConfig _config;
        private EnvironmentsConfig _environmentsConfig;
        private static readonly string EnvironmentsFileName = "environments.json";
        private static readonly string UserPreferencesFileName = "userpreferences.json";

        /// <summary>
        /// Gets the current environment configuration
        /// </summary>
        public EnvironmentConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = LoadConfig();
                }
                return _config;
            }
        }

        /// <summary>
        /// Gets all available environments
        /// </summary>
        public List<string> AvailableEnvironments
        {
            get
            {
                LoadEnvironments();
                return _environmentsConfig.Environments.Keys.ToList();
            }
        }

        /// <summary>
        /// Sets the selected environment and saves user preference
        /// </summary>
        public void SetSelectedEnvironment(string environmentName)
        {
            LoadEnvironments();
            if (_environmentsConfig.Environments.ContainsKey(environmentName))
            {
                _config = _environmentsConfig.Environments[environmentName];
                SaveUserPreference(environmentName);
            }
            else
            {
                throw new ArgumentException($"Environment '{environmentName}' not found in environments.json");
            }
        }

        /// <summary>
        /// Gets the user's selected environment name
        /// </summary>
        public string GetSelectedEnvironmentName()
        {
            LoadEnvironments();
            var preference = LoadUserPreference();
            if (!string.IsNullOrEmpty(preference) && _environmentsConfig.Environments.ContainsKey(preference))
            {
                return preference;
            }
            return _environmentsConfig.DefaultEnvironment;
        }

        /// <summary>
        /// Loads all available environments
        /// </summary>
        private void LoadEnvironments()
        {
            if (_environmentsConfig != null) return;

            try
            {
                // Try to load from file system first
                var envPath = Path.Combine(FileSystem.AppDataDirectory, EnvironmentsFileName);
                if (File.Exists(envPath))
                {
                    var json = File.ReadAllText(envPath);
                    var config = JsonSerializer.Deserialize<EnvironmentsConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (config != null && config.Environments != null && config.Environments.Count > 0)
                    {
                        _environmentsConfig = config;
                        return;
                    }
                }

                // Try to load from MauiAsset
                try
                {
                    using var stream = FileSystem.OpenAppPackageFileAsync(EnvironmentsFileName).GetAwaiter().GetResult();
                    if (stream != null)
                    {
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var config = JsonSerializer.Deserialize<EnvironmentsConfig>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        
                        if (config != null && config.Environments != null && config.Environments.Count > 0)
                        {
                            _environmentsConfig = config;
                            return;
                        }
                    }
                }
                catch
                {
                    // If MauiAsset loading fails, continue to defaults
                }
            }
            catch (Exception ex)
            {
                // Log error but continue to defaults
                // Error loading environments - using defaults
            }

            // Return default environments if file not found
            _environmentsConfig = GetDefaultEnvironments();
        }

        /// <summary>
        /// Loads configuration for the currently selected environment from environments.json
        /// </summary>
        private EnvironmentConfig LoadConfig()
        {
            LoadEnvironments();
            
            // Get user's selected environment
            var selectedEnv = GetSelectedEnvironmentName();
            if (_environmentsConfig.Environments.ContainsKey(selectedEnv))
            {
                return _environmentsConfig.Environments[selectedEnv];
            }

            // Fallback to default
            if (_environmentsConfig.Environments.ContainsKey(_environmentsConfig.DefaultEnvironment))
            {
                return _environmentsConfig.Environments[_environmentsConfig.DefaultEnvironment];
            }

            // Ultimate fallback
            return GetDefaultProductionConfig();
        }

        /// <summary>
        /// Loads user preference for selected environment
        /// </summary>
        private string LoadUserPreference()
        {
            try
            {
                var prefPath = Path.Combine(FileSystem.AppDataDirectory, UserPreferencesFileName);
                if (File.Exists(prefPath))
                {
                    var json = File.ReadAllText(prefPath);
                    var prefs = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (prefs != null && prefs.ContainsKey("SelectedEnvironment"))
                    {
                        return prefs["SelectedEnvironment"];
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
            return null;
        }

        /// <summary>
        /// Saves user preference for selected environment
        /// </summary>
        private void SaveUserPreference(string environmentName)
        {
            try
            {
                var prefPath = Path.Combine(FileSystem.AppDataDirectory, UserPreferencesFileName);
                var prefs = new Dictionary<string, string> { { "SelectedEnvironment", environmentName } };
                var json = JsonSerializer.Serialize(prefs);
                File.WriteAllText(prefPath, json);
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Gets default environments configuration
        /// </summary>
        private EnvironmentsConfig GetDefaultEnvironments()
        {
            return new EnvironmentsConfig
            {
                DefaultEnvironment = "Production",
                Environments = new Dictionary<string, EnvironmentConfig>
                {
                    {
                        "Production",
                        new EnvironmentConfig
                        {
                            AuthorityUri = "https://id.trimble.com/oauth/",
                            ServiceUri = "https://app.connect.trimble.com/tc/api/2.0/",
                            WebAppUri = "https://app.connect.trimble.com",
                            WebViewerUri = "https://web.connect.trimble.com",
                            EnvironmentName = "Production",
                            ClientId = null,
                            ClientSecret = null
                        }
                    },
                    {
                        "Stage",
                        new EnvironmentConfig
                        {
                            AuthorityUri = "https://stage.id.trimblecloud.com/oauth/",
                            ServiceUri = "https://app.stage.connect.trimble.com/tc/api/2.0/",
                            WebAppUri = "https://app.stage.connect.trimble.com",
                            WebViewerUri = "https://web.stage.connect.trimble.com",
                            EnvironmentName = "Stage"
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Gets default production configuration
        /// </summary>
        private EnvironmentConfig GetDefaultProductionConfig()
        {
            return new EnvironmentConfig
            {
                AuthorityUri = "https://id.trimble.com/oauth/",
                ServiceUri = "https://app.connect.trimble.com/tc/api/2.0/",
                WebAppUri = "https://app.connect.trimble.com",
                WebViewerUri = "https://web.connect.trimble.com",
                EnvironmentName = "Production"
            };
        }

        /// <summary>
        /// Reloads the configuration from file
        /// </summary>
        public void Reload()
        {
            _config = null;
            _environmentsConfig = null;
        }
    }
}

