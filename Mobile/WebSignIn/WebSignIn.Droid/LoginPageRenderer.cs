using Examples.Mobile;
using Xamarin.Forms;
[assembly: ExportRenderer(typeof(LoginPage), typeof(LoginPageRenderer))]
namespace Examples.Mobile
{
    using Android.App;
    using Xamarin.Forms.Platform.Android;

    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Page> e)
        {
            base.OnElementChanged(e);

            var page = e.NewElement as LoginPage;
            page.Parameters = new Parameters(this.Context as Activity);
        }
    }
}
