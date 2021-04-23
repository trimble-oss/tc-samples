namespace Examples.Desktop
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Input;

    using Trimble.Identity;

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

        readonly string[] Scopes = new string[] { ClientCredentials.Name };

        /// <summary>
        /// The client credentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

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
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
            this.DisplayAuthenticationResult(null);
            SigninSilentAsync().ContinueWith(
                (t) =>
                    {
                        if (t.IsFaulted)
                        {
                            Debug.WriteLine("AcquireTokenSilentAsync failed: " + t.Exception);
                        }
                        else
                        {
                            this.authenticationResult = t.Result;
                            this.DisplayAuthenticationResult(t.Result);
                        }
                    }, 
                    TaskScheduler.FromCurrentSynchronizationContext()
                );
        }

        private static Task<AuthenticationResult> SigninSilentAsync()
        {
            var user = UserIdentifier.AnyUser;
            var authenticationContext = new AuthenticationContext(ClientCredentials) { AuthorityUri = new Uri(AuthorityUri) };
            return authenticationContext.AcquireTokenSilentAsync(user);
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
            this.ExpiresOn.Text =  string.Empty;
            this.Email.Text = string.Empty;
        }

        private void WebLogin_Click(object sender, RoutedEventArgs e)
        {
            this.WebLogin.IsEnabled = false;

            var authenticationContext = this.GetAuthenticationContext();
            this.Hide();

            authenticationContext.AcquireTokenAsync(new InteractiveAuthenticationRequest()
            {
                Scope = $"openid {string.Join(" ", Scopes)}"
            }).ContinueWith(
                (t) =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine("AcquireTokenAsync failed: " + t.Exception);
                    }
                    else
                    {
                        this.authenticationResult = t.Result;
                        this.DisplayAuthenticationResult(t.Result);
                    }

                    this.WebLogin.IsEnabled = true;
                    this.Show();
                },
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        private void WebLogout_Click(object sender, RoutedEventArgs e)
        {
            var authenticationContext = this.GetAuthenticationContext();
            this.WebLogout.IsEnabled = false;
            authenticationContext.LogoutAsync(this.authenticationResult).ContinueWith(
                t =>
                {
                    this.WebLogout.IsEnabled = true;
                    ClearFields();
                    this.authenticationResult = null;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ClearTokenCache_Click(object sender, RoutedEventArgs e)
        {
            TokenCache.DefaultShared.Clear();
            this.TokenCacheItems.ItemsSource = null;
        }

        private void TokenCacheItems_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var item = this.TokenCacheItems.SelectedItem;
            if (item != null)
            {
                try
                {
                    var userId = new UserIdentifier(item as string, UserIdentifierType.RequiredDisplayableId);
                    var authenticationContext = this.GetAuthenticationContext();
                    authenticationContext.AcquireTokenSilentAsync(userId).ContinueWith(
                        (t) =>
                        {
                            if (t.IsFaulted)
                            {
                                Debug.WriteLine("AcquireTokenAsync failed: " + t.Exception);
                            }
                            else
                            {
                                this.authenticationResult = t.Result;
                                this.DisplayAuthenticationResult(t.Result);
                            }
                        },
                        TaskScheduler.FromCurrentSynchronizationContext()
                    );
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("AcquireTokenAsync failed: " + ex);
                }
            }
        }
    }
}
