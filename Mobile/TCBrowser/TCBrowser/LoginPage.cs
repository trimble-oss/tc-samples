namespace Examples.Mobile
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Trimble.Connect.Client;
    using Trimble.Identity;
    using Xamarin.Forms;

    /// <summary>
    /// To login page.
    /// </summary>
    public class LoginPage : ContentPage
    {
        /// <summary>
        /// The service URI.
        /// </summary>
        private static readonly IDictionary<string, string> ServiceUri = new Dictionary<string, string>
        {
            { "STAGE", "https://staging.qa1.gteam.com/tc/api/2.0/" },
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
        /// Creates the login page.
        /// </summary>
        /// <returns>The page.</returns>
        public LoginPage()
        {
            Entry email, password;
            Button login, relogin;

            if (AppState.Client != null)
            {
                ((TrimbleConnectClient)AppState.Client).Dispose();
                AppState.Client = null;
            }

            var cachedToken = TokenCache.DefaultShared.ReadItems().FirstOrDefault();

            this.Title = "Trimble Login";
            this.BackgroundColor = Color.FromHex(Constants.MENU_PAGE_ITEM_BG_SEL);

            var picker = new Picker
            {
                Title = "Environment",
                VerticalOptions = LayoutOptions.CenterAndExpand,
            };
            picker.Items.Add("STAGE");
            picker.Items.Add("PROD");
            picker.SelectedIndex = 2;

            var pickerNetworkStack = new Picker
            {
                Title = "NetworkStask",
                VerticalOptions = LayoutOptions.CenterAndExpand,
            };
            pickerNetworkStack.Items.Add("Mono");
            pickerNetworkStack.Items.Add("Native");
            pickerNetworkStack.SelectedIndex = 0;

            this.Content = new StackLayout
            {
                Padding = 20,
                Spacing = 20,

                Children =
                {
                    (email = new Entry
                    {
                        Placeholder = "email",
                        Keyboard = Keyboard.Email,
						Text = cachedToken == null ? string.Empty : cachedToken.DisplayableId,
                    }),
                        
                    (password = new Entry
                    {
                        Placeholder = "password",
                        IsPassword = true,
                    }),

                    picker,
                    pickerNetworkStack,

                    (login = new Button
                    {
                        Text = "Login",
                        TextColor = Color.White,
                        BackgroundColor = Color.FromHex(Constants.LOGIN_BUTTON_ENABLED_COLOR),
                    }),

					(relogin = new Button
						{
							Text = "Re-Login",
							TextColor = Color.White,
							BackgroundColor = Color.FromHex(Constants.LOGIN_BUTTON_ENABLED_COLOR),
							IsEnabled = cachedToken != null,
						}),
                },
            };

			login.Clicked += async delegate
            {
                this.IsBusy = true;

                try
                {
                    if (string.IsNullOrWhiteSpace(email.Text))
                    {
                        await this.DisplayAlert("Error", "Empty email", "OK");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(password.Text))
                    {
                        await this.DisplayAlert("Error", "Empty password", "OK");
                        return;
                    }

                    var userCredentials = new NetworkCredential(email.Text, password.Text);

                    try
                    {
                        AppState.AuthContext = new AuthenticationContext(ClientCredential[picker.Items[picker.SelectedIndex]])
                        {
                            AuthorityUri = new Uri(AuthorityUri[picker.Items[picker.SelectedIndex]]),
                            Handlers = new[] { new PerformanceLoggerHandler() }
                        };
                        AppState.CurrentUser = await AppState.AuthContext.AcquireTokenAsync(userCredentials);
                        AppState.Client = pickerNetworkStack.SelectedIndex == 0
                            ? new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new PerformanceLoggerHandler())
#if __IOS__
                            : new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new ModernHttpClient.NativeMessageHandler(), new PerformanceLoggerHandler());
#else
                            : new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new PerformanceLoggerHandler());
#endif

                        await AppState.Client.LoginAsync(AppState.CurrentUser.IdToken);
                    }
                    catch (Exception e)
                    {
                        TokenCache.DefaultShared.Clear();
                        relogin.IsEnabled = false;
                        await this.DisplayAlert("Try again", e.ToString(), "OK");
                        return;
                    }

                    App.Current.MainPage = new MainPage(AppState.Client);
                }
                finally
                {
                    this.IsBusy = false;
                }
            };

			relogin.Clicked += async delegate
			{
				this.IsBusy = true;

				try
				{
					try
					{
                        AppState.AuthContext = new AuthenticationContext(ClientCredential[picker.Items[picker.SelectedIndex]])
                        {
                            AuthorityUri = new Uri(AuthorityUri[picker.Items[picker.SelectedIndex]]),
                            Handlers = new[] { new PerformanceLoggerHandler() }
                        };
                        AppState.CurrentUser = await AppState.AuthContext.AcquireTokenSilentAsync(ClientCredential[picker.Items[picker.SelectedIndex]], new UserIdentifier(cachedToken.DisplayableId, UserIdentifierType.OptionalDisplayableId));
                        AppState.CurrentUser = await AppState.AuthContext.AcquireTokenByRefreshTokenAsync(AppState.CurrentUser);
                        AppState.Client = pickerNetworkStack.SelectedIndex == 0
                            ? new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new PerformanceLoggerHandler())
#if __IOS__
                            : new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new ModernHttpClient.NativeMessageHandler(), new PerformanceLoggerHandler());
#else
                            : new TrimbleConnectClient(ServiceUri[picker.Items[picker.SelectedIndex]], new PerformanceLoggerHandler());
#endif
                        await AppState.Client.LoginAsync(AppState.CurrentUser.IdToken);
                    }
                    catch (Exception e)
                    {
                        TokenCache.DefaultShared.Clear();
                        relogin.IsEnabled = false;
                        await this.DisplayAlert("Try again", e.ToString(), "OK");
                        return;
                    }

                    App.Current.MainPage = new MainPage(AppState.Client);
                }
                finally
				{
					this.IsBusy = false;
				}
			};
        }
    }
}
