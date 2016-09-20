using Xamarin.Forms;
[assembly: ExportRenderer(typeof(SignIn.MainPage), typeof(SignIn.LoginPageRenderer))]
namespace SignIn
{
    using Xamarin.Forms.Platform.iOS;
    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        MainPage page;

        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            this.page = e.NewElement as MainPage;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            this.page.Parameters = new Parameters(this);
        }
    }
}
