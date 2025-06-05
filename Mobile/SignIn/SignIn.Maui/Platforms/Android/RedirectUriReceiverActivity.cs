using Android.App;
using Android.Content.PM;
using Android.OS;
using Trimble.Identity.OAuth.AuthCode;

namespace SignIn.Maui;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
              Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
              DataScheme = "tcps")]

public class RedirectUriReceiverActivity : Activity
{
    private readonly IAuthCodeCredentialsProvider authCodeCredentialsProvider = MauiApplication.Current.Services.GetService<IAuthCodeCredentialsProvider>();
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        new Task(() =>
        {
            authCodeCredentialsProvider.OnReceive(this.Intent.Data.GetQueryParameter("code"));
        }).Start();

        this.Finish();

    }

}
