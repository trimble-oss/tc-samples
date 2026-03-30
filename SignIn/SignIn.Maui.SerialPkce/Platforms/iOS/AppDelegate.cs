using Foundation;
using Trimble.Identity.OAuth.AuthCode;
using UIKit;

namespace SignIn.Maui.SerialPkce
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            IAuthCodeCredentialsProvider? authCodeCredentialsProvider = IPlatformApplication.Current?.Services?.GetService<IAuthCodeCredentialsProvider>();
            if (authCodeCredentialsProvider != null)
            {
                new Task(() =>
                {
                    authCodeCredentialsProvider.OnReceive(url.Query);
                }).Start();
            }
            return true;
        }
    }
}
