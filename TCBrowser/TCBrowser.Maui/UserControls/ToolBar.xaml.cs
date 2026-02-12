using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using TCBrowser.Maui.ViewModels;

namespace TCBrowser.Maui.UserControls;

public partial class ToolBar : ContentView
{
	public ToolBar()
	{
		InitializeComponent();
	}

    public static readonly BindableProperty TabBarVisibilityProperty = BindableProperty.Create(
    propertyName: nameof(TabBarVisibility),
    returnType: typeof(bool),
    declaringType: typeof(ToolBar),
    defaultValue: true);

    public static readonly BindableProperty SignoutButtonVisibilityProperty = BindableProperty.Create(
        propertyName: nameof(SignoutButtonVisibility),
        returnType: typeof(bool),
        declaringType: typeof(ToolBar),
        defaultValue: true);

    public bool TabBarVisibility
    {
        get => (bool)GetValue(TabBarVisibilityProperty);
        set => SetValue(TabBarVisibilityProperty, value);
    }

    public bool SignoutButtonVisibility
    {
        get => (bool)GetValue(SignoutButtonVisibilityProperty);
        set => SetValue(SignoutButtonVisibilityProperty, value);
    }

    [RelayCommand]
    private void UserPopup()
    {
        Shell.Current.CurrentPage.ShowPopup(new UserPopup { Anchor = UserButton});
    }
}
