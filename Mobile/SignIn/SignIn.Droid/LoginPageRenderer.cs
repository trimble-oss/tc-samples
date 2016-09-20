using Xamarin.Forms;
[assembly: ExportRenderer(typeof(SignIn.MainPage), typeof(SignIn.Droid.LoginPageRenderer))]
namespace SignIn.Droid
{
    using Android.App;
    using Xamarin.Forms.Platform.Android;

    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            var page = e.NewElement as MainPage;
            page.Parameters = new Parameters(this.Context as Activity);
        }
    }
}
