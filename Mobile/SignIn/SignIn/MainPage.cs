namespace SignIn
{
    using System;
    using System.Collections.ObjectModel;
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

        private AuthenticationResult _authenticationResult;

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
            Button loginWeb, logoutWeb, clear;

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

                        ItemTemplate = new Xamarin.Forms.DataTemplate(() =>
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
                        
                    new StackLayout
                    {
                        Orientation = StackOrientation.Horizontal,
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        
                        Children =
                        {
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

                    (clear = new Button
					{
						Text = "Clear Cache",
						TextColor = Color.White,
						BackgroundColor = Color.FromHex("77D065"),
					}),

                    new Label { Text = AuthParams.AuthorityUri.OriginalString },
                },
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
                        authCtx.Parameters = this.Parameters;
#endif
                        _authenticationResult = await authCtx.AcquireTokenAsync(new InteractiveAuthenticationRequest()
                        {
                            Scope = $"openid {string.Join(" ", AuthParams.ClientCredentials.Name)}"
                        }).ConfigureAwait(false);

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
                        if (_authenticationResult != null)
                        {
                            await authCtx.LogoutAsync(_authenticationResult);
                        }
                        this.tokens.Clear();
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
            Device.BeginInvokeOnMainThread(() =>
            {
                this.tokens.Clear();

                foreach (var item in tokenCache.ReadItems())
                {
                    this.tokens.Add(item);
                }
            });
        }
    }
}
