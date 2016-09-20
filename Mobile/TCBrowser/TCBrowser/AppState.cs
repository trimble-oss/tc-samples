namespace Examples.Mobile
{
    using System;
    using System.Collections.Generic;

    using Trimble.Identity;
    using Trimble.Connect.Client;
    using Plugin.Settings;
    using System.Threading.Tasks;

    /// <summary>
    /// The application state.
    /// </summary>
    public static class AppState
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
        /// Gets the TID authentication context.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public static AuthenticationContext AuthContext { get; private set; }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        public static AuthenticationResult CurrentUser { get; set; }

        /// <summary>
        /// Gets the TC client wrapper.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public static ITrimbleConnectClient Client { get; private set; }

        public static void Initialize()
        {
            var env = CrossSettings.Current.GetValueOrDefault("environment", "PROD");
            var httpStack = CrossSettings.Current.GetValueOrDefault("httpStack", 0);
            AuthContext = new AuthenticationContext(ClientCredential[env])
            {
                AuthorityUri = new Uri(AuthorityUri[env]),
                Handlers = new[] { new PerformanceLoggerHandler() }
            };

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

        public static bool IsSignedIn()
        {
            return CurrentUser != null;
        }

        public static async Task SignInSilentlyAsync()
        {
            CurrentUser = await AuthContext.AcquireTokenSilentAsync();
            CurrentUser = await AuthContext.AcquireTokenByRefreshTokenAsync(CurrentUser);
            await Client.LoginAsync(CurrentUser.IdToken);
        }

        public static void SignOut()
        {
            CurrentUser = null;
            AuthContext.TokenCache.Clear();
        }
    }
}
