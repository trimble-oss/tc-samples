using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using Trimble.Identity.OAuth.AuthCode;
using Trimble.Connect.Client;

namespace SignIn.Maui.ViewModels
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

        public ShellViewModel(IAuthCodeCredentialsProvider loginContext)
        {
            NeedHelpCommand = new RelayCommand(DoNavigateNeedHelp);
            AboutCommand = new RelayCommand(DoNavigateAbout);
            var clientId = "<Client Id>";
            var clientSecret = "<Client Secret>";
            var redirectUri = "tcps://localhost";
            var appName = "<Name>";
            var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri) { AuthorityUri = new Uri(IdentityUris.StagingUri) };
            loginContext.AuthContext = authCtx;
            var config = new TrimbleConnectClientConfig { ServiceURI = new Uri(Properties.Settings.Default.ServiceUri) };
            TrimbleConnectClient = new TrimbleConnectClient(config, loginContext)
            {
                CurrentUser = new TrimbleConnectUser("Unknown")
            };
        }

        public ICommand NeedHelpCommand { get; private set; }

        public ICommand ContactSupportCommand { get; private set; }

        public ICommand AboutCommand { get; private set; }

        public async Task HandleLogin(bool isLoggedIn)
        {
 
        }
   
        private async void DoNavigateNeedHelp()
        {
        }

        private void DoNavigateContactSupport()
        {

        }

        private void DoNavigateAbout()
        {
            
        }

        [RelayCommand]
        private async Task DoNavigatePreferences()
        {
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
