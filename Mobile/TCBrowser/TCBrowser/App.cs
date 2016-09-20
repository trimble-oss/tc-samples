namespace Examples.Mobile
{
    using System;
    using Xamarin.Forms;

    /// <summary>
    /// Trimble Connect Browser (TC SDK example application).
    /// </summary>
    public class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            AppState.Initialize();
            this.MainPage = new MainPage();
        }
    }
}
