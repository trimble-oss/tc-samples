namespace Examples.Mobile
{
    using Xamarin.Forms;
    using Plugin.Settings;
    using System.Linq;

    /// <summary>
    /// The settings page.
    /// </summary>
    public class SettingsPage : ContentPage
    {
        private readonly Label name;
        private readonly Label email;

        public SettingsPage()
        {
            this.Title = "E";

            var picker = new Picker
            {
                Title = "Environment",
                VerticalOptions = LayoutOptions.CenterAndExpand,
            };
            foreach (var env in AppState.Environments)
            {
                picker.Items.Add(env.Key);
            }

            picker.SelectedIndex = picker.Items.IndexOf(CrossSettings.Current.GetValueOrDefault("environment", AppState.DefaultEnvironment));
            picker.SelectedIndexChanged += (s, e) => 
            {
                CrossSettings.Current.AddOrUpdateValue("environment", AppState.Environments.Keys.ToArray()[picker.SelectedIndex]);
            };

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

                    (this.name = new Label
                    {
                        TextColor = Color.White,
                        HorizontalOptions = LayoutOptions.Center,
                    }),

                    (this.email = new Label
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
                                await (this.Parent as MasterDetailPage).Detail.Navigation.PushAsync(new StatsPage());
                                (this.Parent as MasterDetailPage).IsPresented = false;
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
                                AppState.Instance.SignOut();
                                (this.Parent as MasterDetailPage).IsPresented = false;
                            })
                    },
                }
            };

            MessagingCenter.Subscribe<AppState>(this, AppState.UserChangedMessageId, (sender) => 
            {
                this.Refresh();
            });
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
            if (AppState.Instance.AuthenticationResult != null)
            {
                this.name.Text = AppState.Instance.AuthenticationResult.UserInfo.FamilyName + " " + AppState.Instance.AuthenticationResult.UserInfo.GivenName;
                this.email.Text = AppState.Instance.AuthenticationResult.UserInfo.DisplayableId;
            }
            else
            {
                this.name.Text = string.Empty;
                this.email.Text = string.Empty;
            }
        }
    }
}
