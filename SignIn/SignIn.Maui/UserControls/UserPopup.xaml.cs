using CommunityToolkit.Maui.Views;

namespace SignIn.Maui.UserControls;

public partial class UserPopup : Popup
{
	public UserPopup()
	{
		InitializeComponent();
	}

    void UserMenuItems_ItemSelected(System.Object sender, Microsoft.Maui.Controls.SelectedItemChangedEventArgs e)
    {
        var loginViewModel = Application.Current.Handler.MauiContext.Services.GetService<ILoginViewModel>();
		var selectedMenu = (MenuItem)e.SelectedItem;

		switch (selectedMenu.Text)
		{
            case "Sign out":
                loginViewModel.DoLogOut();
                Close();
                break;
        }
    }


}
