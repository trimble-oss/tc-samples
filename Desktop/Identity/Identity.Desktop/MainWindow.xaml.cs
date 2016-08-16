namespace Examples.Desktop
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
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
        private const string AuthorityUri = "https://identity-stg.trimble.com/i/oauth2/";

        /// <summary>
        /// The client creadentials.
        /// </summary>
        private static readonly ClientCredential ClientCredentials = new ClientCredential("<key>", "<secret>", "<name>")
        {
            RedirectUri = new Uri("http://localhost")
        };

#else
        /// <summary>
        /// The service URI.
        /// </summary>
        private const string AuthorityUri = "https://identity.trimble.com/i/oauth2/";

        /// <summary>
        /// The client creadentials.
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
                var authenticationResult = await authenticationContext.AcquireTokenSilentAsync(user);
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

        private async void EmbeddedLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.ControlGrid.IsEnabled = false;
                this.WindowsFormsHost.Visibility = Visibility.Visible;

                var authenticationContext = this.GetAuthenticationContext();
                authenticationContext.Parameters.EmbedTo = this.HostPanel;
                var authenticationResult = await authenticationContext.AcquireTokenAsync();

                this.DisplayAuthenticationResult(authenticationResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcquireTokenAsync failed: " + ex);
            }
            finally
            {
                this.WindowsFormsHost.Visibility = Visibility.Collapsed;
                this.ControlGrid.IsEnabled = true;
            }
        }

        private async void WebLogin_Click(object sender, RoutedEventArgs e)
        {
            System.Drawing.Icon LoginIcon = null;

            try
            {
                this.Hide();
                this.WebLogin.IsEnabled = false;
                var authenticationContext = this.GetAuthenticationContext();

                var source = this.Icon as System.Windows.Media.Imaging.BitmapSource;
                if (source != null)
                {
                    var bitmap = source.ToBitmap();
                    var hicon = bitmap.GetHicon();
                    LoginIcon = System.Drawing.Icon.FromHandle(hicon);
                }

                authenticationContext.Parameters.EmbedTo = null;
                authenticationContext.Parameters.WindowIcon = LoginIcon;
                authenticationContext.Parameters.WindowTitle = "Sign-in to TC SDK Example app";
                var authenticationResult = await authenticationContext.AcquireTokenAsync();

                this.DisplayAuthenticationResult(authenticationResult);
    
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcquireTokenAsync failed: " + ex);
            }
            finally
            {
                if (LoginIcon != null)
                {
                    Helper.DestroyIcon(LoginIcon.Handle);
                }

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
                await authenticationContext.LogoutAsync();
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

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Login.IsEnabled = false;
                var authenticationContext = this.GetAuthenticationContext();
                var authenticationResult = await authenticationContext.AcquireTokenAsync(new NetworkCredential(this.Username.Text, this.Password.Password));
                this.DisplayAuthenticationResult(authenticationResult);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AcquireTokenAsync failed: " + ex);
            }
            finally
            {
                this.Login.IsEnabled = true;
            }
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
