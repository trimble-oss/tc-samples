using Android.App;
using Android.Content.PM;
using Android.OS;
using DataSyncSampleApp.Models;
using Trimble.Identity.OAuth.AuthCode;

namespace DataSyncSampleApp;

[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = "tcps")]
public class RedirectUriReceiverActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var code = Intent?.Data?.GetQueryParameter("code");
        if (AppSession.AuthProvider != null && !string.IsNullOrEmpty(code))
            _ = Task.Run(() => AppSession.AuthProvider.OnReceive(code));
        Finish();
    }
}
