using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows.Input;
using Trimble.Connect.Client;
using Trimble.Identity.OAuth.AuthCode;
using Windows.Foundation.Collections;
using TCBrowserPro.Maui.Services;
using Microsoft.Extensions.Logging;

namespace TCBrowserPro.Maui.ViewModels
{
    internal partial class ShellViewModel : ObservableObject, IShellViewModel
    {
        [ObservableProperty]
        private string _emailId;

        [ObservableProperty]
        private bool _isSignedIn;

        [ObservableProperty]
        private string _upgradeUrl;

        [ObservableProperty]
        private bool _enableContactSupport;

        [ObservableProperty]
        private bool _enableSignOut;

        internal TrimbleConnectClient TrimbleConnectClient { get; set; }

        private readonly ConfigService _configService;
        private readonly ILogger<ShellViewModel> _logger;
        private IAuthCodeCredentialsProvider _loginContext;

        public ShellViewModel(IAuthCodeCredentialsProvider loginContext, ConfigService configService, ILogger<ShellViewModel> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loginContext = loginContext;
            NeedHelpCommand = new RelayCommand(DoNavigateNeedHelp);
            AboutCommand = new RelayCommand(DoNavigateAbout);

            InitializeTrimbleConnectClient();
        }

        private void InitializeTrimbleConnectClient()
        {
            // Use environment configuration from the selected environment
            var envConfig = _configService.Config;

            // Get client credentials from the selected environment configuration
            // These must be set in environments.json for each environment
            var clientId = envConfig.ClientId;
            var clientSecret = envConfig.ClientSecret;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException(
                    $"ClientId and ClientSecret must be configured in environments.json for environment '{envConfig.EnvironmentName}'. " +
                    $"Please add ClientId and ClientSecret to the '{envConfig.EnvironmentName}' environment in environments.json.");
            }

#if WINDOWS || MACCATALYST
            var redirectUri = "http://localhost";
            var appName = "TC.SDK.Example";
#endif

#if ANDROID || IOS
            var redirectUri = "tcps://localhost";
            var appName = "TC.SDK.Example.Mobile";
#endif
            var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri) 
            { 
                AuthorityUri = new Uri(envConfig.AuthorityUri) 
            };
            _loginContext.AuthContext = authCtx;
            var config = new TrimbleConnectClientConfig 
            { 
                ServiceURI = new Uri(envConfig.ServiceUri) 
            };
            try
            {
                TrimbleConnectClient = new TrimbleConnectClient(config, _loginContext)
                {
                    CurrentUser = new TrimbleConnectUser("Unknown")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize TrimbleConnectClient for environment '{EnvironmentName}'", envConfig.EnvironmentName);
                throw new InvalidOperationException(
                    $"Failed to initialize Trimble Connect client for environment '{envConfig.EnvironmentName}'. " +
                    $"Please verify your configuration in environments.json. Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Reinitializes the TrimbleConnectClient with the current environment configuration
        /// </summary>
        public void Reinitialize()
        {
            _configService.Reload();
            InitializeTrimbleConnectClient();
        }

        public ICommand NeedHelpCommand { get; private set; }

        public ICommand ContactSupportCommand { get; private set; }

        public ICommand AboutCommand { get; private set; }

        public async Task HandleLogin(bool isLoggedIn)
        {
            // Login handling is managed by LoginViewModel
            await Task.CompletedTask;
        }
   
        private async void DoNavigateNeedHelp()
        {
            // Navigation to help/documentation page - not implemented in this sample
            await Task.CompletedTask;
        }

        private void DoNavigateContactSupport()
        {
            // Navigation to contact support page - not implemented in this sample
        }

        private void DoNavigateAbout()
        {
            // Navigation to about page - not implemented in this sample
        }

        [RelayCommand]
        private async Task DoNavigatePreferences()
        {
            // Navigation to preferences/settings page - not implemented in this sample
            await Task.CompletedTask;
        }

        [RelayCommand]
        private async Task NavigateBack()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.GoToAsync($"//{nameof(ProjectsView)}").ConfigureAwait(false);
            });
        }

    }
}
