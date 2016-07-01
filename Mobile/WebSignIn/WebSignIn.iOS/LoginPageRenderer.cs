using Examples.Mobile;
using Xamarin.Forms;
[assembly: ExportRenderer(typeof(LoginPage), typeof(LoginPageRenderer))]
namespace Examples.Mobile
{
    using Xamarin.Forms.Platform.iOS;
    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        LoginPage page;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            this.page = e.NewElement as LoginPage;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.page.Parameters = new Parameters(this);
        }
    }
}
