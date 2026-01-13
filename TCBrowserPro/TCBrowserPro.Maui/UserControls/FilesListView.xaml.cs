using TCBrowserPro.Maui.ViewModels;

namespace TCBrowserPro.Maui.UserControls;

public partial class FilesListView : ContentView
{
	public FilesListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    { 
        base.OnBindingContextChanged();
    }
}