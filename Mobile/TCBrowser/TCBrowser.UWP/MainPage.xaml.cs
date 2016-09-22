namespace SignIn.UWP
{
    public sealed partial class MainPage
    {
        public MainPage()
        {
            this.InitializeComponent();

            LoadApplication(new Examples.Mobile.App());
        }
    }
}
