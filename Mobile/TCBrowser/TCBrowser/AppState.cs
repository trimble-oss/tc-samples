namespace Examples.Mobile
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;

    using Trimble.Identity;
    using Trimble.Identity.OAuth.AuthCode;
    using Trimble.Connect.Client;
    using Plugin.Settings;
    using Xamarin.Forms;
    using Trimble.Connect.Client.Common;
#if __IOS__
    using Foundation;
#elif WINDOWS_UWP
#else
    using Android.Webkit;
#endif

    /// <summary>
    /// The application state.
    /// </summary>
    public class AppState
    {
        public static readonly string DefaultEnvironment = "STAGE";
        
        /// <summary>
        /// The service URI.
        /// </summary>
        public static readonly IDictionary<string, ServiceEnvironment> Environments = new Dictionary<string, ServiceEnvironment>
        {
            { "QA", ServiceEnvironment.Qa },
            { "STAGE", ServiceEnvironment.Staging },
            { "PROD", ServiceEnvironment.Production },
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

        private AuthCodeCredentialsProvider credentialsProvider;

        private AppState()
        {
            var env = CrossSettings.Current.GetValueOrDefault("environment", DefaultEnvironment);
            this.AuthContext = new AuthenticationContext(Environments[env].ClientCredentials)
            {
                AuthorityUri = new Uri(Environments[env].AuthorityUri),
                Handlers = new[] { new PerformanceLoggerHandler() }
            };

            // First create a credentials based on the previously created authentication context.
            // A single credentials provider can be used to create multiple service clients.           
            credentialsProvider = new AuthCodeCredentialsProvider(this.AuthContext);
            credentialsProvider.AuthenticationRequest = new InteractiveAuthenticationRequest()
            {
                Scope = $"openid {string.Join(" ", Environments[env].ClientCredentials.Name)}"
            };
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
        public AuthenticationResult AuthenticationResult { get; set; }

        /// <summary>
        /// Gets the TC client wrapper.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public ITrimbleConnectClient Client { get; private set; }

        public bool IsSignedIn
        {
            get { return this.AuthenticationResult != null; }
        }

        public bool IsInProgress { get; private set; }

        public async Task SignInAsync()
        {
            try
            {
                await this.SignInSilentlyAsync();
            }
            catch (Exception)
            {
                await this.SignInWebAsync();
            }
        }

        public async Task SignInSilentlyAsync()
        {
            try
            {
                IsInProgress = true;
                this.AuthenticationResult = await this.AuthContext.AcquireTokenSilentAsync(RefreshOptions.IdToken);
                await this.CreateClient();
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

                this.AuthenticationResult = await this.AuthContext.AcquireTokenAsync(credentialsProvider.AuthenticationRequest).ConfigureAwait(false);

                await this.CreateClient();
            }
            catch (Exception ex)
            {
                //log
            }
            finally
            {
                IsInProgress = false;
            }

            Notify();
        }

        public void SignOut()
        {
            if (this.AuthenticationResult != null)
            {
                this.AuthContext.LogoutAsync(this.AuthenticationResult);
            }
            this.AuthenticationResult = null;
            this.AuthContext.TokenCache.Clear();
            if (Client != null)
            {
                ((TrimbleConnectClient)Client).Dispose();
                Client = null;
            }

            Notify();
        }

        private async Task CreateClient()
        {
            var env = CrossSettings.Current.GetValueOrDefault("environment", DefaultEnvironment);
            var httpStack = CrossSettings.Current.GetValueOrDefault("httpStack", 0);

            if (Client != null)
            {
                ((TrimbleConnectClient)Client).Dispose();
                Client = null;
            }

            var config = new TrimbleConnectClientConfig { ServiceURI = new Uri(Environments[env].ServiceUri) };
            config.RetryConfig = new RetryConfig { MaxErrorRetry = 1 };
            //config.HttpHandlers.Add(new PerformanceLoggerHandler());

            this.Client = new TrimbleConnectClient(config, credentialsProvider);
            RegionsConfig.RegionsUri = new Uri(config.ServiceURI + "regions");
            await this.Client.InitializeTrimbleConnectUserAsync();
        }

        private void Notify()
        {
            Device.BeginInvokeOnMainThread(() => {
                MessagingCenter.Send(this, UserChangedMessageId);
            });
        }
    }
}
