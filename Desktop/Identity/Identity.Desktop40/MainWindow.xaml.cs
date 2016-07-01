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
            SigninSilentAsync().ContinueWith(
                (t) =>
                    {
                        if (t.IsFaulted)
                        {
                            Debug.WriteLine("AcquireTokenSilentAsync failed: " + t.Exception);
                        }
                        else
                        {
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
        }

        private void EmbeddedLogin_Click(object sender, RoutedEventArgs e)
        {
            this.ControlGrid.IsEnabled = false;
            this.WindowsFormsHost.Visibility = Visibility.Visible;

            var authenticationContext = this.GetAuthenticationContext();

            authenticationContext.Parameters.EmbedTo = this.HostPanel;
            authenticationContext.AcquireTokenAsync().ContinueWith(
                (t) =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine("AcquireTokenAsync failed: " + t.Exception);
                    }
                    else
                    {
                        this.DisplayAuthenticationResult(t.Result);
                    }

                    this.WindowsFormsHost.Visibility = Visibility.Collapsed;
                    this.ControlGrid.IsEnabled = true;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void WebLogin_Click(object sender, RoutedEventArgs e)
        {
            this.WebLogin.IsEnabled = false;
            System.Drawing.Icon LoginIcon = null;

            var source = this.Icon as System.Windows.Media.Imaging.BitmapSource;
            if (source != null)
            {
                var bitmap = source.ToBitmap();
                var hicon = bitmap.GetHicon();
                LoginIcon = System.Drawing.Icon.FromHandle(hicon);
            }

            var authenticationContext = this.GetAuthenticationContext();
            this.Hide();

            authenticationContext.Parameters.EmbedTo = null;
            authenticationContext.Parameters.WindowTitle = "Log into Examples.Desktop app";
            authenticationContext.Parameters.WindowIcon = LoginIcon;
            authenticationContext.AcquireTokenAsync().ContinueWith(
                (t) =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine("AcquireTokenAsync failed: " + t.Exception);
                    }
                    else
                    {
                        this.DisplayAuthenticationResult(t.Result);
                    }

                    if (LoginIcon != null)
                    {
                        Helper.DestroyIcon(LoginIcon.Handle);
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
            authenticationContext.LogoutAsync().ContinueWith(
                t =>
                {
                    this.WebLogout.IsEnabled = true;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ClearTokenCache_Click(object sender, RoutedEventArgs e)
        {
            TokenCache.DefaultShared.Clear();
            this.TokenCacheItems.ItemsSource = null;
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            this.Login.IsEnabled = false;
            var authenticationContext = this.GetAuthenticationContext();
            authenticationContext.AcquireTokenAsync(new NetworkCredential(this.Username.Text, this.Password.Password)).ContinueWith(
                (t) =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.WriteLine("AcquireTokenAsync failed: " + t.Exception);
                    }
                    else
                    {
                        this.DisplayAuthenticationResult(t.Result);
                    }

                    this.Login.IsEnabled = true;
                },
                TaskScheduler.FromCurrentSynchronizationContext()
            );
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
                                this.DisplayAuthenticationResult(t.Result);
                            }

                            this.Login.IsEnabled = true;
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
