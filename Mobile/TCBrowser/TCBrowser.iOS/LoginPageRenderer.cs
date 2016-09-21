using Xamarin.Forms;
[assembly: ExportRenderer(typeof(Examples.Mobile.ProjectListPage), typeof(Examples.Mobile.iOS.LoginPageRenderer))]
namespace Examples.Mobile.iOS
{
    using Xamarin.Forms.Platform.iOS;
    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            AppState.Instance.Parameters = new Parameters(this);
        }
    }
}
