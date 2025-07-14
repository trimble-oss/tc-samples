using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows.Input;
using Trimble.Connect.Client;
using Trimble.Identity.OAuth.AuthCode;
using Windows.Foundation.Collections;

namespace TCBrowser.Maui.ViewModels
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

        //internal IPropertySet psetClient { get; set; }

        public ShellViewModel(IAuthCodeCredentialsProvider loginContext)
        {
            NeedHelpCommand = new RelayCommand(DoNavigateNeedHelp);
            AboutCommand = new RelayCommand(DoNavigateAbout);

#if WINDOWS || MACCATALYST
            var clientId = "951d2e36-75ca-11e6-8cff-020d5a34cb4d";
            var clientSecret = "gwoVp2VdOwMuzIkbHBIsG89emmca";
            var redirectUri = "http://localhost";
            var appName = "TC.SDK.Example";
#endif

#if ANDROID || IOS
            var clientId = "63bd3ab9-a7a1-4380-9e84-33f28e4239e5";
            var clientSecret = "YobGqUa6qF4sYvqlV0OBhoIqidEa";
            var redirectUri = "tcps://localhost";
            var appName = "TC.SDK.Example.Mobile";
#endif
            var authCtx = new AuthContext(clientId, clientSecret, appName, redirectUri) { AuthorityUri = new Uri(IdentityUris.StagingUri) };
            loginContext.AuthContext = authCtx;
            var config = new TrimbleConnectClientConfig { ServiceURI = new Uri("https://app.stage.connect.trimble.com/tc/api/2.0/") };
            try
            {
                TrimbleConnectClient = new TrimbleConnectClient(config, loginContext)
                {
                    CurrentUser = new TrimbleConnectUser("Unknown")
                };
                if (TrimbleConnectClient == null)
                {
                    Debug.WriteLine("CRITICAL ERROR: TrimbleConnectClient is NULL immediately after instantiation in ShellViewModel constructor.");
                }
                else
                {
                    Debug.WriteLine($"TrimbleConnectClient successfully initialized in ShellViewModel. Hash: {TrimbleConnectClient.GetHashCode()}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION during TrimbleConnectClient instantiation in ShellViewModel: {ex.Message}");
            }
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
