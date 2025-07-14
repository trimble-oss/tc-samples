using System.Diagnostics;
using TCBrowser.Maui.ViewModels;

namespace TCBrowser.Maui.UserControls;

public partial class FilesListView : ContentView
{
	public FilesListView()
	{
		InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    { 
        base.OnBindingContextChanged();
        System.Diagnostics.Debug.WriteLine($"[FilesListView] BindingContext = {BindingContext?.GetType().Name}");

        base.OnBindingContextChanged();
        if (BindingContext is ProjectVm vm)
        {
            Debug.WriteLine($"[FilesListView] ViewModel hash: {vm.GetHashCode()}");
        }
    }
}