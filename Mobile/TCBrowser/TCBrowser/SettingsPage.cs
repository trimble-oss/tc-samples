namespace Examples.Mobile
{
    using Xamarin.Forms;

    /// <summary>
    /// The main page.
    /// </summary>
    public class SettingsPage : ContentPage
    {
        private Label name;
        private Label email;

        public SettingsPage()
        {
            this.Title = "E";

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

            this.BackgroundColor = Color.FromHex(Constants.MENU_PAGE_ITEM_BG_SEL);
            this.Content = new StackLayout
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

                    (name = new Label
                    {
                        TextColor = Color.White,
                        HorizontalOptions = LayoutOptions.Center,
                    }),

                    (email = new Label
                    {
                        TextColor = Color.White,
                        HorizontalOptions = LayoutOptions.Center,
                    }),

                    picker,
                    pickerNetworkStack,

                    new Button
                    {
                        Text = "NetStats",
                        TextColor = Color.White,
                        BackgroundColor = Color.FromHex(Constants.LOGIN_BUTTON_ENABLED_COLOR),
                        Command = new Command(
                            async () =>
                            {
                                await (this.ParentView as MasterDetailPage).Detail.Navigation.PushAsync(new StatsPage());
                                (this.ParentView as MasterDetailPage).IsPresented = false;
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
                                AppState.SignOut();
                                App.Current.MainPage = new LoginPage();
                            })
                    },
                }
            };
        }

        /// <inheritdoc />
        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.Refresh();
        }

        /// <inheritdoc />
        public void Refresh()
        {
            if (AppState.CurrentUser != null)
            {
                name.Text = AppState.CurrentUser.UserInfo.FamilyName + " " + AppState.CurrentUser.UserInfo.GivenName;
                email.Text = AppState.CurrentUser.UserInfo.DisplayableId;
            }
            else
            {
                name.Text = string.Empty;
                email.Text = string.Empty;
            }
        }
    }
}
