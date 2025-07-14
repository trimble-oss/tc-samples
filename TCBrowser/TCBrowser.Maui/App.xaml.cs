
using Microsoft.Maui.Devices;

namespace TCBrowser.Maui
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            window.Title = "TCBrowser Demo";
            window.Width = window.MinimumWidth = window.MaximumWidth = 620;
            window.Height = window.MinimumHeight = window.MaximumHeight = 840;

            // Center the window
            //window.X = (displayInfo.Width / displayInfo.Density - window.Width) / 2;
            //window.Y = (displayInfo.Height / displayInfo.Density - window.Height) / 2;
            return window;
        }
    }
}
