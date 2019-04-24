namespace SignIn
{
    using System.Diagnostics;
    using Trimble.Identity;
    using Xamarin.Forms;

    public class App : Application
	{
		public App ()
		{
#if !WINDOWS_UWP
            Trimble.Identity.Logging.Switch.Level = SourceLevels.Information;
            //Trimble.WebUI.Logging.Switch.Level = SourceLevels.Information;
#endif

            var authenticationContext = new AuthenticationContext(AuthParams.ClientCredentials) { AuthorityUri = AuthParams.AuthorityUri };

            // The root page of your application
            this.MainPage = new NavigationPage(new MainPage(authenticationContext))
            {
                Title = "Trimble Identity Example",
            };
        }

        protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}
