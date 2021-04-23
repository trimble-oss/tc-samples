namespace Examples.Desktop
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;

    using Trimble.Identity;

    using MouseEventArgs = System.Windows.Input.MouseEventArgs;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
#if true

        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = AuthorityUris.StagingUri;

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost", UriKind.Absolute)
        };

        readonly string[] Scopes = new string[] { ClientCredentials.Name };

        private AuthenticationResult authenticationResult;

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = AuthorityUris.ProductionUri;

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };
#endif

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            this.DisplayAuthenticationResult(null);
            this.SigninSilentAsync();
        }
        
        private async void SigninSilentAsync()
        {
            var user = UserIdentifier.AnyUser;
            try
            {
                var authenticationContext = new AuthenticationContext(ClientCredentials) { AuthorityUri = new Uri(AuthorityUri) };
                authenticationResult = await authenticationContext.AcquireTokenSilentAsync(user);
                this.DisplayAuthenticationResult(authenticationResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcquireTokenSilentAsync failed: " + ex);
            }
        }

        private AuthenticationContext GetAuthenticationContext()
        {
            return new AuthenticationContext(
                ClientCredentials,
                this.RememberMe.IsChecked.Value ? TokenCache.DefaultShared : new TokenCache())
            {
                AuthorityUri = new Uri(AuthorityUri), 
            };
        }

        private void DisplayAuthenticationResult(AuthenticationResult authenticationResult)
        {
            this.TokenCacheItems.ItemsSource = TokenCache.DefaultShared.ReadItems().Where(i => i.Authority == AuthorityUri).Select(i => i.DisplayableId);
            this.DisplayableId.Text = authenticationResult == null ? string.Empty : authenticationResult.UserInfo.DisplayableId;
            this.ExpiresOn.Text = authenticationResult == null ? string.Empty : authenticationResult.ExpiresOn.LocalDateTime.ToString();
            this.Email.Text = authenticationResult == null ? string.Empty : authenticationResult.UserInfo.Email;
        }

        private void ClearFields()
        {
            this.DisplayableId.Text = string.Empty;
            this.ExpiresOn.Text = string.Empty;
            this.Email.Text = string.Empty;
        }

        private async void WebLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Hide();
                this.WebLogin.IsEnabled = false;
                var authenticationContext = this.GetAuthenticationContext();

                authenticationResult = await authenticationContext.AcquireTokenAsync(new InteractiveAuthenticationRequest()
                {
                    Scope = $"openid {string.Join(" ", Scopes)}"
                });
                this.DisplayAuthenticationResult(authenticationResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcquireTokenAsync failed: " + ex);
            }
            finally
            {
                this.WebLogin.IsEnabled = true;
                this.Show();
            }
        }

        private async void WebLogout_Click(object sender, RoutedEventArgs e)
        {
            var authenticationContext = this.GetAuthenticationContext();
            try
            {
                this.WebLogout.IsEnabled = false;
                await authenticationContext.LogoutAsync(authenticationResult);
                ClearFields();
                this.authenticationResult = null;
            }
            finally
            {
                this.WebLogout.IsEnabled = true;
            }
        }

        private void ClearTokenCache_Click(object sender, RoutedEventArgs e)
        {
            TokenCache.DefaultShared.Clear();
            this.TokenCacheItems.ItemsSource = null;
        }

        async void TokenCacheItems_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = this.TokenCacheItems.SelectedItem;
            if (item != null)
            {
                try
                {
                    var userId = new UserIdentifier(item as string, UserIdentifierType.RequiredDisplayableId);
                    var authenticationContext = this.GetAuthenticationContext();
                    var authenticationResult = await authenticationContext.AcquireTokenSilentAsync(userId);
                    this.DisplayAuthenticationResult(authenticationResult);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("AcquireTokenAsync failed: " + ex);
                }
            }
        }
    }
}
