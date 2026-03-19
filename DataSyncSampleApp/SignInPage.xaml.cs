using DataSyncSampleApp.Models;
using DataSyncSampleApp.Services;
using Trimble.Connect.Client;
using Trimble.Identity.OAuth.AuthCode;

namespace DataSyncSampleApp;

public partial class SignInPage : ContentPage
{
    public SignInPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Show secret field only when not baked into config / env
        SecretSection.IsVisible = string.IsNullOrWhiteSpace(AppOAuthConfig.EffectiveClientSecret());
    }

    private async void OnSignInClicked(object sender, EventArgs e)
    {
        SignInButton.IsEnabled = false;
        try
        {
            var clientSecret = AppOAuthConfig.EffectiveClientSecret();
            if (string.IsNullOrWhiteSpace(clientSecret))
                clientSecret = ClientSecretEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                await DisplayAlert(
                    "Client secret required",
                    "Set AppOAuthConfig.ClientSecret, environment DATASYNC_OAUTH_CLIENT_SECRET, or enter the secret above. Confidential client login does not use PKCE.",
                    "OK");
                return;
            }

            // 4-arg ctor → PkceMode.None: authorization code exchanged with client_secret (no PKCE).
            var authContext = new AuthContext(
                AppOAuthConfig.ClientId,
                clientSecret,
                AppOAuthConfig.AppName,
                AppOAuthConfig.RedirectUri)
            {
                AuthorityUri = new Uri(AppOAuthConfig.AuthorityUrl)
            };

            var provider = new AuthCodeCredentialsProvider(authContext);
            AppSession.AuthProvider = provider;

            var refresh = MobileTokenStorage.LoadRefreshToken();
            if (!string.IsNullOrEmpty(refresh))
            {
                try
                {
                    provider.WithRefreshToken(refresh);
                    await Task.Run(() => provider.RefreshTokenAsync()).ConfigureAwait(true);
                    MobileTokenStorage.SaveRefreshToken(provider.RefreshToken ?? refresh);
                    await FinishSignInAsync(provider, Navigation).ConfigureAwait(true);
                    return;
                }
                catch
                {
                    /* fall through to browser */
                }
            }

#if ANDROID
            var act = await Platform.WaitForActivityAsync();
            provider.WithActivity(act);
#elif IOS
            var vc = Platform.GetCurrentUIViewController();
            if (vc != null) provider.WithViewController(vc);
#endif

            string accessToken = null!;
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                accessToken = await provider.AcquireTokenAsync();
            });

            if (!string.IsNullOrEmpty(accessToken))
            {
                MobileTokenStorage.SaveRefreshToken(provider.RefreshToken ?? "");
                provider.OnTokenRefreshed += (t, _) => MobileTokenStorage.SaveRefreshToken(t);
                await FinishSignInAsync(provider, Navigation).ConfigureAwait(true);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Sign-in failed", ex.Message, "OK");
        }
        finally
        {
            SignInButton.IsEnabled = true;
        }
    }

    private static async Task FinishSignInAsync(AuthCodeCredentialsProvider provider, INavigation navigation)
    {
        var cfg = new TrimbleConnectClientConfig
        {
            ServiceURI = new Uri(AppOAuthConfig.ConnectServiceUrl.TrimEnd('/') + "/")
        };
        var client = new TrimbleConnectClient(cfg, provider);
        await client.InitializeTrimbleConnectUserAsync().ConfigureAwait(true);
        AppSession.ConnectClient = client;
        await navigation.PushAsync(new ProjectsListPage()).ConfigureAwait(true);
    }
}
