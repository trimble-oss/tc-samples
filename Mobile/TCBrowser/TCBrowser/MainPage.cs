namespace Examples.Mobile
{
    using System;
    using Trimble.Connect.Client;
    using Xamarin.Forms;

    /// <summary>
    /// The main page.
    /// </summary>
    public class MainPage : MasterDetailPage
    {
        private ITrimbleConnectClient client;

        public MainPage(ITrimbleConnectClient client)
        {
            this.client = client;

            this.Title = "Trimble Connect";

            this.Master = new ContentPage
            {
                Title = "E",
                BackgroundColor = Color.FromHex(Constants.MENU_PAGE_ITEM_BG_SEL),
                Content = new StackLayout
                {
                    Padding = 20,
                    Spacing = 20,

                    Children =
                    {
                        new Image ()
                        {
                            Source = ImageSource.FromFile("ic_user_account.png"),
                            Aspect = Aspect.AspectFit,
                        },

                        new Label
                        {
                            Text = AppState.CurrentUser.UserInfo.FamilyName + " " + AppState.CurrentUser.UserInfo.GivenName,
                            TextColor = Color.White,
                            HorizontalOptions = LayoutOptions.Center,
                        },

                        new Label
                        {
                            Text = AppState.CurrentUser.UserInfo.DisplayableId,
                            TextColor = Color.White,
                            HorizontalOptions = LayoutOptions.Center,
                        },

                        new Button
                        {
                            Text = "NetStats",
                            TextColor = Color.White,
                            BackgroundColor = Color.FromHex(Constants.LOGIN_BUTTON_ENABLED_COLOR),
                            Command = new Command(
                                async () =>
                                {
                                    await this.Detail.Navigation.PushAsync(new StatsPage());
                                    this.IsPresented = false;
                                })
                        },

                        new Button
                        {
                            Text = "Log out",
                            TextColor = Color.White,
                            BackgroundColor = Color.FromHex(Constants.LOGIN_BUTTON_ENABLED_COLOR),
                            Command = new Command(
                                () =>
                                {
                                    ((TrimbleConnectClient)AppState.Client).Dispose();
                                    AppState.Client = null;
                                    App.Current.MainPage = new LoginPage();
                                })
                        },
                    }
                }
            };

            // Create the detail page using NamedColorPage and wrap it in a
            // navigation page to provide a NavigationBar and Toggle button
            this.Detail = new NavigationPage(new ProjectListPage(this.client))
            {
                BarBackgroundColor = Color.FromHex(Constants.NAV_BAR_COLOR),
				BarTextColor = Color.FromHex(Constants.NAV_BAR_TEXT_COLOR),
			};

            // For Android & Windows Phone, provide a way to get back to the master page.
            if (Device.OS != TargetPlatform.iOS)
            {
                TapGestureRecognizer tap = new TapGestureRecognizer();
                tap.Tapped += (sender, args) => {
                    this.IsPresented = true;
                };
            }
        }
    }
}
