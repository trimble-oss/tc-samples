namespace Examples.Mobile
{
    using System;
    using System.Linq;
    using System.Net;
    using Trimble.Identity;
    using Xamarin.Forms;

    /// <summary>
    /// To login page.
    /// </summary>
    public class LoginPage : ContentPage
    {
        /// <summary>
        /// Creates the login page.
        /// </summary>
        /// <returns>The page.</returns>
        public LoginPage()
        {
            Entry email, password;
            Button login, relogin;

            var cachedToken = AppState.Instance.AuthContext.TokenCache.ReadItems().FirstOrDefault();

            this.Title = "Trimble Login";
            this.BackgroundColor = Color.FromHex(Constants.MENU_PAGE_ITEM_BG_SEL);

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
                        AppState.Instance.CurrentUser = await AppState.Instance.AuthContext.AcquireTokenAsync(userCredentials);
                        // TODO: profile completion web flow support
                        await AppState.Instance.Client.LoginAsync(AppState.Instance.CurrentUser.IdToken);
                    }
                    catch (Exception e)
                    {
                        AppState.Instance.AuthContext.TokenCache.Clear();
                        relogin.IsEnabled = false;
                        await this.DisplayAlert("Try again", e.ToString(), "OK");
                        return;
                    }

                    App.Current.MainPage = new MainPage();
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
                        await AppState.Instance.SignInSilentlyAsync();
                        await AppState.Instance.Client.LoginAsync(AppState.Instance.CurrentUser.IdToken);
                    }
                    catch (Exception e)
                    {
                        AppState.Instance.AuthContext.TokenCache.Clear();
                        relogin.IsEnabled = false;
                        await this.DisplayAlert("Try again", e.ToString(), "OK");
                        return;
                    }

                    App.Current.MainPage = new MainPage();
                }
                finally
				{
					this.IsBusy = false;
				}
			};
        }
    }
}
