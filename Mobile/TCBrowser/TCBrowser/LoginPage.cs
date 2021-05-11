namespace Examples.Mobile
{
    using System;
    using System.Linq;
    using System.Net;
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
                    (login = new Button
                    {
                        Text = "Web-Login",
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
                    try
                    {
                        await AppState.Instance.SignInWebAsync();
                    }
                    catch (Exception e)
                    {
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
                    }
                    catch (Exception e)
                    {
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
