using Foundation;
using Trimble.Identity.OAuth.AuthCode;
using UIKit;

namespace TCBrowser.Maui
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            IAuthCodeCredentialsProvider authCodeCredentialsProvider = IPlatformApplication.Current?.Services?.GetService<IAuthCodeCredentialsProvider>();
            new Task(() =>
            {
                authCodeCredentialsProvider.OnReceive(url.Query);
            }).Start();
            return true;
        }
    }
}
