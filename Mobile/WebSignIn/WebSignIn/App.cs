namespace Examples.Mobile
{
    using System.Diagnostics;

    using Trimble.Identity;
    using Xamarin.Forms;

    /// <summary>
    /// Trimble Identity SDK example Xamarin Forms application.
    /// </summary>
    public class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App ()
        {
            Logging.Switch.Level = SourceLevels.Information;

            var authenticationContext = new AuthenticationContext(AuthParams.ClientCredentials) { AuthorityUri = AuthParams.AuthorityUri };

            this.MainPage = new NavigationPage(new LoginPage(authenticationContext))
            {
                Title = "Trimble Identity Mobile Example",
            };
        }
    }
}
