using Xamarin.Forms;
[assembly: ExportRenderer(typeof(Examples.Mobile.ProjectListPage), typeof(Examples.Mobile.Droid.LoginPageRenderer))]
namespace Examples.Mobile.Droid
{
    using Android.App;
    using Xamarin.Forms.Platform.Android;

    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            AppState.Instance.Parameters = new Parameters(this.Context as Activity) { UseSystemBrowser = true };
        }
    }
}
