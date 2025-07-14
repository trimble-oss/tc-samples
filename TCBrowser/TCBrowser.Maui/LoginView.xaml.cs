namespace TCBrowser.Maui;

public partial class LoginView : ContentPage
{
	public LoginView()
	{
		InitializeComponent();
        var loginViewModel = Application.Current.Handler.MauiContext.Services.GetService<ILoginViewModel>();
        BindingContext = loginViewModel;
        loginViewModel.DoSilentLogin();
    } 
}