using Xamarin.Forms;
[assembly: ExportRenderer(typeof(Examples.Mobile.ProjectListPage), typeof(Examples.Mobile.iOS.LoginPageRenderer))]
namespace Examples.Mobile.iOS
{
    using Xamarin.Forms.Platform.iOS;
    using Trimble.Identity;

    class LoginPageRenderer : PageRenderer
    {
        protected override void OnElementChanged(VisualElementChangedEventArgs e)
        {
            base.OnElementChanged(e);
            AppState.Instance.Parameters = new Parameters(callerViewController: this.ViewController) { UseSystemBrowser = true };
        }
    }
}
