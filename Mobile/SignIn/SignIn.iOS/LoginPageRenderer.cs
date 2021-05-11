using Xamarin.Forms;
[assembly:ExportRenderer(typeof(SignIn.MainPage), typeof(SignIn.iOS.LoginPageRenderer))]
namespace SignIn.iOS
{
    using Xamarin.Forms.Platform.iOS;
    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            var page = e.NewElement as MainPage;
            if (page != null)
            {
                page.Parameters = new Parameters(callerViewController: this.ViewController) { UseSystemBrowser = true };
            }
        }
    }
}
