namespace SignIn
{
    using System;
    using System.Collections.ObjectModel;
    using System.Net;
    using System.Threading.Tasks;
	
    using Trimble.Identity;
    using Xamarin.Forms;

    /// <summary>
    /// Main page.
    /// </summary>
    public class MainPage : ContentPage
    {
        /// <summary>
        /// The collection of cached tokens.
        /// </summary>
        private readonly ObservableCollection<TokenCacheItem> tokens = new ObservableCollection<TokenCacheItem>();

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public Parameters Parameters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage(AuthenticationContext authCtx)
        {
            Entry email, password;
            Button login, loginWeb, logoutWeb, clear;
            Switch useSafary;

            this.Title = "User Info";

            this.Content = new StackLayout
            {
                Padding = 20,
                Spacing = 20,

                Children =
                {
                    new ListView
                    {
                        ItemsSource = this.tokens,

                        ItemTemplate = new DataTemplate(() =>
                        {
                            var cell = new TextCell();
                            cell.SetBinding<TokenCacheItem>(TextCell.TextProperty, _ => _.DisplayableId);
                            cell.SetBinding<TokenCacheItem>(TextCell.DetailProperty, _ => _.ExpiresOn, BindingMode.OneWay, stringFormat: "exp:{0}");
                            cell.Tapped += async (sender, args) =>
                            {
                                var token = cell.BindingContext as TokenCacheItem;
                                await this.Navigation.PushAsync(new TokenDetailsPage(authCtx, token));
                            };
                            return cell;
                        }),
                    },

                    (email = new Entry
                    {
                        Placeholder = "email",
                        Keyboard = Keyboard.Email,
                    }),
                        
                    (password = new Entry
                    {
                        Placeholder = "password",
                        IsPassword = true,
                    }),
                        
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        
                        Children =
                        {
                            (login = new Button
                            {
                                Text = "Sign-in",
                                TextColor = Color.White,
                                BackgroundColor = Color.FromHex("77D065"),
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                            }),

                            (loginWeb = new Button
                            {
                                Text = "Web Sign-in",
                                TextColor = Color.White,
                                BackgroundColor = Color.FromHex("77D065"),
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                            }),

                            (logoutWeb = new Button
                            {
                                Text = "Web Sign-Out",
                                TextColor = Color.White,
                                BackgroundColor = Color.FromHex("77D065"),
                                HorizontalOptions = LayoutOptions.FillAndExpand,
                            }),
                        }   
                    },

                    new StackLayout {
                        Orientation = StackOrientation.Horizontal,
                        Children = {
                            new Label {
                                Text = "Use System Browser",
                            },
                            (useSafary = new Switch()),
                        }
                    },

                    (clear = new Button
					{
						Text = "Clear Cache",
						TextColor = Color.White,
						BackgroundColor = Color.FromHex("77D065"),
					}),

                    new Label { Text = AuthParams.AuthorityUri.OriginalString },
                },
            };

			login.Clicked += async (sender, args) =>
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

			        var errorMessage = string.Empty;
			        var userCredentials = new NetworkCredential(email.Text, password.Text);

			        try
			        {
			            var accessToken = await authCtx.AcquireTokenAsync(userCredentials);
                        this.Refresh(authCtx.TokenCache);
                    }
			        catch (AuthenticationException e)
			        {
			            errorMessage = e.Message;
			        }

			        if (!string.IsNullOrEmpty(errorMessage))
			        {
			            await this.DisplayAlert("Sign-in failed", errorMessage, "OK");
			        }
			    }
			    finally
			    {
			        this.IsBusy = false;
			    }
			};

            loginWeb.Clicked += async (sender, args) =>
            {
                this.IsBusy = true;

                try
                {
                    var errorMessage = string.Empty;

                    try
                    {
#if !WINDOWS_UWP
                        authCtx.Parameters = this.Parameters ?? new Parameters(null);
                        authCtx.Parameters.UseSystemBrowser = useSafary.IsToggled;
#else
                        authCtx.Parameters = this.Parameters ?? new Parameters();
                        //authCtx.Parameters.UseSystemBrowser = useSafary.IsToggled;
#endif
                        var accessToken = await authCtx.AcquireTokenAsync(RefreshOptions.AccessAndIdToken);

                        this.Refresh(authCtx.TokenCache);
                    }
                    catch (Exception e)
                    {
                        errorMessage = e.Message;
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        await this.DisplayAlert("Sign-in failed", errorMessage, "OK");
                    }
                }
                finally
                {
                    this.IsBusy = false;
                }
            };

            logoutWeb.Clicked += async (sender, args) =>
            {
                this.IsBusy = true;

                try
                {
                    var errorMessage = string.Empty;

                    try
                    {
#if !WINDOWS_UWP
                        authCtx.Parameters = this.Parameters ?? new Parameters(null);
                        authCtx.Parameters.UseSystemBrowser = useSafary.IsToggled;
#else
                         authCtx.Parameters = this.Parameters ?? new Parameters();
#endif
                        await authCtx.LogoutAsync();
                    }
                    catch (Exception e)
                    {
                        errorMessage = e.Message;
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        await this.DisplayAlert("Sign-out failed", errorMessage, "OK");
                    }
                }
                finally
                {
                    this.IsBusy = false;
                }
            };

            clear.Clicked += (sender, args) =>
            {
                authCtx.TokenCache.Clear();
                this.Refresh(authCtx.TokenCache);
            };

            this.Appearing += async (sender, args) =>
            {
                var errorMessage = string.Empty;
                this.IsBusy = true;

                try
                {
                    this.Refresh(authCtx.TokenCache);
                }
                catch (Exception e)
                {
                    errorMessage = e.Message;
                }
                finally
                {
                    this.IsBusy = false;
                }

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    await this.DisplayAlert("Error", errorMessage, "OK");
                }
            };
        }

        private void Refresh(TokenCache tokenCache)
        {
            this.tokens.Clear();

            foreach (var item in tokenCache.ReadItems())
            {
                this.tokens.Add(item);
            }
        }
    }
}
