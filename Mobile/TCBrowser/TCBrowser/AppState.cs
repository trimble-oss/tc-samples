namespace Examples.Mobile
{
    using System;
    using System.Collections.Generic;

    using Trimble.Identity;
    using Trimble.Connect.Client;
    using Plugin.Settings;
    using System.Threading.Tasks;
    using Xamarin.Forms;
#if __IOS__
    using Foundation;
#else
    using Android.Webkit;
#endif

    /// <summary>
    /// The application state.
    /// </summary>
    public class AppState
    {
        /// <summary>
        /// The service URI.
        /// </summary>
        private static readonly IDictionary<string, string> ServiceUri = new Dictionary<string, string>
        {
            { "STAGE", "https://app.stage.connect.trimble.com/tc/api/2.0/" },
            { "PROD", "https://app.prod.gteam.com/tc/api/2.0/" },
        };

        /// <summary>
        /// The service URI.
        /// </summary>
        private static readonly IDictionary<string, string> AuthorityUri = new Dictionary<string, string>
        {
            { "STAGE", "https://identity-stg.trimble.com/i/oauth2/" },
            { "PROD", "https://identity.trimble.com/i/oauth2/" },
        };

        /// <summary>
        /// TCD as app.
        /// </summary>
        private static readonly IDictionary<string, ClientCredential> ClientCredential = new Dictionary<string, ClientCredential>
        {
            {
                "STAGE",
                new ClientCredential("<key>", "<secret>", "<name>")
                {
                RedirectUri = new Uri("http://localhost")
                }
            },
            {
                "PROD",
                new ClientCredential("<key>", "<secret>", "<name>")
                {
                    RedirectUri = new Uri("http://localhost")
                }
            },
        };

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static readonly AppState Instance  = new AppState();

        /// <summary>
        /// Gets the message id.
        /// </summary>
        /// <value>
        /// The message id.
        /// </value>
        public static readonly string UserChangedMessageId = "UserChanged";

        private AppState()
        {
            var env = CrossSettings.Current.GetValueOrDefault("environment", "PROD");
            this.AuthContext = new AuthenticationContext(ClientCredential[env])
            {
                AuthorityUri = new Uri(AuthorityUri[env]),
                Handlers = new[] { new PerformanceLoggerHandler() }
            };

            this.CreateClient();
        }

        /// <summary>
        /// Gets the TID authentication context.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public AuthenticationContext AuthContext { get; private set; }

        /// <summary>
        /// Gets or sets the web ui parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public Parameters Parameters
        {
            get { return this.AuthContext.Parameters; }
            set { this.AuthContext.Parameters = value; }
        }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        public AuthenticationResult CurrentUser { get; set; }

        /// <summary>
        /// Gets the TC client wrapper.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ITrimbleConnectClient Client { get; private set; }

        public bool IsSignedIn
        {
            get { return this.CurrentUser != null; }
        }

        public bool IsInProgress { get; private set; }

        public async Task SignInAsync()
        {
            try
            {
                await this.SignInSilentlyAsync();
            }
            catch (Exception e)
            {
                await this.SignInWebAsync();
            }
        }

        public async Task SignInSilentlyAsync()
        {
            try
            {
                IsInProgress = true;
                this.CurrentUser = await this.AuthContext.AcquireTokenSilentAsync();
                this.CurrentUser = await this.AuthContext.AcquireTokenByRefreshTokenAsync(CurrentUser);
                await Client.LoginAsync(this.CurrentUser.IdToken);
            }
            finally
            {
                IsInProgress = false;
            }

            Notify();
        }

        public async Task SignInWebAsync()
        {
            try
            {
                IsInProgress = true;
                this.CurrentUser = await this.AuthContext.AcquireTokenAsync();
                await this.Client.LoginAsync(this.CurrentUser.IdToken);
            }
            finally
            {
                IsInProgress = false;
            }

            Notify();
        }

        public void SignOut()
        {
            this.CurrentUser = null;
            this.AuthContext.TokenCache.Clear();

            ////Trimble.WebUI.WebUIHelper.LogoutAsync();
            this.DeleteCookies();

            this.CreateClient();

            Notify();
        }

        private void DeleteCookies()
        {
#if __IOS__
            NSHttpCookieStorage cookieJar = NSHttpCookieStorage.SharedStorage;
            foreach (var cookies in cookieJar.Cookies)
            {
                cookieJar.DeleteCookie(cookies);

            }
#else
			// CookieSyncManager is supported only till API 20. 
			// RemoveAllCookie is deprecated from API 21
			if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Lollipop)
            {
				
				CookieSyncManager.CreateInstance(Forms.Context);
                CookieManager.Instance.RemoveAllCookie();
				CookieSyncManager.Instance.Sync();
			}
            else
            {
                CookieManager.Instance.RemoveAllCookies(null);
			}
#endif
        }

        private void CreateClient()
        {
            var env = CrossSettings.Current.GetValueOrDefault("environment", "PROD");
            var httpStack = CrossSettings.Current.GetValueOrDefault("httpStack", 0);

            if (Client != null)
            {
                ((TrimbleConnectClient)Client).Dispose();
                Client = null;
            }

            Client = httpStack == 0
                ? new TrimbleConnectClient(ServiceUri[env], new PerformanceLoggerHandler())
#if __IOS__
                            : new TrimbleConnectClient(ServiceUri[env], new ModernHttpClient.NativeMessageHandler(), new PerformanceLoggerHandler());
#else
                            : new TrimbleConnectClient(ServiceUri[env], new PerformanceLoggerHandler());
#endif
        }

        private void Notify()
        {
            Device.BeginInvokeOnMainThread(() => {
                MessagingCenter.Send(this, UserChangedMessageId);
            });
        }
    }
}
