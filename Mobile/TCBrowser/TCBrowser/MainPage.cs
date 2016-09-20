namespace Examples.Mobile
{
    using System;

    using Xamarin.Forms;

    /// <summary>
    /// The main page.
    /// </summary>
    public class MainPage : MasterDetailPage
    {
        public MainPage()
        {
            this.Title = "Trimble Connect";

            this.Master = new SettingsPage();

            // Create the detail page using NamedColorPage and wrap it in a
            // navigation page to provide a NavigationBar and Toggle button
            this.Detail = new NavigationPage(new ProjectListPage())
            {
                BarBackgroundColor = Color.FromHex(Constants.NAV_BAR_COLOR),
				BarTextColor = Color.FromHex(Constants.NAV_BAR_TEXT_COLOR),
			};

            // For Android & Windows Phone, provide a way to get back to the master page.
            if (Device.OS != TargetPlatform.iOS)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (sender, args) => {
                    this.IsPresented = true;
                };
            }
        }
    }
}
